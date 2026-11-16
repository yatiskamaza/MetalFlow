using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta la consulta del catálogo por parte de la empresa compradora
/// (CU-07 y CU-08), que materializa el requisito RF-04.
///
/// Constituye el elemento diferencial del sistema respecto de las plataformas
/// analizadas en el apartado 2.4: la búsqueda opera sobre las dimensiones
/// geométricas de cada pieza, y no sobre su peso o su familia de material,
/// lo que permite localizar un retal apto para una necesidad concreta de
/// mecanizado.
/// </summary>
public class MotorBusqueda
{
    private readonly IRepositorioCatalogo _catalogo;

    public MotorBusqueda(IRepositorioCatalogo catalogo) => _catalogo = catalogo;

    /// <summary>
    /// CU-07. Búsqueda paramétrica del catálogo.
    ///
    /// Los ítems de la empresa solicitante se excluyen del resultado: una
    /// empresa no adquiere su propio material, y su inclusión obligaría al
    /// comprador a descartarlo en cada consulta.
    /// </summary>
    public async Task<ResultadoBusquedaDto> BuscarAsync(
        BusquedaCatalogoDto filtros, Guid empresaSolicitante, CancellationToken ct = default)
    {
        ValidarCoherenciaDeRangos(filtros);

        var criterios = new CriteriosDeBusqueda
        {
            MaterialId = filtros.MaterialId,
            Familia = filtros.Familia,
            EspesorMinimo = filtros.EspesorMinimo,
            EspesorMaximo = filtros.EspesorMaximo,
            LargoMinimo = filtros.LargoMinimo,
            LargoMaximo = filtros.LargoMaximo,
            AnchoMinimo = filtros.AnchoMinimo,
            AnchoMaximo = filtros.AnchoMaximo,
            Localidad = filtros.Localidad,
            EmpresaExcluida = empresaSolicitante,
            Pagina = filtros.Pagina,
            TamanoPagina = filtros.TamanoPagina
        };

        var pagina = await _catalogo.BuscarAsync(criterios, ct);

        return new ResultadoBusquedaDto
        {
            Resultados = pagina.Elementos.Select(Proyectar).ToList(),
            Pagina = pagina.Pagina,
            TamanoPagina = pagina.TamanoPagina,
            TotalRegistros = pagina.TotalRegistros,
            TotalPaginas = pagina.TotalPaginas
        };
    }

    /// <summary>
    /// CU-08. Detalle de un ítem del catálogo.
    ///
    /// Se devuelve también cuando el ítem ha sido reservado entre la consulta
    /// y el acceso al detalle, supuesto previsto como excepción E1: el estado
    /// figura en la respuesta, de modo que la interfaz pueda advertir al
    /// comprador e inhabilitar la acción de reservar.
    /// </summary>
    public async Task<ItemCatalogoDto> ObtenerDetalleAsync(
        Guid itemId, Guid empresaSolicitante, CancellationToken ct = default)
    {
        var item = await _catalogo.ObtenerDelCatalogoAsync(itemId, ct)
            ?? throw new ExcepcionNoEncontrado(
                "El material solicitado no existe o ha sido retirado del catálogo.");

        // El detalle del material propio corresponde a la gestión del
        // inventario (CU-04) y se consulta por su extremo específico, que
        // ofrece los datos de gestión que aquí se omiten.
        if (item.EmpresaId == empresaSolicitante)
            throw new ExcepcionNoEncontrado(
                "El material solicitado no existe o ha sido retirado del catálogo.");

        return Proyectar(item);
    }

    /// <summary>
    /// CU-07, excepción E2. Un rango cuyo mínimo excede al máximo no admite
    /// resultado alguno y responde con seguridad a un error del solicitante,
    /// por lo que se rechaza en lugar de devolver una lista vacía.
    /// </summary>
    private static void ValidarCoherenciaDeRangos(BusquedaCatalogoDto filtros)
    {
        ComprobarRango(filtros.EspesorMinimo, filtros.EspesorMaximo, "espesor");
        ComprobarRango(filtros.LargoMinimo, filtros.LargoMaximo, "largo");
        ComprobarRango(filtros.AnchoMinimo, filtros.AnchoMaximo, "ancho");
    }

    private static void ComprobarRango(decimal? minimo, decimal? maximo, string dimension)
    {
        if (minimo is { } min && maximo is { } max && min > max)
            throw new ExcepcionDeNegocio(
                $"El valor mínimo del {dimension} no puede exceder al máximo.");
    }

    /// <summary>
    /// La información comercial solo se incorpora cuando el ítem se encuentra
    /// disponible y su anuncio está activo (RN-4). La condición sobre el ítem
    /// resulta necesaria en el detalle, que admite también los ítems
    /// reservados (excepción E1 de CU-08): el comprador ha de advertir que el
    /// material ya no está disponible, no recibir las condiciones comerciales
    /// de una oferta que no puede aceptar.
    /// </summary>
    private static bool MuestraInformacionComercial(ItemInventario item)
        => item.Estado == EstadoItem.Disponible && item.Anuncio is { Activo: true };

    /// <summary>
    /// Requiere que la consulta haya cargado el material, la empresa y el
    /// anuncio del ítem, como hacen las dos operaciones del repositorio de
    /// catálogo. Su ausencia constituiría un error de programación, que debe
    /// manifestarse como tal y no traducirse en una respuesta con datos vacíos.
    /// </summary>
    private static ItemCatalogoDto Proyectar(ItemInventario item) => new()
    {
        Id = item.Id,
        Familia = item.Material.Familia,
        Designacion = item.Material.Designacion,
        Norma = item.Material.Norma,
        LargoMm = item.LargoMm,
        AnchoMm = item.AnchoMm,
        EspesorMm = item.EspesorMm,
        Cantidad = item.Cantidad,
        PesoKg = item.PesoKg,
        Clasificacion = item.Clasificacion.ToString(),
        Estado = item.Estado.ToString(),
        EmpresaProveedora = item.Empresa.RazonSocial,
        Localidad = item.Empresa.Localidad,
        EmpresaVerificada = item.Empresa.Verificada,
        FechaAlta = item.FechaAlta,

        // Datos comerciales, presentes solo si el ítem está disponible y cuenta
        // con anuncio activo. Su ausencia no impide que el material figure en
        // el catálogo.
        Titulo = MuestraInformacionComercial(item) ? item.Anuncio!.Titulo : null,
        Descripcion = MuestraInformacionComercial(item) ? item.Anuncio!.Descripcion : null,
        Precio = MuestraInformacionComercial(item) ? item.Anuncio!.Precio : null
    };
}