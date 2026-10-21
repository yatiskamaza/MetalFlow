using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("USUARIO");
        b.HasKey(u => u.Id);

        b.Property(u => u.Email).HasMaxLength(150).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
        b.Property(u => u.FechaAlta).HasColumnType("datetime2").IsRequired();

        // RF-01: unicidad del identificador de acceso
        b.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_USUARIO_EMAIL");

        // Garantiza de forma declarativa que una empresa no tenga más de una cuenta.
        // El filtro excluye los nulos para admitir la cuenta de administrador.
        b.HasIndex(u => u.EmpresaId)
         .IsUnique()
         .HasFilter("[empresa_id] IS NOT NULL")
         .HasDatabaseName("UQ_USUARIO_EMPRESA");

        b.Property(u => u.EmpresaId).HasColumnName("empresa_id");

        b.HasMany(u => u.Roles)
         .WithOne(ur => ur.Usuario)
         .HasForeignKey(ur => ur.UsuarioId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(Usuario.Roles))!
         .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
