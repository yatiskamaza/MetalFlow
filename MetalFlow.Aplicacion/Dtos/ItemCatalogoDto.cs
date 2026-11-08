namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de un ítem del catálogo en las respuestas dirigidas a la
/// empresa compradora (CU-07 y CU-08).
///
/// Difiere del objeto empleado en la gestión del inventario propio: incorpora
/// la identificación de la empresa proveedora, necesaria para que el
/// comprador valore el origen y la proximidad del material (RD-04, RD-07),
/// pero omite todo dato de contacto. Dicha información se revela únicamente
/// una vez confirmada una reserva, conforme al principio de minimización
/// expuesto en el apartado 4.8.
/// </summary>
public class ItemCatalogoDto
{
    public Guid Id { get; set; }

    // Material (RD-03)
    public string Familia { get; set; } = null!;
    public string Designacion { get; set; } = null!;
    public string? Norma { get; set; }

    // Dimensiones geométricas, elemento diferencial del catálogo (RF-04)
    public decimal LargoMm { get; set; }
    public decimal AnchoMm { get; set; }
    public decimal EspesorMm { get; set; }

    public int Cantidad { get; set; }
    public decimal? PesoKg { get; set; }

    /// <summary>Clasificación jurídica del material (RD-01).</summary>
    public string Clasificacion { get; set; } = null!;

    public string Estado { get; set; } = null!;

    // Procedencia. Se identifica a la empresa y su localidad, sin exponer
    // dato de contacto alguno.
    public string EmpresaProveedora { get; set; } = null!;
    public string Localidad { get; set; } = null!;
    public bool EmpresaVerificada { get; set; }

    public DateTime FechaAlta { get; set; }

    // Datos comerciales, disponibles únicamente si el ítem cuenta con un
    // anuncio activo (RF-06).
    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
}