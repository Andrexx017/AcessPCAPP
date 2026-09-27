using System.Text.Json.Serialization;

namespace AppParque.Shared.Models
{
    public class User
    {
        [JsonPropertyName("NurseName")]
        public string nurseName { get; set; }

        [JsonPropertyName("username")]
        public string username { get; set; }

        [JsonPropertyName("password")]
        public string password { get; set; }

        [JsonPropertyName("role")]
        public string role { get; set; } // "admin" o "user"
    }
}
