using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class EmpresaConfiguracion : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("EMPRESA");
        b.HasKey(e => e.Id);

        b.Property(e => e.Nif).HasMaxLength(15).IsRequired();
        b.Property(e => e.RazonSocial).HasMaxLength(150).IsRequired();
        b.Property(e => e.Direccion).HasMaxLength(200).IsRequired();
        b.Property(e => e.Localidad).HasMaxLength(100).IsRequired();
        b.Property(e => e.EmailContacto).HasMaxLength(150).IsRequired();
        b.Property(e => e.TelefonoContacto).HasMaxLength(20);
        b.Property(e => e.FechaAlta).HasColumnType("datetime2").IsRequired();

        // RD-02: unicidad del identificador fiscal
        b.HasIndex(e => e.Nif).IsUnique().HasDatabaseName("UQ_EMPRESA_NIF");

        // 1:1 — toda empresa tiene exactamente una cuenta, creada en CU-01
        b.HasOne(e => e.Cuenta)
         .WithOne(u => u.Empresa)
         .HasForeignKey<Usuario>(u => u.EmpresaId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
