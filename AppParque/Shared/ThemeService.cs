namespace AppParque.Shared
{
    // Tema manual persistido por el usuario (ver ThemeSwitchView), en vez de seguir
    // el tema del sistema operativo — así todas las pantallas se ven consistentes.
    public static class ThemeService
    {
        private const string PreferenceKey = "AppTheme_IsDark";

        public static bool IsDarkMode => Application.Current?.UserAppTheme == AppTheme.Dark;

        public static void Initialize()
        {
            bool isDark = Preferences.Default.Get(PreferenceKey, Application.Current?.PlatformAppTheme == AppTheme.Dark);
            Apply(isDark);
        }

        public static void SetDarkMode(bool isDark)
        {
            Preferences.Default.Set(PreferenceKey, isDark);
            Apply(isDark);
        }

        private static void Apply(bool isDark)
        {
            if (Application.Current is not null)
                Application.Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
        }
    }
}
