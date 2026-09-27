using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppParque.Shared.Models
{
    public class Restrictions
    {
        [JsonPropertyName("grupos")]
        public Dictionary<string, bool> grupos { get; set; } = new();

        [JsonPropertyName("condiciones")]
        public Dictionary<string, bool> condiciones { get; set; } = new();
    }
}
