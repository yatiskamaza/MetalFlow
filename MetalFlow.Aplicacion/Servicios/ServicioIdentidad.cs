using MetalFlow.Aplicacion.Abstracciones;
using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta los casos de uso CU-01 (registrar empresa) y CU-02 (autenticarse),
/// que materializan el requisito RF-01.
///
/// El servicio coordina las entidades del dominio y las operaciones de
/// persistencia, pero no alberga reglas propias del dominio: la validez de las
/// entidades la garantizan sus propios constructores, conforme al criterio
/// expuesto en el apartado 5.2.1.
/// </summary>
public class ServicioIdentidad
{
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IServicioHash _hash;
    private readonly IProveedorTokens _tokens;

    public ServicioIdentidad(IUnitOfWork unidadDeTrabajo, IServicioHash hash, IProveedorTokens tokens)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _hash = hash;
        _tokens = tokens;
    }

    /// <summary>
    /// CU-01. Da de alta la empresa y su única cuenta de acceso en una misma
    /// operación atómica: la relación uno a uno establecida en el apartado
    /// 4.5.2 exige que no pueda existir una empresa sin cuenta asociada.
    /// </summary>
    public async Task<RegistroCreadoDto> RegistrarEmpresaAsync(
        RegistroEmpresaDto datos, CancellationToken ct = default)
    {
        if (!datos.OperaComoProveedora && !datos.OperaComoCompradora)
            throw new ExcepcionDeNegocio(
                "Debe indicarse al menos un rol de operación: proveedora, compradora o ambos.");

        var nif = datos.Nif.Trim().ToUpperInvariant();
        var email = datos.Email.Trim().ToLowerInvariant();

        // E1 y E2 de CU-01. Los mensajes no revelan información adicional
        // sobre los registros existentes.
        if (await _unidadDeTrabajo.Empresas.ExisteAsync(e => e.Nif == nif, ct))
            throw new ExcepcionConflicto(
                "Ya existe una empresa registrada con ese identificador fiscal.");

        if (await _unidadDeTrabajo.Usuarios.ExisteAsync(u => u.Email == email, ct))
            throw new ExcepcionConflicto(
                "No es posible completar el alta con los datos proporcionados.");

        await _unidadDeTrabajo.IniciarTransaccionAsync(ct);

        try
        {
            var empresa = new Empresa(
                nif,
                datos.RazonSocial,
                datos.Direccion,
                datos.Localidad,
                datos.EmailContacto,
                datos.TelefonoContacto);

            var cuenta = new Usuario(
                email,
                _hash.Derivar(datos.Password),
                datos.NombreResponsable,
                empresa.Id);

            if (datos.OperaComoProveedora) cuenta.AsignarRol(Rol.Proveedor);
            if (datos.OperaComoCompradora) cuenta.AsignarRol(Rol.Comprador);

            await _unidadDeTrabajo.Empresas.AgregarAsync(empresa, ct);
            await _unidadDeTrabajo.Usuarios.AgregarAsync(cuenta, ct);

            await _unidadDeTrabajo.ConfirmarAsync(ct);

            return new RegistroCreadoDto
            {
                EmpresaId = empresa.Id,
                RazonSocial = empresa.RazonSocial,
                Verificada = empresa.Verificada,
                Mensaje = "Alta registrada. La empresa queda pendiente de verificación " +
                          "por parte del administrador antes de poder publicar o reservar material."
            };
        }
        catch
        {
            // E3 de CU-01: ante un fallo en la persistencia se revierte la
            // operación completa, de modo que no quede una empresa sin cuenta.
            await _unidadDeTrabajo.RevertirAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// CU-02. Verifica las credenciales y emite el token de acceso.
    /// </summary>
    public async Task<RespuestaAutenticacionDto> AutenticarAsync(
        LoginDto credenciales, CancellationToken ct = default)
    {
        var email = credenciales.Email.Trim().ToLowerInvariant();

        // Se cargan explícitamente los roles y la empresa, necesarios para
        // componer las declaraciones del token.
        var cuenta = await _unidadDeTrabajo.Usuarios.ObtenerUnoAsync(
            u => u.Email == email, ct,
            u => u.Roles,
            u => u.Empresa!);

        // Un mismo mensaje para cuenta inexistente, contraseña incorrecta y
        // cuenta desactivada, con el fin de impedir la enumeración de usuarios.
        if (cuenta is null || !cuenta.Activo)
            throw new ExcepcionCredencialesInvalidas();

        if (!_hash.Verificar(credenciales.Password, cuenta.PasswordHash))
            throw new ExcepcionCredencialesInvalidas();

        var (token, expiracion) = _tokens.Emitir(cuenta);

        return new RespuestaAutenticacionDto
        {
            Token = token,
            Expiracion = expiracion,
            Nombre = cuenta.Nombre,
            RazonSocialEmpresa = cuenta.Empresa?.RazonSocial,
            EmpresaVerificada = cuenta.Empresa?.Verificada ?? false,
            Roles = cuenta.Roles.Select(r => r.Rol.ToString()).ToList()
        };
    }
}
