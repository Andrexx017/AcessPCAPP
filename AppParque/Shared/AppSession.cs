namespace AppParque.Shared
{
    public static class UsuarioGlobal
    {
        // Asignados en el login contra AppParque.Api (ver Features/Auth/LoginViewModel.cs)
        public static int UsuarioId { get; set; }
        public static string Role { get; set; }      // "Admin" o "Enfermero" (RolUsuario del backend)
        public static string nurseName { get; set; } // nombre de usuario logueado (médico/enfermero)

        public static bool EsAdmin => Role == "Admin";

        // Datos del visitante capturados en el formulario, y su Id ya persistido en AppParque.Api
        // (ver Features/RegistroVisitante/RegisterDataView.xaml.cs)
        public static int VisitanteId { get; set; }
        public static string Name { get; set; }
        public static string TypeId { get; set; }
        public static string IdNumber { get; set; }
        public static string Age { get; set; }
        public static string Stature { get; set; }
    }
}
