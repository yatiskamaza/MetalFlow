namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de un anuncio para su propietario, con un resumen del
/// material al que se asocia.
/// </summary>
public class AnuncioDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public bool Activo { get; set; }

    // Resumen del material
    public Guid ItemId { get; set; }
    public string Designacion { get; set; } = null!;
    public decimal LargoMm { get; set; }
    public decimal AnchoMm { get; set; }
    public decimal EspesorMm { get; set; }

    /// <summary>
    /// Estado del material. Un anuncio activo sobre material reservado o
    /// retirado no figura en el catálogo, que comprende únicamente el
    /// material disponible; el dato permite al proveedor advertirlo.
    /// </summary>
    public string EstadoItem { get; set; } = null!;
}