using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class ErrorResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!;

        [JsonPropertyName("errors")]
        public List<Error>? Errors { get; set; }

        public class Error
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("status")]
            public int Status { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("detail")]
            public string? Detail { get; set; }

            [JsonPropertyName("context")]
            public JsonElement? Context { get; set; }
        }
    }
}