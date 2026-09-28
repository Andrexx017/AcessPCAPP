namespace AppParque.Features.Administracion.AdminAtracciones;

public record RestriccionAdminDto(int Id, string Codigo, string Nombre, bool Permitido);

public record AtraccionAdminDto(
    int Id,
    string Nombre,
    string? Descripcion,
    string? ImagenUrl,
    int? AlturaMinima,
    int? AlturaMaxima,
    bool Activa,
    List<RestriccionAdminDto> RestriccionesGrupo,
    List<RestriccionAdminDto> RestriccionesCondicion);

public record GrupoCatalogoDto(int Id, string Codigo, string Nombre, string? Descripcion);

public record CondicionCatalogoDto(int Id, string Codigo, string Nombre, string? Descripcion);
