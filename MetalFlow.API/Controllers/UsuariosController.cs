using MetalFlow.API.Extensiones;
using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Gestión de cuentas y supervisión de reservas por el administrador (CU-11),
/// conforme al contrato recogido en la Tabla 19.
///
/// Todos los extremos quedan reservados al rol de administrador y, como los de
/// verificación de empresas, no comprueban pertenencia: la legitimación del
/// administrador deriva exclusivamente del rol (apartado 4.7).
/// </summary>
[ApiController]
[Route("api/v1/usuarios")]
[Produces("application/json")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly ServicioAdministracion _administracion;

    public UsuariosController(ServicioAdministracion administracion)
        => _administracion = administracion;

    /// <summary>Relación de cuentas registradas, con su empresa y sus roles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CuentaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await _administracion.ListarCuentasAsync(ct));

    /// <summary>
    /// Inhabilita o rehabilita una cuenta. La inhabilitación retira del
    /// catálogo el material disponible de la empresa y mantiene las reservas
    /// en curso; la rehabilitación restituye el acceso, pero no la
    /// disponibilidad del material retirado.
    /// </summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(ResultadoCambioEstadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(
        Guid id, [FromBody] CambioEstadoCuentaDto datos, CancellationToken ct)
    {
        // El identificador del administrador se obtiene del token para impedir
        // que desactive su propia cuenta (CU-11, excepción E3).
        var administradorId = User.ObtenerUsuarioId()
            ?? throw new ExcepcionAccesoDenegado();

        var resultado = await _administracion.CambiarEstadoCuentaAsync(
            id, datos.Activo!.Value, administradorId, ct);

        return Ok(resultado);
    }

    /// <summary>
    /// Totalidad de las reservas del sistema, para la supervisión de la
    /// actividad de la plataforma. No incorpora los datos de contacto de las
    /// partes.
    /// </summary>
    [HttpGet("~/api/v1/supervision/reservas")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaSupervisionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SupervisarReservas(CancellationToken ct)
        => Ok(await _administracion.ListarReservasAsync(ct));
}
