using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Mangadex
{
    public class Manga
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public MangaAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class MangaAttributes
    {
        [JsonPropertyName("title")]
        public Dictionary<string, string>? Title { get; set; }

        [JsonPropertyName("altTitles")]
        public List<Dictionary<string, string>>? AltTitles { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }

        [JsonPropertyName("links")]
        public Dictionary<string, string>? Links { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string OriginalLanguage { get; set; } = default!;

        [JsonPropertyName("lastVolume")]
        public string? LastVolume { get; set; }

        [JsonPropertyName("lastChapter")]
        public string? LastChapter { get; set; }

        [JsonPropertyName("publicationDemographic")]
        public string? PublicationDemographic { get; set; } // "shounen", "shoujo", "josei", "seinen"

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!; // "completed", "ongoing", "cancelled", "hiatus"

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("contentRating")]
        public string ContentRating { get; set; } = default!; // "safe", "suggestive", "erotica", "pornographic"

        [JsonPropertyName("chapterNumbersResetOnNewVolume")]
        public bool ChapterNumbersResetOnNewVolume { get; set; }

        [JsonPropertyName("availableTranslatedLanguages")]
        public List<string>? AvailableTranslatedLanguages { get; set; }

        [JsonPropertyName("latestUploadedChapter")]
        public Guid? LatestUploadedChapter { get; set; }

        [JsonPropertyName("tags")]
        public List<Tag>? Tags { get; set; } // Sẽ được populate nếu có include=tag

        [JsonPropertyName("state")]
        public string State { get; set; } = default!; // "draft", "submitted", "published", "rejected"

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
        // Các lớp Response
    public class MangaResponse : BaseEntityResponse<Manga> { }
    public class MangaList : BaseListResponse<Manga> { }

    // Lớp cơ sở cho các response trả về một entity
    public class BaseEntityResponse<T>
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!; // "ok" or "error"

        [JsonPropertyName("response")]
        public string Response { get; set; } = default!;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    // Lớp cơ sở cho các response trả về một danh sách
    public class BaseListResponse<T>
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!; // "ok" or "error"

        [JsonPropertyName("response")]
        public string Response { get; set; } = default!;

        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}