using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta el caso de uso de reserva de material (CU-09), que materializa el
/// requisito RF-05.
///
/// Constituye el punto del sistema con mayor exposición a condiciones de
/// carrera: dos empresas compradoras pueden solicitar simultáneamente la misma
/// pieza, y la unicidad del material hace que solo una pueda obtenerla. El
/// tratamiento de dicha concurrencia se describe en el método correspondiente.
///
/// Conviene subrayar el alcance de la operación. Conforme a RD-06, la
/// plataforma actúa como intermediario de emparejamiento y bloqueo: no procesa
/// pago alguno ni formaliza contrato. La reserva bloquea el material durante
/// un plazo determinado y habilita el contacto directo entre las partes, a
/// quienes corresponde perfeccionar la operación.
/// </summary>
public class ServicioReservas
{
    private const string AvisoAlcance =
        "La reserva bloquea el material durante 72 horas y habilita el contacto " +
        "entre las empresas. MetalFlow no procesa pagos ni interviene en la " +
        "formalización de la operación, que corresponde a las partes.";

    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IRepositorioReservas _reservas;

    public ServicioReservas(IUnitOfWork unidadDeTrabajo, IRepositorioReservas reservas)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _reservas = reservas;
    }

    /// <summary>
    /// CU-09. Formaliza la reserva de un ítem disponible.
    ///
    /// CONTROL DE CONCURRENCIA
    ///
    /// La operación se ejecuta íntegramente dentro de una transacción que
    /// comienza recuperando el ítem con bloqueo exclusivo de fila. A partir de
    /// ese instante, cualquier otra transacción que pretenda reservar el mismo
    /// ítem queda en espera hasta que la presente confirma o revierte, momento
    /// en el cual observará el estado ya actualizado y será rechazada.
    ///
    /// El bloqueo ha de preceder a la comprobación del estado: leer primero y
    /// bloquear después dejaría abierta la ventana entre ambas operaciones, que
    /// es precisamente donde se produciría la doble reserva. Este es el motivo
    /// por el que la consulta se formula con un indicador de bloqueo en lugar
    /// de recuperar la entidad por los medios ordinarios.
    ///
    /// Se ha adoptado un bloqueo pesimista, conforme a lo previsto en el
    /// apartado 4.4.4. La alternativa optimista —control por versión de fila
    /// con reintento ante conflicto— resultaría preferible en un escenario de
    /// concurrencia elevada, por no inmovilizar recursos durante la
    /// transacción; se descartó por cuanto el volumen de operaciones simultáneas
    /// previsto en el ámbito territorial del prototipo no lo justifica y
    /// exigiría incorporar al modelo un atributo de versión.
    /// </summary>
    public async Task<ReservaDto> ReservarAsync(
        Guid itemId, Guid empresaCompradoraId, CancellationToken ct = default)
    {
        await ComprobarEmpresaVerificadaAsync(empresaCompradoraId, ct);

        await _unidadDeTrabajo.IniciarTransaccionAsync(ct);

        try
        {
            // El bloqueo se adquiere aquí y se mantiene hasta el final de la
            // transacción.
            var item = await _reservas.ObtenerItemConBloqueoAsync(itemId, ct)
                ?? throw new ExcepcionNoEncontrado("El material solicitado no existe.");

            // CU-09, excepción E3: una empresa no adquiere su propio material.
            if (item.PerteneceA(empresaCompradoraId))
                throw new ExcepcionDeNegocio(
                    "No es posible reservar material de la propia empresa.");

            if (item.Estado == EstadoItem.Retirado)
                throw new ExcepcionNoEncontrado(
                    "El material ha sido retirado del catálogo.");

            // Liberación diferida de las reservas vencidas. El sistema no
            // dispone de un proceso que las caduque de forma automática, por lo
            // que la comprobación se practica en el momento en que su vigencia
            // resulta relevante: cuando otra empresa pretende el mismo material.
            if (item.Estado == EstadoItem.Reservado)
                await LiberarSiHaVencidoAsync(item, ct);

            // CU-09, excepción E1. Si el ítem continúa reservado, o bien otra
            // transacción se adelantó, o bien su reserva sigue vigente.
            if (item.Estado != EstadoItem.Disponible)
                throw new ExcepcionConflicto(
                    "El material ya no se encuentra disponible: ha sido reservado " +
                    "por otra empresa.");

            item.Reservar();
            var reserva = new Reserva(item.Id, empresaCompradoraId);

            _unidadDeTrabajo.Items.Actualizar(item);
            await _unidadDeTrabajo.Reservas.AgregarAsync(reserva, ct);

            await _unidadDeTrabajo.ConfirmarAsync(ct);

            // Se recupera con sus relaciones para componer la respuesta, que
            // incorpora los datos de contacto de ambas partes.
            var completa = await _reservas.ObtenerConRelacionesAsync(reserva.Id, ct);
            return Proyectar(completa!);
        }
        catch
        {
            // La reversión libera el bloqueo y deshace tanto el cambio de
            // estado del ítem como la creación de la reserva, de modo que no
            // pueda quedar un ítem reservado sin su reserva correspondiente.
            await _unidadDeTrabajo.RevertirAsync(ct);
            throw;
        }
    }

    /// <summary>Reservas formalizadas por la empresa compradora.</summary>
    public async Task<IReadOnlyList<ReservaDto>> ListarPropiasAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var reservas = await _reservas.ListarDeCompradoraAsync(empresaId, ct);
        return reservas.Select(Proyectar).ToList();
    }

    /// <summary>
    /// Reservas recibidas sobre el material de la empresa proveedora. El
    /// proveedor ha de conocer quién ha bloqueado su material y poder
    /// contactar con la empresa interesada.
    /// </summary>
    public async Task<IReadOnlyList<ReservaDto>> ListarRecibidasAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var reservas = await _reservas.ListarDeProveedoraAsync(empresaId, ct);
        return reservas.Select(Proyectar).ToList();
    }

    /// <summary>
    /// Cancela una reserva y devuelve el material al catálogo.
    ///
    /// Se legitima tanto a la empresa compradora, que puede desistir, como a la
    /// proveedora, que puede haber dispuesto del material entretanto. La
    /// operación es irreversible: una vez liberado, el material queda a
    /// disposición de cualquier otra empresa.
    /// </summary>
    public async Task<ReservaDto> CancelarAsync(
        Guid reservaId, Guid empresaSolicitanteId, CancellationToken ct = default)
    {
        await _unidadDeTrabajo.IniciarTransaccionAsync(ct);

        try
        {
            var reserva = await _reservas.ObtenerConRelacionesAsync(reservaId, ct)
                ?? throw new ExcepcionNoEncontrado("La reserva solicitada no existe.");

            var esCompradora = reserva.EmpresaCompradoraId == empresaSolicitanteId;
            var esProveedora = reserva.Item.EmpresaId == empresaSolicitanteId;

            if (!esCompradora && !esProveedora)
                throw new ExcepcionAccesoDenegado();

            if (!reserva.EstaActiva())
                throw new ExcepcionConflicto(
                    "La reserva no se encuentra activa y no puede cancelarse.");

            // La transición del ítem es inseparable de la de la reserva: el
            // dominio practica ambas de forma conjunta, de modo que no pueda
            // quedar una sin la otra.
            try
            {
                reserva.Cancelar(reserva.Item);
            }
            catch (InvalidOperationException ex)
            {
                throw new ExcepcionConflicto(ex.Message);
            }

            _unidadDeTrabajo.Reservas.Actualizar(reserva);
            _unidadDeTrabajo.Items.Actualizar(reserva.Item);
            await _unidadDeTrabajo.ConfirmarAsync(ct);

            return Proyectar(reserva);
        }
        catch
        {
            await _unidadDeTrabajo.RevertirAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Deja constancia de que la operación se ha perfeccionado entre las
    /// partes. Corresponde a la empresa proveedora, que es quien entrega el
    /// material.
    ///
    /// La confirmación retira el material del catálogo por haber sido
    /// transmitido, con lo que el ciclo de vida del ítem concluye. Permite
    /// asimismo distinguir las reservas que condujeron a una transacción
    /// efectiva de las que decayeron, dato necesario para la supervisión
    /// prevista en CU-11.
    /// </summary>
    public async Task<ReservaDto> ConfirmarAsync(
        Guid reservaId, Guid empresaProveedoraId, CancellationToken ct = default)
    {
        var reserva = await _reservas.ObtenerConRelacionesAsync(reservaId, ct)
            ?? throw new ExcepcionNoEncontrado("La reserva solicitada no existe.");

        if (reserva.Item.EmpresaId != empresaProveedoraId)
            throw new ExcepcionAccesoDenegado();

        // La confirmación retira el material del catálogo por haber sido
        // transmitido, transición que el dominio practica de forma conjunta.
        try
        {
            reserva.Confirmar(reserva.Item);
        }
        catch (InvalidOperationException ex)
        {
            throw new ExcepcionConflicto(ex.Message);
        }

        _unidadDeTrabajo.Reservas.Actualizar(reserva);
        _unidadDeTrabajo.Items.Actualizar(reserva.Item);
        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return Proyectar(reserva);
    }

    // -----------------------------------------------------------------------
    // Operaciones auxiliares
    // -----------------------------------------------------------------------

    private async Task LiberarSiHaVencidoAsync(ItemInventario item, CancellationToken ct)
    {
        var activa = await _reservas.ObtenerReservaActivaAsync(item.Id, ct);

        if (activa is null)
        {
            // Inconsistencia: el ítem figura reservado sin reserva activa que
            // lo respalde. Se devuelve al catálogo.
            item.Liberar();
            return;
        }

        if (activa.FechaExpiracion > DateTime.UtcNow) return;

        activa.Expirar(item);

        _unidadDeTrabajo.Reservas.Actualizar(activa);
    }

    private async Task ComprobarEmpresaVerificadaAsync(Guid empresaId, CancellationToken ct)
    {
        var empresa = await _unidadDeTrabajo.Empresas.ObtenerPorIdAsync(empresaId, ct)
            ?? throw new ExcepcionAccesoDenegado();

        // CU-09, excepción E2. RD-02 condiciona la operativa a la acreditación
        // previa de la condición profesional.
        if (!empresa.Verificada)
            throw new ExcepcionDeNegocio(
                "La empresa se encuentra pendiente de verificación y no puede " +
                "formalizar reservas.");
    }

    private static ReservaDto Proyectar(Reserva reserva) => new()
    {
        Id = reserva.Id,
        Estado = reserva.Estado.ToString(),
        FechaReserva = reserva.FechaReserva,
        FechaExpiracion = reserva.FechaExpiracion,

        ItemId = reserva.ItemId,
        Familia = reserva.Item?.Material?.Familia ?? string.Empty,
        Designacion = reserva.Item?.Material?.Designacion ?? string.Empty,
        LargoMm = reserva.Item?.LargoMm ?? 0,
        AnchoMm = reserva.Item?.AnchoMm ?? 0,
        EspesorMm = reserva.Item?.EspesorMm ?? 0,
        Cantidad = reserva.Item?.Cantidad ?? 0,
        PesoKg = reserva.Item?.PesoKg,
        Ubicacion = reserva.Item?.Ubicacion,

        EmpresaProveedora = reserva.Item?.Empresa?.RazonSocial ?? string.Empty,
        LocalidadProveedora = reserva.Item?.Empresa?.Localidad ?? string.Empty,
        EmailContactoProveedora = reserva.Item?.Empresa?.EmailContacto ?? string.Empty,
        TelefonoContactoProveedora = reserva.Item?.Empresa?.TelefonoContacto,

        EmpresaCompradora = reserva.EmpresaCompradora?.RazonSocial ?? string.Empty,
        LocalidadCompradora = reserva.EmpresaCompradora?.Localidad ?? string.Empty,
        EmailContactoCompradora = reserva.EmpresaCompradora?.EmailContacto ?? string.Empty,
        TelefonoContactoCompradora = reserva.EmpresaCompradora?.TelefonoContacto,

        Aviso = AvisoAlcance
    };
}
