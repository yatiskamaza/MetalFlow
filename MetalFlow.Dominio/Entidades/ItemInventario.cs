using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Entidad central del sistema. Materializa la propuesta de valor del proyecto:
/// a diferencia de los catálogos organizados por peso, los atributos
/// dimensionales son de primer nivel y constituyen el eje de indexación del
/// catálogo (RF-04).
///
/// CICLO DE VIDA
///
/// Un ítem nace disponible. Puede reservarse, y desde ahí volver a estar
/// disponible —si la reserva se cancela o expira— o quedar retirado, si la
/// operación se perfecciona o si su propietario ha sido inhabilitado. Puede
/// asimismo retirarse directamente por decisión de su propietario, y
/// republicarse si no fue transmitido.
/// </summary>
public class ItemInventario
{
    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public int MaterialId { get; private set; }
    public Material Material { get; private set; } = null!;

    /// <summary>
    /// Nulo en los ítems dados de alta manualmente (RF-03); con valor en los
    /// generados por el proceso micro-ETL (RF-02), lo que preserva la
    /// trazabilidad del origen del dato exigida por RD-04.
    /// </summary>
    public Guid? CargaId { get; private set; }
    public CargaInventario? Carga { get; private set; }

    // Dimensiones normalizadas en milímetros (RD-03)
    public decimal LargoMm { get; private set; }
    public decimal AnchoMm { get; private set; }
    public decimal EspesorMm { get; private set; }

    public int Cantidad { get; private set; }
    public decimal? PesoKg { get; private set; }
    public string? Ubicacion { get; private set; }

    public EstadoItem Estado { get; private set; }
    public ClasificacionRegulatoria Clasificacion { get; private set; }
    public DateTime FechaAlta { get; private set; }

    public Anuncio? Anuncio { get; private set; }
    public ICollection<Reserva> Reservas { get; private set; } = new List<Reserva>();

    private ItemInventario() { }

    public ItemInventario(Guid empresaId, int materialId, decimal largoMm, decimal anchoMm,
                          decimal espesorMm, int cantidad, ClasificacionRegulatoria clasificacion,
                          decimal? pesoKg = null, string? ubicacion = null, Guid? cargaId = null)
    {
        ValidarMedidas(largoMm, anchoMm, espesorMm, cantidad, pesoKg);

        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        MaterialId = materialId;
        LargoMm = largoMm;
        AnchoMm = anchoMm;
        EspesorMm = espesorMm;
        Cantidad = cantidad;
        Clasificacion = clasificacion;
        PesoKg = pesoKg;
        Ubicacion = ubicacion;
        CargaId = cargaId;
        Estado = EstadoItem.Disponible;
        FechaAlta = DateTime.UtcNow;
    }

    /// <summary>
    /// Modifica los datos del ítem. La operación se rechaza si el ítem no se
    /// encuentra disponible: alterar las características de una pieza ya
    /// reservada induciría a error a la empresa compradora, que acudiría a
    /// recoger un material distinto del que consultó.
    ///
    /// No admite la modificación de la empresa propietaria ni de la carga de
    /// origen, por constituir ambas datos de trazabilidad (RD-04).
    /// </summary>
    public void Actualizar(int materialId, decimal largoMm, decimal anchoMm, decimal espesorMm,
                           int cantidad, ClasificacionRegulatoria clasificacion,
                           decimal? pesoKg, string? ubicacion)
    {
        if (Estado != EstadoItem.Disponible)
            throw new InvalidOperationException(
                "Solo puede modificarse un ítem disponible.");

        ValidarMedidas(largoMm, anchoMm, espesorMm, cantidad, pesoKg);

        MaterialId = materialId;
        LargoMm = largoMm;
        AnchoMm = anchoMm;
        EspesorMm = espesorMm;
        Cantidad = cantidad;
        Clasificacion = clasificacion;
        PesoKg = pesoKg;
        Ubicacion = ubicacion;
    }

    /// <summary>
    /// Transición de estado encapsulada en el dominio: garantiza que no pueda
    /// invocarse desde la capa de API en condiciones inválidas (CT-06).
    /// </summary>
    public void Reservar()
    {
        if (Estado != EstadoItem.Disponible)
            throw new InvalidOperationException("Solo puede reservarse un ítem disponible.");

        Estado = EstadoItem.Reservado;
    }

