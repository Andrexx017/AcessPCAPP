namespace AppParque.Shared
{
    public partial class ThemeSwitchView : ContentView
    {
        public ThemeSwitchView()
        {
            InitializeComponent();
            UpdateIcon(ThemeService.IsDarkMode);
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            bool modoOscuro = !ThemeService.IsDarkMode;
            ThemeService.SetDarkMode(modoOscuro);
            UpdateIcon(modoOscuro);
        }

        // Font Awesome Free Solid: f185 = sun, f186 = moon.
        private void UpdateIcon(bool isDark) => IconLabel.Text = isDark ? "" : "";
    }
}
