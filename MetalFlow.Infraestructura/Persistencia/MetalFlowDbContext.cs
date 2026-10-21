using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace MetalFlow.Infraestructura.Persistencia;

/// <summary>
/// Contexto de persistencia de MetalFlow. Las configuraciones de cada entidad
/// se declaran mediante la API fluida en clases separadas (carpeta Configuraciones),
/// de modo que el modelo de dominio permanezca libre de anotaciones de infraestructura.
/// </summary>
public class MetalFlowDbContext : DbContext
{
    public MetalFlowDbContext(DbContextOptions<MetalFlowDbContext> options) : base(options) { }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Material> Materiales => Set<Material>();
    public DbSet<ItemInventario> Items => Set<ItemInventario>();
    public DbSet<Anuncio> Anuncios => Set<Anuncio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<CargaInventario> Cargas => Set<CargaInventario>();
    public DbSet<IncidenciaCarga> Incidencias => Set<IncidenciaCarga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MetalFlowDbContext).Assembly);
        DatosSemilla.Aplicar(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
