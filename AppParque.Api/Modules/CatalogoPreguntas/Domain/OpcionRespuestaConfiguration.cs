using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoPreguntas.Domain;

public class OpcionRespuestaConfiguration : IEntityTypeConfiguration<OpcionRespuesta>
{
    public void Configure(EntityTypeBuilder<OpcionRespuesta> builder)
    {
        builder.HasIndex(o => o.Codigo).IsUnique();

        builder.HasOne(o => o.Pregunta)
            .WithMany(p => p.Opciones)
            .HasForeignKey(o => o.PreguntaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
