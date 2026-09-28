using AppParque.Features.Auth;
using AppParque.Services;
using AppParque.Shared;


namespace AppParque
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ThemeService.Initialize();
            MainPage = new NavigationPage(new LoginView());

            // Prueba de conexión contra AppParque.Api (antes verificaba Firebase)
            Task.Run(async () =>
            {
                var resultado = await ApiClient.GetAsync<object>("/health");

                if (!resultado.Success)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MainPage.DisplayAlert(
                            "Error",
                            $"No se pudo conectar con el servidor ({ApiClient.BaseUrl}). Verifica tu conexión.",
                            "OK");
                    });
                }
            });
        }


    }
}
