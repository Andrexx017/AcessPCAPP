using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;
using Microsoft.Maui.Controls.Shapes;

namespace AppParque.Features.EvaluacionAccesibilidad;

public enum EstadoZona
{
    SinRevisar,
    Parcial,
    Completa,
    SinNovedad,
}

/// <summary>Una zona dibujada en el mapa: su recuadro en el lienzo y los colores de su estado.</summary>
public class ZonaMapaItem
{
    public ZonaCorporal Zona { get; init; } = null!;
    public Rect Bounds { get; init; }

    /// <summary>CornerRadius y no double: así el XAML lo ata directo al RoundRectangle del Border.</summary>
    public CornerRadius Radio { get; init; }
    public Color Fondo { get; init; } = Colors.White;
    public Color Trazo { get; init; } = Colors.Gray;
    public double Grosor { get; init; } = 2;
    public DoubleCollection? Guiones { get; init; }
    public Color ColorTexto { get; init; } = Colors.Black;
    public string Etiqueta { get; init; } = string.Empty;

    /// <summary>El estado en palabras: «sin revisar», «a medias», «evaluada», «sin novedad».</summary>
    public string Rotulo { get; init; } = string.Empty;

    /// <summary>Lo que lee el lector de pantalla: nombre completo de la zona más su estado.</summary>
    public string Descripcion { get; init; } = string.Empty;
}

/// <summary>
/// El estado del recorrido por zonas. Lo comparten las tres pantallas del flujo móvil
/// (mapa, puerta de la zona, secuencia de preguntas), así que vive una sola instancia creada en
/// <see cref="MapaCapacidadesView"/> y pasada a las demás por constructor.
/// </summary>
public class MapaCapacidadesViewModel : BaseViewModel
{
    // Lienzo de 358x540 en unidades independientes del dispositivo, igual que el mockup.
    public const double LienzoAncho = 358;
    public const double LienzoAlto = 540;

    private static readonly Dictionary<string, (Rect Caja, double Radio)> Geometria = new()
    {
        ["cabeza"] = (new Rect(141, 6, 76, 76), 38),
        ["tronco"] = (new Rect(114, 90, 130, 156), 30),
        ["brazoIzq"] = (new Rect(61, 96, 44, 200), 22),
        ["brazoDer"] = (new Rect(253, 96, 44, 200), 22),
        ["piernaIzq"] = (new Rect(124, 254, 50, 216), 25),
        ["piernaDer"] = (new Rect(184, 254, 50, 216), 25),
        ["marcha"] = (new Rect(69, 478, 220, 52), 14),
    };

    private readonly Dictionary<string, string> _respuestas = [];
    private readonly HashSet<string> _sinNovedad = [];

    /// <summary>
    /// La zona abierta en el panel de escritorio, para pintarla resaltada en el mapa. En el flujo
    /// móvil no se usa: allá el mapa desaparece mientras se evalúa, así que no hay nada que resaltar.
    /// </summary>
    private string? _seleccionada;

    public ObservableCollection<ZonaMapaItem> ZonasCuerpo { get; } = [];
    public ObservableCollection<ZonaMapaItem> ZonasAparte { get; } = [];

    public MapaCapacidadesViewModel()
    {
        // Estatura y edad ya se pidieron al registrar al visitante: se traen para no preguntarlas
        // dos veces. El peso no existe en el registro (es nuevo, sale de la ficha de Paseo a
        // Caballo) y queda por llenar, así que la zona Medidas no arranca completa.
        if (int.TryParse(UsuarioGlobal.Stature, out var estatura) && estatura > 0)
            _respuestas["medidas.estatura"] = estatura.ToString();

        if (int.TryParse(UsuarioGlobal.Age, out var edad) && edad > 0)
            _respuestas["medidas.edad"] = edad.ToString();
    }

