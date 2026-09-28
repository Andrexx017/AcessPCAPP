namespace AppParque.Features.Administracion.AdminUsuarios;

public partial class AdminUsuariosView : ContentPage
{
    private readonly AdminUsuariosViewModel _viewModel = new();

    public AdminUsuariosView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CargarAsync();
    }

    private async void OnUsuarioSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        collectionUsuarios.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not UsuarioRow row)
            return;

        await Navigation.PushAsync(new AdminUsuarioEditView(row.Model));
    }

    private async void OnNuevoUsuarioClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminUsuarioEditView(usuarioExistente: null));
    }
}
