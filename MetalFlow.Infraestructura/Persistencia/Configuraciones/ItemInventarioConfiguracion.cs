using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class ItemInventarioConfiguracion : IEntityTypeConfiguration<ItemInventario>
{
    public void Configure(EntityTypeBuilder<ItemInventario> b)
    {
        b.ToTable("ITEM_INVENTARIO", t =>
        {
            // Restricciones de comprobación (§4.5.2)
            t.HasCheckConstraint("CK_ITEM_DIMENSIONES",
                "[LargoMm] > 0 AND [AnchoMm] > 0 AND [EspesorMm] > 0");
            t.HasCheckConstraint("CK_ITEM_CANTIDAD", "[Cantidad] > 0");
        });

        b.HasKey(i => i.Id);

        // Dimensiones obligatorias y normalizadas en milímetros (RD-03)
        b.Property(i => i.LargoMm).HasColumnType("decimal(9,2)").IsRequired();
        b.Property(i => i.AnchoMm).HasColumnType("decimal(9,2)").IsRequired();
        b.Property(i => i.EspesorMm).HasColumnType("decimal(7,2)").IsRequired();
        b.Property(i => i.PesoKg).HasColumnType("decimal(9,2)");
        b.Property(i => i.Cantidad).IsRequired();
        b.Property(i => i.Ubicacion).HasMaxLength(100);
        b.Property(i => i.FechaAlta).HasColumnType("datetime2").IsRequired();

        b.Property(i => i.Estado).HasConversion<byte>().IsRequired();

        // RD-01: la clasificación regulatoria es de cumplimentación obligatoria
        b.Property(i => i.Clasificacion).HasConversion<byte>().IsRequired();

        b.HasOne(i => i.Empresa)
         .WithMany(e => e.Inventario)
         .HasForeignKey(i => i.EmpresaId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(i => i.Material)
         .WithMany(m => m.Items)
         .HasForeignKey(i => i.MaterialId)
         .OnDelete(DeleteBehavior.Restrict);

        // N:0..1 — nulo en las altas manuales (RF-03), con valor en las del micro-ETL (RF-02)
        b.HasOne(i => i.Carga)
         .WithMany(c => c.ItemsGenerados)
         .HasForeignKey(i => i.CargaId)
         .IsRequired(false)
         .OnDelete(DeleteBehavior.SetNull);

        // ---------------------------------------------------------------
        // Índice principal del motor de búsqueda (RF-04, RNF-01, CT-05).
        // El orden de las columnas no es arbitrario: el estado encabeza el
        // índice por ser una condición de igualdad de alta selectividad, y le
        // sigue el espesor, criterio de filtrado más habitual y de mayor poder
        // discriminante. Las columnas incluidas evitan accesos adicionales a
        // la tabla al resolver la consulta de listado.
        // ---------------------------------------------------------------
        b.HasIndex(i => new { i.Estado, i.EspesorMm, i.LargoMm, i.AnchoMm })
         .HasDatabaseName("IX_ITEM_BUSQUEDA_PARAMETRICA")
         .IncludeProperties(i => new { i.MaterialId, i.EmpresaId, i.Cantidad });

        b.HasIndex(i => i.MaterialId).HasDatabaseName("IX_ITEM_MATERIAL");
        b.HasIndex(i => i.EmpresaId).HasDatabaseName("IX_ITEM_EMPRESA");
    }
}
