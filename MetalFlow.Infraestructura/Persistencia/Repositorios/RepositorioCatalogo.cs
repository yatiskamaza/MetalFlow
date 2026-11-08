using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace MetalFlow.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Implementación de la consulta del catálogo sobre Entity Framework Core.
///
/// La consulta se compone de forma diferida y no se ejecuta hasta que se
/// solicita su materialización, de modo que la totalidad de los filtros, la
/// ordenación y la paginación se trasladan al gestor de base de datos y se
/// resuelven apoyándose en el índice compuesto. Este es el punto del sistema
/// en que se materializa el requisito de rendimiento RNF-01.
/// </summary>
public class RepositorioCatalogo : IRepositorioCatalogo
{
    /// <summary>Límite del tamaño de página, para acotar el coste de una petición.</summary>
    private const int TamanoMaximoDePagina = 100;

    private readonly MetalFlowDbContext _contexto;

    public RepositorioCatalogo(MetalFlowDbContext contexto) => _contexto = contexto;

    public async Task<ResultadoPaginado<ItemInventario>> BuscarAsync(
        CriteriosDeBusqueda criterios, CancellationToken ct = default)
    {
        var consulta = ComponerConsulta(criterios);

        // El recuento total exige una segunda consulta sobre el mismo
        // conjunto filtrado. Es el coste de informar al cliente del número de
        // páginas disponibles; se ejecuta sobre el índice y no recupera datos.
        var total = await consulta.CountAsync(ct);

        var pagina = Math.Max(1, criterios.Pagina);
        var tamano = Math.Clamp(criterios.TamanoPagina, 1, TamanoMaximoDePagina);

        var elementos = await consulta
            .OrderByDescending(i => i.FechaAlta)
            .ThenBy(i => i.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Include(i => i.Material)
            .Include(i => i.Empresa)
            .Include(i => i.Anuncio)
            .ToListAsync(ct);

        return new ResultadoPaginado<ItemInventario>
        {
            Elementos = elementos,
            Pagina = pagina,
            TamanoPagina = tamano,
            TotalRegistros = total
        };
    }

    public async Task<ItemInventario?> ObtenerDelCatalogoAsync(
        Guid itemId, CancellationToken ct = default)
        => await _contexto.Items
            .AsNoTracking()
            .Include(i => i.Material)
            .Include(i => i.Empresa)
            .Include(i => i.Anuncio)
            .FirstOrDefaultAsync(
                i => i.Id == itemId && i.Estado != EstadoItem.Retirado, ct);

    /// <summary>
    /// Compone la consulta incorporando únicamente los filtros especificados.
    ///
    /// El orden de aplicación reproduce el del índice compuesto: la condición
    /// sobre el estado encabeza la consulta por ser la de mayor selectividad,
    /// y le siguen las condiciones de rango sobre las dimensiones. Dicho orden
    /// no es exigido por el lenguaje de consulta, pero facilita que el
    /// planificador del gestor reconozca la correspondencia con el índice.
    /// </summary>
    private IQueryable<ItemInventario> ComponerConsulta(CriteriosDeBusqueda criterios)
    {
        // El catálogo comprende exclusivamente el material disponible: los
        // reservados y los retirados quedan excluidos por definición.
        var consulta = _contexto.Items
            .AsNoTracking()
            .Where(i => i.Estado == EstadoItem.Disponible);

        if (criterios.EmpresaExcluida is { } propia)
            consulta = consulta.Where(i => i.EmpresaId != propia);

        // Dimensiones (RF-04). Cada extremo del rango es independiente: cabe
        // especificar únicamente el mínimo, únicamente el máximo o ambos.
        if (criterios.EspesorMinimo is { } espesorMin)
            consulta = consulta.Where(i => i.EspesorMm >= espesorMin);

        if (criterios.EspesorMaximo is { } espesorMax)
            consulta = consulta.Where(i => i.EspesorMm <= espesorMax);

        if (criterios.LargoMinimo is { } largoMin)
            consulta = consulta.Where(i => i.LargoMm >= largoMin);

        if (criterios.LargoMaximo is { } largoMax)
            consulta = consulta.Where(i => i.LargoMm <= largoMax);

        if (criterios.AnchoMinimo is { } anchoMin)
            consulta = consulta.Where(i => i.AnchoMm >= anchoMin);

        if (criterios.AnchoMaximo is { } anchoMax)
            consulta = consulta.Where(i => i.AnchoMm <= anchoMax);

        // Material (RD-03). El filtro por identificador se resuelve sobre la
        // clave foránea y su índice propio; el filtro por familia exige
        // reunión con la tabla de materiales, de menor eficiencia pero de
        // cardinalidad reducida.
        if (criterios.MaterialId is { } materialId)
            consulta = consulta.Where(i => i.MaterialId == materialId);

        if (!string.IsNullOrWhiteSpace(criterios.Familia))
            consulta = consulta.Where(i => i.Material.Familia == criterios.Familia);

        // Proximidad (RD-07). Se resuelve por coincidencia de localidad: el
        // modelo no almacena coordenadas, conforme a la limitación reconocida
        // en el apartado 4.9.
        if (!string.IsNullOrWhiteSpace(criterios.Localidad))
            consulta = consulta.Where(i => i.Empresa.Localidad == criterios.Localidad);

        return consulta;
    }
}