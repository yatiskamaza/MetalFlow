namespace MetalFlow.Aplicacion.Excepciones;

/// <summary>
/// Excepción base de las reglas de negocio. Su existencia permite que la capa
/// de API distinga los incumplimientos de regla de los fallos técnicos y los
/// traduzca al código de estado HTTP que corresponda, en lugar de responder
/// indistintamente con un error interno.
/// </summary>
public class ExcepcionDeNegocio : Exception
{
    public ExcepcionDeNegocio(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Credenciales no válidas o cuenta inactiva (CU-02, excepciones E1 y E2).
/// El mensaje es deliberadamente genérico: distinguir entre cuenta inexistente
/// y contraseña incorrecta permitiría enumerar los usuarios registrados,
/// riesgo contemplado en la Tabla 23 (OWASP A07).
/// </summary>
public class ExcepcionCredencialesInvalidas : ExcepcionDeNegocio
{
    public ExcepcionCredencialesInvalidas()
        : base("Las credenciales proporcionadas no son válidas.") { }
}

/// <summary>Conflicto con un registro preexistente (CU-01, excepciones E1 y E2).</summary>
public class ExcepcionConflicto : ExcepcionDeNegocio
{
    public ExcepcionConflicto(string mensaje) : base(mensaje) { }
}