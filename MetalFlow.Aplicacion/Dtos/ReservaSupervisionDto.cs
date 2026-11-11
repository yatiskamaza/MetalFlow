namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de una reserva para la supervisión del administrador
/// (CU-11).
///
/// Comprende el estado de la operación y las empresas implicadas, pero no sus
/// datos de contacto: el administrador supervisa la actividad de la
/// plataforma sin intervenir en la relación comercial entre las partes,
/// conforme al principio de minimización del apartado 4.8.
/// </summary>
public class ReservaSupervisionDto
{
    public Guid Id { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaReserva { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public Guid ItemId { get; set; }
    public string Designacion { get; set; } = null!;
    public string EmpresaProveedora { get; set; } = null!;
    public string EmpresaCompradora { get; set; } = null!;
}