using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Cuenta de acceso al sistema. Entidad de autenticación única: concentra las
/// credenciales de todas las cuentas, tanto las vinculadas a una empresa (1:1)
/// como la del administrador de la plataforma, que carece de empresa asociada
/// y se aprovisiona como dato semilla en el despliegue inicial.
/// </summary>
public class Usuario
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Nulo únicamente en la cuenta de administrador. La unicidad de este campo
    /// garantiza de forma declarativa que una empresa no tenga más de una cuenta.
    /// </summary>
    public Guid? EmpresaId { get; private set; }
    public Empresa? Empresa { get; private set; }

    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; }
    public DateTime FechaAlta { get; private set; }

    private readonly List<UsuarioRol> _roles = new();
    public IReadOnlyCollection<UsuarioRol> Roles => _roles.AsReadOnly();

    private Usuario() { }

    public Usuario(string email, string passwordHash, string nombre, Guid? empresaId)
    {
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Nombre = nombre.Trim();
        EmpresaId = empresaId;
        Activo = true;
        FechaAlta = DateTime.UtcNow;
    }

    public void AsignarRol(Rol rol)
    {
        if (_roles.Any(r => r.Rol == rol)) return;
        _roles.Add(new UsuarioRol(Id, rol));
    }

    public bool TieneRol(Rol rol) => _roles.Any(r => r.Rol == rol);

    public bool EsAdministrador() => TieneRol(Rol.Administrador);

    public void Desactivar() => Activo = false;

    /// <summary>
    /// Invariante del dominio: solo una cuenta de administrador puede carecer de empresa.
    /// No es expresable como restricción declarativa simple por implicar dos tablas.
    /// </summary>
    public bool CumpleInvarianteDeEmpresa() => EmpresaId is not null || EsAdministrador();
}
