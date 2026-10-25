using MetalFlow.Aplicacion.Abstracciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MetalFlow.Infraestructura.Persistencia;

/// <summary>
/// Aprovisiona la cuenta del administrador de la plataforma en el arranque de
/// la aplicación.
///
/// La verificación de empresas corresponde al administrador (CU-10) y el
/// registro crea las empresas en estado no verificado (CU-01); sin una cuenta
/// de administrador preexistente, ninguna empresa podría llegar a operar. Dado
/// que dicha cuenta no se crea mediante el flujo de registro, por carecer de
/// empresa asociada, su aprovisionamiento automático constituye la única vía
/// posible.
///
/// Se ha descartado incorporarla como dato semilla de la migración, alternativa
/// inicialmente adoptada, porque habría exigido versionar la derivación de su
/// contraseña en el repositorio. Aunque una derivación BCrypt es irreversible,
/// su publicación permitiría someterla a un ataque de diccionario y, sobre
/// todo, constituiría una excepción al criterio establecido en el apartado 5.1,
/// según el cual ningún secreto figura en los artefactos versionados. La
/// contraseña se obtiene, por tanto, de la configuración del entorno, y su
/// derivación se calcula en el arranque.
///
/// La operación es idempotente: si la cuenta ya existe, no se modifica.
/// </summary>
public static class InicializadorDeDatos
{
    public const string ClaveEmail = "Administrador:Email";
    public const string ClavePassword = "Administrador:Password";

    private const int LongitudMinimaPassword = 12;

    public static async Task AprovisionarAdministradorAsync(
        MetalFlowDbContext contexto,
        IConfiguration configuracion,
        IServicioHash hash,
        ILogger logger,
        CancellationToken ct = default)
    {
        var email = configuracion[ClaveEmail]?.Trim().ToLowerInvariant();
        var password = configuracion[ClavePassword];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                $"No se han definido las credenciales del administrador ('{ClaveEmail}' y " +
                $"'{ClavePassword}'). Defínalas mediante los secretos de usuario en desarrollo " +
                "o como variables de entorno en el despliegue contenedorizado.");

        if (password.Length < LongitudMinimaPassword)
            throw new InvalidOperationException(
                $"La contraseña del administrador debe tener al menos {LongitudMinimaPassword} caracteres.");

        var yaExiste = await contexto.Usuarios.AnyAsync(u => u.Email == email, ct);

        if (yaExiste)
        {
            logger.LogInformation(
                "La cuenta de administrador ya se encuentra aprovisionada. No se realiza ninguna acción.");
            return;
        }

        var administrador = new Usuario(
            email,
            hash.Derivar(password),
            "Administrador del sistema",
            empresaId: null);          // Única cuenta sin empresa asociada

        administrador.AsignarRol(Rol.Administrador);

        await contexto.Usuarios.AddAsync(administrador, ct);
        await contexto.SaveChangesAsync(ct);

        // Se registra el hecho del aprovisionamiento, nunca la credencial.
        logger.LogInformation("Cuenta de administrador aprovisionada correctamente.");
    }
}
