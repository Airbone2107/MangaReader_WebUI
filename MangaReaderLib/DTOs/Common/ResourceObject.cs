using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class ResourceObject<TAttributes> where TAttributes : class
    {
        [JsonPropertyName("id")]
        [JsonPropertyOrder(1)]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [JsonPropertyOrder(2)]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        [JsonPropertyOrder(3)]
        public TAttributes Attributes { get; set; } = null!;

        [JsonPropertyName("relationships")]
        [JsonPropertyOrder(4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RelationshipObject>? Relationships { get; set; }
    }
} 