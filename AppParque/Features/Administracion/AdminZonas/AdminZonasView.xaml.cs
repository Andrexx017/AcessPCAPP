namespace AppParque.Features.Administracion.AdminZonas;

public partial class AdminZonasView : ContentPage
{
    private readonly AdminZonasViewModel _viewModel = new();

    public AdminZonasView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CargarAsync();
    }

    private async void OnZonaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        collectionCuerpo.SelectedItem = null;
        collectionAparte.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not ZonaRow row)
            return;

        await Navigation.PushAsync(new AdminZonaEditView(row.Model));
    }

    private async void OnNuevaZonaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminZonaEditView(zonaExistente: null));
    }
}
