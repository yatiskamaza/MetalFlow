using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta el caso de uso de gestión manual del inventario (CU-04), que
/// materializa el requisito RF-03.
///
/// Todas las operaciones reciben el identificador de la empresa solicitante y
/// comprueban que el recurso le pertenece. Dicha comprobación reside aquí y no
/// en la capa de API por una razón de diseño: la regla debe aplicarse con
/// independencia del punto de entrada desde el que se invoque el caso de uso.
/// </summary>
public class ServicioInventario
{
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IRepositorioReservas _reservas;

    public ServicioInventario(IUnitOfWork unidadDeTrabajo, IRepositorioReservas reservas)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _reservas = reservas;
    }

    /// <summary>Catálogo normalizado de aleaciones (RD-03).</summary>
    public async Task<IReadOnlyList<MaterialDto>> ListarMaterialesAsync(
        CancellationToken ct = default)
    {
        var materiales = await _unidadDeTrabajo.Materiales.ListarAsync(ct);

        return materiales
            .OrderBy(m => m.Familia).ThenBy(m => m.Designacion)
            .Select(m => new MaterialDto
            {
                Id = m.Id,
                Familia = m.Familia,
                Designacion = m.Designacion,
                Norma = m.Norma
            })
            .ToList();
    }

    /// <summary>
    /// Relación del inventario de la empresa solicitante. Incluye los ítems
    /// retirados, por cuanto el proveedor ha de poder consultar su histórico;
    /// dichos ítems no figuran, en cambio, en las búsquedas del catálogo.
    /// </summary>
    public async Task<IReadOnlyList<ItemInventarioDto>> ListarDeLaEmpresaAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var items = await _unidadDeTrabajo.Items.BuscarAsync(
            i => i.EmpresaId == empresaId, ct, i => i.Material);

        return items
            .OrderByDescending(i => i.FechaAlta)
            .Select(Proyectar)
            .ToList();
    }

    public async Task<ItemInventarioDto> ObtenerDeLaEmpresaAsync(
        Guid itemId, Guid empresaId, CancellationToken ct = default)
    {
        var item = await RecuperarPropioAsync(itemId, empresaId, ct);
        return Proyectar(item);
    }

    /// <summary>
    /// Alta manual de un ítem. El ítem no queda vinculado a carga alguna, lo
    /// que lo distingue de los generados por el proceso de ingesta masiva.
    /// </summary>
    public async Task<ItemInventarioDto> CrearAsync(
        Guid empresaId, CrearItemInventarioDto datos, CancellationToken ct = default)
    {
        await ComprobarEmpresaVerificadaAsync(empresaId, ct);
        await ComprobarMaterialAsync(datos.MaterialId, ct);

        ItemInventario item;

        // Las validaciones dimensionales residen en la entidad; su
        // incumplimiento se traduce aquí a excepción de negocio para que la
        // capa de API la convierta en una respuesta y no en un error interno.
        try
        {
            item = new ItemInventario(
                empresaId,
                datos.MaterialId,
                datos.LargoMm,
                datos.AnchoMm,
                datos.EspesorMm,
                datos.Cantidad,
                datos.Clasificacion,
                datos.PesoKg,
                datos.Ubicacion);
        }
        catch (ArgumentException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }

        await _unidadDeTrabajo.Items.AgregarAsync(item, ct);
        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return await ObtenerDeLaEmpresaAsync(item.Id, empresaId, ct);
    }

    /// <summary>
    /// Modificación de un ítem propio. La operación se rechaza si el ítem no
    /// se encuentra disponible (CU-04, excepción E3).
    /// </summary>
    public async Task<ItemInventarioDto> ActualizarAsync(
        Guid itemId, Guid empresaId, ActualizarItemInventarioDto datos,
        CancellationToken ct = default)
    {
        await ComprobarMaterialAsync(datos.MaterialId, ct);

        // Se recupera con seguimiento de cambios: a diferencia de las
        // consultas, esta entidad sí será modificada y persistida.
        var item = await _unidadDeTrabajo.Items.ObtenerPorIdAsync(itemId, ct)
            ?? throw new ExcepcionNoEncontrado("El ítem solicitado no existe.");

        if (!item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        try
        {
            item.Actualizar(
                datos.MaterialId,
                datos.LargoMm,
                datos.AnchoMm,
                datos.EspesorMm,
                datos.Cantidad,
                datos.Clasificacion,
                datos.PesoKg,
                datos.Ubicacion);
        }
        catch (ArgumentException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }

        _unidadDeTrabajo.Items.Actualizar(item);
        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return await ObtenerDeLaEmpresaAsync(itemId, empresaId, ct);
    }

    /// <summary>
    /// Baja de un ítem propio. Se practica como baja lógica: el registro se
    /// conserva y pasa a estado retirado, por cuanto las reservas que lo
    /// refieran constituyen histórico de operaciones y su procedencia es
    /// información de trazabilidad (RD-04).
    /// </summary>
    public async Task RetirarAsync(Guid itemId, Guid empresaId, CancellationToken ct = default)
    {
        var item = await _unidadDeTrabajo.Items.ObtenerPorIdAsync(itemId, ct)
            ?? throw new ExcepcionNoEncontrado("El ítem solicitado no existe.");

        if (!item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        try
        {
            item.Retirar();
        }
        catch (InvalidOperationException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }

        _unidadDeTrabajo.Items.Actualizar(item);
        await _unidadDeTrabajo.ConfirmarAsync(ct);
    }

    /// <summary>
    /// Devuelve al catálogo un ítem propio previamente retirado.
    ///
    /// Permite a la empresa reincorporar el material que conserva, tanto tras
    /// una retirada voluntaria como tras su rehabilitación (CU-11). No procede
    /// sobre el material transmitido: el modelo representa con un mismo estado
    /// la retirada por decisión del propietario y la transmisión del material,
    /// de modo que la distinción se practica comprobando si el ítem fue objeto
    /// de una reserva confirmada. Republicar un material ya vendido lo ofrecería
    /// a otras empresas pese a no existir físicamente.
    /// </summary>
    public async Task<ItemInventarioDto> RepublicarAsync(
        Guid itemId, Guid empresaId, CancellationToken ct = default)
    {
        await ComprobarEmpresaVerificadaAsync(empresaId, ct);

        var item = await _unidadDeTrabajo.Items.ObtenerPorIdAsync(itemId, ct)
            ?? throw new ExcepcionNoEncontrado("El ítem solicitado no existe.");

        if (!item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        if (await _reservas.FueTransmitidoAsync(itemId, ct))
            throw new ExcepcionConflicto(
                "El material fue transmitido mediante una reserva confirmada y no " +
                "puede republicarse.");

        try
        {
            item.Republicar();
        }
        catch (InvalidOperationException ex)
        {
            throw new ExcepcionDeNegocio(ex.Message);
        }

        _unidadDeTrabajo.Items.Actualizar(item);
        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return await ObtenerDeLaEmpresaAsync(itemId, empresaId, ct);
    }

    // -----------------------------------------------------------------------
    // Comprobaciones auxiliares
    // -----------------------------------------------------------------------

    private async Task<ItemInventario> RecuperarPropioAsync(
        Guid itemId, Guid empresaId, CancellationToken ct)
    {
        var item = await _unidadDeTrabajo.Items.ObtenerUnoAsync(
            i => i.Id == itemId, ct, i => i.Material)
            ?? throw new ExcepcionNoEncontrado("El ítem solicitado no existe.");

        // Control frente a la pérdida de control de acceso (Tabla 23, A01).
        // La comprobación del rol no basta: un proveedor legítimo podría
        // alterar el identificador de la petición para alcanzar el inventario
        // de otra empresa.
        if (!item.PerteneceA(empresaId))
            throw new ExcepcionAccesoDenegado();

        return item;
    }

    private async Task ComprobarEmpresaVerificadaAsync(Guid empresaId, CancellationToken ct)
    {
        var empresa = await _unidadDeTrabajo.Empresas.ObtenerPorIdAsync(empresaId, ct)
            ?? throw new ExcepcionAccesoDenegado();

        // RD-02: la operativa queda condicionada a la verificación previa de
        // la condición profesional de la empresa (CU-10).
        if (!empresa.Verificada)
            throw new ExcepcionDeNegocio(
                "La empresa se encuentra pendiente de verificación y no puede " +
                "incorporar material al catálogo.");
    }

    private async Task ComprobarMaterialAsync(int materialId, CancellationToken ct)
    {
        var existe = await _unidadDeTrabajo.Materiales.ExisteAsync(m => m.Id == materialId, ct);

        // RD-03: la designación ha de proceder del catálogo normalizado.
        if (!existe)
            throw new ExcepcionDeNegocio(
                "El material indicado no figura en el catálogo de aleaciones.");
    }

    private static ItemInventarioDto Proyectar(ItemInventario item) => new()
    {
        Id = item.Id,
        MaterialId = item.MaterialId,
        Familia = item.Material?.Familia ?? string.Empty,
        Designacion = item.Material?.Designacion ?? string.Empty,
        Norma = item.Material?.Norma,
        LargoMm = item.LargoMm,
        AnchoMm = item.AnchoMm,
        EspesorMm = item.EspesorMm,
        Cantidad = item.Cantidad,
        PesoKg = item.PesoKg,
        Ubicacion = item.Ubicacion,
        Estado = item.Estado.ToString(),
        Clasificacion = item.Clasificacion.ToString(),
        ProcedeDeCargaMasiva = item.CargaId is not null,
        FechaAlta = item.FechaAlta
    };
}
