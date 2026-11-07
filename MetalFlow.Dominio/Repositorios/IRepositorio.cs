using System.Linq.Expressions;

namespace MetalFlow.Dominio.Repositorios;

/// <summary>
/// Abstrae el conjunto de objetos de dominio como si fuera una colección en
/// memoria (Fowler, 2002). Se declara en el dominio y se implementa en
/// infraestructura: de este modo la dirección de la dependencia se invierte
/// respecto del flujo de ejecución y el dominio queda aislado de la tecnología
/// de persistencia.
///
/// Las operaciones de consulta admiten la carga explícita de relaciones, lo
/// que resulta necesario en los casos de uso que precisan navegar a entidades
/// asociadas.
/// </summary>
public interface IRepositorio<T> where T : class
{
    Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    Task<T?> ObtenerUnoAsync(Expression<Func<T, bool>> criterio,
        CancellationToken ct = default,
        params Expression<Func<T, object>>[] incluir);

    Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default);

    Task<IReadOnlyList<T>> BuscarAsync(Expression<Func<T, bool>> criterio,
        CancellationToken ct = default,
        params Expression<Func<T, object>>[] incluir);

    Task<bool> ExisteAsync(Expression<Func<T, bool>> criterio, CancellationToken ct = default);
    Task AgregarAsync(T entidad, CancellationToken ct = default);
    Task AgregarRangoAsync(IEnumerable<T> entidades, CancellationToken ct = default);
    void Actualizar(T entidad);
    void Eliminar(T entidad);
}