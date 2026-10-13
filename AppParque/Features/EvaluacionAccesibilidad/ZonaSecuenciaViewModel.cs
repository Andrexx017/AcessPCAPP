using System.Collections.ObjectModel;
using AppParque.Shared;

namespace AppParque.Features.EvaluacionAccesibilidad;

public class OpcionItem
{
    public OpcionZona Opcion { get; init; } = null!;
    public string Texto => Opcion.Texto;
    public bool Activa { get; init; }
    public Color Fondo { get; init; } = Colors.White;
    public Color Trazo { get; init; } = Colors.Gray;
    public double Grosor { get; init; } = 1;
    public Color ColorTexto { get; init; } = Colors.Black;
    public FontAttributes Peso { get; init; } = FontAttributes.None;

    /// <summary>Qué hacer al tocarla. Va en el item y no en la vista para que móvil y escritorio
    /// compartan el mismo manejador de toque sin saber de qué pantalla vienen.</summary>
    public Action? AlElegir { get; init; }
}

public class SegmentoItem
{
    public Color Color { get; init; } = Colors.Gray;
}

/// <summary>
/// El paso actual de la secuencia de una zona. Una pregunta a la vez, con el progreso de ESA zona
/// («2 de 4»), no del test completo: el enfermero entra a resolver una parte del cuerpo y sale.
/// </summary>
public class ZonaSecuenciaViewModel : BaseViewModel
{
    public static readonly Color Acento = Color.FromArgb("#0F6B5C");

    /// <summary>
    /// La regla de los tres ejes del vocabulario, con su color. La comparten la secuencia móvil y el
    /// panel de escritorio: <c>Texto</c> es la versión larga (hay sitio en una pregunta a pantalla
    /// completa) y <c>Corto</c> la que cabe en la tarjeta de una columna.
    /// </summary>
    public static readonly Dictionary<int, (string Texto, string Corto, Color Color)> EjesPorNivel = new()
    {
        [1] = ("1 ESTADO · lo que es el cuerpo", "1 ESTADO", Color.FromArgb("#15191E")),
        [2] = ("2 DISPOSITIVO · lo que lleva puesto", "2 DISPOSITIVO", Color.FromArgb("#1B4E8A")),
        [3] = ("3 CAPACIDAD · lo que pide el mecanismo", "3 CAPACIDAD", Color.FromArgb("#6B3FA0")),
    };

    private readonly ZonaCorporal _zona;
    private readonly MapaCapacidadesViewModel _mapa;
    private int _indice;

    public ZonaSecuenciaViewModel(ZonaCorporal zona, MapaCapacidadesViewModel mapa)
    {
        _zona = zona;
        _mapa = mapa;
        Refrescar();
    }

    public ObservableCollection<OpcionItem> Opciones { get; } = [];
    public ObservableCollection<SegmentoItem> Segmentos { get; } = [];

    /// <summary>Las preguntas se recalculan en cada paso: responder una puede activar o desactivar
    /// otra (la removibilidad solo si hay prótesis).</summary>
    private IReadOnlyList<PreguntaZona> Activas => _mapa.PreguntasActivas(_zona);

    private PreguntaZona Actual => Activas[Math.Min(_indice, Activas.Count - 1)];

    private string ClaveActual => $"{_zona.Id}.{Actual.Clave}";

    public string Nombre => _zona.Nombre;

    public string Contador => $"Pregunta {Math.Min(_indice, Activas.Count - 1) + 1} de {Activas.Count}";

    public string Titulo => Actual.Titulo;

    public string Ayuda => Actual.Ayuda ?? string.Empty;

    public bool TieneAyuda => !string.IsNullOrWhiteSpace(Actual.Ayuda);

    public bool TieneEje => EjesPorNivel.ContainsKey(Actual.Eje);

    public string EjeTexto => EjesPorNivel.TryGetValue(Actual.Eje, out var eje) ? eje.Texto : string.Empty;

