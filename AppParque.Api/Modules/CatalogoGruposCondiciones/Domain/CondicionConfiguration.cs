using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

public class CondicionConfiguration : IEntityTypeConfiguration<Condicion>
{
    public void Configure(EntityTypeBuilder<Condicion> builder)
    {
        builder.HasIndex(c => c.Codigo).IsUnique();
    }
}
