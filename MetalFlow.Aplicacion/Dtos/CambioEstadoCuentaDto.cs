using System.ComponentModel.DataAnnotations;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>Estado al que se pretende llevar una cuenta (CU-11).</summary>
public class CambioEstadoCuentaDto
{
    [Required(ErrorMessage = "Debe indicarse el estado de la cuenta.")]
    public bool? Activo { get; set; }
}