using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Visitantes.Domain;

public class VisitanteConfiguration : IEntityTypeConfiguration<Visitante>
{
    public void Configure(EntityTypeBuilder<Visitante> builder)
    {
        builder.HasIndex(v => new { v.TipoDocumento, v.NumeroDocumento }).IsUnique();
    }
}
