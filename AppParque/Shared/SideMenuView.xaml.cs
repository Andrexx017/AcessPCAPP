namespace AppParque.Shared
{
    public partial class SideMenuView : ContentView
    {
        private const double DrawerWidth = 260;
        private bool _isOpen;

        public event EventHandler? PerfilTapped;
        public event EventHandler? CerrarSesionTapped;

        public SideMenuView()
        {
            InitializeComponent();

            // En Android esta vista, aun "invisible" (Opacity 0 + fuera de pantalla), estaba
            // tapando todo lo de abajo en negro — fijar esas propiedades por código tampoco lo
            // arregló. En vez de seguir dependiendo de que el backdrop/panel se vean bien estando
            // técnicamente presentes, ahora se oculta del todo (IsVisible=false) mientras el menú
            // está cerrado: así no participa para nada del dibujo ni puede tapar la pantalla.
            backdrop.Opacity = 0;
            backdrop.InputTransparent = true;
            panel.TranslationX = -DrawerWidth;
            InputTransparent = true;
            IsVisible = false;
        }

        public string HeaderText
        {
            get => lblHeader.Text;
            set => lblHeader.Text = value;
        }

        /// <summary>Muestra la foto de perfil si hay una; si no, las iniciales (nombre + primer apellido).</summary>
        public void SetAvatar(string? fotoBase64, string? nombreCompleto)
        {
            lblIniciales.Text = CalcularIniciales(nombreCompleto);

            if (string.IsNullOrWhiteSpace(fotoBase64))
            {
                imgAvatar.IsVisible = false;
                lblIniciales.IsVisible = true;
                return;
            }

            try
            {
                var bytes = Convert.FromBase64String(fotoBase64);
                imgAvatar.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                imgAvatar.IsVisible = true;
                lblIniciales.IsVisible = false;
            }
            catch
            {
                imgAvatar.IsVisible = false;
                lblIniciales.IsVisible = true;
            }
        }

        private static string CalcularIniciales(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return "?";

            var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var iniciales = string.Concat(partes.Take(2).Select(p => char.ToUpperInvariant(p[0])));
            return string.IsNullOrEmpty(iniciales) ? "?" : iniciales;
        }

        public Task ToggleAsync() => _isOpen ? CloseAsync() : OpenAsync();

        public async Task OpenAsync()
        {
            if (_isOpen) return;
            _isOpen = true;
            IsVisible = true;
            InputTransparent = false;
            backdrop.InputTransparent = false;
            await Task.WhenAll(
                backdrop.FadeTo(0.4, 200),
                panel.TranslateTo(0, 0, 250, Easing.CubicOut));
        }

        public async Task CloseAsync()
        {
            if (!_isOpen) return;
            _isOpen = false;
            await Task.WhenAll(
                backdrop.FadeTo(0, 200),
                panel.TranslateTo(-DrawerWidth, 0, 250, Easing.CubicIn));
            backdrop.InputTransparent = true;
            InputTransparent = true;
            IsVisible = false;
        }

        private async void OnBackdropTapped(object? sender, TappedEventArgs e) => await CloseAsync();

        private async void OnPerfilTapped(object? sender, TappedEventArgs e)
        {
            await CloseAsync();
            PerfilTapped?.Invoke(this, EventArgs.Empty);
        }

        private async void OnCerrarSesionTapped(object? sender, TappedEventArgs e)
        {
            await CloseAsync();
            CerrarSesionTapped?.Invoke(this, EventArgs.Empty);
        }
    }
}
