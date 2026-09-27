namespace AppParque.Api.Modules.Evaluaciones.Application;

/// <summary>
/// Traduce los códigos de las opciones de respuesta seleccionadas por el visitante a los
/// grupos (movilidad) y condiciones (médicas/sensoriales) que activa, cruzando después esos
/// códigos contra las restricciones de cada atracción.
///
/// Es un port fiel de TestViewModel.MapAnswersToRestrictions (AppParque/Features/EvaluacionAccesibilidad),
/// la lógica que ya usaba el prototipo — incluye una particularidad heredada: grupo_2 y grupo_3
/// se activan bajo exactamente la misma condición ((caminaSinApoyo || subeConAyuda) && agarreUna),
/// que la sección 5 de Requisitos-App-Evaluacion-Accesibilidad.md marca como pendiente de
/// confirmar con el conocimiento del negocio. No se "arregló" aquí para no inventar una regla
/// de negocio que nadie ha validado todavía.
/// </summary>
public static class MotorReglas
{
    public static (HashSet<string> GruposActivados, HashSet<string> CondicionesActivadas) Evaluar(IReadOnlySet<string> codigosRespuesta)
    {
        bool Tiene(string codigo) => codigosRespuesta.Contains(codigo);

        var caminaSinApoyo = Tiene("camina_sin_apoyo");
        var noCamina = Tiene("no_camina");

        var subeSinAyuda = Tiene("sube_sin_ayuda");
        var subeConAyuda = Tiene("sube_con_ayuda");

        var agarreAmbas = Tiene("agarre_ambas_manos");
        var agarreUna = Tiene("agarre_una_mano");
        var sinAgarre = Tiene("sin_agarre");

        var protesisPiernaDebajoRodilla = Tiene("protesis_pierna_debajo_rodilla");
        var protesisPiernaArribaRodilla = Tiene("protesis_pierna_arriba_rodilla");
        var protesisPiernaAmbas = Tiene("protesis_pierna_ambas");
        var protesisManoUna = Tiene("protesis_mano_una");
        var protesisManoAmbas = Tiene("protesis_mano_ambas");

        var grupos = new HashSet<string>();

        if (noCamina)
            grupos.Add("grupo_1");

        if (caminaSinApoyo && subeSinAyuda && sinAgarre)
            grupos.Add("grupo_1_1");

        if ((caminaSinApoyo || subeConAyuda) && agarreUna)
        {
            grupos.Add("grupo_2");
            grupos.Add("grupo_3");
        }

        if (subeConAyuda && agarreAmbas)
            grupos.Add("grupo_4");

        if (caminaSinApoyo && subeSinAyuda && agarreUna && !protesisPiernaDebajoRodilla && !protesisPiernaArribaRodilla)
            grupos.Add("grupo_5");

        if (caminaSinApoyo && subeSinAyuda && agarreAmbas && (protesisPiernaDebajoRodilla || protesisPiernaAmbas))
            grupos.Add("grupo_5_1");

        if (caminaSinApoyo && subeSinAyuda && protesisPiernaArribaRodilla)
            grupos.Add("grupo_5_2");

        if (Tiene("grupo10_si"))
            grupos.Add("grupo_10");

        if (protesisManoUna || protesisManoAmbas)
            grupos.Add("protesis_mano");

        if (protesisPiernaArribaRodilla)
            grupos.Add("protesis_pierna_arriba_rodilla");

        var condiciones = new HashSet<string>();

        if (Tiene("discapacidad_visual_total") || Tiene("discapacidad_visual_parcial"))
            condiciones.Add("discapacidad_visual");

        if (Tiene("discapacidad_auditiva_si"))
            condiciones.Add("discapacidad_auditiva");

        if (Tiene("discapacidad_cognitiva_si"))
            condiciones.Add("discapacidad_cognitiva");

        if (Tiene("cond_embarazo"))
            condiciones.Add("embarazo");

        if (Tiene("cond_cardiacos"))
            condiciones.Add("problemas_cardiacos");

        if (Tiene("cond_columna"))
            condiciones.Add("problemas_columna");

        if (Tiene("cond_mareo_vertigo"))
            condiciones.Add("mareo_vertigo");

        if (Tiene("cond_miedo_espacios_cerrados"))
            condiciones.Add("miedo_espacios_cerrados");

        return (grupos, condiciones);
    }
}
