using System.ComponentModel.DataAnnotations;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Datos de modificación de un anuncio existente. Sustituye la totalidad de
/// la información comercial, por coherencia con la modificación de ítems.
/// </summary>
public class ActualizarAnuncioDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, MinimumLength = 3,
        ErrorMessage = "El título debe tener entre 3 y 150 caracteres.")]
    public string Titulo { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder de 1000 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio; indique 0 para la cesión gratuita.")]
    [Range(0, 99999999.99, ErrorMessage = "El precio debe estar comprendido entre 0 y 99.999.999,99.")]
    public decimal? Precio { get; set; }
}