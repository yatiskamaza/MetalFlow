using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Controllers;

/// <summary>
/// Administración de las empresas registradas (CU-10), conforme al contrato
/// recogido en la Tabla 19.
///
/// Todos los extremos quedan reservados al rol de administrador. A diferencia
/// de los controladores de empresa, estos no extraen identificador de empresa
/// alguno del token ni comprueban pertenencia: la cuenta del administrador
/// carece de empresa asociada y su legitimación deriva únicamente del rol
/// </summary>
[ApiController]
[Route("api/v1/empresas")]
[Produces("application/json")]
[Authorize(Roles = "Administrador")]
public class EmpresasController : ControllerBase
{
    private readonly ServicioAdministracion _administracion;

    public EmpresasController(ServicioAdministracion administracion)
        => _administracion = administracion;

    /// <summary>
    /// Relación de empresas registradas. El parámetro de consulta permite
    /// restringirla a las pendientes de verificación, que constituyen la cola
    /// de trabajo ordinaria del administrador.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmpresaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool soloPendientes, CancellationToken ct)
        => Ok(await _administracion.ListarEmpresasAsync(soloPendientes, ct));

    /// <summary>Detalle de una empresa registrada.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmpresaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
        => Ok(await _administracion.ObtenerEmpresaAsync(id, ct));

    /// <summary>
    /// CU-10. Habilita a la empresa para operar en la plataforma, una vez
    /// contrastada su condición profesional (RD-02).
    ///
    /// Se emplea el método PATCH por tratarse de la modificación de un
    /// atributo concreto del recurso y no de su sustitución íntegra. El
    /// cuerpo de la petición resulta innecesario: la operación no admite más
    /// resultado que la habilitación.
    /// </summary>
    [HttpPatch("{id:guid}/verificacion")]
    [ProducesResponseType(typeof(VerificacionEmpresaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verificar(Guid id, CancellationToken ct)
        => Ok(await _administracion.VerificarEmpresaAsync(id, ct));
}
