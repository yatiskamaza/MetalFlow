using MetalFlow.Aplicacion.Dtos;
using MetalFlow.Aplicacion.Excepciones;
using MetalFlow.Dominio.Entidades;
using MetalFlow.Dominio.Enumeraciones;
using MetalFlow.Dominio.Repositorios;

namespace MetalFlow.Aplicacion.Servicios;

/// <summary>
/// Orquesta los casos de uso reservados al administrador de la plataforma. En
/// la presente iteración comprende la verificación de empresas (CU-10).
///
/// A diferencia de los restantes servicios, sus operaciones no reciben el
/// identificador de una empresa solicitante ni comprueban pertenencia alguna:
/// el administrador no pertenece a empresa alguna y su legitimación deriva
/// exclusivamente del rol. Dicha asimetría es deliberada pues la cuenta de
/// administrador queda sujeta a una regla de autorización distinta de la
/// aplicable a las cuentas de empresa.
/// </summary>
public class ServicioAdministracion
{
    private readonly IUnitOfWork _unidadDeTrabajo;

    public ServicioAdministracion(IUnitOfWork unidadDeTrabajo)
        => _unidadDeTrabajo = unidadDeTrabajo;

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
