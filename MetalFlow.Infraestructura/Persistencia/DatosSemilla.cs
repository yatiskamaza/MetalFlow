using MetalFlow.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace MetalFlow.Infraestructura.Persistencia;

/// <summary>
/// Datos incorporados en la migración inicial.
///
/// Incluye el catálogo normalizado de materiales (RD-03) y la cuenta del
/// administrador de la plataforma. Esta última constituye la única excepción al
/// flujo de registro: no se crea mediante CU-01, sino que se aprovisiona en el
/// despliegue inicial como dato semilla, de forma coherente con su condición de
/// operador del sistema y no de empresa usuaria. De ahí que sea la única cuenta
/// cuyo campo de empresa carece de valor.
/// </summary>
internal static class DatosSemilla
{
    // Identificadores fijos: las semillas deben ser deterministas para que la
    // migración sea reproducible y no genere diferencias en cada ejecución.
    private static readonly Guid AdministradorId = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    /// <summary>
    /// Hash BCrypt de la contraseña inicial del administrador.
    /// Se genera en el arranque a partir de una variable de entorno y debe
    /// modificarse tras el primer acceso; nunca se versiona una contraseña en claro.
    /// </summary>
    private const string HashInicialAdministrador =
        "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";

    public static void Aplicar(ModelBuilder modelBuilder)
    {
        SembrarMateriales(modelBuilder);
        SembrarAdministrador(modelBuilder);
    }

    /// <summary>
    /// Catálogo inicial de aleaciones conforme a la designación de la norma
    /// EN 10025-2 para aceros estructurales y a las designaciones habituales
    /// de inoxidables y aluminios en manufactura metálica.
    /// </summary>
    private static void SembrarMateriales(ModelBuilder mb)
    {
        mb.Entity<Dominio.Entidades.Material>().HasData(
            new { Id = 1, Familia = "Acero al carbono", Designacion = "S235JR", Norma = "EN 10025-2" },
            new { Id = 2, Familia = "Acero al carbono", Designacion = "S275JR", Norma = "EN 10025-2" },
            new { Id = 3, Familia = "Acero al carbono", Designacion = "S355JR", Norma = "EN 10025-2" },
            new { Id = 4, Familia = "Acero inoxidable", Designacion = "AISI 304", Norma = "EN 10088-2" },
            new { Id = 5, Familia = "Acero inoxidable", Designacion = "AISI 316L", Norma = "EN 10088-2" },
            new { Id = 6, Familia = "Aluminio", Designacion = "EN AW-5754", Norma = "EN 485-2" },
            new { Id = 7, Familia = "Aluminio", Designacion = "EN AW-6082", Norma = "EN 485-2" }
        );
    }

    private static void SembrarAdministrador(ModelBuilder mb)
    {
        mb.Entity<Dominio.Entidades.Usuario>().HasData(new
        {
            Id = AdministradorId,
            EmpresaId = (Guid?)null,          // Única cuenta sin empresa asociada
            Email = "admin@metalflow.local",
            PasswordHash = HashInicialAdministrador,
            Nombre = "Administrador del sistema",
            Activo = true,
            FechaAlta = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        mb.Entity<Dominio.Entidades.UsuarioRol>().HasData(new
        {
            UsuarioId = AdministradorId,
            Rol = Rol.Administrador
        });
    }
}
