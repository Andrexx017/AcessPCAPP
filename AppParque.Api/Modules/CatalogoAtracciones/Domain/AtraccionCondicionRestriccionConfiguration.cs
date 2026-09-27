using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain;

public class AtraccionCondicionRestriccionConfiguration : IEntityTypeConfiguration<AtraccionCondicionRestriccion>
{
    public void Configure(EntityTypeBuilder<AtraccionCondicionRestriccion> builder)
    {
        builder.HasKey(x => new { x.AtraccionId, x.CondicionId });

        builder.HasOne(x => x.Atraccion)
            .WithMany(a => a.RestriccionesCondicion)
            .HasForeignKey(x => x.AtraccionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Condicion)
            .WithMany(c => c.AtraccionRestricciones)
            .HasForeignKey(x => x.CondicionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