    public Color EjeColor => EjesPorNivel.TryGetValue(Actual.Eje, out var eje) ? eje.Color : Colors.Transparent;

    public bool EsOpciones => Actual.Tipo == TipoRespuestaZona.Opciones;

    public bool EsNumero => Actual.Tipo == TipoRespuestaZona.Numero;

    public string Unidad => Actual.Unidad ?? string.Empty;

    public string ValorNumero
    {
        get => _mapa.Respuesta(ClaveActual) ?? string.Empty;
        set
        {
            var limpio = (value ?? string.Empty).Trim();
            if (limpio.Length == 0)
                return;

            _mapa.Guardar(_zona.Id, Actual.Clave, limpio);
            OnPropertyChanged();
            OnPropertyChanged(nameof(PuedeAvanzar));
        }
    }

    public bool PuedeAvanzar => !string.IsNullOrWhiteSpace(_mapa.Respuesta(ClaveActual));

    public bool EsUltima => Math.Min(_indice, Activas.Count - 1) == Activas.Count - 1;

    public string TextoSiguiente => EsUltima ? "Terminar zona" : "Siguiente";

    public void Elegir(OpcionZona opcion)
    {
        _mapa.Guardar(_zona.Id, Actual.Clave, opcion.Valor);
        Refrescar();
    }

    /// <summary>true si se salió de la zona (ya no hay más preguntas): la vista debe volver al mapa.</summary>
    public bool Avanzar()
    {
        if (EsUltima)
            return true;

        _indice = Math.Min(_indice, Activas.Count - 1) + 1;
        Refrescar();
        return false;
    }

    /// <summary>true si hay que salir hacia atrás de la zona (estábamos en la primera pregunta).</summary>
    public bool Atras()
    {
        if (_indice == 0)
            return true;

        _indice--;
        Refrescar();
        return false;
    }

    private void Refrescar()
    {
        if (_indice > Activas.Count - 1)
            _indice = Math.Max(0, Activas.Count - 1);

        Opciones.Clear();
        if (EsOpciones)
        {
            var valor = _mapa.Respuesta(ClaveActual);
            var color = TieneEje ? EjeColor : Acento;

            foreach (var opcion in Actual.OpcionesSeguras)
            {
                var activa = opcion.Valor == valor;
                Opciones.Add(new OpcionItem
                {
                    Opcion = opcion,
                    Activa = activa,
                    Fondo = Colors.Transparent,
                    Trazo = activa ? color : Color.FromArgb("#D5DCE4"),
                    Grosor = activa ? 2 : 1,
                    ColorTexto = activa ? color : Color.FromArgb("#15191E"),
                    Peso = activa ? FontAttributes.Bold : FontAttributes.None,
                    AlElegir = () => Elegir(opcion),
                });
            }
        }

        Segmentos.Clear();
        var posicion = Math.Min(_indice, Activas.Count - 1);
        for (var i = 0; i < Activas.Count; i++)
        {
            Segmentos.Add(new SegmentoItem
            {
                Color = i < posicion
                    ? Acento
                    : i == posicion
                        ? Color.FromArgb("#15191E")
                        : Color.FromArgb("#DDE3E9"),
            });
        }

        OnPropertyChanged(nameof(Contador));
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(Ayuda));
        OnPropertyChanged(nameof(TieneAyuda));
        OnPropertyChanged(nameof(TieneEje));
        OnPropertyChanged(nameof(EjeTexto));
        OnPropertyChanged(nameof(EjeColor));
        OnPropertyChanged(nameof(EsOpciones));
        OnPropertyChanged(nameof(EsNumero));
        OnPropertyChanged(nameof(Unidad));
        OnPropertyChanged(nameof(ValorNumero));
        OnPropertyChanged(nameof(PuedeAvanzar));
        OnPropertyChanged(nameof(EsUltima));
        OnPropertyChanged(nameof(TextoSiguiente));
    }
}
