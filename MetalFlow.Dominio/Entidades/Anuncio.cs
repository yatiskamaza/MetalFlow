namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Publicación comercial asociada a un ítem (RF-06).
/// La restricción de unicidad sobre ItemId impide más de un anuncio por ítem.
/// </summary>
public class Anuncio
{
    public Guid Id { get; private set; }

    public Guid ItemId { get; private set; }
    public ItemInventario Item { get; private set; } = null!;

    public string Titulo { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public decimal Precio { get; private set; }
    public DateTime FechaPublicacion { get; private set; }
    public bool Activo { get; private set; }

    private Anuncio() { }

    public Anuncio(Guid itemId, string titulo, decimal precio, string? descripcion = null)
    {
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));

        Id = Guid.NewGuid();
        ItemId = itemId;
        Titulo = titulo.Trim();
        Descripcion = descripcion;
        Precio = precio;
        FechaPublicacion = DateTime.UtcNow;
        Activo = true;
    }

    public void Despublicar() => Activo = false;
}
