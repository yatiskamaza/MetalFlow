using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Extremos de registro y autenticación (CU-01 y CU-02), conforme al contrato
/// recogido en la Tabla 19. Son los dos únicos extremos de acceso público del
/// sistema; el resto exige un token válido.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ServicioIdentidad _identidad;

    public AuthController(ServicioIdentidad identidad) => _identidad = identidad;

    /// <summary>
    /// CU-01. Da de alta una empresa y su cuenta de acceso. La empresa queda en
    /// estado no verificado hasta que el administrador complete CU-10.
    /// </summary>
    [HttpPost("registro")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistroEmpresaDto datos, CancellationToken ct)
    {
        try
        {
            var resultado = await _identidad.RegistrarEmpresaAsync(datos, ct);
            return StatusCode(StatusCodes.Status201Created, resultado);
        }
        catch (ExcepcionConflicto ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (ExcepcionDeNegocio ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// CU-02. Valida las credenciales y emite el token de acceso.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Autenticar(
        [FromBody] LoginDto credenciales, CancellationToken ct)
    {
        try
        {
            var resultado = await _identidad.AutenticarAsync(credenciales, ct);
            return Ok(resultado);
        }
        catch (ExcepcionCredencialesInvalidas ex)
        {
            // El criterio de aceptación de RF-01 exige responder con 401 ante
            // credenciales incorrectas, con un mensaje que no permita deducir
            // si la cuenta existe (Tabla 23, OWASP A07).
            return Unauthorized(new { mensaje = ex.Message });
        }
    }
}
