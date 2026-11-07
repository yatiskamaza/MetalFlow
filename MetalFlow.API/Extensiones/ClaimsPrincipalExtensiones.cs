using System.Security.Claims;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Infraestructura.ServiciosTecnicos;

namespace MetalFlow.API.Extensiones;

/// <summary>
/// Lectura de las declaraciones del token de acceso.
///
/// Concentra en un único punto la extracción del identificador de empresa, que
/// constituye el fundamento de la autorización por pertenencia descrita en el
/// apartado 4.7. Obtenerlo de forma dispersa en cada controlador multiplicaría
/// las ocasiones de omitir la comprobación.
/// </summary>
public static class ClaimsPrincipalExtensiones
{
    /// <summary>
    /// Devuelve la empresa declarada en el token.
    ///
    /// Su ausencia solo es legítima en la cuenta del administrador, que no
    /// pertenece a empresa alguna; invocar este método desde un extremo
    /// reservado a empresas constituye, por tanto, un acceso no autorizado.
    /// </summary>
    public static Guid ObtenerEmpresaId(this ClaimsPrincipal solicitante)
    {
        var valor = solicitante.FindFirstValue(ProveedorJWT.ClaimEmpresa);

        if (string.IsNullOrWhiteSpace(valor) || !Guid.TryParse(valor, out var empresaId))
            throw new ExcepcionAccesoDenegado();

        return empresaId;
    }

    public static Guid? ObtenerUsuarioId(this ClaimsPrincipal solicitante)
    {
        var valor = solicitante.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? solicitante.FindFirstValue("sub");

        return Guid.TryParse(valor, out var usuarioId) ? usuarioId : null;
    }
}