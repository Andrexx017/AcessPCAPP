using AppParque.Features.Administracion.Inicio;
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

        private const string RememberMeKey = "appparque_remember_me";
        private const string RememberedUserKey = "appparque_remembered_user";
        private const string RememberedPasswordKey = "appparque_remembered_password";

        public LoginView()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Las credenciales van en SecureStorage (igual que los tokens en ApiClient), nunca en
            // Preferences plano. Solo el booleano "recordar" es Preferences porque no es sensible.
            bool recordar = Preferences.Default.Get(RememberMeKey, false);
            chkRecordar.IsChecked = recordar;

            if (recordar)
            {
                entryUser.Text = await SecureStorage.GetAsync(RememberedUserKey);
                entryPassword.Text = await SecureStorage.GetAsync(RememberedPasswordKey);
            }
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

                // En escritorio el panel llena toda la columna (alto del Grid), así que no necesita
                // una altura propia.
                brandPanel.HeightRequest = -1;
                brandPanel.IsClippedToBounds = false;
            }
            else
            {
                rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                Grid.SetRow(brandPanel, 0);
                Grid.SetColumn(brandPanel, 0);
                Grid.SetRow(formPanel, 1);
                Grid.SetColumn(formPanel, 0);

                // En celular la fila del panel es "Auto", y su alto "natural" termina dictado por
                // los círculos decorativos (320x260 con márgenes negativos grandes) en vez del logo
                // y el texto — eso dejaba un bloque de marca desproporcionadamente alto y un hueco
                // vacío antes del formulario. Con una altura fija el panel queda compacto y
                // IsClippedToBounds recorta los círculos que se salen de ese alto.
                brandPanel.HeightRequest = 240;
                brandPanel.IsClippedToBounds = true;
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

        // ====== Checkbox "Recordar contraseña": el tap en el label también lo marca/desmarca ======
        private void OnRecordarLabelTapped(object sender, EventArgs e)
        {
            chkRecordar.IsChecked = !chkRecordar.IsChecked;
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
                if (chkRecordar.IsChecked)
                {
                    Preferences.Default.Set(RememberMeKey, true);
                    await SecureStorage.SetAsync(RememberedUserKey, username);
                    await SecureStorage.SetAsync(RememberedPasswordKey, password);
                }
                else
                {
                    Preferences.Default.Remove(RememberMeKey);
                    SecureStorage.Remove(RememberedUserKey);
                    SecureStorage.Remove(RememberedPasswordKey);
                }

                if (UsuarioGlobal.EsAdmin)
                    await Navigation.PushAsync(new InicioAdminView());
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
