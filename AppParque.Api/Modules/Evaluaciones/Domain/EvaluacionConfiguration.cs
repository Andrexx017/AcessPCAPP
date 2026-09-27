using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionConfiguration : IEntityTypeConfiguration<Evaluacion>
{
    public void Configure(EntityTypeBuilder<Evaluacion> builder)
    {
        builder.HasOne(ev => ev.Visitante)
            .WithMany(v => v.Evaluaciones)
            .HasForeignKey(ev => ev.VisitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ev => ev.Usuario)
            .WithMany(u => u.Evaluaciones)
            .HasForeignKey(ev => ev.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
