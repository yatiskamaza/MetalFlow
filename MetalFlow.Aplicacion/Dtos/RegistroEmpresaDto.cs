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

    /// <summary>
    /// Identificador fiscal de la empresa. Se admite la estructura de los tres
    /// identificadores españoles con los que puede operar una entidad:
    /// NIF de persona física (ocho dígitos y letra), NIE (X, Y o Z, siete
    /// dígitos y letra) y NIF de persona jurídica, antiguo CIF (letra de forma
    /// jurídica, siete dígitos y carácter de control, numérico o alfabético).
    ///
    /// La validación es estructural: no calcula el carácter de control. La
    /// acreditación efectiva de la empresa corresponde a la verificación
    /// manual del administrador (CU-10), por lo que el cálculo no añadiría
    /// garantía alguna sobre la condición profesional de la entidad.
    ///
    /// Se admiten minúsculas porque el servicio normaliza el identificador a
    /// mayúsculas antes de comprobar su unicidad y almacenarlo.
    /// </summary>
    [Required(ErrorMessage = "El identificador fiscal es obligatorio.")]
    [RegularExpression(
        @"^([0-9]{8}[A-Za-z]|[XYZxyz][0-9]{7}[A-Za-z]|[ABCDEFGHJNPQRSUVWabcdefghjnpqrsuvw][0-9]{7}[0-9A-Ja-j])$",
        ErrorMessage = "El identificador fiscal no tiene un formato válido de NIF, NIE o CIF.")]
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
    /// Canal de contacto que se revelará a la contraparte tras formalizarse una
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
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula y un número.")]
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