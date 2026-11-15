using MetalFlow.API.Extensiones;
using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Publicación de anuncios comerciales sobre el material propio (CU-06),
/// conforme al contrato recogido en la Tabla 19.
///
/// Todos los extremos quedan reservados al rol de proveedor y sujetos a la
/// autorización por pertenencia: una empresa solo puede anunciar su propio
/// material. La consulta de los anuncios por parte de las empresas
/// compradoras no requiere extremos propios, por cuanto su información
/// comercial se incorpora a los resultados del catálogo.
/// </summary>
[ApiController]
[Route("api/v1/anuncios")]
[Produces("application/json")]
[Authorize(Roles = "Proveedor")]
public class AnunciosController : ControllerBase
{
    private readonly ServicioAnuncios _anuncios;

    public AnunciosController(ServicioAnuncios anuncios) => _anuncios = anuncios;

    /// <summary>Anuncios de la empresa solicitante, activos y despublicados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AnuncioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPropios(CancellationToken ct)
        => Ok(await _anuncios.ListarPropiosAsync(User.ObtenerEmpresaId(), ct));

    /// <summary>CU-06. Publica un anuncio sobre un ítem propio y disponible.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AnuncioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publicar(
        [FromBody] PublicarAnuncioDto datos, CancellationToken ct)
    {
        var anuncio = await _anuncios.PublicarAsync(datos, User.ObtenerEmpresaId(), ct);
        return StatusCode(StatusCodes.Status201Created, anuncio);
    }

    /// <summary>
    /// Modifica la información comercial de un anuncio propio y lo publica de
    /// nuevo si estaba despublicado.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AnuncioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        Guid id, [FromBody] ActualizarAnuncioDto datos, CancellationToken ct)
        => Ok(await _anuncios.ActualizarAsync(id, datos, User.ObtenerEmpresaId(), ct));

    /// <summary>
    /// Retira el anuncio del catálogo sin eliminarlo. El material permanece
    /// disponible y sigue figurando en las búsquedas, sin información
    /// comercial.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Despublicar(Guid id, CancellationToken ct)
    {
        await _anuncios.DespublicarAsync(id, User.ObtenerEmpresaId(), ct);
        return NoContent();
    }
}
