using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MetalFlow.Aplicacion.Abstracciones;
using MetalFlow.Dominio.Entidades;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MetalFlow.Infraestructura.ServiciosTecnicos;

/// <summary>
/// Emisión de tokens JSON Web Token (RF-01, CT-01).
///
/// El token incorpora como declaraciones el identificador de la cuenta, el de
/// su empresa —ausente en la cuenta de administrador— y el conjunto de roles
/// asignados. Estas declaraciones son las que sustentan la autorización en dos
/// niveles descrita en el apartado 4.7: por rol y por pertenencia a la empresa
/// propietaria del recurso.
/// </summary>
public class ProveedorJWT : IProveedorTokens
{
    public const string ClaimEmpresa = "empresa_id";

    private readonly string _clave;
    private readonly string _emisor;
    private readonly string _destinatario;
    private readonly int _minutosDeVigencia;

    public ProveedorJWT(IConfiguration configuracion)
    {
        _clave = configuracion["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "No se ha definido la clave de firma 'Jwt:Secret'. Configúrela mediante " +
                "los secretos de usuario o la variable de entorno Jwt__Secret.");

        if (Encoding.UTF8.GetByteCount(_clave) < 32)
            throw new InvalidOperationException(
                "La clave de firma debe tener una longitud mínima de 32 bytes.");

        _emisor = configuracion["Jwt:Issuer"] ?? "MetalFlow";
        _destinatario = configuracion["Jwt:Audience"] ?? "MetalFlowClientes";
        _minutosDeVigencia = int.TryParse(configuracion["Jwt:ExpiracionMinutos"], out var m) ? m : 120;
    }

    public (string token, DateTime expiracion) Emitir(Usuario usuario)
    {
        var expiracion = DateTime.UtcNow.AddMinutes(_minutosDeVigencia);

        var declaraciones = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // La cuenta de administrador carece de empresa asociada, por lo que la
        // declaración correspondiente solo se incorpora cuando procede.
        if (usuario.EmpresaId is not null)
            declaraciones.Add(new Claim(ClaimEmpresa, usuario.EmpresaId.Value.ToString()));

        foreach (var asignacion in usuario.Roles)
            declaraciones.Add(new Claim(ClaimTypes.Role, asignacion.Rol.ToString()));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _emisor,
            audience: _destinatario,
            claims: declaraciones,
            expires: expiracion,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
