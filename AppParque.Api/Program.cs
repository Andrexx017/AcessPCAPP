
using System.Text;
using AppParque.Api.Data;
using AppParque.Api.Modules.CatalogoAtracciones.Api;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Api;
using AppParque.Api.Modules.CatalogoPreguntas.Api;
using AppParque.Api.Modules.Evaluaciones.Api;
using AppParque.Api.Modules.Identidad.Api;
using AppParque.Api.Modules.Identidad.Application;
using AppParque.Api.Modules.Visitantes.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AppParque.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Key (appsettings, user-secrets o variable de entorno Jwt__Key).");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        builder.Services.AddScoped<ITokenService, TokenService>();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Aplica migraciones pendientes y carga datos semilla (catálogos, usuarios, atracciones)
        // al arrancar. Es idempotente: DbSeeder no hace nada si ya hay usuarios en la BD.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            await DbSeeder.SeedAsync(db, app.Environment.ContentRootPath);
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // TLS se termina en el reverse proxy (Caddy) delante del contenedor, no aquí dentro.
        app.UseAuthentication();
        app.UseAuthorization();

        // Sin autenticación: lo usa el cliente MAUI al arrancar solo para confirmar que hay red hacia la API.
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.MapAuthEndpoints();
        app.MapPreguntasEndpoints();
        app.MapVisitantesEndpoints();
        app.MapEvaluacionesEndpoints();
        app.MapGruposEndpoints();
        app.MapCondicionesEndpoints();
        app.MapAtraccionesEndpoints();
        app.MapPreguntasAdminEndpoints();
        app.MapUsuariosAdminEndpoints();
        app.MapPerfilEndpoints();

        await app.RunAsync();
    }
}
