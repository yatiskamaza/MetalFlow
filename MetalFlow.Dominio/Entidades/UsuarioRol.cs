using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Entidad de asociación que resuelve la relación N:M entre cuentas y roles.
/// Su clave primaria compuesta impide la asignación duplicada de un mismo rol.
/// </summary>
public class UsuarioRol
{
    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public Rol Rol { get; private set; }

    private UsuarioRol() { }

    public UsuarioRol(Guid usuarioId, Rol rol)
    {
        UsuarioId = usuarioId;
        Rol = rol;
    }
}
