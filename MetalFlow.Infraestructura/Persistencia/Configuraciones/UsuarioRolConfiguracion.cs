using MetalFlow.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetalFlow.Infraestructura.Persistencia.Configuraciones;

public class UsuarioRolConfiguracion : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> b)
    {
        b.ToTable("USUARIO_ROL");

        // Clave primaria compuesta: impide asignar dos veces el mismo rol
        b.HasKey(ur => new { ur.UsuarioId, ur.Rol });

        b.Property(ur => ur.Rol).HasConversion<byte>().IsRequired();

        // La relación se declara desde UsuarioConfiguracion
    }
}
