namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Pantalla 2 del flujo móvil: la bifurcación de la zona, a pantalla completa y sin mapa.
/// «Sin novedad» marca las dimensiones de la zona como normales y vuelve al mapa de una;
/// «Evaluar» entra a la secuencia de preguntas.
/// </summary>
public partial class ZonaPuertaView : ContentPage
{
    private readonly ZonaCorporal _zona;
    private readonly MapaCapacidadesViewModel _vm;

    public ZonaPuertaView(ZonaCorporal zona, MapaCapacidadesViewModel vm)
    {
        _zona = zona;
        _vm = vm;

        InitializeComponent();
        BindingContext = this;
    }

    public string Nombre => _zona.Nombre;

    public string Nota => _zona.Nota;

    public string Cuantas
    {
        get
        {
            var cuantas = _vm.PreguntasActivas(_zona).Count;
            return cuantas == 1 ? "1 pregunta" : $"{cuantas} preguntas";
        }
    }

    public string ResumenSinNovedad => _zona.ResumenSinNovedad;

    public string ResumenEvaluar => $"Responder {Cuantas}, una por una";

    public bool PermiteSinNovedad => _zona.PermiteSinNovedad;

    public bool ExigeLlenado => !_zona.PermiteSinNovedad;

    private async void OnSinNovedadTapped(object sender, TappedEventArgs e)
    {
        _vm.MarcarSinNovedad(_zona);
        await Navigation.PopAsync();
    }

    private async void OnEvaluarTapped(object sender, TappedEventArgs e)
    {
        var navegacion = Navigation;

        await navegacion.PushAsync(new ZonaSecuenciaView(_zona, _vm));

        // Se saca esta pantalla de la pila: así al terminar la secuencia un solo "atrás" vuelve al
        // mapa, en vez de pasar otra vez por la bifurcación que el enfermero ya resolvió.
        navegacion.RemovePage(this);
    }
}