    /// <summary>
    /// Trae el catálogo de zonas de la API (una sola vez por sesión de la app, ver
    /// CatalogoZonas.CargarAsync) y repinta. Hay que llamarlo antes de leer cualquier otra
    /// propiedad de este ViewModel — se hace en el primer OnAppearing de MapaCapacidadesView /
    /// MapaCapacidadesEscritorioView, con un indicador de carga mientras tanto.
    /// </summary>
    public async Task CargarYRefrescarAsync()
    {
        IsBusy = true;
        try
        {
            await CatalogoZonas.CargarAsync();
            Refrescar();
        }
        catch (ApiException ex)
        {
            UltimoError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string NombreVisitante => string.IsNullOrWhiteSpace(UsuarioGlobal.Name)
        ? "Visitante sin nombre"
        : UsuarioGlobal.Name;

    public string Avance
    {
        get
        {
            var resueltas = CatalogoZonas.Todas.Count(z => EstadoDe(z) is EstadoZona.SinNovedad or EstadoZona.Completa);
            return $"{resueltas} de {CatalogoZonas.Todas.Count} zonas";
        }
    }

    public string Dimensiones
    {
        get
        {
            var respondidas = CatalogoZonas.Todas.Sum(z => PreguntasActivas(z).Count(p => Tiene($"{z.Id}.{p.Clave}")));
            var total = CatalogoZonas.Todas.Sum(z => PreguntasActivas(z).Count);
            return $"{respondidas}/{total}";
        }
    }

    public string Pie
    {
        get
        {
            var pendientes = CatalogoZonas.Todas.Count(z => EstadoDe(z) != EstadoZona.SinNovedad && EstadoDe(z) != EstadoZona.Completa);
            return pendientes == 0
                ? "Listo para el dictamen"
                : pendientes == 1
                    ? "1 zona deja atracciones sin decidir"
                    : $"{pendientes} zonas dejan atracciones sin decidir";
        }
    }

    public bool TodoResuelto => CatalogoZonas.Todas.All(z => EstadoDe(z) is EstadoZona.SinNovedad or EstadoZona.Completa);

    public string TextoBotonPie => TodoResuelto ? "Ver dictamen" : "Empezar";

    /// <summary>Primera zona sin tocar, para el botón «Empezar». null si no queda ninguna.</summary>
    public ZonaCorporal? PrimeraSinRevisar =>
        CatalogoZonas.Todas.FirstOrDefault(z => EstadoDe(z) == EstadoZona.SinRevisar);

    /// <summary>
    /// Primera zona con alguna pregunta activa sin responder, tocada o no (sin revisar o a medias).
    /// Con 10 zonas y 42 dimensiones repartidas entre ellas, encontrar a mano cuál falta es lento;
    /// esto es lo que usa el botón «Ver dictamen» del panel de escritorio para saltar directo ahí en
    /// vez de pedir que se recorra todo el mapa buscando el hueco.
    /// </summary>
    public ZonaCorporal? PrimeraZonaPendiente =>
        CatalogoZonas.Todas.FirstOrDefault(z => EstadoDe(z) is EstadoZona.SinRevisar or EstadoZona.Parcial);

    /// <summary>Títulos de las preguntas activas de la zona que todavía no tienen respuesta.</summary>
    public IReadOnlyList<string> PreguntasPendientes(ZonaCorporal zona) =>
        PreguntasActivas(zona)
            .Where(p => !Tiene($"{zona.Id}.{p.Clave}"))
            .Select(p => p.Titulo)
            .ToList();

    public ZonaCorporal? SiguienteDespuesDe(ZonaCorporal zona)
    {
        var indice = CatalogoZonas.Todas.ToList().FindIndex(z => z.Id == zona.Id);
        if (indice < 0 || indice == CatalogoZonas.Todas.Count - 1)
            return null;

        return CatalogoZonas.Todas[indice + 1];
    }

    private bool Tiene(string clave) => _respuestas.TryGetValue(clave, out var v) && !string.IsNullOrWhiteSpace(v);

    public string? Respuesta(string clave) => _respuestas.TryGetValue(clave, out var v) ? v : null;

    /// <summary>Todas las respuestas crudas ("zona.pregunta" → valor), para la pantalla de Resumen
    /// que lista lo contestado antes de pedir el dictamen. No es una copia: refleja el estado
    /// actual del recorrido.</summary>
    public IReadOnlyDictionary<string, string> Respuestas => _respuestas;

    /// <summary>
    /// Las preguntas que de verdad aplican con lo respondido hasta ahora: salta las que dependen de
    /// otra que no se contestó o que tomó otro valor (la removibilidad solo si hay prótesis, los
    /// datos del acompañante solo si viene acompañado).
    /// </summary>
    public IReadOnlyList<PreguntaZona> PreguntasActivas(ZonaCorporal zona) =>
        zona.Preguntas.Where(p =>
        {
            if (p.DependeDe is null || p.ActivaCuando is null)
                return true;

            var valor = Respuesta($"{zona.Id}.{p.DependeDe}");
            return valor is not null && p.ActivaCuando.Contains(valor);
        }).ToList();

    public EstadoZona EstadoDe(ZonaCorporal zona)
    {
        if (_sinNovedad.Contains(zona.Id))
            return EstadoZona.SinNovedad;

        var activas = PreguntasActivas(zona);
        var respondidas = activas.Count(p => Tiene($"{zona.Id}.{p.Clave}"));

        if (respondidas == 0)
            return EstadoZona.SinRevisar;

        return respondidas == activas.Count ? EstadoZona.Completa : EstadoZona.Parcial;
    }

    public void Guardar(string zonaId, string clave, string valor)
    {
        _respuestas[$"{zonaId}.{clave}"] = valor;
        // Responder algo contradice «sin novedad»: si quedaran las dos, el perfil diría dos cosas.
        _sinNovedad.Remove(zonaId);
        Refrescar();
    }

    public void MarcarSinNovedad(ZonaCorporal zona)
    {
        foreach (var (clave, valor) in zona.ValoresSinNovedad)
            _respuestas[$"{zona.Id}.{clave}"] = valor;

        _sinNovedad.Add(zona.Id);
        Refrescar();
    }

    public void Seleccionar(ZonaCorporal? zona)
    {
        _seleccionada = zona?.Id;
        Refrescar();
    }

    public void Refrescar()
    {
        ZonasCuerpo.Clear();
        ZonasAparte.Clear();

        foreach (var zona in CatalogoZonas.Todas)
        {
            var item = Construir(zona);
            if (zona.EnCuerpo)
                ZonasCuerpo.Add(item);
            else
                ZonasAparte.Add(item);
        }

        OnPropertyChanged(nameof(Avance));
        OnPropertyChanged(nameof(Dimensiones));
        OnPropertyChanged(nameof(Pie));
        OnPropertyChanged(nameof(TodoResuelto));
        OnPropertyChanged(nameof(TextoBotonPie));
        OnPropertyChanged(nameof(PrimeraSinRevisar));
    }

    private ZonaMapaItem Construir(ZonaCorporal zona)
    {
        var estado = EstadoDe(zona);
        var (caja, radio) = zona.EnCuerpo ? Geometria[zona.Id] : (Rect.Zero, 11);

        // La zona abierta en escritorio se pinta oscura por encima de su estado: en ese layout el
        // mapa y el panel se ven juntos, y hay que poder decir de un vistazo cuál se está editando.
        var (fondo, trazo, texto, guiones) = zona.Id == _seleccionada
            ? (Color.FromArgb("#15191E"), Color.FromArgb("#15191E"), Colors.White, (DoubleCollection?)null)
            : estado switch
        {
            EstadoZona.SinNovedad => (Color.FromArgb("#E7F1EF"), Color.FromArgb("#0F6B5C"), Color.FromArgb("#0A4D42"), (DoubleCollection?)null),
            EstadoZona.Completa => (Color.FromArgb("#E3EBF5"), Color.FromArgb("#1B4E8A"), Color.FromArgb("#163E6E"), null),
            EstadoZona.Parcial => (Color.FromArgb("#FDF8EC"), Color.FromArgb("#D9A400"), Color.FromArgb("#6B4600"), null),
            _ => (Color.FromArgb("#FAFBFC"), Color.FromArgb("#C6CED7"), Color.FromArgb("#5A6472"), new DoubleCollection { 4, 3 }),
        };

        var rotulo = estado switch
        {
            EstadoZona.SinNovedad => "sin novedad",
            EstadoZona.Completa => "evaluada",
            EstadoZona.Parcial => "a medias",
            _ => "sin revisar",
        };

        return new ZonaMapaItem
        {
            Zona = zona,
            Bounds = caja,
            Radio = new CornerRadius(radio),
            Fondo = fondo,
            Trazo = trazo,
            Grosor = 2,
            Guiones = guiones,
            ColorTexto = texto,
            Etiqueta = zona.Corto,
            Rotulo = rotulo,
            Descripcion = $"{zona.Nombre}: {rotulo}",
        };
    }

    /// <summary>
    /// Envía el perfil al motor. Devuelve null y deja el mensaje en <paramref name="error"/> si
    /// falla, para que la vista decida cómo avisar.
    /// </summary>
    /// <summary>El perfil que se mandó en la última llamada a <see cref="PedirDictamenAsync"/> —
    /// lo necesita la pantalla de confirmación para guardar el mismo perfil en el historial, sin
    /// reconstruirlo ni arriesgarse a que quede desincronizado con lo que de verdad se evaluó.</summary>
    public PerfilCapacidadesDto? UltimoPerfil { get; private set; }

    public async Task<DiagnosticoResponseDto?> PedirDictamenAsync()
    {
        IsBusy = true;

        try
        {
            var perfil = PerfilBuilder.Construir(_respuestas);
            UltimoPerfil = perfil;

            var resultado = await ApiClient.PostAsync<DiagnosticoResponseDto>(
                "/api/diagnostico",
                new DiagnosticoRequestDto(perfil));

            if (resultado.Success)
                return resultado.Data;

            UltimoError = resultado.ErrorMessage ?? "No se pudo obtener el dictamen.";
            return null;
        }
        catch (ApiException ex)
        {
            UltimoError = ex.Message;
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string? UltimoError { get; private set; }
}
