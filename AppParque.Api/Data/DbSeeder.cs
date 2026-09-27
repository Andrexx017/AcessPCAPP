using System.Text.Json;
using System.Text.Json.Serialization;
using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;
using AppParque.Api.Modules.CatalogoPreguntas.Domain;
using AppParque.Api.Modules.Identidad.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Data;

/// <summary>
/// Carga inicial de catálogos, usuarios y atracciones a partir de Data/Seed/admin.json
/// (copia de AppParque/DB_data/admin.JSON, la data real usada por el prototipo).
/// Idempotente: no hace nada si ya hay usuarios en la base de datos.
/// </summary>
public static class DbSeeder
{
    // Grupo/Condicion no vienen con nombre en el admin.json (solo el código como clave de un bool),
    // así que el catálogo con nombre/descripción se define aquí a partir de la sección 5 del
    // documento de requisitos. Pendiente de confirmar con el conocimiento del negocio (ver
    // Requierements/Requisitos-App-Evaluacion-Accesibilidad.md, sección 9).
    private static readonly (string Codigo, string Nombre, string? Descripcion)[] GruposCatalogo =
    [
        ("grupo_1", "Grupo 1", "No puede caminar sin apoyo"),
        ("grupo_1_1", "Grupo 1.1", "Camina sin apoyo, sube escaleras sin ayuda, sin capacidad de agarre"),
        ("grupo_2", "Grupo 2", "Camina sin apoyo, agarre en una sola mano"),
        ("grupo_3", "Grupo 3", "Sube escaleras con ayuda, agarre en una sola mano"),
        ("grupo_4", "Grupo 4", "Sube escaleras con ayuda, agarre en ambas manos"),
        ("grupo_5", "Grupo 5", "Camina y sube sin ayuda, agarre en una mano, sin prótesis de pierna"),
        ("grupo_5_1", "Grupo 5.1", "Camina y sube sin ayuda, agarre en ambas manos, con prótesis de pierna (bajo rodilla o ambas)"),
        ("grupo_5_2", "Grupo 5.2", "Camina y sube sin ayuda, con prótesis por encima de la rodilla"),
        ("grupo_10", "Grupo 10", "Discapacidad certificada por alteración del sistema nervioso o trastorno mental severo"),
        ("protesis_mano", "Prótesis de mano", "Tiene prótesis en una o ambas manos"),
        ("protesis_pierna_arriba_rodilla", "Prótesis de pierna sobre rodilla", "Tiene prótesis de pierna por encima de la rodilla"),
    ];

    private static readonly (string Codigo, string Nombre)[] CondicionesCatalogo =
    [
        ("discapacidad_visual", "Discapacidad visual"),
        ("discapacidad_auditiva", "Discapacidad auditiva"),
        ("discapacidad_cognitiva", "Discapacidad cognitiva"),
        ("embarazo", "Embarazo"),
        ("problemas_cardiacos", "Problemas cardíacos"),
        ("problemas_columna", "Problemas de columna"),
        ("mareo_vertigo", "Mareo / vértigo"),
        ("miedo_espacios_cerrados", "Miedo a espacios cerrados"),
    ];

