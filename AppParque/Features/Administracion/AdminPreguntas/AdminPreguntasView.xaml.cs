namespace AppParque.Features.Administracion.AdminPreguntas;

public partial class AdminPreguntasView : ContentPage
{
    private readonly AdminPreguntasViewModel _viewModel = new();

    public AdminPreguntasView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CargarAsync();
    }

    private async void OnPreguntaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        collectionPreguntas.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not PreguntaRow row)
            return;

        await Navigation.PushAsync(new AdminPreguntaEditView(row.Model));
    }

    private async void OnNuevaPreguntaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminPreguntaEditView(preguntaExistente: null));
    }
}
