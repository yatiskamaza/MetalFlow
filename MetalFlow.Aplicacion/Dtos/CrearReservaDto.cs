using System.ComponentModel.DataAnnotations;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Datos de entrada de la formalización de una reserva (CU-09).
///
/// Comprende únicamente el identificador del ítem: la empresa compradora se
/// obtiene del token y la vigencia de la reserva viene determinada por el
/// propio dominio, sin que el solicitante pueda alterarla.
/// </summary>
public class CrearReservaDto
{
    [Required(ErrorMessage = "El identificador del material es obligatorio.")]
    public Guid ItemId { get; set; }
}