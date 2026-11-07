namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Resultado de la verificación de una empresa (CU-10).
/// </summary>
public class VerificacionEmpresaDto
{
    public Guid EmpresaId { get; set; }
    public string RazonSocial { get; set; } = null!;
    public string Nif { get; set; } = null!;
    public bool Verificada { get; set; }
    public string Mensaje { get; set; } = null!;
}