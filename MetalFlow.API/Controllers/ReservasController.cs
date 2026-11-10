using MetalFlow.API.Extensiones;
using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Flujo de reserva de material (CU-09), conforme al contrato recogido en la
/// Tabla 19.
///
/// Los extremos se reparten entre ambos roles, por cuanto la reserva es una
/// relación entre dos empresas: la compradora formaliza y consulta las suyas;
/// la proveedora consulta las recibidas sobre su material y deja constancia de
/// que la operación se perfeccionó. La cancelación corresponde a cualquiera de
/// las dos.
/// </summary>
[ApiController]
[Route("api/v1/reservas")]
[Produces("application/json")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly ServicioReservas _reservas;

    public ReservasController(ServicioReservas reservas) => _reservas = reservas;

    /// <summary>
    /// CU-09. Formaliza la reserva de un material disponible. La respuesta
    /// incorpora los datos de contacto de ambas partes, cuya revelación
    /// constituye el objeto de la operación (RD-06).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Comprador")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reservar(
        [FromBody] CrearReservaDto datos, CancellationToken ct)
    {
        var reserva = await _reservas.ReservarAsync(
            datos.ItemId, User.ObtenerEmpresaId(), ct);

        return StatusCode(StatusCodes.Status201Created, reserva);
    }

    /// <summary>Reservas formalizadas por la empresa solicitante.</summary>
    [HttpGet]
    [Authorize(Roles = "Comprador")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPropias(CancellationToken ct)
        => Ok(await _reservas.ListarPropiasAsync(User.ObtenerEmpresaId(), ct));

    /// <summary>Reservas recibidas sobre el material de la empresa solicitante.</summary>
    [HttpGet("recibidas")]
    [Authorize(Roles = "Proveedor")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarRecibidas(CancellationToken ct)
        => Ok(await _reservas.ListarRecibidasAsync(User.ObtenerEmpresaId(), ct));

    /// <summary>
    /// Cancela una reserva activa y devuelve el material al catálogo. Resulta
    /// accesible a ambas partes de la operación.
    /// </summary>
    [HttpPatch("{id:guid}/cancelacion")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
        => Ok(await _reservas.CancelarAsync(id, User.ObtenerEmpresaId(), ct));

    /// <summary>
    /// Deja constancia de que la operación se perfeccionó entre las partes.
    /// Corresponde a la empresa proveedora, que es quien entrega el material.
    /// </summary>
    [HttpPatch("{id:guid}/confirmacion")]
    [Authorize(Roles = "Proveedor")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken ct)
        => Ok(await _reservas.ConfirmarAsync(id, User.ObtenerEmpresaId(), ct));
}