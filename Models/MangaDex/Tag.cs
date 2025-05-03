using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Mangadex
{
    public class Tag
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public TagAttributes? Attributes { get; set; }
    }

    public class TagAttributes
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("group")]
        public string? Group { get; set; } // "content", "format", "genre", "theme"
        
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }

    // Response cho danh sách Tag
    public class TagListResponse : BaseListResponse<Tag> { }
}