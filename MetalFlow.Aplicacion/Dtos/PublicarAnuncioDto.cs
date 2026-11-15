using System.ComponentModel.DataAnnotations;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Datos de entrada de la publicación de un anuncio (CU-06, RF-06).
/// </summary>
public class PublicarAnuncioDto
{
    [Required(ErrorMessage = "El identificador del material es obligatorio.")]
    public Guid? ItemId { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, MinimumLength = 3,
        ErrorMessage = "El título debe tener entre 3 y 150 caracteres.")]
    public string Titulo { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder de 1000 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Se declara anulable y obligatorio de forma deliberada. Un tipo no
    /// anulable tomaría el valor cero cuando el campo se omitiera, y el
    /// anuncio se publicaría como cesión gratuita sin que el proveedor lo
    /// hubiera indicado. La obligatoriedad expresa exige que el precio, cero
    /// incluido, sea siempre una decisión explícita.
    /// </summary>
    [Required(ErrorMessage = "El precio es obligatorio; indique 0 para la cesión gratuita.")]
    [Range(0, 99999999.99, ErrorMessage = "El precio debe estar comprendido entre 0 y 99.999.999,99.")]
    public decimal? Precio { get; set; }
}