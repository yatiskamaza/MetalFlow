namespace MetalFlow.Aplicacion.Dtos;

/// <summary>
/// Página de resultados de una búsqueda del catálogo, con los datos de
/// paginación que la interfaz precisa para componer la navegación.
/// </summary>
public class ResultadoBusquedaDto
{
    public IReadOnlyList<ItemCatalogoDto> Resultados { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
}