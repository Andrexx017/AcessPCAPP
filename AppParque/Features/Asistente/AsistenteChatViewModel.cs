using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Asistente;

public class MensajeChatItem : BaseViewModel
{
    public string Texto { get; init; } = "";
    public bool EsUsuario { get; init; }
    public bool EsError { get; init; }

    public bool EsAsistente => !EsUsuario;

    public Color Fondo => EsError
        ? Color.FromArgb("#FBEDEB")
        : EsUsuario ? Color.FromArgb("#0F6B5C") : Color.FromArgb("#F0F2F5");

    public Color ColorTexto => EsError
        ? Color.FromArgb("#8A2317")
        : EsUsuario ? Colors.White : Color.FromArgb("#1f2430");

    public LayoutOptions Alineacion => EsUsuario ? LayoutOptions.End : LayoutOptions.Start;

    /// <summary>Copiar/compartir solo tiene sentido para lo que respondió el bot, no para lo que
    /// el propio usuario acaba de escribir (ya lo tiene).</summary>
    public bool MostrarAcciones => !EsUsuario && !EsError;

    private static readonly Regex RegexNegrita = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

    /// <summary>El Label de la burbuja (ver AsistenteChatContentView) no interpreta Markdown: sin
    /// esto, "**Permitidas**" se veía con los asteriscos literales. Solo se traduce negrita — es lo
    /// único que el prompt del asistente usa (ver FormatoChat en AsistenteOrquestador, que además
    /// le prohíbe tablas y encabezados porque tampoco tendrían dónde verse bien en una burbuja
    /// angosta de celular).</summary>
    public FormattedString TextoFormateado
    {
        get
        {
            var formateado = new FormattedString();
            var posicion = 0;

            foreach (Match coincidencia in RegexNegrita.Matches(Texto))
            {
                if (coincidencia.Index > posicion)
                    formateado.Spans.Add(NuevoSpan(Texto[posicion..coincidencia.Index], negrita: false));

                formateado.Spans.Add(NuevoSpan(coincidencia.Groups[1].Value, negrita: true));
                posicion = coincidencia.Index + coincidencia.Length;
            }

            if (posicion < Texto.Length || formateado.Spans.Count == 0)
                formateado.Spans.Add(NuevoSpan(Texto[posicion..], negrita: false));

            return formateado;
        }
    }

    private Span NuevoSpan(string texto, bool negrita) => new()
    {
        Text = texto,
        TextColor = ColorTexto,
        FontSize = 13.5,
        FontAttributes = negrita ? FontAttributes.Bold : FontAttributes.None,
    };

    /// <summary>Clave de pantalla (ver whitelist en AsistenteChatContentView) que el modelo sugirió
    /// con el marcador [[ir:clave]] al final de su respuesta — ya validada contra la whitelist y el
    /// rol del usuario. Null = no hay destino, o el que sugirió no es válido.</summary>
    public string? NavClave { get; init; }
    public string? NavEtiqueta { get; init; }
    public bool TieneNav => NavClave is not null;

    /// <summary>"¿Tu duda fue resuelta?" — solo se prende en la última respuesta del asistente de
    /// cada turno; se apaga sola al mandar un mensaje nuevo o al tocar cualquiera de los dos chips.</summary>
    private bool _mostrarBotonesCierre;
    public bool MostrarBotonesCierre
    {
        get => _mostrarBotonesCierre;
        set => SetProperty(ref _mostrarBotonesCierre, value);
    }

    // --- avatar del usuario (solo se usa cuando EsUsuario) --------------------------------------
    // Se graba en el mensaje al crearlo (ver AsistenteChatViewModel) en vez de mirar un estado
    // compartido en el momento de pintar, así que no cambia retroactivamente si el perfil se edita
    // a mitad de una conversación ya mostrada en pantalla.
    public string? FotoUsuarioBase64 { get; init; }
    public string InicialesUsuario { get; init; } = "?";
    public bool TieneFotoUsuario => !string.IsNullOrWhiteSpace(FotoUsuarioBase64);

    public ImageSource? FotoUsuarioImagen
    {
        get
        {
            if (!TieneFotoUsuario)
                return null;
            try
            {
                var bytes = Convert.FromBase64String(FotoUsuarioBase64!);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                return null;
            }
        }
    }
}

