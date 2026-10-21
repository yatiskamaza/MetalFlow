using MetalFlow.Dominio.Entidades;

namespace MetalFlow.Dominio.Repositorios;

/// <summary>
/// Agrupa en una única transacción todas las operaciones de escritura de un caso
/// de uso (Fowler, 2002), garantizando la atomicidad que exigen CT-02 y CT-06.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IRepositorio<Empresa> Empresas { get; }
    IRepositorio<Usuario> Usuarios { get; }
    IRepositorio<Material> Materiales { get; }
    IRepositorio<ItemInventario> Items { get; }
    IRepositorio<Anuncio> Anuncios { get; }
    IRepositorio<Reserva> Reservas { get; }
    IRepositorio<CargaInventario> Cargas { get; }

    Task IniciarTransaccionAsync(CancellationToken ct = default);
    Task<int> ConfirmarAsync(CancellationToken ct = default);
    Task RevertirAsync(CancellationToken ct = default);
}
