using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class CargaInventarioConfiguracion : IEntityTypeConfiguration<CargaInventario>
{
    public void Configure(EntityTypeBuilder<CargaInventario> b)
    {
        b.ToTable("CARGA_INVENTARIO");
        b.HasKey(c => c.Id);

        b.Property(c => c.NombreArchivo).HasMaxLength(255).IsRequired();
        b.Property(c => c.FechaCarga).HasColumnType("datetime2").IsRequired();

        // Se restringe el borrado en cascada desde EMPRESA por dos motivos.
        // En el plano técnico, la cascada generaría dos trayectorias de borrado
        // hacia ITEM_INVENTARIO —una directa desde EMPRESA y otra a través de
        // esta entidad—, situación que el gestor no admite.
        // En el plano del dominio, el registro de carga constituye la evidencia
        // de procedencia del inventario exigida por RD-04, por lo que su
        // eliminación debe ser una acción deliberada y no un efecto derivado
        // del borrado de la empresa.
        b.HasOne(c => c.Empresa)
            .WithMany(e => e.Cargas)
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(c => c.Incidencias)
            .WithOne(i => i.Carga)
            .HasForeignKey(i => i.CargaId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(CargaInventario.Incidencias))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}