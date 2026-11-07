using System.ComponentModel.DataAnnotations;
using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Datos de entrada del alta manual de un ítem de inventario (CU-04, RF-03).
///
/// Las dimensiones se expresan siempre en milímetros y la aleación se
/// selecciona del catálogo normalizado, conforme a RD-03: la interfaz no
/// admite texto libre para la designación, lo que previene en origen las
/// inconsistencias que el proceso de ingesta masiva ha de corregir.
/// </summary>
public class CrearItemInventarioDto
{
    [Required(ErrorMessage = "El material es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El material indicado no es válido.")]
    public int MaterialId { get; set; }

    [Required(ErrorMessage = "El largo es obligatorio.")]
    [Range(0.01, 99999.99, ErrorMessage = "El largo debe ser un valor positivo en milímetros.")]
    public decimal LargoMm { get; set; }

    [Required(ErrorMessage = "El ancho es obligatorio.")]
    [Range(0.01, 99999.99, ErrorMessage = "El ancho debe ser un valor positivo en milímetros.")]
    public decimal AnchoMm { get; set; }

    [Required(ErrorMessage = "El espesor es obligatorio.")]
    [Range(0.01, 99999.99, ErrorMessage = "El espesor debe ser un valor positivo en milímetros.")]
    public decimal EspesorMm { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos de una unidad.")]
    public int Cantidad { get; set; }

    [Range(0.01, 9999999.99, ErrorMessage = "El peso debe ser un valor positivo en kilogramos.")]
    public decimal? PesoKg { get; set; }

    [StringLength(100)]
    public string? Ubicacion { get; set; }

    /// <summary>
    /// Clasificación jurídica del material (RD-01). Su carácter obligatorio
    /// responde a que el sistema no cataloga como residuo el retal geométrico
    /// reutilizable, distinción que la Ley 7/2022 exige explicitar.
    /// </summary>
    [Required(ErrorMessage = "La clasificación regulatoria es obligatoria.")]
    [EnumDataType(typeof(ClasificacionRegulatoria),
        ErrorMessage = "La clasificación indicada no es válida.")]
    public ClasificacionRegulatoria Clasificacion { get; set; }
}
