using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionRespuestaConfiguration : IEntityTypeConfiguration<EvaluacionRespuesta>
{
    public void Configure(EntityTypeBuilder<EvaluacionRespuesta> builder)
    {
        builder.HasKey(x => new { x.EvaluacionId, x.OpcionRespuestaId });

        builder.HasOne(x => x.Evaluacion)
            .WithMany(ev => ev.Respuestas)
            .HasForeignKey(x => x.EvaluacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OpcionRespuesta)
            .WithMany(o => o.EvaluacionRespuestas)
            .HasForeignKey(x => x.OpcionRespuestaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
