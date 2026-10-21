using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class AnuncioConfiguracion : IEntityTypeConfiguration<Anuncio>
{
    public void Configure(EntityTypeBuilder<Anuncio> b)
    {
        b.ToTable("ANUNCIO", t =>
            t.HasCheckConstraint("CK_ANUNCIO_PRECIO", "[Precio] >= 0"));

        b.HasKey(a => a.Id);

        b.Property(a => a.Titulo).HasMaxLength(150).IsRequired();
        b.Property(a => a.Descripcion).HasMaxLength(1000);
        b.Property(a => a.Precio).HasColumnType("decimal(10,2)").IsRequired();
        b.Property(a => a.FechaPublicacion).HasColumnType("datetime2").IsRequired();

        // 1:0..1 — un ítem no puede tener más de un anuncio
        b.HasOne(a => a.Item)
         .WithOne(i => i.Anuncio)
         .HasForeignKey<Anuncio>(a => a.ItemId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(a => a.ItemId).IsUnique().HasDatabaseName("UQ_ANUNCIO_ITEM");
    }
}