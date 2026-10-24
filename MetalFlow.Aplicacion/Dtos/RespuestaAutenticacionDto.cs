namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Resultado de una autenticación satisfactoria. No incluye dato personal
/// alguno más allá del nombre de la persona responsable, conforme al principio
/// de minimización del apartado 4.8.
/// </summary>
public class RespuestaAutenticacionDto
{
    public string Token { get; set; } = null!;
    public DateTime Expiracion { get; set; }
    public string Nombre { get; set; } = null!;
    public string? RazonSocialEmpresa { get; set; }
    public bool EmpresaVerificada { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}