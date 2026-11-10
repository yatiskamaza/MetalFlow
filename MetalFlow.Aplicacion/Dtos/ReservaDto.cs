namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Representación de una reserva en las respuestas de la API (CU-09).
///
/// A diferencia de la representación del catálogo, esta sí incorpora los
/// datos de contacto de la empresa proveedora. Dicha revelación constituye
/// precisamente el objeto de la reserva: conforme a RD-06, la plataforma
/// actúa como intermediario de emparejamiento y bloqueo, y el perfeccionamiento
/// de la operación corresponde a las partes, que han de poder comunicarse.
///
/// El contacto expuesto es el canal comercial de la empresa, nunca la
/// credencial de acceso de su cuenta, conforme a la separación establecida en
/// el apartado 4.8.
/// </summary>
public class ReservaDto
{
    public Guid Id { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaReserva { get; set; }
    public DateTime FechaExpiracion { get; set; }

    // Material reservado
    public Guid ItemId { get; set; }
    public string Familia { get; set; } = null!;
    public string Designacion { get; set; } = null!;
    public decimal LargoMm { get; set; }
    public decimal AnchoMm { get; set; }
    public decimal EspesorMm { get; set; }
    public int Cantidad { get; set; }
    public decimal? PesoKg { get; set; }
    public string? Ubicacion { get; set; }

    // Contraparte proveedora, con su canal de contacto comercial
    public string EmpresaProveedora { get; set; } = null!;
    public string LocalidadProveedora { get; set; } = null!;
    public string EmailContactoProveedora { get; set; } = null!;
    public string? TelefonoContactoProveedora { get; set; }

    // Contraparte compradora
    public string EmpresaCompradora { get; set; } = null!;
    public string LocalidadCompradora { get; set; } = null!;
    public string EmailContactoCompradora { get; set; } = null!;
    public string? TelefonoContactoCompradora { get; set; }

    /// <summary>
    /// Advertencia expresa sobre el alcance de la operación (RD-06). Se
    /// incorpora a la respuesta para que la interfaz no haya de replicar el
    /// texto y para que resulte igualmente visible a quien consuma la API
    /// directamente.
    /// </summary>
    public string Aviso { get; set; } = null!;
}