    // Las 10 preguntas del test guiado, portadas tal cual de TestViewModel.MapAnswersToRestrictions
    // (AppParque/Features/EvaluacionAccesibilidad) — mismo texto y mismas opciones que el prototipo,
    // con un Codigo estable por opción que es lo que consume MotorReglas (no el texto ni la posición).
    // "cond_cirugia_reciente" y "cond_ninguna" no activan ningún grupo/condición: así era en el
    // motor original (esas dos opciones nunca tuvieron efecto), se preserva tal cual.
    private static readonly (string Texto, TipoPregunta Tipo, (string Texto, string Codigo)[] Opciones)[] PreguntasCatalogo =
    [
        ("¿Puede caminar sin ningún tipo de apoyo como bastón, muletas o prótesis?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "camina_sin_apoyo"),
            ("No", "no_camina"),
        ]),
        ("¿Tiene prótesis en las piernas?", TipoPregunta.UnicaSeleccion,
        [
            ("No", "sin_protesis_pierna"),
            ("Sí, por debajo de rodilla", "protesis_pierna_debajo_rodilla"),
            ("Sí, por encima de rodilla", "protesis_pierna_arriba_rodilla"),
            ("Ambas", "protesis_pierna_ambas"),
        ]),
        ("¿Puede subir y bajar escaleras sin ayuda?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "sube_sin_ayuda"),
            ("Sí, con ayuda (bastón/muleta)", "sube_con_ayuda"),
            ("No", "no_sube"),
        ]),
        ("¿Tiene capacidad de agarre en ambas manos?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "agarre_ambas_manos"),
            ("Solo una mano", "agarre_una_mano"),
            ("Ninguna", "sin_agarre"),
        ]),
        ("¿Tiene alguna ausencia o prótesis de brazo o mano?", TipoPregunta.UnicaSeleccion,
        [
            ("No", "sin_protesis_mano"),
            ("Prótesis en una mano", "protesis_mano_una"),
            ("Prótesis en ambas manos", "protesis_mano_ambas"),
        ]),
        ("¿Tiene discapacidad visual total?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "discapacidad_visual_total"),
            ("No", "sin_discapacidad_visual"),
            ("Parcial", "discapacidad_visual_parcial"),
        ]),
        ("¿Tiene discapacidad auditiva total?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "discapacidad_auditiva_si"),
            ("No", "discapacidad_auditiva_no"),
        ]),
        ("¿Tiene alguna condición cognitiva diagnosticada o dificultad para entender instrucciones complejas?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "discapacidad_cognitiva_si"),
            ("No", "discapacidad_cognitiva_no"),
        ]),
        ("¿Tiene una discapacidad certificada por alteración del sistema nervioso o trastornos mentales severos?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "grupo10_si"),
            ("No", "grupo10_no"),
        ]),
        ("¿Tiene actualmente alguna de las siguientes condiciones médicas? (Marca todas las que apliquen)", TipoPregunta.SeleccionMultiple,
        [
            ("Embarazo", "cond_embarazo"),
            ("Problemas cardíacos, presión alta, marcapasos", "cond_cardiacos"),
            ("Cirugías recientes, yesos o lesiones", "cond_cirugia_reciente"),
            ("Problemas de cuello, columna o huesos", "cond_columna"),
            ("Mareo, vértigo o miedo a las alturas", "cond_mareo_vertigo"),
            ("Miedo a espacios cerrados", "cond_miedo_espacios_cerrados"),
            ("Ninguna", "cond_ninguna"),
        ]),
    ];

    public static async Task SeedAsync(AppDbContext db, string contentRootPath)
    {
        if (await db.Usuarios.AnyAsync())
            return;

        var grupos = GruposCatalogo
            .Select(g => new Grupo { Codigo = g.Codigo, Nombre = g.Nombre, Descripcion = g.Descripcion })
            .ToList();
        db.Grupos.AddRange(grupos);

        var condiciones = CondicionesCatalogo
            .Select(c => new Condicion { Codigo = c.Codigo, Nombre = c.Nombre })
            .ToList();
        db.Condiciones.AddRange(condiciones);

        var ordenPregunta = 1;
        foreach (var p in PreguntasCatalogo)
        {
            var pregunta = new Pregunta { Texto = p.Texto, Tipo = p.Tipo, Orden = ordenPregunta++, Activa = true };

            var ordenOpcion = 1;
            foreach (var (textoOpcion, codigo) in p.Opciones)
                pregunta.Opciones.Add(new OpcionRespuesta { Texto = textoOpcion, Codigo = codigo, Orden = ordenOpcion++ });

            db.Preguntas.Add(pregunta);
        }

        await db.SaveChangesAsync();

        var jsonPath = Path.Combine(contentRootPath, "Data", "Seed", "admin.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        var seed = JsonSerializer.Deserialize<SeedRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new InvalidOperationException($"No se pudo leer el archivo de datos semilla: {jsonPath}");

        foreach (var user in seed.Users.Values)
        {
            db.Usuarios.Add(new Usuario
            {
                NombreCompleto = user.NurseName,
                Username = user.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                Rol = user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? RolUsuario.Admin : RolUsuario.Enfermero,
                Activo = true,
            });
        }

        var grupoPorCodigo = grupos.ToDictionary(g => g.Codigo);
        var condicionPorCodigo = condiciones.ToDictionary(c => c.Codigo);

        foreach (var a in seed.Atracciones.Values)
        {
            var atraccion = new Atraccion
            {
                Nombre = a.Nombre,
                Descripcion = a.Descripcion,
                ImagenUrl = a.ImagenUrl,
                AlturaMinima = a.AlturaMinima,
                AlturaMaxima = a.AlturaMaxima,
                Activa = true,
            };

            foreach (var (codigo, permitido) in a.Restricciones.Grupos)
            {
                if (grupoPorCodigo.TryGetValue(codigo, out var grupo))
                    atraccion.RestriccionesGrupo.Add(new AtraccionGrupoRestriccion { Grupo = grupo, Permitido = permitido });
            }

            foreach (var (codigo, permitido) in a.Restricciones.Condiciones)
            {
                if (condicionPorCodigo.TryGetValue(codigo, out var condicion))
                    atraccion.RestriccionesCondicion.Add(new AtraccionCondicionRestriccion { Condicion = condicion, Permitido = permitido });
            }

            db.Atracciones.Add(atraccion);
        }

        await db.SaveChangesAsync();
    }

    private sealed class SeedRoot
    {
        public Dictionary<string, SeedUser> Users { get; set; } = new();
        public Dictionary<string, SeedAtraccion> Atracciones { get; set; } = new();
    }

    private sealed class SeedUser
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string NurseName { get; set; } = null!;
    }

    private sealed class SeedAtraccion
    {
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }

        [JsonPropertyName("altura_minima")]
        public int? AlturaMinima { get; set; }

        [JsonPropertyName("altura_maxima")]
        public int? AlturaMaxima { get; set; }

        [JsonPropertyName("imagenURL")]
        public string? ImagenUrl { get; set; }

        public SeedRestricciones Restricciones { get; set; } = new();
    }

    private sealed class SeedRestricciones
    {
        public Dictionary<string, bool> Grupos { get; set; } = new();
        public Dictionary<string, bool> Condiciones { get; set; } = new();
    }
}
