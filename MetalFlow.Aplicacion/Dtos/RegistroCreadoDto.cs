namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Resultado del alta de una empresa. La empresa queda pendiente de
/// verificación por parte del administrador (CU-10), por lo que no puede
/// publicar ni reservar material hasta que esta se complete (RD-02).
/// </summary>
public class RegistroCreadoDto
{
    public Guid EmpresaId { get; set; }
    public string RazonSocial { get; set; } = null!;
    public bool Verificada { get; set; }
    public string Mensaje { get; set; } = null!;
}