/// <summary>
/// La "memoria" de la conversación vive en el servidor (tabla MensajesSesion, ventana de 5
/// minutos) — este ViewModel guarda el SesionId para reenviarlo en cada mensaje. Además de eso,
/// gobierna el menú de temas obligatorio (ModoAsistente), los chips de "¿se resolvió tu duda?" y
/// el cierre automático por inactividad del lado cliente (que refleja la misma ventana de 5
/// minutos que ya usa el servidor, para que la UI no quede mostrando una sesión que el servidor ya
/// olvidó).
/// </summary>
public class AsistenteChatViewModel : BaseViewModel
{
    private static readonly TimeSpan VentanaInactividad = TimeSpan.FromMinutes(5);

    /// <summary>El modelo debe terminar su respuesta con esto cuando sugiere ir a una pantalla
    /// (ver el prompt "contexto_app" en AppParque.Api). Se recorta del texto que ve el usuario sin
    /// importar si la clave matchea la whitelist o no, para que nunca se vea el marcador crudo.</summary>
    private static readonly Regex RegexNav = new(@"\s*\[\[ir:([a-z_]+)\]\]\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Pantallas a las que el asistente puede ofrecer un acceso directo — debe ser el
    /// mismo conjunto de claves que lista el prompt "contexto_app". La resolución a una Page de
    /// verdad vive en AsistenteChatContentView (capa de vista), acá solo se valida y se etiqueta.</summary>
    private static readonly Dictionary<string, (string Etiqueta, bool SoloAdmin)> DestinosNav = new()
    {
        ["historial"] = ("Historial", false),
        ["registro_visitante"] = ("Registrar visitante", false),
        ["perfil"] = ("Mi perfil", false),
        ["admin_atracciones"] = ("Catálogo de atracciones", true),
        ["admin_preguntas"] = ("Catálogo de preguntas", true),
        ["admin_usuarios"] = ("Usuarios", true),
    };

    private Guid? _sesionId;
    private IDispatcherTimer? _timerInactividad;

    public ObservableCollection<MensajeChatItem> Mensajes { get; } = [];

    public IReadOnlyList<ModoAsistente> Temas => ModoAsistente.Todos;

    private ModoAsistente? _modoActual;
    public ModoAsistente? ModoActual
    {
        get => _modoActual;
        private set
        {
            if (SetProperty(ref _modoActual, value))
                OnPropertyChanged(nameof(Subtitulo));
        }
    }

    /// <summary>Lo que muestra el encabezado bajo el título — el del tema elegido, o una
    /// invitación genérica mientras se está eligiendo.</summary>
    public string Subtitulo => ModoActual?.Subtitulo ?? "Elige un tema para empezar";

    private bool _mostrandoMenu = true;
    /// <summary>true = hay que elegir un tema antes de poder escribir (arranca así, y vuelve a
    /// true al cerrar la conversación, a mano o por inactividad).</summary>
    public bool MostrandoMenu
    {
        get => _mostrandoMenu;
        private set => SetProperty(ref _mostrandoMenu, value);
    }

    public string? UltimoModeloUsado { get; private set; }

    /// <summary>La foto/iniciales del enfermero logueado, para estampar en sus propios mensajes.
    /// La pantalla las carga una vez al abrir (ver AsistenteChatView) con el mismo patrón que
    /// SideMenuView.SetAvatar.</summary>
    public string? FotoUsuarioBase64 { get; set; }
    public string InicialesUsuario { get; set; } = "?";

    public void SeleccionarModo(ModoAsistente modo)
    {
        _sesionId = null;
        ModoActual = modo;
        Mensajes.Clear();
        Mensajes.Add(new MensajeChatItem { Texto = modo.MensajeBienvenida });
        MostrandoMenu = false;
        ReiniciarTemporizadorInactividad();
    }

    public async Task EnviarAsync(string texto, string? imagenBase64 = null)
    {
        texto = texto.Trim();
        if ((texto.Length == 0 && imagenBase64 is null) || IsBusy || ModoActual is null)
            return;

        OcultarBotonesCierrePendientes();

        Mensajes.Add(new MensajeChatItem
        {
            Texto = imagenBase64 is null ? texto : $"{texto}\n📎 Imagen adjunta",
            EsUsuario = true,
            FotoUsuarioBase64 = FotoUsuarioBase64,
            InicialesUsuario = InicialesUsuario,
        });

        IsBusy = true;

        var resultado = await ApiClient.PostAsync<ChatResponseDto>(
            "/api/asistente/chat",
            new ChatRequestDto(texto, _sesionId, imagenBase64, ModoActual.Clave));

        IsBusy = false;

        if (!resultado.Success)
        {
            Mensajes.Add(new MensajeChatItem
            {
                Texto = resultado.ErrorMessage ?? "No se pudo contactar al asistente. Avísale a un administrador.",
                EsError = true,
            });
            return;
        }

        _sesionId = resultado.Data.SesionId;
        UltimoModeloUsado = resultado.Data.ModeloUsado;

        var (textoLimpio, navClave, navEtiqueta) = ExtraerNav(resultado.Data.Respuesta);
        Mensajes.Add(new MensajeChatItem
        {
            Texto = textoLimpio,
            NavClave = navClave,
            NavEtiqueta = navEtiqueta,
            MostrarBotonesCierre = true,
        });

        ReiniciarTemporizadorInactividad();
    }

    /// <summary>Chip "💬 Tengo otra pregunta": solo oculta los botones de esa burbuja y reinicia el
    /// reloj de inactividad — la conversación sigue igual.</summary>
    public void ContinuarConversacion(MensajeChatItem mensaje)
    {
        mensaje.MostrarBotonesCierre = false;
        ReiniciarTemporizadorInactividad();
    }

    /// <summary>Chip "✅ Listo, gracias", el botón de "cambiar de tema" del encabezado, o el
    /// vencimiento del temporizador de inactividad: en los tres casos la conversación se da por
    /// cerrada y hay que volver a elegir un tema.</summary>
    public async Task CerrarConversacionAsync(bool porInactividad = false)
    {
        DetenerTemporizadorInactividad();
        OcultarBotonesCierrePendientes();

        Mensajes.Add(new MensajeChatItem
        {
            Texto = porInactividad
                ? "⏳ Esta conversación se cerró automáticamente por inactividad. Elige un tema cuando quieras retomarla."
                : "✅ ¡Listo! Si necesitas algo más, elige un tema cuando quieras.",
        });

        await Task.Delay(1200);

        _sesionId = null;
        ModoActual = null;
        Mensajes.Clear();
        MostrandoMenu = true;
    }

    /// <summary>Botón "↺ Cambiar de tema" del encabezado: a diferencia de
    /// <see cref="CerrarConversacionAsync"/>, no es un cierre narrado (la persona no dijo que ya
    /// terminó) — solo vuelve al menú de una, sin burbuja de despedida.</summary>
    public void CambiarTema()
    {
        DetenerTemporizadorInactividad();
        _sesionId = null;
        ModoActual = null;
        Mensajes.Clear();
        MostrandoMenu = true;
    }

    private void OcultarBotonesCierrePendientes()
    {
        foreach (var mensaje in Mensajes)
            if (mensaje.MostrarBotonesCierre)
                mensaje.MostrarBotonesCierre = false;
    }

    private static (string Texto, string? NavClave, string? NavEtiqueta) ExtraerNav(string respuesta)
    {
        var match = RegexNav.Match(respuesta);
        if (!match.Success)
            return (respuesta, null, null);

        var texto = respuesta[..match.Index].TrimEnd();
        var clave = match.Groups[1].Value.ToLowerInvariant();

        if (!DestinosNav.TryGetValue(clave, out var destino) || (destino.SoloAdmin && !UsuarioGlobal.EsAdmin))
            return (texto, null, null); // clave inventada o fuera de alcance para este rol: se recorta el marcador igual, sin chip

        return (texto, clave, destino.Etiqueta);
    }

    // --- temporizador de inactividad (refleja del lado cliente la misma ventana de 5 min que
    // AsistenteOrquestador.VentanaSesion usa del lado servidor) ---------------------------------

    private void ReiniciarTemporizadorInactividad()
    {
        DetenerTemporizadorInactividad();

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        _timerInactividad = dispatcher.CreateTimer();
        _timerInactividad.Interval = VentanaInactividad;
        _timerInactividad.IsRepeating = false;
        _timerInactividad.Tick += async (_, _) => await CerrarConversacionAsync(porInactividad: true);
        _timerInactividad.Start();
    }

    private void DetenerTemporizadorInactividad()
    {
        _timerInactividad?.Stop();
        _timerInactividad = null;
    }

    /// <summary>Llamado desde el code-behind cuando la vista se descarga, para no dejar un timer
    /// vivo referenciando este ViewModel.</summary>
    public void Detener() => DetenerTemporizadorInactividad();
}
