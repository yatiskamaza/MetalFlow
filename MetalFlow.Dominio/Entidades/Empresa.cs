using MetalFlow.Dominio.Enumeraciones;

namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Unidad organizativa y sujeto de todas las operaciones del modelo B2B (RD-02).
/// Toda empresa se crea junto con su única cuenta de acceso en la misma
/// operación (CU-01), por lo que no puede existir sin ella.
/// </summary>
public class Empresa
{
    public Guid Id { get; private set; }
    public string Nif { get; private set; } = null!;
    public string RazonSocial { get; private set; } = null!;
    public string Direccion { get; private set; } = null!;
    public string Localidad { get; private set; } = null!;

    /// <summary>Canal de contacto que se revela a la contraparte tras confirmarse una reserva (RD-06).</summary>
    public string EmailContacto { get; private set; } = null!;
    public string? TelefonoContacto { get; private set; }

    public bool Verificada { get; private set; }
    public DateTime FechaAlta { get; private set; }

    // Relaciones
    public Usuario? Cuenta { get; private set; }
    public ICollection<ItemInventario> Inventario { get; private set; } = new List<ItemInventario>();
    public ICollection<CargaInventario> Cargas { get; private set; } = new List<CargaInventario>();
    public ICollection<Reserva> Reservas { get; private set; } = new List<Reserva>();

    private Empresa() { }

    public Empresa(string nif, string razonSocial, string direccion, string localidad,
                   string emailContacto, string? telefonoContacto = null)
    {
        if (string.IsNullOrWhiteSpace(nif))
            throw new ArgumentException("El identificador fiscal es obligatorio.", nameof(nif));
        if (string.IsNullOrWhiteSpace(razonSocial))
            throw new ArgumentException("La razón social es obligatoria.", nameof(razonSocial));

        Id = Guid.NewGuid();
        Nif = nif.Trim().ToUpperInvariant();
        RazonSocial = razonSocial.Trim();
        Direccion = direccion;
        Localidad = localidad;
        EmailContacto = emailContacto.Trim().ToLowerInvariant();
        TelefonoContacto = telefonoContacto;
        Verificada = false;          // Pendiente de verificación por el administrador (CU-10)
        FechaAlta = DateTime.UtcNow;
    }

    /// <summary>Habilita a la empresa para publicar y reservar material (CU-10, RD-02).</summary>
    public void Verificar() => Verificada = true;
}
