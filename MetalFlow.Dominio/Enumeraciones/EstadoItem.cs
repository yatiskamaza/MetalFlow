namespace MetalFlow.Dominio.Enumeraciones;

/// <summary>Estado de disponibilidad de un ítem de inventario (RF-05).</summary>
public enum EstadoItem : byte
{
    Disponible = 1,
    Reservado = 2,
    Retirado = 3
}
