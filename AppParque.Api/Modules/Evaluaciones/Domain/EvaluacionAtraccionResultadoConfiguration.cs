using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionAtraccionResultadoConfiguration : IEntityTypeConfiguration<EvaluacionAtraccionResultado>
{
    public void Configure(EntityTypeBuilder<EvaluacionAtraccionResultado> builder)
    {
        builder.HasIndex(x => new { x.EvaluacionId, x.AtraccionId }).IsUnique();

        builder.HasOne(x => x.Evaluacion)
            .WithMany(ev => ev.AtraccionResultados)
            .HasForeignKey(x => x.EvaluacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Atraccion)
            .WithMany(a => a.EvaluacionResultados)
            .HasForeignKey(x => x.AtraccionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
