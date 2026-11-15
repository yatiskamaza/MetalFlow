using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta la publicación de anuncios comerciales sobre el material (CU-06),
/// que materializa el requisito RF-06.
///
/// El anuncio constituye información comercial añadida al ítem y no una
/// condición para su aparición en el catálogo, conforme a la decisión expuesta
/// en el apartado 5.4: el requisito de búsqueda, clasificado como imprescindible,
/// no puede depender de uno clasificado como deseable.
///
/// Las operaciones quedan sujetas a la misma autorización por pertenencia que
/// la gestión del inventario: una empresa solo puede anunciar su propio
/// material.
/// </summary>
public class ServicioAnuncios
{
    private readonly IUnitOfWork _unidadDeTrabajo;

    public ServicioAnuncios(IUnitOfWork unidadDeTrabajo) => _unidadDeTrabajo = unidadDeTrabajo;

    /// <summary>Anuncios de la empresa, activos y despublicados.</summary>
    public async Task<IReadOnlyList<AnuncioDto>> ListarPropiosAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var anuncios = await _unidadDeTrabajo.Anuncios.BuscarAsync(
            a => a.Item.EmpresaId == empresaId, ct,
            a => a.Item, a => a.Item.Material);

        return anuncios
            .OrderByDescending(a => a.Activo)
            .ThenByDescending(a => a.FechaPublicacion)
            .Select(Proyectar)
            .ToList();
    }

    /// <summary>
    /// CU-06. Publica un anuncio sobre un ítem propio y disponible.
    ///
    /// La comprobación de que el ítem carece de anuncio previo ofrece una
    /// respuesta comprensible en el caso ordinario. La garantía efectiva, sin
    /// embargo, la aporta la restricción de unicidad de la base de datos: dos
    /// solicitudes prácticamente simultáneas podrían superar ambas la
    /// comprobación, pero la segunda sería rechazada por el gestor, de modo
    /// que nunca llegarían a coexistir dos anuncios sobre el mismo material.
    /// </summary>
    public async Task<AnuncioDto> PublicarAsync(
        PublicarAnuncioDto datos, Guid empresaId, CancellationToken ct = default)
    {
        await ComprobarEmpresaVerificadaAsync(empresaId, ct);

        var itemId = datos.ItemId!.Value;

        var item = await _unidadDeTrabajo.Items.ObtenerPorIdAsync(itemId, ct)
            ?? throw new ExcepcionNoEncontrado("El material indicado no existe.");

        if (!item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        ComprobarDisponible(item);

        if (await _unidadDeTrabajo.Anuncios.ExisteAsync(a => a.ItemId == itemId, ct))
            throw new ExcepcionConflicto(
                "El material ya cuenta con un anuncio. Modifíquelo en lugar de crear otro.");

        Anuncio anuncio;
        try
        {
            anuncio = new Anuncio(itemId, datos.Titulo, datos.Precio!.Value, datos.Descripcion);
        }
        catch (ArgumentException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }

        await _unidadDeTrabajo.Anuncios.AgregarAsync(anuncio, ct);
        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return await ObtenerPropioAsync(anuncio.Id, empresaId, ct);
    }

    /// <summary>
    /// Modifica la información comercial de un anuncio propio y, si estaba
    /// despublicado, lo publica de nuevo.
    ///
    /// La reactivación se integra en la modificación porque la restricción de
    /// unicidad impide crear un segundo anuncio sobre el mismo material: quien
    /// desee volver a anunciarlo ha de partir del existente, y lo natural es
    /// que al hacerlo revise sus condiciones.
    /// </summary>
    public async Task<AnuncioDto> ActualizarAsync(
        Guid anuncioId, ActualizarAnuncioDto datos, Guid empresaId, CancellationToken ct = default)
    {
        await ComprobarEmpresaVerificadaAsync(empresaId, ct);

        var (anuncio, item) = await RecuperarParaModificarAsync(anuncioId, empresaId, ct);

        ComprobarDisponible(item);

        try
        {
            anuncio.Actualizar(datos.Titulo, datos.Precio!.Value, datos.Descripcion);
            if (!anuncio.Activo) anuncio.Reactivar();
        }
        catch (ArgumentException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }

        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return await ObtenerPropioAsync(anuncioId, empresaId, ct);
    }

    /// <summary>
    /// Retira el anuncio del catálogo sin eliminarlo. Procede con
    /// independencia del estado del material, por cuanto retirar información
    /// comercial no compromete operación alguna.
    /// </summary>
    public async Task DespublicarAsync(Guid anuncioId, Guid empresaId, CancellationToken ct = default)
    {
        var (anuncio, _) = await RecuperarParaModificarAsync(anuncioId, empresaId, ct);

        try
        {
            anuncio.Despublicar();
        }
        catch (InvalidOperationException ex)
        {
            throw new ExcepcionConflicto(ex.Message);
        }

        await _unidadDeTrabajo.ConfirmarAsync(ct);
    }

    // -----------------------------------------------------------------------
    // Comprobaciones auxiliares
    // -----------------------------------------------------------------------

    private async Task<AnuncioDto> ObtenerPropioAsync(
        Guid anuncioId, Guid empresaId, CancellationToken ct)
        => Proyectar(await RecuperarPropioAsync(anuncioId, empresaId, ct));

    /// <summary>
    /// Recupera un anuncio con sus relaciones para su lectura, comprobando que
    /// el material al que se asocia pertenece a la empresa solicitante.
    /// </summary>
    private async Task<Anuncio> RecuperarPropioAsync(
        Guid anuncioId, Guid empresaId, CancellationToken ct)
    {
        var anuncio = await _unidadDeTrabajo.Anuncios.ObtenerUnoAsync(
                a => a.Id == anuncioId, ct,
                a => a.Item, a => a.Item.Material)
            ?? throw new ExcepcionNoEncontrado("El anuncio solicitado no existe.");

        if (!anuncio.Item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        return anuncio;
    }

    /// <summary>
    /// Recupera un anuncio para su modificación, junto con el ítem al que se
    /// asocia para comprobar la pertenencia y el estado.
    ///
    /// El anuncio se obtiene con seguimiento de cambios y sin relaciones,
    /// mientras que el ítem se consulta aparte y sin seguimiento. La
    /// separación es deliberada. Si el anuncio se recuperase junto con su ítem
    /// y se marcase después como modificado, el marco de persistencia
    /// reescribiría también el ítem con los valores leídos: si entretanto
    /// hubiera sido reservado, la modificación del anuncio lo devolvería
    /// indebidamente a disponible. Por el mismo motivo, los cambios se
    /// persisten mediante la detección automática del marco, sin marcar la
    /// entidad de forma expresa.
    /// </summary>
    private async Task<(Anuncio Anuncio, ItemInventario Item)> RecuperarParaModificarAsync(
        Guid anuncioId, Guid empresaId, CancellationToken ct)
    {
        var anuncio = await _unidadDeTrabajo.Anuncios.ObtenerPorIdAsync(anuncioId, ct)
            ?? throw new ExcepcionNoEncontrado("El anuncio solicitado no existe.");

        var item = await _unidadDeTrabajo.Items.ObtenerUnoAsync(i => i.Id == anuncio.ItemId, ct)
            ?? throw new ExcepcionNoEncontrado("El material del anuncio no existe.");

        if (!item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        return (anuncio, item);
    }

    /// <summary>
    /// Solo se anuncia material disponible. Publicar o modificar el anuncio de
    /// un material reservado induciría a error, pues no figura en el catálogo
    /// y no puede ser solicitado; anunciar material retirado carecería de
    /// efecto alguno.
    /// </summary>
    private static void ComprobarDisponible(ItemInventario item)
    {
        if (item.Estado != EstadoItem.Disponible)
            throw new ExcepcionDeNegocio(
                "Solo puede anunciarse material disponible.");
    }

    private async Task ComprobarEmpresaVerificadaAsync(Guid empresaId, CancellationToken ct)
    {
        var empresa = await _unidadDeTrabajo.Empresas.ObtenerPorIdAsync(empresaId, ct)
            ?? throw new ExcepcionAccesoDenegado();

        // RD-02 condiciona la operativa a la acreditación previa de la
        // condición profesional.
        if (!empresa.Verificada)
            throw new ExcepcionDeNegocio(
                "La empresa se encuentra pendiente de verificación y no puede publicar anuncios.");
    }

    private static AnuncioDto Proyectar(Anuncio anuncio) => new()
    {
        Id = anuncio.Id,
        Titulo = anuncio.Titulo,
        Descripcion = anuncio.Descripcion,
        Precio = anuncio.Precio,
        FechaPublicacion = anuncio.FechaPublicacion,
        Activo = anuncio.Activo,
        ItemId = anuncio.ItemId,
        Designacion = anuncio.Item.Material.Designacion,
        LargoMm = anuncio.Item.LargoMm,
        AnchoMm = anuncio.Item.AnchoMm,
        EspesorMm = anuncio.Item.EspesorMm,
        EstadoItem = anuncio.Item.Estado.ToString()
    };
}
