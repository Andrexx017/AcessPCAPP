using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AppParque.Api.Data;
using AppParque.Api.Modules.Asistente.Domain;
using AppParque.Api.Modules.CatalogoAtracciones.Domain.Requisitos;
using AppParque.Api.Modules.Evaluaciones.Api;
using AppParque.Api.Modules.Evaluaciones.Application;
using AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;
using AppParque.Api.Modules.InformacionParque.Application;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Asistente.Application;

public record MensajeEntrada(string Rol, string Contenido);

public record ResultadoChat(string Respuesta, string ModeloUsado, Guid SesionId);

/// <summary>
/// Orquesta la conversación: recorre los proveedores activos en orden de prioridad (la cadena de
/// fallback), y dentro de cada uno corre el ciclo de tool calling hasta que el modelo responde con
/// texto en vez de pedir otra herramienta. Ver PromptAsistente para el porqué del diseño.
/// </summary>
public class AsistenteOrquestador(
    IHttpClientFactory httpClientFactory,
    AppDbContext db,
    ILogger<AsistenteOrquestador> logger,
    IDataProtectionProvider dataProtectionProvider,
    CatalogoRequisitos catalogo,
    HorarioParqueService horarioParqueService)
{
    private const string PropositoProteccion = "AsistenteIA.ApiKeys.v1";
    private const int MaxLlamadasHerramientas = 8;

    /// <summary>"Memoria" del chat: mientras el último mensaje de la sesión tenga menos de esto,
    /// el siguiente mensaje ve el historial. No es un borrado explícito — es el filtro de
    /// CargarOCrearSesionAsync. Pasado esto, la siguiente pregunta arranca una sesión nueva.</summary>
    private static readonly TimeSpan VentanaSesion = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Cifrar(string apiKeyPlana) =>
        dataProtectionProvider.CreateProtector(PropositoProteccion).Protect(apiKeyPlana);

    private string Descifrar(string apiKeyCifrada) =>
        dataProtectionProvider.CreateProtector(PropositoProteccion).Unprotect(apiKeyCifrada);

    /// <param name="usuarioId">Dueño de la sesión — la memoria nunca cruza entre usuarios distintos.</param>
    /// <param name="sesionId">Null o vencida (sin actividad en VentanaSesion) = arranca una sesión
    /// nueva. El llamador debe guardar el SesionId que devuelve ResultadoChat y reenviarlo en el
    /// siguiente mensaje para mantener la memoria.</param>
    /// <param name="imagenBase64">Imagen adjunta (ya en base64, sin el prefijo data:), si el
    /// personal cargó una — ej. un permiso médico o una ficha de capacidades del visitante.</param>
    /// <param name="modo">Uno de <see cref="ClavesPrompt"/> — el tema que el personal eligió en el
    /// menú antes de escribir (ver AsistenteChatViewModel). Null = sin preferencia (compatibilidad
    /// hacia atrás); el segmento de ese tema se antepone al resto del prompt de sistema.</param>
    public async Task<ResultadoChat> ResponderAsync(int usuarioId, Guid? sesionId, string mensajeNuevo, string? imagenBase64, string? modo, CancellationToken ct)
    {
        // Housekeeping barato: nada se borra por sesión vencida (eso es solo un filtro de lectura),
        // pero sin esto la tabla crece para siempre. Una vez por mensaje alcanza para este volumen.
        var limiteRetencion = DateTime.UtcNow.AddDays(-1);
        await db.MensajesSesion.Where(m => m.CreadoEn < limiteRetencion).ExecuteDeleteAsync(ct);

        var (sesion, historial) = await CargarOCrearSesionAsync(usuarioId, sesionId, ct);

        db.MensajesSesion.Add(new MensajeSesion { SesionId = sesion, UsuarioId = usuarioId, Rol = "user", Contenido = mensajeNuevo });
        await db.SaveChangesAsync(ct);
        historial.Add(new MensajeEntrada("user", mensajeNuevo));

        var promptSistema = await ObtenerPromptSistemaAsync(modo, ct);

        var proveedores = await db.ProveedoresIA
            .Where(p => p.Activo)
            .OrderBy(p => p.Prioridad)
            .ToListAsync(ct);

        if (proveedores.Count == 0)
            throw new InvalidOperationException(
                "No hay ningún proveedor de IA activo. Un administrador debe agregar uno desde Asistente IA.");

        Exception? ultimoError = null;

        foreach (var proveedor in proveedores)
        {
            if (!proveedor.SoportaToolCalling)
                continue; // sin tool calling confiable, el modelo terminaría "razonando" en texto libre: fuera de la cadena.

            try
            {
                var respuesta = await ConversarConProveedorAsync(proveedor, promptSistema, historial, imagenBase64, ct);
                proveedor.UltimoEstado = "ok";
                proveedor.UltimoUsoEn = DateTime.UtcNow;

                db.MensajesSesion.Add(new MensajeSesion { SesionId = sesion, UsuarioId = usuarioId, Rol = "assistant", Contenido = respuesta });
                await db.SaveChangesAsync(ct);

                return new ResultadoChat(respuesta, proveedor.Nombre, sesion);
            }
            catch (LimiteAlcanzadoException ex)
            {
                proveedor.UltimoEstado = "limite_alcanzado";
                await db.SaveChangesAsync(ct);
                ultimoError = ex;
            }
            catch (Exception ex)
            {
                proveedor.UltimoEstado = "error";
                await db.SaveChangesAsync(ct);
                ultimoError = ex;
            }
        }

        throw new InvalidOperationException(
            "Ningún proveedor de IA de la cadena pudo responder. Revisa el estado en Asistente IA.", ultimoError);
    }

    /// <summary>Si sesionId sigue vigente (actividad dentro de VentanaSesion) trae su historial de
    /// texto; si no, arranca una sesión nueva vacía. Solo texto user/assistant — las llamadas a
    /// herramientas no son "memoria", se resuelven de nuevo en cada turno.</summary>
    private async Task<(Guid SesionId, List<MensajeEntrada> Historial)> CargarOCrearSesionAsync(int usuarioId, Guid? sesionId, CancellationToken ct)
    {
        if (sesionId is { } sid)
        {
            var mensajesPrevios = await db.MensajesSesion
                .Where(m => m.SesionId == sid && m.UsuarioId == usuarioId)
                .OrderBy(m => m.CreadoEn)
                .ToListAsync(ct);

            var ultimo = mensajesPrevios.LastOrDefault();
            if (ultimo is not null && DateTime.UtcNow - ultimo.CreadoEn < VentanaSesion)
                return (sid, mensajesPrevios.Select(m => new MensajeEntrada(m.Rol, m.Contenido)).ToList());
        }

        return (Guid.NewGuid(), []);
    }

    /// <summary>Si la tabla está vacía (primer arranque) la siembra con PromptAsistente.Defaults —
    /// después de eso, lo que manda es la base, no esa clase. Usado tanto para armar el prompt de
    /// sistema como para que el admin los liste/edite.</summary>
    private async Task<Dictionary<string, PromptIA>> AsegurarPromptsSembradosAsync(CancellationToken ct)
    {
        var existentes = await db.PromptsIA.ToDictionaryAsync(p => p.Clave, ct);

        var faltantes = ClavesPrompt.Todas.Where(c => !existentes.ContainsKey(c)).ToList();
        var huboCambios = false;

        foreach (var clave in faltantes)
        {
            var (titulo, contenido) = PromptAsistente.Defaults[clave];
            var nuevo = new PromptIA { Clave = clave, Titulo = titulo, Contenido = contenido };
            db.PromptsIA.Add(nuevo);
            existentes[clave] = nuevo;
            huboCambios = true;
        }

        // Upgrade de los defaults viejos (instalaciones que ya tenían "evaluacion" y
        // "consulta_atracciones" sembrados antes de esta revisión): solo pisa el contenido si
        // sigue siendo EXACTAMENTE el texto default anterior — si un admin ya lo editó a mano
        // desde Asistente IA, ese texto no matchea y se deja intacto.
        foreach (var (clave, contenidoAnterior) in PromptAsistente.DefaultsAnteriores)
        {
            if (existentes.TryGetValue(clave, out var fila) && fila.Contenido == contenidoAnterior)
            {
                var (titulo, contenido) = PromptAsistente.Defaults[clave];
                fila.Titulo = titulo;
                fila.Contenido = contenido;
                fila.ActualizadoEn = DateTime.UtcNow;
                huboCambios = true;
            }
        }

        if (huboCambios)
            await db.SaveChangesAsync(ct);

        return existentes;
    }

    /// <summary>Concatena los cinco prompts editables. Si el personal eligió un tema en el menú
    /// (<paramref name="modo"/>), ese segmento va primero — el modelo sigue teniendo los otros
    /// cuatro disponibles (las herramientas no se restringen por tema), pero el foco declarado es
    /// el del menú.</summary>
    private async Task<string> ObtenerPromptSistemaAsync(string? modo, CancellationToken ct)
    {
        var prompts = await AsegurarPromptsSembradosAsync(ct);

        var orden = modo is not null && ClavesPrompt.Todas.Contains(modo)
            ? ClavesPrompt.Todas.OrderByDescending(c => c == modo)
            : ClavesPrompt.Todas.AsEnumerable();

        var cuerpo = string.Join("\n\n", orden.Select(c => prompts[c].Contenido));

        if (modo is null || !ClavesPrompt.Todas.Contains(modo))
            return cuerpo;

        return $"Tema elegido por el personal en el menú: \"{prompts[modo].Titulo}\". " +
            "Prioriza responder dentro de ese tema; si la pregunta se sale de él pero la puedes " +
            "responder con las herramientas disponibles, respóndela igual en vez de negarte.\n\n" + cuerpo;
    }

    /// <summary>Para la pantalla de admin: los tres prompts (entidades de dominio — el endpoint
    /// arma el DTO), en el mismo orden en que se concatenan.</summary>
    public async Task<List<PromptIA>> ObtenerPromptsAsync(CancellationToken ct = default)
    {
        var prompts = await AsegurarPromptsSembradosAsync(ct);
        return ClavesPrompt.Todas.Select(c => prompts[c]).ToList();
    }

    private async Task<string> ConversarConProveedorAsync(
        ProveedorIA proveedor, string promptSistema, List<MensajeEntrada> historial, string? imagenBase64, CancellationToken ct)
    {
        var apiKey = Descifrar(proveedor.ApiKeyCifrada);

        using var http = httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(proveedor.BaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        http.Timeout = TimeSpan.FromSeconds(45);

        var mensajes = new List<OpenAiMensaje> { new("system", promptSistema) };

        for (var i = 0; i < historial.Count; i++)
        {
            var h = historial[i];
            var esElMensajeNuevo = i == historial.Count - 1;

            if (esElMensajeNuevo && imagenBase64 is not null)
            {
                mensajes.Add(proveedor.SoportaVision
                    ? OpenAiMensaje.ConImagen(h.Rol, h.Contenido, imagenBase64)
                    : new OpenAiMensaje(h.Rol, h.Contenido +
                        "\n\n[El personal adjuntó una imagen, pero este modelo no la puede procesar. " +
                        "Dile que un administrador debe activar un proveedor con soporte de visión desde Asistente IA.]"));
            }
            else
            {
                mensajes.Add(new OpenAiMensaje(h.Rol, h.Contenido));
            }
        }

        var herramientas = ObtenerDefinicionesHerramientas();

        for (var intento = 0; intento < MaxLlamadasHerramientas; intento++)
        {
            var cuerpo = new OpenAiChatRequest
            {
                Model = proveedor.Modelo,
                Messages = mensajes,
                Tools = herramientas,
                ToolChoice = "auto",
            };

            using var respuestaHttp = await http.PostAsJsonAsync("chat/completions", cuerpo, JsonOpciones, ct);

            if (respuestaHttp.StatusCode == HttpStatusCode.TooManyRequests)
                throw new LimiteAlcanzadoException(proveedor.Nombre);

            var crudo = await respuestaHttp.Content.ReadAsStringAsync(ct);

            if (!respuestaHttp.IsSuccessStatusCode)
            {
                if (crudo.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                    crudo.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
                    crudo.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    throw new LimiteAlcanzadoException(proveedor.Nombre);

                logger.LogWarning("{Proveedor} respondió {Status}: {Cuerpo}", proveedor.Nombre, (int)respuestaHttp.StatusCode, crudo);
                throw new InvalidOperationException($"{proveedor.Nombre} respondió {(int)respuestaHttp.StatusCode}: {crudo}");
            }

            var payload = JsonSerializer.Deserialize<OpenAiChatResponse>(crudo, JsonOpciones);
            var eleccion = payload?.Choices.FirstOrDefault();
            var mensaje = eleccion?.Message
                ?? throw new InvalidOperationException($"{proveedor.Nombre} devolvió una respuesta sin contenido.");

            if (mensaje.ToolCalls is not { Count: > 0 })
            {
                var texto = mensaje.TextoPlano();
                logger.LogInformation("{Proveedor} intento {Intento}: respuesta final, finish_reason={Finish}, largo_contenido={Largo}",
                    proveedor.Nombre, intento, eleccion?.FinishReason, texto.Length);
                return texto;
            }

            mensajes.Add(mensaje);

            foreach (var llamada in mensaje.ToolCalls)
            {
                logger.LogInformation("{Proveedor} intento {Intento}: llamó {Herramienta}({Args})",
                    proveedor.Nombre, intento, llamada.Function.Name, llamada.Function.Arguments);
                var resultado = await EjecutarHerramientaAsync(llamada, ct);
                mensajes.Add(new OpenAiMensaje("tool", resultado) { ToolCallId = llamada.Id });
            }
        }

        throw new InvalidOperationException($"{proveedor.Nombre} encadenó demasiadas llamadas a herramientas sin responder.");
    }

    // --- Herramientas ------------------------------------------------------------------------

    private List<OpenAiTool> ObtenerDefinicionesHerramientas() =>
    [
        new OpenAiTool
        {
            Function = new OpenAiFunctionDef
            {
                Name = "evaluar_atraccion",
                Description = "Evalúa qué atracciones puede usar un visitante según su perfil. atracciones vacío = las 29.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["perfil"] = JsonSchemaGenerator.Generar(typeof(PerfilCapacidades)),
                        ["atracciones"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = new JsonObject { ["type"] = "string" },
                        },
                    },
                    ["required"] = new JsonArray("perfil"),
                },
            },
        },
        new OpenAiTool
        {
            Function = new OpenAiFunctionDef
            {
                Name = "buscar_ayuda",
                Description = "Busca en las guías internas de uso de la app.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["tema"] = new JsonObject { ["type"] = "string" },
                    },
                    ["required"] = new JsonArray("tema"),
                },
            },
        },
        new OpenAiTool
        {
            Function = new OpenAiFunctionDef
            {
                Name = "consultar_atracciones",
                Description = "Info general del catálogo (nombre, descripción, intensidad, alturas), sin evaluar a ningún visitante. atraccion vacío = lista las 29.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["atraccion"] = new JsonObject { ["type"] = "string" },
                    },
                },
            },
        },
        new OpenAiTool
        {
            Function = new OpenAiFunctionDef
            {
                Name = "consultar_horario_parque",
                Description = "Horario de hoy de cada servicio del parque (parqueadero, taquillas, registradoras, teleféricos, museos, atracciones, paseo a caballo, trenes) y si el parque está abierto ahora mismo, según el horario oficial publicado por el parque.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject(),
                },
            },
        },
    ];

    private async Task<string> EjecutarHerramientaAsync(OpenAiToolCall llamada, CancellationToken ct)
    {
        try
        {
            return llamada.Function.Name switch
            {
                "evaluar_atraccion" => EjecutarEvaluarAtraccion(llamada.Function.Arguments),
                "buscar_ayuda" => await EjecutarBuscarAyudaAsync(llamada.Function.Arguments, ct),
                "consultar_atracciones" => EjecutarConsultarAtracciones(llamada.Function.Arguments),
                "consultar_horario_parque" => await EjecutarConsultarHorarioAsync(ct),
                _ => JsonSerializer.Serialize(new { error = $"Herramienta desconocida: {llamada.Function.Name}" }),
            };
        }
        catch (Exception ex)
        {
            // Un error acá (JSON mal formado del modelo, perfil inválido) no debe tumbar la
            // conversación entera: se lo devolvemos al modelo como resultado de la herramienta
            // para que pueda corregir o avisarle al usuario, en vez de romper la respuesta HTTP.
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private string EjecutarEvaluarAtraccion(string argumentosJson)
    {
        var argumentos = JsonSerializer.Deserialize<JsonObject>(argumentosJson, JsonOpciones)
            ?? throw new InvalidOperationException("Argumentos vacíos para evaluar_atraccion.");

        var nodoPerfil = argumentos["perfil"] ?? throw new InvalidOperationException("Falta el perfil del visitante.");
        var perfil = nodoPerfil.Deserialize<PerfilCapacidades>(JsonOpciones)
            ?? throw new InvalidOperationException("No se pudo interpretar el perfil enviado.");

        var codigos = argumentos["atracciones"]?.Deserialize<List<string>>(JsonOpciones);

        // El modelo no conoce los códigos internos (rin_rin) y le pedimos que no los invente, así
        // que acá se resuelve también por nombre — evaluando solo lo pedido en vez de las 29, el
        // resultado que vuelve a la conversación pesa una fracción de los tokens.
        var seleccionadas = codigos is { Count: > 0 }
            ? codigos.Select(BuscarPorCodigoONombre).OfType<RequisitosAtraccion>().ToList()
            : catalogo.Todas.ToList();

        if (seleccionadas.Count == 0)
            return JsonSerializer.Serialize(new { error = "No se encontró ninguna atracción con ese nombre o código en el catálogo." });

        var dictamen = MotorCapacidades.Evaluar(perfil, seleccionadas);
        var dto = DiagnosticoMapper.ADto(dictamen, catalogo);

        return JsonSerializer.Serialize(dto, JsonOpciones);
    }

    /// <summary>Por código exacto primero (lo que de verdad usa el catálogo); si no hay match, por
    /// nombre tal como lo escribiría una persona ("Rin-Rin", sin tildes, sin importar mayúsculas).</summary>
    private RequisitosAtraccion? BuscarPorCodigoONombre(string texto)
    {
        var porCodigo = catalogo.Buscar(texto);
        if (porCodigo is not null)
            return porCodigo;

        var normalizado = Normalizar(texto);
        return catalogo.Todas.FirstOrDefault(a => Normalizar(a.Nombre) == normalizado)
            ?? catalogo.Todas.FirstOrDefault(a => Normalizar(a.Nombre).Contains(normalizado) || normalizado.Contains(Normalizar(a.Nombre)));
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sinAcentos = new string(descompuesto
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        return sinAcentos.Replace("-", " ").Replace("_", " ").Trim().ToLowerInvariant();
    }

    /// <summary>Catálogo general, sin evaluar a nadie — complementa evaluar_atraccion (que siempre
    /// necesita un perfil) para preguntas tipo "contame de Rin-Rin" o "qué atracciones tienen".
    /// Lee CatalogoRequisitos directo: es la misma fuente que usa todo lo demás, nunca una copia
    /// que se pueda desincronizar.</summary>
    private string EjecutarConsultarAtracciones(string argumentosJson)
    {
        var argumentos = JsonSerializer.Deserialize<JsonObject>(argumentosJson, JsonOpciones);
        var texto = argumentos?["atraccion"]?.GetValue<string>()?.Trim();

        if (string.IsNullOrWhiteSpace(texto))
        {
            // Lista compacta a propósito (sin descripción) para no repetir el problema de tokens
            // que tuvimos con evaluar_atraccion antes de resolver por nombre: acá nadie necesita
            // las 29 descripciones completas solo para saber qué atracciones existen.
            var todas = catalogo.Todas
                .OrderBy(a => a.Nombre)
                .Select(a => new { codigo = a.Codigo, nombre = a.Nombre, intensidad = a.Intensidad })
                .ToList();
            return JsonSerializer.Serialize(new { atracciones = todas }, JsonOpciones);
        }

        var requisitos = BuscarPorCodigoONombre(texto);
        if (requisitos is null)
            return JsonSerializer.Serialize(new { error = $"No se encontró ninguna atracción que coincida con \"{texto}\"." });

        // Por rol: solo si la atracción distingue conductor/pasajero (Karts Dobles, Carros
        // Chocones) — para las otras 27 sería ruido repetir lo mismo que "general".
        object? porRol = requisitos.DistinguePorRol
            ? new
            {
                conductor = AlturaPeso(requisitos.ParaRol(RolVisitante.Conductor)),
                pasajero = AlturaPeso(requisitos.ParaRol(RolVisitante.Pasajero)),
            }
            : null;

        return JsonSerializer.Serialize(new
        {
            codigo = requisitos.Codigo,
            nombre = requisitos.Nombre,
            descripcion = requisitos.Descripcion,
            intensidad = requisitos.Intensidad,
            alturaMinimaCm = requisitos.General.AlturaMinimaCm,
            alturaMaximaCm = requisitos.General.AlturaMaximaCm,
            pesoMaximoKg = requisitos.General.PesoMaximoKg,
            porRol,
            acompanante = new
            {
                umbralEstaturaCm = requisitos.Acompanante.UmbralEstaturaCm,
                estaturaMinimaAcompananteCm = requisitos.Acompanante.EstaturaMinimaAcompananteCm,
                requeridoPorCognitiva = requisitos.Acompanante.RequeridoPorCognitiva,
                requeridoPorVisual = requisitos.Acompanante.RequeridoPorVisual,
                requeridoPorMovilidadReducida = requisitos.Acompanante.RequeridoPorMovilidadReducida,
            },
            restriccionesMedicas = requisitos.RestriccionesMedicas
                .Select(c => CodigosCondicion.Etiquetas.TryGetValue(c, out var e) ? e : c).ToList(),
            restriccionesMedicasAcompanante = requisitos.RestriccionesMedicasAcompanante
                .Select(c => CodigosCondicion.Etiquetas.TryGetValue(c, out var e) ? e : c).ToList(),
        }, JsonOpciones);
    }

    private static object AlturaPeso(RequisitosRol req) => new
    {
        alturaMinimaCm = req.AlturaMinimaCm,
        alturaMaximaCm = req.AlturaMaximaCm,
        pesoMaximoKg = req.PesoMaximoKg,
    };

    /// <summary>Todos los horarios de hoy (no solo el resumen de "abierto/cerrado"): es una
    /// herramienta informativa, así que el modelo debe poder responder "¿a qué hora cierra la
    /// taquilla?" o "¿a qué hora sale el tren de Armenia?" igual que "¿está abierto el parque?".</summary>
    private async Task<string> EjecutarConsultarHorarioAsync(CancellationToken ct)
    {
        var estadoGeneral = await horarioParqueService.ObtenerEstadoAsync(ct);
        var horarios = await horarioParqueService.ObtenerTodosAsync(ct);

        return JsonSerializer.Serialize(new { estadoGeneral, horarios }, JsonOpciones);
    }

    private async Task<string> EjecutarBuscarAyudaAsync(string argumentosJson, CancellationToken ct)
    {
        var argumentos = JsonSerializer.Deserialize<JsonObject>(argumentosJson, JsonOpciones);
        var tema = argumentos?["tema"]?.GetValue<string>()?.Trim() ?? "";

        if (tema.Length == 0)
            return JsonSerializer.Serialize(new { error = "Falta el tema a buscar." });

        var palabras = tema.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var documentos = await db.DocumentosContexto
            .Where(d => d.Activo)
            .ToListAsync(ct);

        var coincidencias = documentos
            .Where(d => palabras.Any(p =>
                d.Nombre.Contains(p, StringComparison.OrdinalIgnoreCase) ||
                d.Contenido.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .Take(3)
            .Select(d => new
            {
                documento = d.Nombre,
                // Recorte: evita inflar el contexto del modelo con guías enteras cuando solo
                // hace falta la parte relevante.
                extracto = d.Contenido.Length > 2000 ? d.Contenido[..2000] + "…" : d.Contenido,
            })
            .ToList();

        if (coincidencias.Count == 0)
            return JsonSerializer.Serialize(new { encontrado = false, mensaje = "No hay ninguna guía cargada sobre ese tema." });

        return JsonSerializer.Serialize(new { encontrado = true, resultados = coincidencias }, JsonOpciones);
    }
}
