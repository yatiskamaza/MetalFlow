namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de un ítem de inventario en las respuestas de la API.
///
/// Incorpora la designación de la aleación además de su identificador, de modo
/// que el cliente no haya de resolver la referencia por separado. No expone
/// identificador alguno de la carga de origen ni de la empresa propietaria
/// cuando la consulta procede de un tercero.
/// </summary>
public class ItemInventarioDto
{
    public Guid Id { get; set; }

    public int MaterialId { get; set; }
    public string Familia { get; set; } = null!;
    public string Designacion { get; set; } = null!;
    public string? Norma { get; set; }

    public decimal LargoMm { get; set; }
    public decimal AnchoMm { get; set; }
    public decimal EspesorMm { get; set; }
    public int Cantidad { get; set; }
    public decimal? PesoKg { get; set; }
    public string? Ubicacion { get; set; }

    public string Estado { get; set; } = null!;
    public string Clasificacion { get; set; } = null!;

    /// <summary>
    /// Indica si el ítem procede de una ingesta masiva o de un alta manual,
    /// sin revelar el identificador concreto de la carga (RD-04).
    /// </summary>
    public bool ProcedeDeCargaMasiva { get; set; }

    public DateTime FechaAlta { get; set; }
}