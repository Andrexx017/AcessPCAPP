using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;
using AppParque.Api.Modules.CatalogoPreguntas.Domain;
using AppParque.Api.Modules.Evaluaciones.Domain;
using AppParque.Api.Modules.Identidad.Domain;
using AppParque.Api.Modules.Visitantes.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Visitante> Visitantes => Set<Visitante>();
    public DbSet<Pregunta> Preguntas => Set<Pregunta>();
    public DbSet<OpcionRespuesta> OpcionesRespuesta => Set<OpcionRespuesta>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Condicion> Condiciones => Set<Condicion>();
    public DbSet<Atraccion> Atracciones => Set<Atraccion>();
    public DbSet<AtraccionGrupoRestriccion> AtraccionGrupoRestricciones => Set<AtraccionGrupoRestriccion>();
    public DbSet<AtraccionCondicionRestriccion> AtraccionCondicionRestricciones => Set<AtraccionCondicionRestriccion>();
    public DbSet<Evaluacion> Evaluaciones => Set<Evaluacion>();
    public DbSet<EvaluacionRespuesta> EvaluacionRespuestas => Set<EvaluacionRespuesta>();
    public DbSet<EvaluacionGrupo> EvaluacionGrupos => Set<EvaluacionGrupo>();
    public DbSet<EvaluacionCondicion> EvaluacionCondiciones => Set<EvaluacionCondicion>();
    public DbSet<EvaluacionAtraccionResultado> EvaluacionAtraccionResultados => Set<EvaluacionAtraccionResultado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cada módulo trae su propia configuración junto a su entidad (IEntityTypeConfiguration<T>
        // en Modules/<Modulo>/Domain/), así este DbContext no crece con cada módulo nuevo.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
