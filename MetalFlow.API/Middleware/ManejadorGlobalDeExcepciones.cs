using System.Diagnostics;
using MetalFlow.Aplicacion.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MetalFlow.API.Middleware;

/// <summary>
/// Tratamiento homogéneo de los errores de la API, previsto como componente
/// transversal en el apartado 4.2.
///
/// Su incorporación responde a tres finalidades. En primer lugar, implementa
/// el control frente a la filtración de información técnica a través de los
/// errores (Tabla 23, OWASP A02 y A10): las respuestas no incorporan trazas ni
/// detalles de la implementación, que quedan registrados exclusivamente en el
/// servidor. En segundo lugar, concentra en un único punto una responsabilidad
/// que de otro modo habría de replicarse en cada controlador. Y en tercer
/// lugar, garantiza que el cliente reciba siempre una estructura de error
/// uniforme, condición necesaria para su tratamiento sistemático en la interfaz.
///
/// Las respuestas adoptan el formato normalizado de detalles de problema
/// definido por la RFC 7807, en lugar de una estructura propia, por tratarse
/// del estándar que el propio marco emplea para los errores de validación.
/// </summary>
public class ManejadorGlobalDeExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorGlobalDeExcepciones> _registro;
    private readonly IHostEnvironment _entorno;

    public ManejadorGlobalDeExcepciones(
        ILogger<ManejadorGlobalDeExcepciones> registro, IHostEnvironment entorno)
    {
        _registro = registro;
        _entorno = entorno;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        // Identificador de correlación: permite vincular la respuesta recibida
        // por el cliente con el detalle registrado en el servidor, sin exponer
        // este último.
        var identificador = Activity.Current?.Id ?? contexto.TraceIdentifier;

        var (codigo, titulo, detalle) = Traducir(excepcion);

        if (codigo == StatusCodes.Status500InternalServerError)
        {
            // Solo los fallos técnicos se registran como error: los
            // incumplimientos de regla de negocio son respuestas previstas del
            // sistema y no incidencias.
            _registro.LogError(excepcion,
                "Error no controlado al procesar {Metodo} {Ruta}. Correlación: {Identificador}",
                contexto.Request.Method, contexto.Request.Path, identificador);
        }
        else
        {
            _registro.LogInformation(
                "Solicitud rechazada con código {Codigo} en {Metodo} {Ruta}: {Motivo}",
                codigo, contexto.Request.Method, contexto.Request.Path, titulo);
        }

        var problema = new ProblemDetails
        {
            Status = codigo,
            Title = titulo,
            Detail = detalle,
            Instance = contexto.Request.Path
        };

        problema.Extensions["traceId"] = identificador;

        // En desarrollo se incorpora el tipo de excepción como ayuda a la
        // depuración. Nunca la traza, y nunca fuera de dicho entorno.
        if (_entorno.IsDevelopment() && codigo == StatusCodes.Status500InternalServerError)
            problema.Extensions["excepcion"] = excepcion.GetType().Name;

        contexto.Response.StatusCode = codigo;
        await contexto.Response.WriteAsJsonAsync(problema, ct);

        return true;
    }

    /// <summary>
    /// Traduce cada excepción al código de estado que le corresponde. La
    /// jerarquía de excepciones de la capa de aplicación permite distinguir
    /// los incumplimientos de regla de negocio de los fallos técnicos, que
    /// reciben un tratamiento deliberadamente opaco.
    /// </summary>
    private static (int codigo, string titulo, string detalle) Traducir(Exception excepcion)
        => excepcion switch
        {
            ExcepcionCredencialesInvalidas e => (
                StatusCodes.Status401Unauthorized,
                "Credenciales no válidas",
                e.Message),

            ExcepcionAccesoDenegado e => (
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                e.Message),

            ExcepcionNoEncontrado e => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                e.Message),

            ExcepcionConflicto e => (
                StatusCodes.Status409Conflict,
                "Conflicto con un registro existente",
                e.Message),

            ExcepcionDeNegocio e => (
                StatusCodes.Status400BadRequest,
                "Solicitud no válida",
                e.Message),

            // Los fallos técnicos no revelan su naturaleza al cliente. El
            // detalle queda únicamente en el registro del servidor.
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Se ha producido un error al procesar la solicitud. " +
                "Si el problema persiste, comunique el identificador de correlación.")
        };
}
