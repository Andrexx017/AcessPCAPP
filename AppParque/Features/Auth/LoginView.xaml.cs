using AppParque.Features.Administracion;
using AppParque.Shared;
// El namespace AppParque.Features.MenuPrincipal choca de nombre con la clase MenuPrincipal
// que contiene, así que se referencia con un alias en vez de "using" el namespace.
using MenuPrincipalPage = AppParque.Features.MenuPrincipal.MenuPrincipal;

namespace AppParque.Features.Auth
{
    public partial class LoginView : ContentPage
    {
        private readonly LoginViewModel _viewModel = new LoginViewModel();

        // Por debajo de esto se apila el panel de marca sobre el formulario (celular);
        // por encima, van lado a lado (escritorio/tablet). MAUI no tiene media queries,
        // así que el cambio de layout se hace a mano según el ancho disponible.
        private const double WideBreakpoint = 800;
        private bool? _isWide;

        public LoginView()
        {
            InitializeComponent();
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
                rootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(42, GridUnitType.Star)));
                rootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(58, GridUnitType.Star)));
                Grid.SetRow(brandPanel, 0);
                Grid.SetColumn(brandPanel, 0);
                Grid.SetRow(formPanel, 0);
                Grid.SetColumn(formPanel, 1);
            }
            else
            {
                rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                Grid.SetRow(brandPanel, 0);
                Grid.SetColumn(brandPanel, 0);
                Grid.SetRow(formPanel, 1);
                Grid.SetColumn(formPanel, 0);
            }

            // El párrafo largo y la leyenda del panel de marca solo caben cómodos en escritorio;
            // en celular la leyenda se repite bajo el botón en su lugar.
            lblSupport.IsVisible = isWide;
            lblCaption.IsVisible = isWide;
            lblFormFooter.IsVisible = !isWide;
            lblTagline.FontSize = isWide ? 24 : 18;
        }

        // ====== Metodo para mostrar/ocultar la contrasena ======
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            // Alterna el estado de visibilidad de la contrasena
            entryPassword.IsPassword = !entryPassword.IsPassword;

            // Cambia el icono segun el estado
            btnTogglePassword.Source = entryPassword.IsPassword ? "eye_closed.png" : "eye_open.png";
        }

        // ====== Inicio de sesion ======
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = entryUser.Text?.Trim();
            string password = entryPassword.Text?.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Por favor ingrese usuario y contraseña.", "OK");
                return;
            }

            var (success, error) = await _viewModel.LoginAsync(username, password);

            if (success)
            {
                await DisplayAlert("Éxito", $"Bienvenido {UsuarioGlobal.nurseName}", "OK");

                if (UsuarioGlobal.EsAdmin)
                    await Navigation.PushAsync(new MenuAdmin());
                else
                    await Navigation.PushAsync(new MenuPrincipalPage());
            }
            else
            {
                await DisplayAlert("Error", error, "OK");
            }
        }
    }
}
