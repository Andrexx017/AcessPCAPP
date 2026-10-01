using AppParque.Features.EvaluacionAccesibilidad;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.RegistroVisitante;

public partial class RegistroCompletoView : ContentPage
{
    // Mismo umbral y mecanismo que Features/Auth/LoginView.xaml.cs y Features/Historial/HistorialView.xaml.cs:
    // MAUI no tiene media queries, así que el cambio entre panel de resumen fijo (ancho) y formulario
    // de una sola columna (angosto) se hace a mano según el ancho disponible.
    private const double WideBreakpoint = 800;
    private bool? _isWide;

    public RegistroCompletoView(string? tipoDocumento = null, string? numeroDocumento = null)
    {
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(tipoDocumento))
            pickerTipoId.SelectedItem = tipoDocumento;

        if (!string.IsNullOrWhiteSpace(numeroDocumento))
            entryNumeroId.Text = numeroDocumento;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        bool isWide = width >= WideBreakpoint;
        if (_isWide == isWide) return;
        _isWide = isWide;

        rootGrid.RowDefinitions.Clear();
        rootGrid.ColumnDefinitions.Clear();

        if (isWide)
        {
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(320)));
            Grid.SetRow(mainPanel, 0);
            Grid.SetColumn(mainPanel, 0);
            Grid.SetRow(asidePanel, 0);
            Grid.SetColumn(asidePanel, 1);

            asidePanel.IsVisible = true;
        }
        else
        {
            rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Grid.SetRow(mainPanel, 0);
            Grid.SetColumn(mainPanel, 0);
            Grid.SetRow(asidePanel, 0);
            Grid.SetColumn(asidePanel, 0);

            asidePanel.IsVisible = false;
        }
    }

    private async void OnComenzarClicked(object sender, EventArgs e)
    {
        var tipoDocumento = pickerTipoId.SelectedItem?.ToString();
        var numeroDocumento = entryNumeroId.Text?.Trim();
        var nombre = entryNombre.Text?.Trim();
        var edad = entryEdad.Text?.Trim();
        var estatura = entryEstatura.Text?.Trim();
        var acompananteNombre = entryAcompananteNombre.Text?.Trim();
        var parentesco = pickerParentesco.SelectedItem?.ToString();
        var acompananteTelefono = entryAcompananteTelefono.Text?.Trim();
        var tipoSangre = pickerTipoSangre.SelectedItem?.ToString();
        var eps = entryEps.Text?.Trim();
        var alergias = editorAlergias.Text?.Trim();

        if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(numeroDocumento) ||
            string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(edad) || string.IsNullOrWhiteSpace(estatura) ||
            string.IsNullOrWhiteSpace(acompananteNombre) || string.IsNullOrWhiteSpace(parentesco) || string.IsNullOrWhiteSpace(acompananteTelefono) ||
            string.IsNullOrWhiteSpace(tipoSangre) || string.IsNullOrWhiteSpace(eps) || string.IsNullOrWhiteSpace(alergias))
        {
            await DisplayAlert("Error", "Todos los campos son obligatorios por la seguridad del visitante.", "OK");
            return;
        }

        if (!checkboxConsentimientoDatos.IsChecked)
        {
            await DisplayAlert("Autorización requerida", "Para continuar, el visitante (o su acudiente) debe autorizar el tratamiento de sus datos personales y de salud.", "OK");
            return;
        }

        if (!checkboxAceptaPoliticas.IsChecked)
        {
            await DisplayAlert("Autorización requerida", "Para continuar, el visitante debe aceptar las políticas de seguridad del parque.", "OK");
            return;
        }

        var creacion = await ApiClient.PostAsync<VisitanteDto>("/api/visitantes", new
        {
            tipoDocumento,
            numeroDocumento,
            nombre,
            consentimientoTratamientoDatos = true,
            nombreAcompanante = acompananteNombre,
            parentescoAcompanante = parentesco,
            telefonoAcompanante = acompananteTelefono,
            tipoSangre,
            eps,
            alergias,
            aceptaPoliticasParque = true,
        });

        if (!creacion.Success)
        {
            await DisplayAlert("Error", creacion.ErrorMessage ?? "No se pudo registrar el visitante.", "OK");
            return;
        }

        UsuarioGlobal.VisitanteId = creacion.Data.Id;
        UsuarioGlobal.Name = creacion.Data.Nombre;
        UsuarioGlobal.TypeId = tipoDocumento;
        UsuarioGlobal.IdNumber = numeroDocumento;
        UsuarioGlobal.Age = edad;
        UsuarioGlobal.Stature = estatura;

        await Navigation.PushAsync(new TestView());
    }
}
