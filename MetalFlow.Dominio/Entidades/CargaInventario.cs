namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Registro de una ejecución del proceso micro-ETL (RF-02).
/// Materializa la trazabilidad del origen de los datos exigida por RD-04.
/// </summary>
public class CargaInventario
{
    /// <summary>Límites que hacen viable el procesamiento síncrono (CT-02).</summary>
    public const int MaximoFilas = 5000;
    public const long MaximoBytes = 2 * 1024 * 1024;

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public string NombreArchivo { get; private set; } = null!;
    public DateTime FechaCarga { get; private set; }
    public int FilasTotales { get; private set; }
    public int FilasValidas { get; private set; }
    public int FilasRechazadas { get; private set; }

    private readonly List<IncidenciaCarga> _incidencias = new();
    public IReadOnlyCollection<IncidenciaCarga> Incidencias => _incidencias.AsReadOnly();

    public ICollection<ItemInventario> ItemsGenerados { get; private set; } = new List<ItemInventario>();

    private CargaInventario() { }

    public CargaInventario(Guid empresaId, string nombreArchivo)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        NombreArchivo = nombreArchivo;
        FechaCarga = DateTime.UtcNow;
    }

    public void RegistrarFilaValida()
    {
        FilasTotales++;
        FilasValidas++;
    }

    /// <summary>
    /// Registra una fila inconsistente sin interrumpir el procesamiento del resto,
    /// conforme al criterio de aceptación de RF-02.
    /// </summary>
    public void RegistrarIncidencia(int numeroFila, string campo, string? valorOriginal,
                                    string tipoError, string mensaje)
    {
        FilasTotales++;
        FilasRechazadas++;
        _incidencias.Add(new IncidenciaCarga(Id, numeroFila, campo, valorOriginal, tipoError, mensaje));
    }

    public decimal TasaExito() =>
        FilasTotales == 0 ? 0 : Math.Round((decimal)FilasValidas / FilasTotales * 100, 1);
}
