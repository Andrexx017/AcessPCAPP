using System.Globalization;
using AppParque.Features.EvaluacionAccesibilidad;
using AppParque.Features.ValidacionAtracciones;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.RegistroVisitante;

public partial class ConfirmacionEvaluacionView : ContentPage
{
    // Pendiente de confirmar con el negocio (igual que las reglas de MotorReglas.cs): a partir de
    // cuántos días una evaluación anterior deja de ser confiable para "reutilizar sin repetir el test".
    // 180 días es un valor de partida razonable, no una decisión de negocio ya validada.
    private const int DiasVigenciaRecomendada = 180;

    private readonly VisitanteDto _visitante;
    private EvaluacionResponseDto? _ultimaEvaluacion;

    public ConfirmacionEvaluacionView(VisitanteDto visitante)
    {
        InitializeComponent();
        _visitante = visitante;

        lblNombre.Text = visitante.Nombre;
        lblDocumento.Text = $"{visitante.TipoDocumento} · {visitante.NumeroDocumento}";

        _ = CargarUltimaEvaluacionAsync();
    }

    private async Task CargarUltimaEvaluacionAsync()
    {
        var resumen = await ApiClient.GetAsync<List<EvaluacionResumenDto>>($"/api/visitantes/{_visitante.Id}/evaluaciones");

        loading.IsRunning = false;
        loading.IsVisible = false;

        if (!resumen.Success || resumen.Data is null || resumen.Data.Count == 0)
        {
            lblSinEvaluaciones.IsVisible = true;
            btnSiguenIgual.IsVisible = false;
            btnAlgoCambio.Text = "Continuar al test";
            panelBotones.IsVisible = true;
            return;
        }

        var masReciente = resumen.Data[0];
        var detalle = await ApiClient.GetAsync<EvaluacionResponseDto>($"/api/evaluaciones/{masReciente.Id}");

        if (!detalle.Success || detalle.Data is null)
        {
            lblSinEvaluaciones.Text = "No se pudo cargar tu última evaluación. Puedes continuar con el test.";
            lblSinEvaluaciones.IsVisible = true;
            btnSiguenIgual.IsVisible = false;
            btnAlgoCambio.Text = "Continuar al test";
            panelBotones.IsVisible = true;
            return;
        }

        _ultimaEvaluacion = detalle.Data;

        var dias = (DateTime.UtcNow - masReciente.Fecha).Days;
        lblFecha.Text = $"{masReciente.Fecha:d 'de' MMMM 'de' yyyy} · hace {DescribirTiempo(dias)}";

        flexCondiciones.Children.Clear();
        foreach (var codigo in _ultimaEvaluacion.GruposActivados.Concat(_ultimaEvaluacion.CondicionesActivadas))
        {
            flexCondiciones.Children.Add(new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 999 },
                Stroke = Colors.Transparent,
                BackgroundColor = Color.FromArgb("#eef0fb"),
                Padding = new Thickness(10, 5),
                Margin = new Thickness(0, 0, 6, 6),
                Content = new Label
                {
                    Text = Humanizar(codigo),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#3949ab"),
                },
            });
        }

        var permitidas = _ultimaEvaluacion.Atracciones.Count(a => a.PreseleccionadaAutomatica);
        var total = _ultimaEvaluacion.Atracciones.Count;
        lblResultado.Text = $"{permitidas} de {total} atracciones permitidas";

        var vigenciaVencida = dias > DiasVigenciaRecomendada;
        if (vigenciaVencida)
        {
            bannerVigencia.IsVisible = true;
            lblVigencia.Text = $"Ya pasaron más de {DiasVigenciaRecomendada / 30} meses desde tu última evaluación. Por tu seguridad, te recomendamos repetir el test completo.";

            btnAlgoCambio.Text = "Repetir test completo";
            btnAlgoCambio.BackgroundColor = Color.FromArgb("#4CAF50");
            btnAlgoCambio.TextColor = Colors.White;
            btnAlgoCambio.BorderWidth = 0;

            btnSiguenIgual.Text = "Sí, siguen igual";
            btnSiguenIgual.BackgroundColor = Color.FromArgb("#FFFFFF");
            btnSiguenIgual.TextColor = Color.FromArgb("#8a7b62");
            btnSiguenIgual.BorderColor = Color.FromArgb("#e9e1d3");
            btnSiguenIgual.BorderWidth = 1.5;
        }

        panelEvaluacion.IsVisible = true;
        panelBotones.IsVisible = true;
    }

    private async void OnSiguenIgualClicked(object sender, EventArgs e)
    {
        if (_ultimaEvaluacion is null) return;

        btnSiguenIgual.IsEnabled = false;
        btnAlgoCambio.IsEnabled = false;

        var resultado = await ApiClient.PostAsync<EvaluacionResponseDto>($"/api/evaluaciones/{_ultimaEvaluacion.Id}/reutilizar", new { });

        btnSiguenIgual.IsEnabled = true;
        btnAlgoCambio.IsEnabled = true;

        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo reutilizar la evaluación anterior.", "OK");
            return;
        }

        await Navigation.PushAsync(new ValidacionAtraccionesView(resultado.Data));
    }

    private async void OnAlgoCambioClicked(object sender, EventArgs e)
    {
        UsuarioGlobal.Age = _ultimaEvaluacion?.Edad?.ToString();
        UsuarioGlobal.Stature = _ultimaEvaluacion?.Estatura?.ToString();

        await Navigation.PushAsync(new TestView());
    }

    private static string DescribirTiempo(int dias) => dias switch
    {
        < 1 => "unas horas",
        1 => "1 día",
        < 14 => $"{dias} días",
        < 60 => $"{dias / 7} semanas",
        _ => $"{dias / 30} meses",
    };

    private static string Humanizar(string codigo)
    {
        var texto = codigo.Replace('_', ' ');
        return CultureInfo.GetCultureInfo("es-CO").TextInfo.ToTitleCase(texto);
    }
}
