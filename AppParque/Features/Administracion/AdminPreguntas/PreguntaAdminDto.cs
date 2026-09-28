namespace AppParque.Features.Administracion.AdminPreguntas;

public record OpcionRespuestaAdminDto(int Id, string Codigo, string Texto, int Orden);

public record PreguntaAdminDto(int Id, string Texto, string Tipo, int Orden, bool Activa, List<OpcionRespuestaAdminDto> Opciones);
