using System.Text.Json.Serialization;

namespace AppParque.Shared.Models
{
    public class Attraction
    {
        [JsonPropertyName("nombre")]
        public string name { get; set; }

        [JsonPropertyName("descripcion")]
        public string description { get; set; }

        [JsonPropertyName("altura_minima")]
        public int? stature_min { get; set; }

        [JsonPropertyName("altura_maxima")]
        public int? stature_max { get; set; }

        [JsonPropertyName("imagenURL")]
        public string imageURL { get; set; }

        [JsonPropertyName("restricciones")]
        public Restrictions restrictions { get; set; }
    }
}
