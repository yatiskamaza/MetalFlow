namespace MetalFlow.Dominio.Enumeraciones;

/// <summary>
/// Roles que puede acumular una cuenta de acceso (RD-02).
/// Una misma cuenta puede ser simultáneamente proveedora y compradora,
/// supuesto característico de la simbiosis industrial.
/// </summary>
public enum Rol : byte
{
    Proveedor = 1,
    Comprador = 2,
    Administrador = 3
}
