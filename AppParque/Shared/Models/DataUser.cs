using System.Text.Json.Serialization;

namespace AppParque.Shared.Models
{
    // 👇 Usamos este modelo para: registro del visitante y resultado del test
    public class DataUser
    {
        [JsonPropertyName("VisitorName")]
        public string name { get; set; }
        [JsonPropertyName("VisitorId")]
        public string idNumber { get; set; }
        [JsonPropertyName("Date")]
        public string date { get; set; }
        [JsonPropertyName("Answers")]
        public string answers { get; set; }
        public string typeId { get; set; }
        public string age { get; set; }
        public string stature { get; set; }
    }
}
