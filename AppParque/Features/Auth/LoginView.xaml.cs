using AppParque.Services;
using AppParque.Features.MenuPrincipal;
using AppParque.Shared;

namespace AppParque.Features.Auth
{
    public partial class LoginView : ContentPage
    {
        private readonly LoginViewModel _viewModel = new LoginViewModel();

        public LoginView()
        {
            InitializeComponent();
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
                await DisplayAlert("Error", "Por favor ingrese usuario y contrase�a.", "OK");
                return;
            }

            try
            {
                var firebase = new FireBaseService();
                var users = await firebase.GetUserAsync();

                if (users != null)
                {
                    // Evitar NullReference: filtrar entradas con Value null antes de evaluar propiedades
                    var usuarioEncontrado = users
                        .Where(u => u.Value != null)
                        .FirstOrDefault(u => u.Value.username == username && u.Value.password == password);

                    if (usuarioEncontrado.Value != null)
                    {
                        var uid = usuarioEncontrado.Key;  // Este es el ID unico (nurse_001, nurse_002, etc.)
                        var datosUsuario = usuarioEncontrado.Value;

                        // Guardar datos en UsuarioGlobal
                        UsuarioGlobal.Uid = uid;
                        UsuarioGlobal.Role = datosUsuario.role;
                        UsuarioGlobal.nurseName = datosUsuario.nurseName;

                        await DisplayAlert("�xito", $"Bienvenido {UsuarioGlobal.nurseName}", "OK");

                        // Ir al menu principal
                        await Navigation.PushAsync(new MenuPrincipal());
                    }
                    else
                    {
                        await DisplayAlert("Error", "Usuario o contrase�a incorrectos", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo conectar a la base de datos", "OK");
                }
            }
            catch (Exception ex)
            {
                // Mostrar error y registrar (mejor enviar a un logger)
                await DisplayAlert("Error", $"Ocurri� un error: {ex.Message}", "OK");
                throw;
            }
        }
    }
}
