using System.Text.Json.Serialization;

namespace manga_reader_web.Models.Auth
{
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class GoogleAuthUrlResponse
    {
        [JsonPropertyName("authUrl")]
        public string AuthUrl { get; set; }
    }
} 