using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionCondicionConfiguration : IEntityTypeConfiguration<EvaluacionCondicion>
{
    public void Configure(EntityTypeBuilder<EvaluacionCondicion> builder)
    {
        builder.HasKey(x => new { x.EvaluacionId, x.CondicionId });

        builder.HasOne(x => x.Evaluacion)
            .WithMany(ev => ev.Condiciones)
            .HasForeignKey(x => x.EvaluacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Condicion)
            .WithMany(c => c.EvaluacionCondiciones)
            .HasForeignKey(x => x.CondicionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
