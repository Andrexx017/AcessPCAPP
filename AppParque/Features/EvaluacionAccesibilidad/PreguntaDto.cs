namespace AppParque.Features.EvaluacionAccesibilidad;

public record OpcionRespuestaDto(int Id, string Texto, int Orden);

public record PreguntaDto(int Id, string Texto, int Orden, string Tipo, List<OpcionRespuestaDto> Opciones);
