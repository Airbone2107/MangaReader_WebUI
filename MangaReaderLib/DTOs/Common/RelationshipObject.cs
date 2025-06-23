using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class RelationshipObject
    {
        [JsonPropertyName("id")]
        [JsonPropertyOrder(1)]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [JsonPropertyOrder(2)]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")] 
        [JsonPropertyOrder(3)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? Attributes { get; set; } 
    }
} 