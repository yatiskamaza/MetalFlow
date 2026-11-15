namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Publicación comercial asociada a un ítem (RF-06).
///
/// El anuncio añade al material la información comercial —título, descripción
/// y precio— que el catálogo muestra a las empresas compradoras. Su existencia
/// no condiciona la aparición del material en el catálogo: un ítem disponible
/// figura en las búsquedas cuente o no con anuncio.
///
/// La restricción de unicidad sobre el ítem impide más de un anuncio por ítem,
/// también a lo largo del tiempo: un anuncio despublicado no se sustituye por
/// otro nuevo, sino que se reactiva. Ello conserva un único registro comercial
/// por material.
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
        Validar(titulo, precio);

        Id = Guid.NewGuid();
        ItemId = itemId;
        Titulo = titulo.Trim();
        Descripcion = NormalizarDescripcion(descripcion);
        Precio = precio;
        FechaPublicacion = DateTime.UtcNow;
        Activo = true;
    }

    /// <summary>
    /// Modifica la información comercial del anuncio. No altera su situación
    /// de publicación, que se gobierna mediante las operaciones específicas.
    /// </summary>
    public void Actualizar(string titulo, decimal precio, string? descripcion)
    {
        Validar(titulo, precio);

        Titulo = titulo.Trim();
        Descripcion = NormalizarDescripcion(descripcion);
        Precio = precio;
    }

    /// <summary>
    /// Vuelve a publicar un anuncio previamente despublicado. La fecha de
    /// publicación se actualiza, por cuanto el anuncio reaparece en el
    /// catálogo en ese momento.
    /// </summary>
    public void Reactivar()
    {
        if (Activo)
            throw new InvalidOperationException("El anuncio ya se encuentra publicado.");

        Activo = true;
        FechaPublicacion = DateTime.UtcNow;
    }

    /// <summary>
    /// Retira el anuncio del catálogo sin eliminarlo. El material permanece
    /// disponible y sigue figurando en las búsquedas, aunque sin información
    /// comercial asociada.
    /// </summary>
    public void Despublicar()
    {
        if (!Activo)
            throw new InvalidOperationException("El anuncio ya se encuentra despublicado.");

        Activo = false;
    }

    /// <summary>
    /// El precio admite el valor cero, que representa la cesión gratuita del
    /// material: práctica no infrecuente en el sector, donde la retirada de un
    /// retal puede resultar más ventajosa para su propietario que su
    /// almacenamiento.
    /// </summary>
    private static void Validar(string titulo, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título del anuncio es obligatorio.", nameof(titulo));

        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));
    }

    private static string? NormalizarDescripcion(string? descripcion)
        => string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
}
