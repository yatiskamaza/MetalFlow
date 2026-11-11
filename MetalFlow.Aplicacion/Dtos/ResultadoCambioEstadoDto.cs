namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Resultado del cambio de estado de una cuenta, con el alcance de sus efectos
/// sobre el catálogo y las operaciones en curso.
/// </summary>
public class ResultadoCambioEstadoDto
{
    public Guid CuentaId { get; set; }
    public bool Activo { get; set; }

    /// <summary>Ítems disponibles retirados del catálogo a raíz de la inhabilitación.</summary>
    public int ItemsRetirados { get; set; }

    /// <summary>
    /// Reservas pendientes sobre material de la empresa, que no se cancelan por
    /// existir una expectativa legítima de la empresa compradora.
    /// </summary>
    public int ReservasPendientes { get; set; }

    public string Mensaje { get; set; } = null!;
}