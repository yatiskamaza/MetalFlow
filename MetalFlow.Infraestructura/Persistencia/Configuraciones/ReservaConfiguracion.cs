using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class ReservaConfiguracion : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> b)
    {
        b.ToTable("RESERVA");
        b.HasKey(r => r.Id);

        b.Property(r => r.FechaReserva).HasColumnType("datetime2").IsRequired();
        b.Property(r => r.FechaExpiracion).HasColumnType("datetime2").IsRequired();
        b.Property(r => r.Estado).HasConversion<byte>().IsRequired();

        // Restringe el borrado para preservar el histórico de operaciones
        b.HasOne(r => r.Item)
         .WithMany(i => i.Reservas)
         .HasForeignKey(r => r.ItemId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(r => r.EmpresaCompradora)
         .WithMany(e => e.Reservas)
         .HasForeignKey(r => r.EmpresaCompradoraId)
         .OnDelete(DeleteBehavior.Restrict);

        // Permite comprobar de forma eficiente si un ítem tiene reservas activas
        b.HasIndex(r => new { r.ItemId, r.Estado }).HasDatabaseName("IX_RESERVA_ITEM_ESTADO");
    }
}
