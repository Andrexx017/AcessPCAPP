namespace AppParque.Features.Historial
{
    public class TestRecord
    {
        public string VisitorName { get; set; } = "";
        public string VisitorId { get; set; } = "";
        public int NurseId { get; set; }
        public string NurseName { get; set; } = "";
        public DateTime FechaValue { get; set; }
        public string Date { get; set; } = "";
        public string Stature { get; set; } = "";
        public List<RestriccionDto> Restricciones { get; set; } = new();

        public string Iniciales
        {
            get
            {
                var partes = (VisitorName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var iniciales = string.Concat(partes.Take(2).Select(p => char.ToUpperInvariant(p[0])));
                return string.IsNullOrEmpty(iniciales) ? "?" : iniciales;
            }
        }

        public bool TieneRestricciones => Restricciones.Count > 0;

        public string MetaLinea => $"{NurseName} · {Date} · {Stature} cm";

        /// <summary>Texto plano usado tanto en la tarjeta como en la exportación a CSV/Excel.</summary>
        public string Answers => Restricciones.Count == 0
            ? "Sin restricciones activadas"
            : string.Join(", ", Restricciones.Select(r => r.Nombre));
    }
}
