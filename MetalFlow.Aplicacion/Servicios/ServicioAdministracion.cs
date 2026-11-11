using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta los casos de uso reservados al administrador de la plataforma: la
/// verificación de empresas (CU-10) y la gestión de cuentas y supervisión de
/// reservas (CU-11).
///
/// A diferencia de los restantes servicios, sus operaciones no reciben el
/// identificador de una empresa solicitante ni comprueban pertenencia alguna:
/// el administrador no pertenece a empresa alguna y su legitimación deriva
/// exclusivamente del rol. Dicha asimetría es deliberada y se corresponde con
/// la distinción establecida en el apartado 4.7, según la cual las cuentas de
/// administrador quedan sujetas a una regla de autorización distinta de la
/// aplicable a las cuentas de empresa.
/// </summary>
public class ServicioAdministracion
{
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IRepositorioReservas _reservas;

    public ServicioAdministracion(IUnitOfWork unidadDeTrabajo, IRepositorioReservas reservas)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _reservas = reservas;
    }

    /// <summary>
    /// Relación de empresas registradas, con posibilidad de restringirla a las
    /// pendientes de verificación, que constituyen la cola de trabajo
    /// ordinaria del administrador.
    /// </summary>
    public async Task<IReadOnlyList<EmpresaDto>> ListarEmpresasAsync(
        bool soloPendientes = false, CancellationToken ct = default)
    {
        var empresas = soloPendientes
            ? await _unidadDeTrabajo.Empresas.BuscarAsync(e => !e.Verificada, ct)
            : await _unidadDeTrabajo.Empresas.ListarAsync(ct);

        // El recuento de inventario se resuelve en una consulta independiente
        // por cada empresa. Resulta admisible en el volumen previsto para el
        // prototipo; un catálogo de mayor tamaño aconsejaría una consulta
        // agregada única.
        var resultado = new List<EmpresaDto>();

        foreach (var empresa in empresas.OrderBy(e => e.Verificada).ThenBy(e => e.FechaAlta))
        {
            var items = await _unidadDeTrabajo.Items.BuscarAsync(
                i => i.EmpresaId == empresa.Id, ct);

            resultado.Add(Proyectar(empresa, items.Count));
        }

        return resultado;
    }

    public async Task<EmpresaDto> ObtenerEmpresaAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var empresa = await _unidadDeTrabajo.Empresas.ObtenerPorIdAsync(empresaId, ct)
            ?? throw new ExcepcionNoEncontrado("La empresa solicitada no existe.");

        var items = await _unidadDeTrabajo.Items.BuscarAsync(
            i => i.EmpresaId == empresaId, ct);

        return Proyectar(empresa, items.Count);
    }

    /// <summary>
    /// CU-10. Habilita a la empresa para operar en la plataforma, una vez
    /// contrastada su condición profesional (RD-02).
    ///
    /// La operación no admite reversión en la presente iteración: ningún caso
    /// de uso contempla la revocación de la habilitación, cuyo tratamiento
    /// plantearía además cuestiones que el diseño no resuelve, señaladamente
    /// el destino del inventario publicado y de las reservas en curso.
    /// </summary>
    public async Task<VerificacionEmpresaDto> VerificarEmpresaAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var empresa = await _unidadDeTrabajo.Empresas.ObtenerPorIdAsync(empresaId, ct)
            ?? throw new ExcepcionNoEncontrado("La empresa solicitada no existe.");

        // CU-10, excepción E2: la operación no procede sobre una empresa ya
        // habilitada. Se responde con conflicto y no con éxito silencioso,
        // para que el administrador advierta que su acción no ha tenido efecto.
        if (empresa.Verificada)
            throw new ExcepcionConflicto("La empresa ya se encuentra verificada.");

        empresa.Verificar();

        _unidadDeTrabajo.Empresas.Actualizar(empresa);
        await _unidadDeTrabajo.ConfirmarAsync(ct);

        return new VerificacionEmpresaDto
        {
            EmpresaId = empresa.Id,
            RazonSocial = empresa.RazonSocial,
            Nif = empresa.Nif,
            Verificada = empresa.Verificada,
            Mensaje = "Empresa verificada. Queda habilitada para incorporar material " +
                      "al catálogo y para formalizar reservas."
        };
    }

    // -----------------------------------------------------------------------
    // CU-11. Gestión de cuentas
    // -----------------------------------------------------------------------

    /// <summary>Relación de cuentas registradas, con su empresa y sus roles.</summary>
    public async Task<IReadOnlyList<CuentaDto>> ListarCuentasAsync(CancellationToken ct = default)
    {
        var cuentas = await _unidadDeTrabajo.Usuarios.BuscarAsync(
            u => true, ct, u => u.Roles, u => u.Empresa!);

        return cuentas
            .OrderBy(u => u.Activo).ThenBy(u => u.FechaAlta)
            .Select(ProyectarCuenta)
            .ToList();
    }

    /// <summary>
    /// CU-11. Inhabilita o rehabilita una cuenta.
    ///
    /// Dado que el modelo asigna una única cuenta por empresa, la operación
    /// inhabilita materialmente a la entidad, y produce efectos sobre su
    /// material que se practican en la misma transacción:
    ///
    /// La inhabilitación retira del catálogo los ítems disponibles, por cuanto
    /// una empresa que no puede acceder al sistema tampoco puede atender las
    /// solicitudes que se formulen sobre su material. Los ítems reservados se
    /// mantienen, al existir una expectativa legítima de la empresa compradora;
    /// cuando su reserva concluya sin confirmarse, la regla de liberación
    /// establecida en el dominio impedirá que vuelvan al catálogo.
    ///
    /// La rehabilitación restituye el acceso, pero no la disponibilidad del
    /// material retirado: durante la inhabilitación pudo haber sido consumido
    /// o enajenado, por lo que su reincorporación corresponde a la empresa.
    /// </summary>
    public async Task<ResultadoCambioEstadoDto> CambiarEstadoCuentaAsync(
        Guid cuentaId, bool activar, Guid administradorId, CancellationToken ct = default)
    {
        // CU-11, excepción E3: un administrador no puede inhabilitarse a sí
        // mismo, lo que podría dejar la plataforma sin cuenta capaz de operar.
        if (cuentaId == administradorId && !activar)
            throw new ExcepcionDeNegocio(
                "Un administrador no puede desactivar su propia cuenta.");

        await _unidadDeTrabajo.IniciarTransaccionAsync(ct);

        try
        {
            var cuenta = await _unidadDeTrabajo.Usuarios.ObtenerPorIdAsync(cuentaId, ct)
                ?? throw new ExcepcionNoEncontrado("La cuenta solicitada no existe.");

            try
            {
                if (activar) cuenta.Reactivar();
                else cuenta.Desactivar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ExcepcionConflicto(ex.Message);
            }

            _unidadDeTrabajo.Usuarios.Actualizar(cuenta);

            var itemsRetirados = 0;
            var reservasPendientes = 0;

            if (!activar && cuenta.EmpresaId is { } empresaId)
            {
                var items = await _unidadDeTrabajo.Items.BuscarAsync(
                    i => i.EmpresaId == empresaId &&
                         (i.Estado == EstadoItem.Disponible || i.Estado == EstadoItem.Reservado),
                    ct);

                foreach (var item in items)
                {
                    if (item.Estado == EstadoItem.Reservado)
                    {
                        reservasPendientes++;
                        continue;
                    }

                    item.RetirarPorInhabilitacion();
                    _unidadDeTrabajo.Items.Actualizar(item);
                    itemsRetirados++;
                }
            }

            await _unidadDeTrabajo.ConfirmarAsync(ct);

            return new ResultadoCambioEstadoDto
            {
                CuentaId = cuenta.Id,
                Activo = cuenta.Activo,
                ItemsRetirados = itemsRetirados,
                ReservasPendientes = reservasPendientes,
                Mensaje = activar
                    ? "Cuenta rehabilitada. El material retirado durante la inhabilitación " +
                      "no se restituye automáticamente: corresponde a la empresa republicarlo."
                    : $"Cuenta inhabilitada. Se han retirado {itemsRetirados} ítems del catálogo." +
                      (reservasPendientes > 0
                          ? $" Existen {reservasPendientes} reservas pendientes, que seguirán su " +
                            "curso; su material no volverá al catálogo al concluir."
                          : string.Empty)
            };
        }
        catch
        {
            await _unidadDeTrabajo.RevertirAsync(ct);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // CU-11. Supervisión de reservas
    // -----------------------------------------------------------------------

    /// <summary>
    /// Totalidad de las reservas del sistema, con su estado y las empresas
    /// implicadas. Permite al administrador apreciar la actividad de la
    /// plataforma sin intervenir en la operativa comercial de empresa alguna.
    /// </summary>
    public async Task<IReadOnlyList<ReservaSupervisionDto>> ListarReservasAsync(
        CancellationToken ct = default)
    {
        var reservas = await _reservas.ListarTodasAsync(ct);

        return reservas.Select(r => new ReservaSupervisionDto
        {
            Id = r.Id,
            Estado = r.Estado.ToString(),
            FechaReserva = r.FechaReserva,
            FechaExpiracion = r.FechaExpiracion,
            ItemId = r.ItemId,
            Designacion = r.Item?.Material?.Designacion ?? string.Empty,
            EmpresaProveedora = r.Item?.Empresa?.RazonSocial ?? string.Empty,
            EmpresaCompradora = r.EmpresaCompradora?.RazonSocial ?? string.Empty
        }).ToList();
    }

    private static CuentaDto ProyectarCuenta(Usuario cuenta) => new()
    {
        Id = cuenta.Id,
        Email = cuenta.Email,
        Nombre = cuenta.Nombre,
        Activo = cuenta.Activo,
        FechaAlta = cuenta.FechaAlta,
        Roles = cuenta.Roles.Select(r => r.Rol.ToString()).ToList(),
        EmpresaId = cuenta.EmpresaId,
        RazonSocial = cuenta.Empresa?.RazonSocial,
        EmpresaVerificada = cuenta.Empresa?.Verificada
    };

    private static EmpresaDto Proyectar(Empresa empresa, int itemsEnCatalogo) => new()
    {
        Id = empresa.Id,
        Nif = empresa.Nif,
        RazonSocial = empresa.RazonSocial,
        Direccion = empresa.Direccion,
        Localidad = empresa.Localidad,
        EmailContacto = empresa.EmailContacto,
        TelefonoContacto = empresa.TelefonoContacto,
        Verificada = empresa.Verificada,
        FechaAlta = empresa.FechaAlta,
        ItemsEnCatalogo = itemsEnCatalogo
    };
}
