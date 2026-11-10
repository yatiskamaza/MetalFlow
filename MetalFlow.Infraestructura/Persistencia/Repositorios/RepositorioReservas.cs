using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace MetalFlow.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Implementación de las consultas propias del flujo de reserva.
/// </summary>
public class RepositorioReservas : IRepositorioReservas
{
    private readonly MetalFlowDbContext _contexto;

    public RepositorioReservas(MetalFlowDbContext contexto) => _contexto = contexto;

    /// <summary>
    /// Recupera el ítem aplicando un bloqueo exclusivo de fila que persiste
    /// hasta la conclusión de la transacción.
    ///
    /// La consulta se formula en el lenguaje del gestor porque el marco de
    /// persistencia no ofrece abstracción para expresar indicadores de
    /// bloqueo. El primero de ellos reserva la fila para una actualización
    /// inminente; el segundo acota el bloqueo a la fila y evita que se
    /// extienda a la página completa, lo que inmovilizaría ítems ajenos a la
    /// operación.
    ///
    /// El parámetro se transmite de forma parametrizada y no por
    /// concatenación de cadenas, de modo que la consulta no resulta
    /// susceptible de inyección (Tabla 23, OWASP A03).
    /// </summary>
    public async Task<ItemInventario?> ObtenerItemConBloqueoAsync(
        Guid itemId, CancellationToken ct = default)
        => await _contexto.Items
            .FromSql($@"
                SELECT * FROM ITEM_INVENTARIO WITH (UPDLOCK, ROWLOCK)
                WHERE Id = {itemId}")
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Reservas pendientes cuya vigencia ha transcurrido, con su ítem cargado.
    ///
    /// La comparación se practica sobre la hora universal, criterio empleado
    /// de forma uniforme en el registro de todas las marcas temporales del
    /// sistema, de modo que el resultado no dependa de la zona horaria del
    /// servidor.
    /// </summary>
    public async Task<IReadOnlyList<Reserva>> ListarVencidasAsync(
        CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;

        return await _contexto.Reservas
            .Include(r => r.Item)
            .Where(r => r.Estado == EstadoReserva.Pendiente && r.FechaExpiracion <= ahora)
            .OrderBy(r => r.FechaExpiracion)
            .ToListAsync(ct);
    }

    public async Task<Reserva?> ObtenerReservaActivaAsync(
        Guid itemId, CancellationToken ct = default)
        => await _contexto.Reservas
            .Include(r => r.Item)
            .FirstOrDefaultAsync(
                r => r.ItemId == itemId && r.Estado == EstadoReserva.Pendiente, ct);

    public async Task<IReadOnlyList<Reserva>> ListarDeCompradoraAsync(
        Guid empresaId, CancellationToken ct = default)
        => await _contexto.Reservas
            .AsNoTracking()
            .Include(r => r.Item).ThenInclude(i => i.Material)
            .Include(r => r.Item).ThenInclude(i => i.Empresa)
            .Include(r => r.EmpresaCompradora)
            .Where(r => r.EmpresaCompradoraId == empresaId)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Reserva>> ListarDeProveedoraAsync(
        Guid empresaId, CancellationToken ct = default)
        => await _contexto.Reservas
            .AsNoTracking()
            .Include(r => r.Item).ThenInclude(i => i.Material)
            .Include(r => r.Item).ThenInclude(i => i.Empresa)
            .Include(r => r.EmpresaCompradora)
            .Where(r => r.Item.EmpresaId == empresaId)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync(ct);

    /// <summary>
    /// Recupera la reserva con sus relaciones y con seguimiento de cambios:
    /// las operaciones que la emplean modifican tanto la reserva como el ítem
    /// asociado, por lo que ambos han de ser objeto de seguimiento.
    /// </summary>
    public async Task<Reserva?> ObtenerConRelacionesAsync(
        Guid reservaId, CancellationToken ct = default)
        => await _contexto.Reservas
            .Include(r => r.Item).ThenInclude(i => i.Material)
            .Include(r => r.Item).ThenInclude(i => i.Empresa)
            .Include(r => r.EmpresaCompradora)
            .FirstOrDefaultAsync(r => r.Id == reservaId, ct);
}
