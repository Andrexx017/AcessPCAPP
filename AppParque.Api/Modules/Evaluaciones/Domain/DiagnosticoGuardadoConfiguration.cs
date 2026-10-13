using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class DiagnosticoGuardadoConfiguration : IEntityTypeConfiguration<DiagnosticoGuardado>
{
    public void Configure(EntityTypeBuilder<DiagnosticoGuardado> builder)
    {
        builder.HasIndex(d => d.Fecha);

        builder.HasOne(d => d.Visitante)
            .WithMany()
            .HasForeignKey(d => d.VisitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Usuario)
            .WithMany()
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
