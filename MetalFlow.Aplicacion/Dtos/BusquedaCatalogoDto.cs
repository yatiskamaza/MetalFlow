using System.ComponentModel.DataAnnotations;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Criterios de búsqueda recibidos como parámetros de consulta (CU-07, RF-04).
///
/// La totalidad de los filtros es opcional: una búsqueda sin criterio alguno
/// devuelve el catálogo completo paginado, conforme a la excepción E3 de las
/// especificaciones de CU-07.
/// </summary>
public class BusquedaCatalogoDto
{
    public int? MaterialId { get; set; }

    [StringLength(50)]
    public string? Familia { get; set; }

    [Range(0.01, 99999.99, ErrorMessage = "El espesor mínimo debe ser un valor positivo.")]
    public decimal? EspesorMinimo { get; set; }

    [Range(0.01, 99999.99, ErrorMessage = "El espesor máximo debe ser un valor positivo.")]
    public decimal? EspesorMaximo { get; set; }

    [Range(0.01, 99999.99, ErrorMessage = "El largo mínimo debe ser un valor positivo.")]
    public decimal? LargoMinimo { get; set; }

    [Range(0.01, 99999.99, ErrorMessage = "El largo máximo debe ser un valor positivo.")]
    public decimal? LargoMaximo { get; set; }

    [Range(0.01, 99999.99, ErrorMessage = "El ancho mínimo debe ser un valor positivo.")]
    public decimal? AnchoMinimo { get; set; }

    [Range(0.01, 99999.99, ErrorMessage = "El ancho máximo debe ser un valor positivo.")]
    public decimal? AnchoMaximo { get; set; }

    [StringLength(100)]
    public string? Localidad { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser al menos 1.")]
    public int Pagina { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "El tamaño de página admitido es de 1 a 100 registros.")]
    public int TamanoPagina { get; set; } = 20;
}