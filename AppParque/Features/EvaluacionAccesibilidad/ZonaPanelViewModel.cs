using System.Collections.ObjectModel;
using AppParque.Shared;

namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Una pregunta del panel de escritorio. Es <see cref="BaseViewModel"/> y no un record porque el
/// dato numérico se edita con un Entry de doble vía: el valor tiene que notificar.
/// </summary>
public class PreguntaPanelItem : BaseViewModel
{
    private readonly ZonaCorporal _zona;
    private readonly PreguntaZona _pregunta;
    private readonly MapaCapacidadesViewModel _mapa;
    private readonly Action _alCambiar;

    public PreguntaPanelItem(
        ZonaCorporal zona,
        PreguntaZona pregunta,
        MapaCapacidadesViewModel mapa,
        Action alCambiar)
    {
        _zona = zona;
        _pregunta = pregunta;
        _mapa = mapa;
        _alCambiar = alCambiar;

        Opciones = [];
        Reconstruir();
    }

    public ObservableCollection<OpcionItem> Opciones { get; }

    public string Titulo => _pregunta.Titulo;

    public string Ayuda => _pregunta.Ayuda ?? string.Empty;

    public bool TieneAyuda => !string.IsNullOrWhiteSpace(_pregunta.Ayuda);

    public bool TieneEje => ZonaSecuenciaViewModel.EjesPorNivel.ContainsKey(_pregunta.Eje);

    public string EjeTexto => ZonaSecuenciaViewModel.EjesPorNivel.TryGetValue(_pregunta.Eje, out var eje)
        ? eje.Corto
        : string.Empty;

    public Color EjeColor => ZonaSecuenciaViewModel.EjesPorNivel.TryGetValue(_pregunta.Eje, out var eje)
        ? eje.Color
        : Colors.Transparent;

    public bool EsOpciones => _pregunta.Tipo == TipoRespuestaZona.Opciones;

    public bool EsNumero => _pregunta.Tipo == TipoRespuestaZona.Numero;

    public string Unidad => _pregunta.Unidad ?? string.Empty;

    private string Clave => $"{_zona.Id}.{_pregunta.Clave}";

    public string Valor
    {
        get => _mapa.Respuesta(Clave) ?? string.Empty;
        set
        {
            var limpio = (value ?? string.Empty).Trim();
            if (limpio.Length == 0 || limpio == _mapa.Respuesta(Clave))
                return;

            _mapa.Guardar(_zona.Id, _pregunta.Clave, limpio);
            OnPropertyChanged();
            _alCambiar();
        }
    }

    public void Reconstruir()
    {
        Opciones.Clear();

        if (!EsOpciones)
        {
            OnPropertyChanged(nameof(Valor));
            return;
        }

        var valor = _mapa.Respuesta(Clave);
        var color = TieneEje ? EjeColor : ZonaSecuenciaViewModel.Acento;

        foreach (var opcion in _pregunta.OpcionesSeguras)
        {
            var activa = opcion.Valor == valor;
            Opciones.Add(new OpcionItem
            {
                Opcion = opcion,
                Activa = activa,
                Trazo = activa ? color : Color.FromArgb("#D5DCE4"),
                Grosor = activa ? 2 : 1,
                ColorTexto = activa ? color : Color.FromArgb("#15191E"),
                Peso = activa ? FontAttributes.Bold : FontAttributes.None,
                AlElegir = () =>
                {
                    _mapa.Guardar(_zona.Id, _pregunta.Clave, opcion.Valor);
                    _alCambiar();
                },
            });
        }

        OnPropertyChanged(nameof(Valor));
    }
}

/// <summary>
/// El panel derecho de la pantalla de escritorio: la zona abierta con TODAS sus preguntas a la vez.
/// En móvil ese mismo contenido se recorre pregunta por pregunta
/// (<see cref="ZonaSecuenciaViewModel"/>), porque en un teléfono no caben juntas; acá sí, y verlas
/// de un golpe es más rápido para el enfermero que ya sabe qué va a responder.
/// </summary>
public class ZonaPanelViewModel : BaseViewModel
{
    private readonly MapaCapacidadesViewModel _mapa;

    public ZonaPanelViewModel(ZonaCorporal zona, MapaCapacidadesViewModel mapa)
    {
        Zona = zona;
        _mapa = mapa;
        Preguntas = [];
        Reconstruir();
    }

    public ZonaCorporal Zona { get; }

    public ObservableCollection<PreguntaPanelItem> Preguntas { get; }

    public string Nombre => Zona.Nombre;

    public string Nota => Zona.Nota;

    public string ResumenSinNovedad => Zona.ResumenSinNovedad;

    public bool PermiteSinNovedad => Zona.PermiteSinNovedad;

    public bool ExigeLlenado => !Zona.PermiteSinNovedad;

    public bool EstaSinNovedad => _mapa.EstadoDe(Zona) == EstadoZona.SinNovedad;

    public string Contador
    {
        get
        {
            if (EstaSinNovedad)
                return "Sin novedad";

            var activas = _mapa.PreguntasActivas(Zona);
            var respondidas = activas.Count(p => !string.IsNullOrWhiteSpace(_mapa.Respuesta($"{Zona.Id}.{p.Clave}")));
            return $"{respondidas} de {activas.Count} respondidas";
        }
    }

    public void MarcarSinNovedad()
    {
        _mapa.MarcarSinNovedad(Zona);
        Reconstruir();
    }

    public void Reconstruir()
    {
        // Responder algo puede activar o desactivar otra pregunta de la misma zona (la removibilidad
        // solo si hay prótesis), así que la lista se rearma completa en cada cambio de opción.
        // Los Entry numéricos son la excepción: se les avisa con ActualizarResumen, sin tocar la
        // colección, porque recrear el Entry mientras se escribe le tira el foco y el cursor al
        // usuario en cada tecla (Medidas quedaba imposible de llenar).
        Preguntas.Clear();

        foreach (var pregunta in _mapa.PreguntasActivas(Zona))
        {
            Action alCambiar = pregunta.Tipo == TipoRespuestaZona.Numero ? ActualizarResumen : Reconstruir;
            Preguntas.Add(new PreguntaPanelItem(Zona, pregunta, _mapa, alCambiar));
        }

        ActualizarResumen();
    }

    private void ActualizarResumen()
    {
        OnPropertyChanged(nameof(Contador));
        OnPropertyChanged(nameof(EstaSinNovedad));
    }
}
