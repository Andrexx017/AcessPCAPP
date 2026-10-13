using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppParque.Api.Modules.Asistente.Domain;

public class ProveedorIAConfiguration : IEntityTypeConfiguration<ProveedorIA>
{
    public void Configure(EntityTypeBuilder<ProveedorIA> builder)
    {
        builder.Property(p => p.Nombre).HasMaxLength(120);
        builder.Property(p => p.BaseUrl).HasMaxLength(300);
        builder.Property(p => p.Modelo).HasMaxLength(120);
        builder.HasIndex(p => p.Prioridad);
    }
}

public class DocumentoContextoConfiguration : IEntityTypeConfiguration<DocumentoContexto>
{
    public void Configure(EntityTypeBuilder<DocumentoContexto> builder)
    {
        builder.Property(d => d.Nombre).HasMaxLength(200);
    }
}

public class PromptIAConfiguration : IEntityTypeConfiguration<PromptIA>
{
    public void Configure(EntityTypeBuilder<PromptIA> builder)
    {
        builder.Property(p => p.Clave).HasMaxLength(40);
        builder.Property(p => p.Titulo).HasMaxLength(120);
        builder.HasIndex(p => p.Clave).IsUnique();
    }
}

public class MensajeSesionConfiguration : IEntityTypeConfiguration<MensajeSesion>
{
    public void Configure(EntityTypeBuilder<MensajeSesion> builder)
    {
        builder.Property(m => m.Rol).HasMaxLength(20);
        builder.HasIndex(m => new { m.SesionId, m.CreadoEn });
    }
}
