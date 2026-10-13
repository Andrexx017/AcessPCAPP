using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.CatalogoZonas.Domain;

public class ZonaCorporalConfiguration : IEntityTypeConfiguration<ZonaCorporal>
{
    public void Configure(EntityTypeBuilder<ZonaCorporal> builder)
    {
        builder.HasIndex(z => z.Codigo).IsUnique();
        builder.Property(z => z.Codigo).HasMaxLength(40);
    }
}
