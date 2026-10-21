using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class IncidenciaCargaConfiguracion : IEntityTypeConfiguration<IncidenciaCarga>
{
    public void Configure(EntityTypeBuilder<IncidenciaCarga> b)
    {
        b.ToTable("INCIDENCIA_CARGA");
        b.HasKey(i => i.Id);

        b.Property(i => i.Campo).HasMaxLength(50).IsRequired();
        b.Property(i => i.ValorOriginal).HasMaxLength(255);
        b.Property(i => i.TipoError).HasMaxLength(50).IsRequired();
        b.Property(i => i.Mensaje).HasMaxLength(255).IsRequired();

        // La relación se declara desde CargaInventarioConfiguracion

        b.HasIndex(i => i.CargaId).HasDatabaseName("IX_INCIDENCIA_CARGA");
    }
}
