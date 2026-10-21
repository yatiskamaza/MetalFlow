using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class MaterialConfiguracion : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> b)
    {
        b.ToTable("MATERIAL");
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).ValueGeneratedOnAdd();

        b.Property(m => m.Familia).HasMaxLength(50).IsRequired();
        b.Property(m => m.Designacion).HasMaxLength(50).IsRequired();
        b.Property(m => m.Norma).HasMaxLength(50);

        // RD-03: una misma aleación no puede registrarse con grafías distintas
        b.HasIndex(m => m.Designacion).IsUnique().HasDatabaseName("UQ_MATERIAL_DESIGNACION");
    }
}
