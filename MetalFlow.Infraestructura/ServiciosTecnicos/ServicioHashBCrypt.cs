using MetalFlow.Aplicacion.Abstracciones;

namespace MetalFlow.Infraestructura.ServiciosTecnicos;

/// <summary>
/// Derivación de contraseñas mediante BCrypt (RNF-03, CT-08).
///
/// BCrypt incorpora una sal aleatoria en cada derivación, de modo que dos
/// contraseñas idénticas producen hashes distintos, e incluye un factor de
/// coste que determina el número de iteraciones y que permite endurecer el
/// algoritmo conforme aumenta la capacidad de cómputo disponible.
/// </summary>
public class ServicioHashBCrypt : IServicioHash
{
    /// <summary>
    /// Factor de coste. Cada incremento unitario duplica el tiempo de cálculo.
    /// El valor adoptado equilibra la resistencia frente a ataques de fuerza
    /// bruta con un tiempo de respuesta aceptable en la autenticación.
    /// </summary>
    private const int FactorDeCoste = 12;

    public string Derivar(string textoPlano)
        => BCrypt.Net.BCrypt.HashPassword(textoPlano, FactorDeCoste);

    public bool Verificar(string textoPlano, string hashAlmacenado)
    {
        // Un hash malformado en la base de datos no debe propagarse como error
        // interno: se trata como verificación fallida.
        try
        {
            return BCrypt.Net.BCrypt.Verify(textoPlano, hashAlmacenado);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}