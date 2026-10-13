using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoZonas.Domain;

public class OpcionCapacidadConfiguration : IEntityTypeConfiguration<OpcionCapacidad>
{
    public void Configure(EntityTypeBuilder<OpcionCapacidad> builder)
    {
        builder.HasIndex(o => new { o.PreguntaId, o.Valor }).IsUnique();

        builder.HasOne(o => o.Pregunta)
            .WithMany(p => p.Opciones)
            .HasForeignKey(o => o.PreguntaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
