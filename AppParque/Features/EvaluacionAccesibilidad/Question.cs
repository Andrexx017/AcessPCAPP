using System.Collections.Generic;

namespace AppParque.Features.EvaluacionAccesibilidad
{
    /// <summary>Opción de respuesta tal como se muestra en el test — envuelve OpcionRespuestaDto
    /// para exponer solo lo que la UI necesita (Id para enviarlo de vuelta, Texto para mostrarlo).</summary>
    public class OptionDisplay
    {
        public int Id { get; }
        public string Texto { get; }

        public OptionDisplay(OpcionRespuestaDto dto)
        {
            Id = dto.Id;
            Texto = dto.Texto;
        }
    }

    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public List<OptionDisplay> Options { get; set; } = new();
        public List<int> SelectedOptionIds { get; set; } = new();

        public bool IsMultipleChoice { get; set; } = false;
    }
}
