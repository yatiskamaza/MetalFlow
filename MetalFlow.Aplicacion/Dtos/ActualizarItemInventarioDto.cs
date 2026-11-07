namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Datos de entrada de la modificación de un ítem (CU-04).
///
/// Comparte la estructura del alta: la operación sustituye la totalidad de los
/// atributos modificables, en lugar de admitir cambios parciales, lo que evita
/// la ambigüedad entre un campo omitido y un campo que se desea vaciar. No
/// incorpora la empresa propietaria ni la carga de origen, por constituir
/// ambas datos de trazabilidad no modificables (RD-04).
/// </summary>
public class ActualizarItemInventarioDto : CrearItemInventarioDto
{
}