using AppParque.Api.Modules.CatalogoPreguntas.Domain;

namespace AppParque.Api.Modules.CatalogoPreguntas.Api;

public record OpcionRespuestaAdminRequest(string Codigo, string Texto, int Orden);

public record OpcionRespuestaAdminResponse(int Id, string Codigo, string Texto, int Orden)
{
    public static OpcionRespuestaAdminResponse From(OpcionRespuesta o) => new(o.Id, o.Codigo, o.Texto, o.Orden);
}

public record PreguntaAdminRequest(string Texto, string Tipo, int Orden, bool Activa);

public record CrearPreguntaRequest(string Texto, string Tipo, int Orden, bool Activa, List<OpcionRespuestaAdminRequest> Opciones);

public record PreguntaAdminResponse(int Id, string Texto, string Tipo, int Orden, bool Activa, List<OpcionRespuestaAdminResponse> Opciones)
{
    public static PreguntaAdminResponse From(Pregunta p) => new(
        p.Id,
        p.Texto,
        p.Tipo.ToString(),
        p.Orden,
        p.Activa,
        p.Opciones.OrderBy(o => o.Orden).Select(OpcionRespuestaAdminResponse.From).ToList());
}
