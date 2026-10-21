using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Entidad central del sistema. Materializa la propuesta de valor del proyecto:
/// a diferencia de los catálogos organizados por peso, los atributos dimensionales
/// son de primer nivel y constituyen el eje de indexación del catálogo (RF-04).
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
        if (largoMm <= 0 || anchoMm <= 0 || espesorMm <= 0)
            throw new ArgumentException("Las dimensiones deben ser estrictamente positivas.");
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser estrictamente positiva.");

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
    /// Transición de estado encapsulada en el dominio: garantiza que no pueda
    /// invocarse desde la capa de API en condiciones inválidas (CT-06).
    /// </summary>
    public void Reservar()
    {
        if (Estado != EstadoItem.Disponible)
            throw new InvalidOperationException("Solo puede reservarse un ítem disponible.");
        Estado = EstadoItem.Reservado;
    }

    public void Liberar()
    {
        if (Estado != EstadoItem.Reservado)
            throw new InvalidOperationException("Solo puede liberarse un ítem reservado.");
        Estado = EstadoItem.Disponible;
    }

    public void Retirar() => Estado = EstadoItem.Retirado;

    /// <summary>Un retal se considera reutilizable mientras no haya sido retirado del catálogo (RD-01).</summary>
    public bool EsReutilizable() => Estado != EstadoItem.Retirado;
}
