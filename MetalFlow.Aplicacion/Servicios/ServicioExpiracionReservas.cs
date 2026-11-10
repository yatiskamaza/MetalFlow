using MetalFlow.Dominio.Repositorios;
using Microsoft.Extensions.Logging;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Practica la expiración de las reservas cuya vigencia ha transcurrido.
///
/// La lógica reside en la capa de aplicación y no en el proceso que la
/// desencadena, por dos motivos. El primero es de arquitectura: el proceso en
/// segundo plano pertenece a la infraestructura de alojamiento y su cometido
/// se agota en invocar la operación con la periodicidad establecida. El
/// segundo es de verificabilidad: situada aquí, la operación puede someterse a
/// prueba sin necesidad de poner en marcha un servicio alojado ni de esperar a
/// que transcurra intervalo alguno.
///
/// El servicio se limita a orquestar: recupera las reservas vencidas, invoca
/// sobre cada una la transición que el dominio define y confirma los cambios.
/// No modifica estado alguno por sí mismo.
/// </summary>
public class ServicioExpiracionReservas
{
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IRepositorioReservas _reservas;
    private readonly ILogger<ServicioExpiracionReservas> _registro;

    public ServicioExpiracionReservas(
        IUnitOfWork unidadDeTrabajo,
        IRepositorioReservas reservas,
        ILogger<ServicioExpiracionReservas> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _reservas = reservas;
        _registro = registro;
    }

    /// <summary>
    /// Expira las reservas vencidas y devuelve su material al catálogo.
    ///
    /// La totalidad de las expiraciones se confirma en una única operación de
    /// persistencia, de modo que resulte atómica: o bien se practican todas, o
    /// bien ninguna. Una confirmación por reserva dejaría el sistema en estado
    /// intermedio si el proceso se interrumpiera a mitad de la ejecución.
    /// </summary>
    /// <returns>Número de reservas expiradas.</returns>
    public async Task<int> ExpirarVencidasAsync(CancellationToken ct = default)
    {
        var vencidas = await _reservas.ListarVencidasAsync(ct);

        if (vencidas.Count == 0) return 0;

        var expiradas = 0;

        foreach (var reserva in vencidas)
        {
            // La transición del ítem es inseparable de la de la reserva, y el
            // dominio la practica de forma conjunta. El ítem se obtiene de la
            // relación, que la consulta ha cargado expresamente.
            try
            {
                reserva.Expirar(reserva.Item);
                expiradas++;
            }
            catch (InvalidOperationException ex)
            {
                // Una reserva cuyo ítem no se encuentra en el estado esperado
                // revela una inconsistencia previa. Se deja constancia y se
                // continúa con las restantes, antes que interrumpir el ciclo
                // completo por un registro anómalo.
                _registro.LogWarning(ex,
                    "No ha podido expirarse la reserva {ReservaId}: el material " +
                    "no se encuentra en el estado esperado.", reserva.Id);
            }
        }

        if (expiradas == 0) return 0;

        await _unidadDeTrabajo.ConfirmarAsync(ct);

        _registro.LogInformation(
            "Expiradas {Expiradas} reservas vencidas; su material vuelve al catálogo.",
            expiradas);

        return expiradas;
    }
}
