using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore.Storage;

namespace MetalFlow.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Implementación del patrón Unit of Work (Fowler, 2002). Agrupa en una única
/// transacción todas las operaciones de escritura de un caso de uso,
/// garantizando la atomicidad exigida por la carga masiva de inventario
/// (CT-02) y por el flujo de reserva (CT-06).
///
/// Los repositorios se instancian de forma diferida y comparten el mismo
/// contexto, de modo que las entidades modificadas a través de cualquiera de
/// ellos participan en la misma transacción.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MetalFlowDbContext _contexto;
    private IDbContextTransaction? _transaccion;

    private IRepositorio<Empresa>? _empresas;
    private IRepositorio<Usuario>? _usuarios;
    private IRepositorio<Material>? _materiales;
    private IRepositorio<ItemInventario>? _items;
    private IRepositorio<Anuncio>? _anuncios;
    private IRepositorio<Reserva>? _reservas;
    private IRepositorio<CargaInventario>? _cargas;

    public UnitOfWork(MetalFlowDbContext contexto) => _contexto = contexto;

    public IRepositorio<Empresa> Empresas
        => _empresas ??= new RepositorioEF<Empresa>(_contexto);

    public IRepositorio<Usuario> Usuarios
        => _usuarios ??= new RepositorioEF<Usuario>(_contexto);

    public IRepositorio<Material> Materiales
        => _materiales ??= new RepositorioEF<Material>(_contexto);

    public IRepositorio<ItemInventario> Items
        => _items ??= new RepositorioEF<ItemInventario>(_contexto);

    public IRepositorio<Anuncio> Anuncios
        => _anuncios ??= new RepositorioEF<Anuncio>(_contexto);

    public IRepositorio<Reserva> Reservas
        => _reservas ??= new RepositorioEF<Reserva>(_contexto);

    public IRepositorio<CargaInventario> Cargas
        => _cargas ??= new RepositorioEF<CargaInventario>(_contexto);

    /// <summary>
    /// Abre una transacción explícita. Solo resulta necesaria en los casos de
    /// uso que escriben sobre varias entidades o que requieren bloqueo de
    /// registros; para una escritura simple basta con confirmar los cambios.
    /// </summary>
    public async Task IniciarTransaccionAsync(CancellationToken ct = default)
    {
        if (_transaccion is not null)
            throw new InvalidOperationException("Ya existe una transacción abierta.");

        _transaccion = await _contexto.Database.BeginTransactionAsync(ct);
    }

    public async Task<int> ConfirmarAsync(CancellationToken ct = default)
    {
        var registrosAfectados = await _contexto.SaveChangesAsync(ct);

        if (_transaccion is not null)
        {
            await _transaccion.CommitAsync(ct);
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }

        return registrosAfectados;
    }

    public async Task RevertirAsync(CancellationToken ct = default)
    {
        if (_transaccion is null) return;

        await _transaccion.RollbackAsync(ct);
        await _transaccion.DisposeAsync();
        _transaccion = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaccion is not null)
            await _transaccion.DisposeAsync();

        await _contexto.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
