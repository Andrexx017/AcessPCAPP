using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionGrupoConfiguration : IEntityTypeConfiguration<EvaluacionGrupo>
{
    public void Configure(EntityTypeBuilder<EvaluacionGrupo> builder)
    {
        builder.HasKey(x => new { x.EvaluacionId, x.GrupoId });

        builder.HasOne(x => x.Evaluacion)
            .WithMany(ev => ev.Grupos)
            .HasForeignKey(x => x.EvaluacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Grupo)
            .WithMany(g => g.EvaluacionGrupos)
            .HasForeignKey(x => x.GrupoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
