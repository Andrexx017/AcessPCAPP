namespace AppParque.Features.Administracion.AdminZonas;

public record OpcionCapacidadAdminDto(int Id, string Texto, string Valor, int Orden);

public record PreguntaCapacidadAdminDto(
    int Id,
    int ZonaId,
    string Clave,
    string Titulo,
    string? Ayuda,
    int Eje,
    string Tipo,
    string? Unidad,
    int Orden,
    string? DependeDeClave,
    string? ActivaCuandoCsv,
    string? ValorSinNovedad,
    bool Activa,
    bool EsInformativa,
    List<OpcionCapacidadAdminDto> Opciones);

public record ZonaCapacidadAdminDto(
    int Id,
    string Codigo,
    string Nombre,
    string Corto,
    string Nota,
    string ResumenSinNovedad,
    bool EnCuerpo,
    bool PermiteSinNovedad,
    int Orden,
    bool Activa,
    List<PreguntaCapacidadAdminDto> Preguntas);
