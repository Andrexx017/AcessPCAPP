using AppParque.Api.Modules.Asistente.Domain;
using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;
using AppParque.Api.Modules.CatalogoPreguntas.Domain;
using AppParque.Api.Modules.CatalogoZonas.Domain;
using AppParque.Api.Modules.Evaluaciones.Domain;
using AppParque.Api.Modules.Identidad.Domain;
using AppParque.Api.Modules.Visitantes.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Data;

/// <summary>
/// Implementa IDataProtectionKeyContext para que las claves de cifrado de Data Protection (las
/// que protegen, entre otras cosas, las API keys de ProveedorIA) se guarden en Postgres y no en el
/// filesystem del contenedor — si no, cada rebuild del contenedor generaría claves nuevas y las
/// API keys ya guardadas quedarían indescifrables. Ver Program.cs: AddDataProtection().
/// </summary>
public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Visitante> Visitantes => Set<Visitante>();
    public DbSet<Pregunta> Preguntas => Set<Pregunta>();
    public DbSet<OpcionRespuesta> OpcionesRespuesta => Set<OpcionRespuesta>();
    public DbSet<ZonaCorporal> ZonasCorporales => Set<ZonaCorporal>();
    public DbSet<PreguntaCapacidad> PreguntasCapacidad => Set<PreguntaCapacidad>();
    public DbSet<OpcionCapacidad> OpcionesCapacidad => Set<OpcionCapacidad>();
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
    public DbSet<DiagnosticoGuardado> DiagnosticosGuardados => Set<DiagnosticoGuardado>();
    public DbSet<ProveedorIA> ProveedoresIA => Set<ProveedorIA>();
    public DbSet<DocumentoContexto> DocumentosContexto => Set<DocumentoContexto>();
    public DbSet<PromptIA> PromptsIA => Set<PromptIA>();
    public DbSet<MensajeSesion> MensajesSesion => Set<MensajeSesion>();
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cada módulo trae su propia configuración junto a su entidad (IEntityTypeConfiguration<T>
        // en Modules/<Modulo>/Domain/), así este DbContext no crece con cada módulo nuevo.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
