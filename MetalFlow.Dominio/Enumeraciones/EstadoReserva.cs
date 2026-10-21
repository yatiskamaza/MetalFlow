namespace MetalFlow.Dominio.Enumeraciones;

/// <summary>Ciclo de vida de una reserva (RF-05, RD-06).</summary>
public enum EstadoReserva : byte
{
    Pendiente = 1,
    Confirmada = 2,
    Cancelada = 3,
    Expirada = 4
}
