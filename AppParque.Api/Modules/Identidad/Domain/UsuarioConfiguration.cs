using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Identidad.Domain;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
    }
}
