using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain;

public class AtraccionGrupoRestriccionConfiguration : IEntityTypeConfiguration<AtraccionGrupoRestriccion>
{
    public void Configure(EntityTypeBuilder<AtraccionGrupoRestriccion> builder)
    {
        builder.HasKey(x => new { x.AtraccionId, x.GrupoId });

        builder.HasOne(x => x.Atraccion)
            .WithMany(a => a.RestriccionesGrupo)
            .HasForeignKey(x => x.AtraccionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Grupo)
            .WithMany(g => g.AtraccionRestricciones)
            .HasForeignKey(x => x.GrupoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
