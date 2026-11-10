using MetalFlow.Aplicacion.Servicios;

namespace MetalFlow.API.Tareas;

/// <summary>
/// Desencadena con periodicidad la expiración de las reservas vencidas.
///
/// Su incorporación resuelve una deficiencia advertida al implementar el
/// flujo de reserva. La vigencia de setenta y dos horas carecía de mecanismo
/// que la hiciera efectiva: un ítem cuya reserva hubiera vencido permanecía
/// bloqueado, y al no figurar los ítems reservados en el catálogo, ninguna
/// empresa llegaría a solicitarlo, de modo que el material quedaba
/// indefinidamente fuera de circulación.
///
/// El proceso no alberga lógica de negocio alguna: su cometido se limita a
/// invocar con la periodicidad establecida la operación definida en la capa
/// de aplicación.
/// </summary>
public class ProcesoDeExpiracionDeReservas : BackgroundService
{
    private const int IntervaloPredeterminadoEnMinutos = 15;

    private readonly IServiceScopeFactory _ambitos;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<ProcesoDeExpiracionDeReservas> _registro;

    public ProcesoDeExpiracionDeReservas(
        IServiceScopeFactory ambitos,
        IConfiguration configuracion,
        ILogger<ProcesoDeExpiracionDeReservas> registro)
    {
        _ambitos = ambitos;
        _configuracion = configuracion;
        _registro = registro;
    }

    protected override async Task ExecuteAsync(CancellationToken tokenDeParada)
    {
        var minutos = ObtenerIntervaloEnMinutos();
        var intervalo = TimeSpan.FromMinutes(minutos);

        _registro.LogInformation(
            "Proceso de expiración de reservas iniciado, con intervalo de {Minutos} minutos.",
            minutos);

        using var temporizador = new PeriodicTimer(intervalo);

        do
        {
            await EjecutarCicloAsync(tokenDeParada);
        }
        while (await EsperarSiguienteCicloAsync(temporizador, tokenDeParada));

        _registro.LogInformation("Proceso de expiración de reservas detenido.");
    }

    /// <summary>
    /// Ejecuta un ciclo completo dentro de un ámbito propio.
    ///
    /// El ámbito resulta imprescindible: los servicios de acceso a datos se
    /// registran con ciclo de vida de petición, y este proceso se ejecuta al
    /// margen de petición alguna. Resolverlos directamente desde el
    /// contenedor produciría un error en tiempo de ejecución.
    ///
    /// La captura de excepciones es igualmente necesaria. Una excepción no
    /// controlada en un servicio alojado interrumpe su ejecución de forma
    /// definitiva, sin reinicio ni aviso, de modo que las reservas dejarían
    /// de expirar hasta que la aplicación volviera a arrancar. Se registra el
    /// fallo y el proceso continúa con el ciclo siguiente.
    /// </summary>
    private async Task EjecutarCicloAsync(CancellationToken ct)
    {
        try
        {
            await using var ambito = _ambitos.CreateAsyncScope();

            var expiracion = ambito.ServiceProvider
                .GetRequiredService<ServicioExpiracionReservas>();

            await expiracion.ExpirarVencidasAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Detención ordenada de la aplicación: no constituye incidencia.
        }
        catch (Exception excepcion)
        {
            _registro.LogError(excepcion,
                "Error durante el ciclo de expiración de reservas. " +
                "El proceso continuará con el ciclo siguiente.");
        }
    }

    private static async Task<bool> EsperarSiguienteCicloAsync(
        PeriodicTimer temporizador, CancellationToken ct)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// El intervalo se obtiene de la configuración y no se fija en el código,
    /// de modo que pueda reducirse para la verificación sin recompilar la
    /// aplicación.
    /// </summary>
    private int ObtenerIntervaloEnMinutos()
    {
        var configurado = _configuracion.GetValue<int?>("Reservas:IntervaloExpiracionMinutos");

        return configurado is > 0 ? configurado.Value : IntervaloPredeterminadoEnMinutos;
    }
}
