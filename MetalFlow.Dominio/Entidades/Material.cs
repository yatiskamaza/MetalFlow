namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Catálogo normalizado de familias y designaciones de aleación (RD-03).
/// Su existencia como tabla independiente elimina la dependencia transitiva
/// que supondría almacenar la designación en cada ítem, y evita que una misma
/// aleación se registre con grafías distintas por parte de empresas distintas.
/// </summary>
public class Material
{
    public int Id { get; private set; }
    public string Familia { get; private set; } = null!;
    public string Designacion { get; private set; } = null!;
    public string? Norma { get; private set; }

    public ICollection<ItemInventario> Items { get; private set; } = new List<ItemInventario>();

    private Material() { }

    public Material(string familia, string designacion, string? norma = null)
    {
        Familia = familia;
        Designacion = designacion.Trim().ToUpperInvariant();
        Norma = norma;
    }
}
