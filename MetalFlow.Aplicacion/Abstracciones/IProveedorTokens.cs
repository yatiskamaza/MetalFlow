using MetalFlow.Dominio.Entidades;

namespace MetalFlow.Aplicacion.Abstracciones;

/// <summary>
/// Emisión de los tokens de acceso (RF-01, CT-01). Al igual que el servicio de
/// derivación de contraseñas, se declara como abstracción en la capa de
/// aplicación para evitar que esta dependa de la implementación concreta del
/// mecanismo de autenticación.
/// </summary>
public interface IProveedorTokens
{
    (string token, DateTime expiracion) Emitir(Usuario usuario);
}