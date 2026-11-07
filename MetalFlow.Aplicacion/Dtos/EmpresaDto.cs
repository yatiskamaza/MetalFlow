namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de una empresa en las respuestas destinadas al
/// administrador de la plataforma (CU-10 y CU-11).
///
/// Incorpora los datos identificativos y de contacto necesarios para
/// contrastar la condición profesional de la entidad, así como el estado de
/// su verificación. No expone el correo de acceso de su cuenta, que opera
/// exclusivamente como credencial conforme al apartado 4.8.
/// </summary>
public class EmpresaDto
{
    public Guid Id { get; set; }
    public string Nif { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public string Localidad { get; set; } = null!;
    public string EmailContacto { get; set; } = null!;
    public string? TelefonoContacto { get; set; }
    public bool Verificada { get; set; }
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Número de ítems que la empresa mantiene en su catálogo. Permite al
    /// administrador advertir el alcance de la actividad de cada entidad sin
    /// necesidad de consultar su inventario, al que no tiene acceso.
    /// </summary>
    public int ItemsEnCatalogo { get; set; }
}