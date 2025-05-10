using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Mangadex
{
    public class ScanlationGroup
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public ScanlationGroupAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class ScanlationGroupAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("altNames")]
        public List<Dictionary<string, string>>? AltNames { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("ircServer")]
        public string? IrcServer { get; set; }

        [JsonPropertyName("ircChannel")]
        public string? IrcChannel { get; set; }

        [JsonPropertyName("discord")]
        public string? Discord { get; set; }

        [JsonPropertyName("contactEmail")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("mangaUpdates")]
        public string? MangaUpdates { get; set; }

        [JsonPropertyName("focusedLanguage")]

        public List<string>? FocusedLanguages { get; set; }
        [JsonPropertyName("locked")]

        public bool Locked { get; set; }

        [JsonPropertyName("official")]
        public bool Official { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("inactive")]
        public bool Inactive { get; set; }

        [JsonPropertyName("exLicensed")]
        public bool ExLicensed { get; set; }

        [JsonPropertyName("publishDelay")]
        public string? PublishDelay { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    // Các lớp Response
    public class ScanlationGroupResponse : BaseEntityResponse<ScanlationGroup> { }
    public class ScanlationGroupList : BaseListResponse<ScanlationGroup> { }
}