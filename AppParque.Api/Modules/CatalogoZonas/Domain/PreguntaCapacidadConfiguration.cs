using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoZonas.Domain;

public class PreguntaCapacidadConfiguration : IEntityTypeConfiguration<PreguntaCapacidad>
{
    public void Configure(EntityTypeBuilder<PreguntaCapacidad> builder)
    {
        builder.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Clave).HasMaxLength(40);

        builder.HasIndex(p => new { p.ZonaId, p.Clave }).IsUnique();

        builder.HasOne(p => p.Zona)
            .WithMany(z => z.Preguntas)
            .HasForeignKey(p => p.ZonaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
