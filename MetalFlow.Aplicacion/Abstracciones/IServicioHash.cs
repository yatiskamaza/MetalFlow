namespace MetalFlow.Aplicacion.Abstracciones;

/// <summary>
/// Derivación y verificación irreversible de contraseñas (RNF-03, CT-08).
/// La interfaz se declara en la capa de aplicación y se implementa en la de
/// infraestructura, de modo que la lógica de los casos de uso no dependa de
/// una biblioteca criptográfica concreta y esta pueda sustituirse sin afectar
/// al resto del sistema.
/// </summary>
public interface IServicioHash
{
    string Derivar(string textoPlano);
    bool Verificar(string textoPlano, string hashAlmacenado);
}