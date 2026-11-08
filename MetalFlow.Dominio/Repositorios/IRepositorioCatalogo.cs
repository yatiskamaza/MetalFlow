using MetalFlow.Dominio.Entidades;

namespace MetalFlow.Dominio.Repositorios;

/// <summary>
/// Criterios de la búsqueda paramétrica (RF-04).
///
/// Todos los filtros son opcionales y se combinan mediante conjunción: un ítem
/// figura en el resultado únicamente si satisface simultáneamente la totalidad
/// de los criterios especificados.
/// </summary>
public class CriteriosDeBusqueda
{
    public int? MaterialId { get; init; }
    public string? Familia { get; init; }

    public decimal? EspesorMinimo { get; init; }
    public decimal? EspesorMaximo { get; init; }
    public decimal? LargoMinimo { get; init; }
    public decimal? LargoMaximo { get; init; }
    public decimal? AnchoMinimo { get; init; }
    public decimal? AnchoMaximo { get; init; }

    public string? Localidad { get; init; }

    /// <summary>
    /// Empresa cuyos ítems quedan excluidos del resultado. Se corresponde con
    /// la del solicitante: una empresa no adquiere su propio material, y su
    /// inclusión obligaría al comprador a descartarlo manualmente en cada
    /// búsqueda.
    /// </summary>
    public Guid? EmpresaExcluida { get; init; }

    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 20;
}

/// <summary>Página de resultados junto con los datos de paginación.</summary>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Elementos { get; init; } = [];
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }

    public int TotalPaginas =>
        TamanoPagina == 0 ? 0 : (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);
}

/// <summary>
/// Repositorio especializado en la consulta del catálogo.
///
/// Su existencia obedece a una limitación del repositorio genérico: este
/// devuelve colecciones ya materializadas, de modo que el filtrado se
/// resolvería en memoria tras haber recuperado la totalidad de la tabla. El
/// índice compuesto descrito en el apartado 4.5.2 no llegaría a emplearse y el
/// umbral de respuesta establecido por RNF-01 resultaría inalcanzable conforme
/// creciera el catálogo.
///
/// La composición de la consulta se mantiene, por tanto, en la capa de
/// infraestructura, que la traslada íntegra al gestor de base de datos. El
/// dominio declara únicamente el contrato, con lo que la inversión de
/// dependencias del apartado 4.2 permanece intacta.
/// </summary>
public interface IRepositorioCatalogo
{
    /// <summary>
    /// Devuelve la página de ítems disponibles que satisfacen los criterios.
    /// </summary>
    Task<ResultadoPaginado<ItemInventario>> BuscarAsync(
        CriteriosDeBusqueda criterios, CancellationToken ct = default);

    /// <summary>
    /// Recupera un ítem del catálogo con sus relaciones. Incluye los ítems
    /// reservados, por cuanto el comprador ha de poder advertir que el
    /// material consultado ha dejado de estar disponible (CU-08, excepción E1);
    /// excluye en cambio los retirados, que no forman parte del catálogo.
    /// </summary>
    Task<ItemInventario?> ObtenerDelCatalogoAsync(
        Guid itemId, CancellationToken ct = default);
}