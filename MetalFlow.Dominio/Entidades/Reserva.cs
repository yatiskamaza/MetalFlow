using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Bloqueo de un ítem por parte de una empresa compradora (RF-05).
/// La plataforma actúa como intermediario de emparejamiento y reserva:
/// no procesa pago alguno (RD-06).
///
/// CICLO DE VIDA
///
/// La reserva nace pendiente y admite tres desenlaces, todos ellos
/// terminales: se confirma, cuando la operación se perfecciona entre las
/// partes; se cancela, cuando alguna de ellas desiste; o expira, cuando
/// transcurre su vigencia sin que ninguna de las dos cosas ocurra.
///
/// Cada transición determina necesariamente la del ítem reservado, por lo
/// que ambas se practican de forma conjunta: los métodos de transición
/// reciben el ítem y operan sobre él. De este modo resulta imposible
/// modificar el estado de la reserva olvidando el del material, situación
/// que dejaría el sistema en estado inconsistente.
/// </summary>
public class Reserva
{
    public const int HorasDeVigencia = 72;

    public Guid Id { get; private set; }

    public Guid ItemId { get; private set; }
    public ItemInventario Item { get; private set; } = null!;

    public Guid EmpresaCompradoraId { get; private set; }
    public Empresa EmpresaCompradora { get; private set; } = null!;

    public DateTime FechaReserva { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public EstadoReserva Estado { get; private set; }

    private Reserva() { }

    public Reserva(Guid itemId, Guid empresaCompradoraId)
    {
        Id = Guid.NewGuid();
        ItemId = itemId;
        EmpresaCompradoraId = empresaCompradoraId;
        FechaReserva = DateTime.UtcNow;
        FechaExpiracion = FechaReserva.AddHours(HorasDeVigencia);
        Estado = EstadoReserva.Pendiente;
    }

    /// <summary>
    /// Deja constancia de que la operación se ha perfeccionado entre las
    /// partes, y retira el material del catálogo por haber sido transmitido.
    ///
    /// Corresponde a la empresa proveedora, que es quien entrega el material.
    /// </summary>
    public void Confirmar(ItemInventario item)
    {
        ComprobarQueCorresponde(item);

        if (Estado != EstadoReserva.Pendiente)
            throw new InvalidOperationException(
                "Solo puede confirmarse una reserva pendiente.");

        Estado = EstadoReserva.Confirmada;
        item.RetirarPorVenta();
    }

    /// <summary>
    /// Deja sin efecto la reserva y libera el material.
    ///
    /// Resulta accesible a ambas partes: la compradora puede desistir y la
    /// proveedora puede haber dispuesto del material entretanto. No procede,
    /// en cambio, sobre una reserva ya confirmada, por cuanto el material ha
    /// sido transmitido.
    /// </summary>
    /// <param name="propietarioHabilitado">
    /// Situación de la empresa propietaria del material, que determina si este
    /// vuelve al catálogo o queda retirado.
    /// </param>
    public void Cancelar(ItemInventario item, bool propietarioHabilitado)
    {
        ComprobarQueCorresponde(item);

        if (Estado != EstadoReserva.Pendiente)
            throw new InvalidOperationException(
                "Solo puede cancelarse una reserva pendiente.");

        Estado = EstadoReserva.Cancelada;
        item.Liberar(propietarioHabilitado);
    }

    /// <summary>
    /// Deja sin efecto la reserva por transcurso de su vigencia y libera el
    /// material.
    ///
    /// No corresponde a actor alguno: es el propio sistema quien la practica,
    /// conforme al proceso descrito en el apartado 5.5.
    /// </summary>
    /// <param name="propietarioHabilitado">
    /// Situación de la empresa propietaria del material, que determina si este
    /// vuelve al catálogo o queda retirado.
    /// </param>
    public void Expirar(ItemInventario item, bool propietarioHabilitado)
    {
        ComprobarQueCorresponde(item);

        if (Estado != EstadoReserva.Pendiente)
            throw new InvalidOperationException(
                "Solo puede expirar una reserva pendiente.");

        Estado = EstadoReserva.Expirada;
        item.Liberar(propietarioHabilitado);
    }

    /// <summary>
    /// Una reserva se considera activa mientras retiene el material. La
    /// confirmación no la mantiene activa: el ítem queda retirado por
    /// transmisión y la operación concluye.
    /// </summary>
    public bool EstaActiva() => Estado == EstadoReserva.Pendiente;

    public bool HaVencido() => Estado == EstadoReserva.Pendiente
                               && FechaExpiracion <= DateTime.UtcNow;

    /// <summary>
    /// Impide operar sobre un ítem distinto del reservado. La comprobación
    /// resulta necesaria porque el ítem se recibe como parámetro en lugar de
    /// obtenerse de la propia entidad, decisión adoptada para que la
    /// dependencia resulte explícita y no quede supeditada a que la relación
    /// se haya cargado desde la persistencia.
    /// </summary>
    private void ComprobarQueCorresponde(ItemInventario item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (item.Id != ItemId)
            throw new InvalidOperationException(
                "El ítem indicado no se corresponde con el de la reserva.");
    }
}