namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Entrada del catálogo normalizado de aleaciones (RD-03). El cliente lo
/// emplea para componer la selección de material, de modo que la designación
/// no se introduzca como texto libre.
/// </summary>
public class MaterialDto
{
    public int Id { get; set; }
    public string Familia { get; set; } = null!;
    public string Designacion { get; set; } = null!;
    public string? Norma { get; set; }
}