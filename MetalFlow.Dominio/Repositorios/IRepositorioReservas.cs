using MetalFlow.Dominio.Entidades;

namespace MetalFlow.Dominio.Repositorios;

/// <summary>
/// Repositorio especializado en las operaciones de reserva.
///
/// Su existencia responde a necesidades que el repositorio genérico no cubre.
/// La principal es la recuperación de un ítem con bloqueo exclusivo, pieza
/// central del control de concurrencia descrito en el apartado 4.4.4: el marco
/// de persistencia no ofrece abstracción alguna para expresar indicadores de
/// bloqueo, de modo que dicha consulta ha de formularse en el lenguaje del
/// gestor. Se añaden las consultas que requieren cargar relaciones concretas.
/// </summary>
public interface IRepositorioReservas
{
    /// <summary>
    /// Recupera un ítem bloqueándolo frente a otras transacciones hasta la
    /// conclusión de la presente.
    ///
    /// Esta es la operación que impide la doble reserva: una segunda petición
    /// concurrente sobre el mismo ítem queda en espera hasta que la primera
    /// confirma o revierte, momento en el cual observará el estado ya
    /// actualizado. Requiere, por tanto, una transacción abierta.
    /// </summary>
    Task<ItemInventario?> ObtenerItemConBloqueoAsync(
        Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Reservas pendientes cuya vigencia ha transcurrido.
    ///
    /// Se cargan junto con el ítem reservado y con la cuenta de su empresa
    /// propietaria: la transición de la reserva determina la del material, y
    /// el destino de este depende de si dicha empresa se encuentra habilitada.
    /// Se recuperan con seguimiento de cambios, al ser objeto de modificación.
    /// </summary>
    Task<IReadOnlyList<Reserva>> ListarVencidasAsync(CancellationToken ct = default);

    /// <summary>
    /// Recupera la reserva activa que recae sobre un ítem, si existe.
    /// </summary>
    Task<Reserva?> ObtenerReservaActivaAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Indica si el ítem fue objeto de una reserva confirmada, esto es, si fue
    /// transmitido. Permite distinguir el material vendido del retirado por su
    /// propietario, supuestos que el modelo representa con un mismo estado.
    /// </summary>
    Task<bool> FueTransmitidoAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Totalidad de las reservas del sistema, para la supervisión del
    /// administrador (CU-11).
    /// </summary>
    Task<IReadOnlyList<Reserva>> ListarTodasAsync(CancellationToken ct = default);

    /// <summary>Reservas formalizadas por una empresa compradora.</summary>
    Task<IReadOnlyList<Reserva>> ListarDeCompradoraAsync(
        Guid empresaId, CancellationToken ct = default);

    /// <summary>Reservas recibidas sobre el material de una empresa proveedora.</summary>
    Task<IReadOnlyList<Reserva>> ListarDeProveedoraAsync(
        Guid empresaId, CancellationToken ct = default);

    /// <summary>
    /// Recupera una reserva concreta con sus relaciones cargadas y con
    /// seguimiento de cambios, para las operaciones que la modifican. Incluye
    /// la cuenta de la empresa propietaria del material, cuya situación
    /// determina el destino de este al cancelarse la reserva.
    /// </summary>
    Task<Reserva?> ObtenerConRelacionesAsync(Guid reservaId, CancellationToken ct = default);
}