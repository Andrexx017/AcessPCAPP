namespace AppParque.Shared
{
    public partial class ThemeSwitchView : ContentView
    {
        public ThemeSwitchView()
        {
            InitializeComponent();
            ThemeSwitch.IsToggled = ThemeService.IsDarkMode;
            UpdateIcon(ThemeSwitch.IsToggled);
        }

        private void OnThemeToggled(object sender, ToggledEventArgs e)
        {
            ThemeService.SetDarkMode(e.Value);
            UpdateIcon(e.Value);
        }

        private void UpdateIcon(bool isDark) => IconLabel.Text = isDark ? "\U0001F319" : "☀";
    }
}
