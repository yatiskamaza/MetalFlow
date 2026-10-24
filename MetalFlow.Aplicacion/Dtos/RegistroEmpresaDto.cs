using System.ComponentModel.DataAnnotations;

namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Datos de entrada del alta de empresa (CU-01). Los objetos de transferencia
/// evitan exponer las entidades del dominio al exterior de la API, conforme a
/// lo establecido en el apartado 4.2.
/// </summary>
public class RegistroEmpresaDto
{
    // --- Datos de la empresa (RD-02) ---

    [Required(ErrorMessage = "El identificador fiscal es obligatorio.")]
    [StringLength(15, MinimumLength = 8,
        ErrorMessage = "El identificador fiscal debe tener entre 8 y 15 caracteres.")]
    public string Nif { get; set; } = null!;

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(150)]
    public string RazonSocial { get; set; } = null!;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200)]
    public string Direccion { get; set; } = null!;

    [Required(ErrorMessage = "La localidad es obligatoria.")]
    [StringLength(100)]
    public string Localidad { get; set; } = null!;

    /// <summary>
    /// Canal de contacto que se revelará a la contraparte tras confirmarse una
    /// reserva (RD-06). Se recomienda emplear un buzón funcional, de modo que
    /// no constituya dato personal (apartado 4.8).
    /// </summary>
    [Required(ErrorMessage = "El correo de contacto comercial es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo de contacto no tiene un formato válido.")]
    [StringLength(150)]
    public string EmailContacto { get; set; } = null!;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [StringLength(20)]
    public string? TelefonoContacto { get; set; }

    // --- Datos de la cuenta de acceso (RF-01) ---

    [Required(ErrorMessage = "El nombre de la persona responsable es obligatorio.")]
    [StringLength(100)]
    public string NombreResponsable { get; set; } = null!;

    [Required(ErrorMessage = "El correo de acceso es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo de acceso no tiene un formato válido.")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = null!;

    // --- Roles solicitados ---

    /// <summary>
    /// Una misma empresa puede operar simultáneamente en ambos lados del
    /// mercado, supuesto característico de la simbiosis industrial descrita en
    /// el apartado 2.1.2. Debe seleccionarse al menos uno de los dos.
    /// </summary>
    public bool OperaComoProveedora { get; set; }

    public bool OperaComoCompradora { get; set; }
}
