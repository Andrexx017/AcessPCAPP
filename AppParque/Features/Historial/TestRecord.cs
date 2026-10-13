namespace AppParque.Features.Historial
{
    /// <summary>Una fila del historial nuevo (motor de capacidades, ver /api/historial). No tiene
    /// Grupos/Condiciones — esos pertenecen al <see cref="HistorialItemDto"/> del motor viejo, que
    /// sigue existiendo solo para el agregado de InicioAdminViewModel.</summary>
    public class TestRecord
    {
        public int Id { get; set; }
        public string VisitorName { get; set; } = "";
        public string VisitorId { get; set; } = "";
        public int NurseId { get; set; }
        public string NurseName { get; set; } = "";
        public DateTime FechaValue { get; set; }
        public string Date { get; set; } = "";

        public int Permitidas { get; set; }
        public int ConMitigacion { get; set; }
        public int PorVerificar { get; set; }
        public int Restringidas { get; set; }
        public bool TuvoCondicionAdicional { get; set; }
        public bool TuvoRespuestaIA { get; set; }

        public string Iniciales
        {
            get
            {
                var partes = (VisitorName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var iniciales = string.Concat(partes.Take(2).Select(p => char.ToUpperInvariant(p[0])));
                return string.IsNullOrEmpty(iniciales) ? "?" : iniciales;
            }
        }

        public bool TieneAlertas => ConMitigacion + PorVerificar + Restringidas > 0;

        public string MetaLinea => $"{NurseName} · {Date}";

        /// <summary>Texto plano usado tanto en la tarjeta como en la exportación a CSV/Excel.</summary>
        public string Answers
        {
            get
            {
                var partes = new List<string>();
                if (ConMitigacion > 0) partes.Add($"{ConMitigacion} con condición");
                if (PorVerificar > 0) partes.Add($"{PorVerificar} por verificar");
                if (Restringidas > 0) partes.Add($"{Restringidas} restringidas");
                if (partes.Count == 0) partes.Add($"{Permitidas} permitidas, sin restricciones");

                if (TuvoCondicionAdicional) partes.Add("✎ nota agregada");
                if (TuvoRespuestaIA) partes.Add("🩺 con IA");

                return string.Join(" · ", partes);
            }
        }
    }
}
