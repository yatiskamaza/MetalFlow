using MetalFlow.API.Extensiones;
using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Gestión manual del inventario (CU-04), conforme al contrato recogido en la
/// Tabla 19. Materializa el requisito RF-03.
///
/// Todos los extremos exigen token válido y rol de proveedor. La comprobación
/// del rol no agota, sin embargo, el control de acceso: el servicio verifica
/// además que cada recurso pertenece a la empresa declarada en el token,
/// conforme a la autorización en dos niveles del apartado 4.7.
/// </summary>
[ApiController]
[Route("api/v1/items")]
[Produces("application/json")]
[Authorize(Roles = "Proveedor")]
public class InventarioController : ControllerBase
{
    private readonly ServicioInventario _inventario;

    public InventarioController(ServicioInventario inventario) => _inventario = inventario;

    /// <summary>Relación del inventario de la empresa solicitante.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ItemInventarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var items = await _inventario.ListarDeLaEmpresaAsync(User.ObtenerEmpresaId(), ct);
        return Ok(items);
    }

    /// <summary>Detalle de un ítem propio.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ItemInventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var item = await _inventario.ObtenerDeLaEmpresaAsync(id, User.ObtenerEmpresaId(), ct);
        return Ok(item);
    }

    /// <summary>
    /// Alta manual de un ítem. Constituye la vía alternativa a la ingesta
    /// masiva, prevista para las empresas que operan con registros puntuales.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ItemInventarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearItemInventarioDto datos, CancellationToken ct)
    {
        var item = await _inventario.CrearAsync(User.ObtenerEmpresaId(), datos, ct);
        return CreatedAtAction(nameof(Obtener), new { id = item.Id }, item);
    }

    /// <summary>
    /// Modificación de un ítem propio. Se rechaza si el ítem no se encuentra
    /// disponible, por cuanto alterar las características de una pieza
    /// reservada induciría a error a la empresa compradora.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ItemInventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        Guid id, [FromBody] ActualizarItemInventarioDto datos, CancellationToken ct)
    {
        var item = await _inventario.ActualizarAsync(id, User.ObtenerEmpresaId(), datos, ct);
        return Ok(item);
    }

    /// <summary>
    /// Baja de un ítem propio, practicada como baja lógica para preservar la
    /// trazabilidad y el histórico de reservas (RD-04).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retirar(Guid id, CancellationToken ct)
    {
        await _inventario.RetirarAsync(id, User.ObtenerEmpresaId(), ct);
        return NoContent();
    }
}
