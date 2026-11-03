using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Extremos de registro y autenticación (CU-01 y CU-02), conforme al contrato
/// recogido en la Tabla 19. Son los dos únicos extremos de acceso público del
/// sistema; el resto exige un token válido.
///
/// Los controladores no capturan excepciones: su traducción a códigos de
/// estado corresponde al manejador global descrito en el apartado 5.2, lo que
/// evita replicar dicha responsabilidad en cada extremo.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ServicioIdentidad _identidad;

    public AuthController(ServicioIdentidad identidad) => _identidad = identidad;

    /// <summary>
    /// CU-01. Da de alta una empresa y su cuenta de acceso. La empresa queda en
    /// estado no verificado hasta que el administrador complete CU-10.
    /// </summary>
    [HttpPost("registro")]
    [ProducesResponseType(typeof(RegistroCreadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistroEmpresaDto datos, CancellationToken ct)
    {
        var resultado = await _identidad.RegistrarEmpresaAsync(datos, ct);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>
    /// CU-02. Valida las credenciales y emite el token de acceso. Un fallo de
    /// autenticación produce un código 401 con un mensaje que no permite
    /// distinguir si la cuenta existe (Tabla 23, OWASP A07).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(RespuestaAutenticacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Autenticar(
        [FromBody] LoginDto credenciales, CancellationToken ct)
    {
        var resultado = await _identidad.AutenticarAsync(credenciales, ct);
        return Ok(resultado);
    }
}