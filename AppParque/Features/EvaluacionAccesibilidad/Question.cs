using System.Collections.Generic;

namespace AppParque.Features.EvaluacionAccesibilidad
{
    public class Question
    {
        public string Text { get; set; }
        public List<string> Options { get; set; }
        public List<string> SelectedAnswers { get; set; } = new List<string>();

        public bool IsMultipleChoice { get; set; } = false;
    }
}
