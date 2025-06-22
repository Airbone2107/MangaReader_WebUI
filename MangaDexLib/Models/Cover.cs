using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Cover
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public CoverAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class CoverAttributes
    {
        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = default!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    // Các lớp Response
    public class CoverResponse : BaseEntityResponse<Cover> { }
    public class CoverList : BaseListResponse<Cover> { }
} 