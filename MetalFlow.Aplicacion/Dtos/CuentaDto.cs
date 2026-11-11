namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de una cuenta de acceso para el administrador (CU-11).
///
/// Incorpora la empresa asociada y los roles, información necesaria para
/// supervisar el acceso al sistema. No expone la credencial almacenada en
/// forma alguna.
/// </summary>
public class CuentaDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];

    // Nulos en la cuenta del administrador, única carente de empresa.
    public Guid? EmpresaId { get; set; }
    public string? RazonSocial { get; set; }
    public bool? EmpresaVerificada { get; set; }
}