    /// <summary>
    /// Libera el ítem de la reserva que lo retenía. Se invoca desde la
    /// cancelación y desde la expiración de la reserva.
    ///
    /// El destino del material depende de la situación de su propietario. Si
    /// la empresa se encuentra habilitada, el ítem vuelve al catálogo. Si ha
    /// sido inhabilitada, queda retirado: devolverlo al catálogo expondría
    /// material de una entidad que no puede ya atender las solicitudes que se
    /// formulen sobre él.
    ///
    /// La regla reside aquí, y no en cada una de las operaciones que liberan
    /// material, porque es el único punto común a todas ellas: cualquier
    /// camino presente o futuro que libere un ítem queda sujeto a ella. El
    /// estado del propietario se recibe como parámetro en lugar de
    /// consultarse, de modo que la entidad no dependa de otra ajena a su
    /// propio agregado.
    /// </summary>
    public void Liberar(bool propietarioHabilitado)
    {
        if (Estado != EstadoItem.Reservado)
            throw new InvalidOperationException("Solo puede liberarse un ítem reservado.");

        Estado = propietarioHabilitado ? EstadoItem.Disponible : EstadoItem.Retirado;
    }

    /// <summary>
    /// Devuelve al catálogo un ítem previamente retirado.
    ///
    /// Permite a una empresa reincorporar el material que conserva tras una
    /// retirada voluntaria o tras su rehabilitación. No procede sobre el
    /// material transmitido: dado que el modelo emplea un mismo estado para
    /// ambos supuestos, corresponde al servicio de aplicación verificar, antes
    /// de invocar esta operación, que el ítem no fue objeto de una reserva
    /// confirmada.
    /// </summary>
    public void Republicar()
    {
        if (Estado != EstadoItem.Retirado)
            throw new InvalidOperationException("Solo puede republicarse un ítem retirado.");

        Estado = EstadoItem.Disponible;
    }

    /// <summary>
    /// Retira el ítem del catálogo como consecuencia de la inhabilitación de su
    /// propietario (CU-11). Afecta únicamente al material disponible: el
    /// reservado se mantiene, por existir una expectativa legítima de la
    /// empresa compradora, y el ya retirado no requiere acción alguna.
    /// </summary>
    public void RetirarPorInhabilitacion()
    {
        if (Estado == EstadoItem.Disponible)
            Estado = EstadoItem.Retirado;
    }

    /// <summary>
    /// Retira el ítem del catálogo por haber sido transmitido a la empresa
    /// compradora. Se invoca desde la confirmación de la reserva.
    ///
    /// Comparte estado con la retirada por decisión del propietario, por
    /// cuanto el efecto sobre el catálogo es idéntico y el modelo no
    /// distingue ambos supuestos. El motivo de la retirada resulta deducible
    /// de la existencia de una reserva confirmada sobre el ítem, lo que
    /// preserva la trazabilidad exigida por RD-04 sin necesidad de un estado
    /// adicional.
    /// </summary>
    public void RetirarPorVenta()
    {
        if (Estado != EstadoItem.Reservado)
            throw new InvalidOperationException(
                "Solo puede retirarse por transmisión un ítem reservado.");

        Estado = EstadoItem.Retirado;
    }

    /// <summary>
    /// Baja lógica del ítem por decisión de su propietario. No se elimina el
    /// registro, por cuanto las reservas que lo refieran deben conservarse
    /// como histórico de operaciones y su procedencia constituye información
    /// de trazabilidad (RD-04). Un ítem retirado deja de figurar en los
    /// resultados de búsqueda.
    /// </summary>
    public void Retirar()
    {
        if (Estado == EstadoItem.Reservado)
            throw new InvalidOperationException(
                "No puede retirarse un ítem con una reserva en curso.");

        Estado = EstadoItem.Retirado;
    }

    /// <summary>Un retal se considera reutilizable mientras no haya sido retirado (RD-01).</summary>
    public bool EsReutilizable() => Estado != EstadoItem.Retirado;

    public bool PerteneceA(Guid empresaId) => EmpresaId == empresaId;

    private static void ValidarMedidas(decimal largoMm, decimal anchoMm, decimal espesorMm,
                                       int cantidad, decimal? pesoKg)
    {
        if (largoMm <= 0 || anchoMm <= 0 || espesorMm <= 0)
            throw new ArgumentException("Las dimensiones deben ser estrictamente positivas.");
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser estrictamente positiva.");
        if (pesoKg is <= 0)
            throw new ArgumentException("El peso, de indicarse, debe ser estrictamente positivo.");
    }
}
