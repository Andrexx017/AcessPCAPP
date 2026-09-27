namespace AppParque.Shared
{
    public static class UsuarioGlobal
    {
        // Asignar en el login real
        public static string Uid { get; set; }      // uid del usuario logueado
        public static string Role { get; set; }     // "admin" o "user"
        public static string nurseName { get; set; } // nombre de usuario logueado (médico/enfermero)

        // Datos del visitante capturados en el formulario
        public static string Name { get; set; }
        public static string TypeId { get; set; }
        public static string IdNumber { get; set; }
        public static string Age { get; set; }
        public static string Stature { get; set; }
    }
}
