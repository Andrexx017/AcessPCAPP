namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Decide con qué pantalla arranca la evaluación. Es el único sitio que lo sabe, así que los puntos
/// de entrada (registro del visitante y confirmación de evaluación previa) no tienen que repetir
/// la decisión ni enterarse de que hay dos flujos.
///
/// No son dos layouts del mismo flujo: en móvil el mapa y el test se alternan a pantalla completa
/// (tres pasos), y en escritorio conviven en dos paneles con las preguntas de la zona desplegadas.
/// Por eso se elige por idioma de dispositivo y no por un breakpoint de ancho — el ancho decide
/// dentro de la pantalla de escritorio si los paneles van al lado o uno encima del otro.
/// </summary>
public static class EvaluacionFlujo
{
    public static Page CrearPagina() => DeviceInfo.Idiom == DeviceIdiom.Phone
        ? new MapaCapacidadesView()
        : new MapaCapacidadesEscritorioView();
}
