namespace AppParque.Features.Perfil
{
    public partial class PerfilView : ContentPage
    {
        private readonly PerfilViewModel _viewModel = new();

        public PerfilView()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool ok = await _viewModel.CargarAsync();
            if (!ok)
            {
                await DisplayAlert("Error", "No se pudo cargar tu perfil.", "OK");
                return;
            }

            entryNombre.Text = _viewModel.NombreCompleto;
            entryEmail.Text = _viewModel.Email;
            lblUsername.Text = _viewModel.Username;
            lblRol.Text = _viewModel.Rol.ToUpperInvariant();
            AplicarAvatar(_viewModel.FotoBase64);
        }

        private void AplicarAvatar(string? fotoBase64)
        {
            if (string.IsNullOrWhiteSpace(fotoBase64))
            {
                imgAvatar.Source = "icon_usuarios.png";
                return;
            }

            try
            {
                var bytes = Convert.FromBase64String(fotoBase64);
                imgAvatar.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                imgAvatar.Source = "icon_usuarios.png";
            }
        }

        private async void OnCambiarFotoTapped(object? sender, EventArgs e)
        {
            try
            {
                var foto = await MediaPicker.Default.PickPhotoAsync();
                if (foto is null)
                    return;

                using var stream = await foto.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                var bytes = buffer.ToArray();

                var (success, error) = await _viewModel.SubirFotoAsync(bytes);
                if (!success)
                {
                    await DisplayAlert("Error", error, "OK");
                    return;
                }

                AplicarAvatar(_viewModel.FotoBase64);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo seleccionar la foto ({ex.Message}).", "OK");
            }
        }

        private async void OnGuardarClicked(object? sender, EventArgs e)
        {
            var nombre = entryNombre.Text?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                await DisplayAlert("Error", "El nombre completo es obligatorio.", "OK");
                return;
            }

            _viewModel.NombreCompleto = nombre;
            _viewModel.Email = entryEmail.Text?.Trim();

            var (success, error) = await _viewModel.GuardarAsync();
            if (!success)
            {
                await DisplayAlert("Error", error, "OK");
                return;
            }

            await DisplayAlert("Éxito", "Tu perfil se actualizó correctamente.", "OK");
        }
    }
}
