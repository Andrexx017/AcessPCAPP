using AppParque.Features.Auth;
using AppParque.Services;


namespace AppParque
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new LoginView());

            // Ejecutar prueba de conexión
            Task.Run(async () =>
            {
                var firebaseService = new FireBaseService();
                var isConnected = await firebaseService.TestConnectionAsync();
                Console.WriteLine("Conexión con Firebase: " + (isConnected ? "Exitosa" : "Fallida"));

                if (!isConnected)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MainPage.DisplayAlert("Error", "No se pudo conectar con Firebase", "OK");
                    });
                }
            });
        }


    }
}