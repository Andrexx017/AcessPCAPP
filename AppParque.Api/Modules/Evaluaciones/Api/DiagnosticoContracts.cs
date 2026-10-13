using AppParque.Api.Modules.CatalogoAtracciones.Domain.Requisitos;
using AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;

namespace AppParque.Api.Modules.Evaluaciones.Api;

/// <summary>
/// El perfil viaja tal cual lo define el vocabulario: el contrato de la API ES el vocabulario de
/// capacidades, no una traducción de él. Así una dimensión nueva se agrega en un solo lugar.
/// </summary>
/// <param name="Perfil">Dimensiones del visitante. Lo que no se envía queda en
/// <c>NoDeterminado</c>, y el motor lo reporta como dato faltante en vez de asumirlo.</param>
/// <param name="Atracciones">Códigos a evaluar. Vacío o null = las 29 del catálogo.</param>
public record DiagnosticoRequest(PerfilCapacidades Perfil, List<string>? Atracciones = null);

public record MotivoResponse(string Codigo, string Dimension, string Texto, string Severidad);

/// <param name="Descripcion">Para la guía del visitante en PDF: lo que es la atracción, no por qué
/// quedó en este veredicto (eso lo dicen <see cref="Motivos"/>).</param>
public record DictamenAtraccionResponse(
    string Codigo,
    string Nombre,
    string Intensidad,
    string Rol,
    string Veredicto,
    List<MotivoResponse> Motivos,
    List<string> MitigacionesRequeridas,
    List<string> Advertencias,
    string? Descripcion,
    int? AlturaMinimaCm,
    int? AlturaMaximaCm);

/// <summary>
/// RF-05 reescrito: ya no es una lista de atracciones preseleccionadas sino un dictamen por
/// atracción. <see cref="DimensionesSinDato"/> es la parte que el modelo de grupos no podía
/// producir: las preguntas que faltan por hacerle al visitante.
/// </summary>
public record DiagnosticoResponse(
    int TotalEvaluadas,
    int Permitidas,
    int ConMitigacion,
    int PorVerificar,
    int Restringidas,
    List<string> DimensionesSinDato,
    List<DictamenAtraccionResponse> Atracciones);

public record RequisitosResumenResponse(
    string Codigo,
    string Nombre,
    string Intensidad,
    string? Descripcion,
    bool SinRequisitosFisicos,
    bool DistinguePorRol,
    int? AlturaMinimaCm,
    int? AlturaMaximaCm,
    int? PesoMaximoKg,
    string Marcha,
    string Agarre,
    List<string> RestriccionesMedicas,
    List<string> Mitigaciones);

public static class DiagnosticoMapper
{
    public static DiagnosticoResponse ADto(DictamenVisita dictamen, CatalogoRequisitos catalogo) => new(
        dictamen.Atracciones.Count,
        dictamen.Permitidas,
        dictamen.ConMitigacion,
        dictamen.PorVerificar,
        dictamen.Restringidas,
        dictamen.DimensionesSinDato.ToList(),
        dictamen.Atracciones.Select(a => ADto(a, catalogo)).ToList());

    private static DictamenAtraccionResponse ADto(DictamenAtraccion dictamen, CatalogoRequisitos catalogo)
    {
        var ficha = catalogo.Buscar(dictamen.CodigoAtraccion);
        var requisitosRol = ficha?.ParaRol(dictamen.Rol);

        return new(
            dictamen.CodigoAtraccion,
            dictamen.NombreAtraccion,
            ficha?.Intensidad ?? "media",
            dictamen.Rol.ToString(),
            dictamen.Veredicto.ToString(),
            dictamen.Motivos
                .Where(m => m.Severidad != Severidad.Informativo)
                .Select(m => new MotivoResponse(m.Codigo, m.Dimension, m.Texto, m.Severidad.ToString()))
                .ToList(),
            dictamen.MitigacionesRequeridas.ToList(),
            // Lo informativo no decide nada, pero es lo que debe salir impreso en la guía del visitante
            // (asegurar la prótesis, ir en la silla del centro…), así que viaja aparte de los motivos.
            dictamen.Motivos
                .Where(m => m.Severidad == Severidad.Informativo)
                .Select(m => m.Texto)
                .ToList(),
            ficha?.Descripcion,
            requisitosRol?.AlturaMinimaCm,
            requisitosRol?.AlturaMaximaCm);
    }

    public static RequisitosResumenResponse ADto(RequisitosAtraccion requisitos) => new(
        requisitos.Codigo,
        requisitos.Nombre,
        requisitos.Intensidad,
        requisitos.Descripcion,
        requisitos.SinRequisitosFisicos,
        requisitos.DistinguePorRol,
        requisitos.General.AlturaMinimaCm,
        requisitos.General.AlturaMaximaCm,
        requisitos.General.PesoMaximoKg,
        requisitos.General.Marcha,
        requisitos.General.Agarre,
        requisitos.RestriccionesMedicas,
        requisitos.Acompanante.Mitigaciones);
}
