using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class ApiError
    {
        [JsonPropertyName("id")]
        [JsonPropertyOrder(1)]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        [JsonPropertyOrder(2)]
        public int Status { get; set; }

        [JsonPropertyName("title")]
        [JsonPropertyOrder(3)]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        [JsonPropertyOrder(4)]
        public string? Detail { get; set; }

        [JsonPropertyName("context")]
        [JsonPropertyOrder(5)]
        public object? Context { get; set; }

        public ApiError(int status, string title, string? detail = null, string? id = null, object? context = null)
        {
            Status = status;
            Title = title;
            Detail = detail;
            Id = id;
            Context = context;
        }
    }
} 