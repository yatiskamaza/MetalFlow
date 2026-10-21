namespace MetalFlow.Dominio.Entidades;

/// <summary>
/// Fila rechazada durante el procesamiento de un archivo (RF-02).
/// Da soporte al informe de incidencias que el usuario consulta para corregir
/// el archivo de origen (CU-05).
/// </summary>
public class IncidenciaCarga
{
    public Guid Id { get; private set; }

    public Guid CargaId { get; private set; }
    public CargaInventario Carga { get; private set; } = null!;

    public int NumeroFila { get; private set; }
    public string Campo { get; private set; } = null!;
    public string? ValorOriginal { get; private set; }
    public string TipoError { get; private set; } = null!;
    public string Mensaje { get; private set; } = null!;

    private IncidenciaCarga() { }

    public IncidenciaCarga(Guid cargaId, int numeroFila, string campo, string? valorOriginal,
                           string tipoError, string mensaje)
    {
        Id = Guid.NewGuid();
        CargaId = cargaId;
        NumeroFila = numeroFila;
        Campo = campo;
        ValorOriginal = valorOriginal;
        TipoError = tipoError;
        Mensaje = mensaje;
    }
}
