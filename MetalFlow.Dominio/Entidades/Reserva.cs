using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Bloqueo de un ítem por parte de una empresa compradora (RF-05).
/// La plataforma actúa como intermediario de emparejamiento y reserva:
/// no procesa pago alguno (RD-06).
/// </summary>
public class Reserva
{
    public const int HorasDeVigencia = 72;

    public Guid Id { get; private set; }

    public Guid ItemId { get; private set; }
    public ItemInventario Item { get; private set; } = null!;

    public Guid EmpresaCompradoraId { get; private set; }
    public Empresa EmpresaCompradora { get; private set; } = null!;

    public DateTime FechaReserva { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public EstadoReserva Estado { get; private set; }

    private Reserva() { }

    public Reserva(Guid itemId, Guid empresaCompradoraId)
    {
        Id = Guid.NewGuid();
        ItemId = itemId;
        EmpresaCompradoraId = empresaCompradoraId;
        FechaReserva = DateTime.UtcNow;
        FechaExpiracion = FechaReserva.AddHours(HorasDeVigencia);
        Estado = EstadoReserva.Pendiente;
    }

    public void Confirmar()
    {
        if (Estado != EstadoReserva.Pendiente)
            throw new InvalidOperationException("Solo puede confirmarse una reserva pendiente.");
        Estado = EstadoReserva.Confirmada;
    }

    public void Cancelar()
    {
        if (Estado is EstadoReserva.Cancelada or EstadoReserva.Expirada)
            throw new InvalidOperationException("La reserva ya no se encuentra activa.");
        Estado = EstadoReserva.Cancelada;
    }

    public void Expirar() => Estado = EstadoReserva.Expirada;

    public bool EstaActiva() => Estado is EstadoReserva.Pendiente or EstadoReserva.Confirmada;
}
