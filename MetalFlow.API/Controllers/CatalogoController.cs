using MetalFlow.API.Extensiones;
using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Consulta del catálogo por parte de la empresa compradora (CU-07 y CU-08),
/// conforme al contrato recogido en la Tabla 19.
///
/// Conviene distinguir estos extremos de los de inventario. Aquellos operan
/// sobre el material propio de la empresa proveedora y quedan sujetos a la
/// autorización por pertenencia; los presentes exponen el material publicado
/// por terceros, con un conjunto de datos distinto que excluye toda
/// información de contacto hasta la confirmación de una reserva.
/// </summary>
[ApiController]
[Route("api/v1/catalogo")]
[Produces("application/json")]
[Authorize(Roles = "Comprador")]
public class CatalogoController : ControllerBase
{
    private readonly MotorBusqueda _busqueda;

    public CatalogoController(MotorBusqueda busqueda) => _busqueda = busqueda;

    /// <summary>
    /// CU-07. Búsqueda paramétrica del catálogo mediante filtros compuestos
    /// de material y dimensiones geométricas.
    ///
    /// Los criterios se transmiten como parámetros de consulta y no en el
    /// cuerpo de la petición, por tratarse de una operación de lectura: de
    /// este modo el resultado es enlazable y la petición resulta idempotente,
    /// conforme a la semántica del método empleado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoBusquedaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Buscar(
        [FromQuery] BusquedaCatalogoDto filtros, CancellationToken ct)
    {
        var resultado = await _busqueda.BuscarAsync(filtros, User.ObtenerEmpresaId(), ct);
        return Ok(resultado);
    }

    /// <summary>
    /// CU-08. Detalle de un material publicado. No incorpora los datos de
    /// contacto de la empresa proveedora, que se revelan únicamente tras la
    /// confirmación de una reserva (RD-06).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ItemCatalogoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(Guid id, CancellationToken ct)
    {
        var item = await _busqueda.ObtenerDetalleAsync(id, User.ObtenerEmpresaId(), ct);
        return Ok(item);
    }
}
