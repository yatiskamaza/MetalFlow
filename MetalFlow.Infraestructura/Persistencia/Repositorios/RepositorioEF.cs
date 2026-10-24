using System.Linq.Expressions;
using MetalFlow.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace MetalFlow.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Implementación del patrón Repository sobre Entity Framework Core
/// (Fowler, 2002). Reside en la capa de infraestructura e implementa la
/// interfaz declarada en el dominio, materializando así la inversión de
/// dependencias descrita en el apartado 4.2.
///
/// Las operaciones de escritura no persisten por sí mismas: se limitan a
/// registrar el cambio en el contexto, correspondiendo a la unidad de trabajo
/// la confirmación de la transacción. De este modo, todas las escrituras de un
/// mismo caso de uso se confirman de forma atómica (CT-02, CT-06).
/// </summary>
public class RepositorioEF<T> : IRepositorio<T> where T : class
{
    protected readonly MetalFlowDbContext Contexto;
    protected readonly DbSet<T> Conjunto;

    public RepositorioEF(MetalFlowDbContext contexto)
    {
        Contexto = contexto;
        Conjunto = contexto.Set<T>();
    }

    public async Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
        => await Conjunto.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default)
        => await Conjunto.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> BuscarAsync(
        Expression<Func<T, bool>> criterio, CancellationToken ct = default)
        => await Conjunto.AsNoTracking().Where(criterio).ToListAsync(ct);

    public async Task<bool> ExisteAsync(
        Expression<Func<T, bool>> criterio, CancellationToken ct = default)
        => await Conjunto.AnyAsync(criterio, ct);

    public async Task AgregarAsync(T entidad, CancellationToken ct = default)
        => await Conjunto.AddAsync(entidad, ct);

    public async Task AgregarRangoAsync(IEnumerable<T> entidades, CancellationToken ct = default)
        => await Conjunto.AddRangeAsync(entidades, ct);

    public void Actualizar(T entidad) => Conjunto.Update(entidad);

    public void Eliminar(T entidad) => Conjunto.Remove(entidad);
}
