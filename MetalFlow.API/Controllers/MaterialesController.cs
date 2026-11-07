using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Consulta del catálogo normalizado de aleaciones (RD-03).
///
/// Su finalidad es permitir que la interfaz componga la selección de material
/// a partir de valores normalizados, de modo que la designación no se
/// introduzca como texto libre. Es accesible a cualquier cuenta autenticada,
/// por tratarse de información de referencia y no de datos de empresa alguna.
/// </summary>
[ApiController]
[Route("api/v1/materiales")]
[Produces("application/json")]
[Authorize]
public class MaterialesController : ControllerBase
{
    private readonly ServicioInventario _inventario;

    public MaterialesController(ServicioInventario inventario) => _inventario = inventario;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await _inventario.ListarMaterialesAsync(ct));
}