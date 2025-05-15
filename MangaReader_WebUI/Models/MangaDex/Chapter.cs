using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Mangadex
{
    public class Chapter
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public ChapterAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class ChapterAttributes
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("chapter")]
        public string? ChapterNumber { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("translatedLanguage")]
        public string TranslatedLanguage { get; set; } = default!;

        [JsonPropertyName("uploader")]
        public Guid? Uploader { get; set; } // ID của User

        [JsonPropertyName("externalUrl")]
        public string? ExternalUrl { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("publishAt")]
        public DateTimeOffset PublishAt { get; set; }

        [JsonPropertyName("readableAt")]
        public DateTimeOffset ReadableAt { get; set; }
    }

    // Các lớp Response
    public class ChapterResponse : BaseEntityResponse<Chapter> { }
    public class ChapterList : BaseListResponse<Chapter> { }

    // Model cho AtHome Server Response
    public class AtHomeServerResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!;

        [JsonPropertyName("baseUrl")]
        public string? BaseUrl { get; set; }

        [JsonPropertyName("chapter")]
        public AtHomeChapterData? Chapter { get; set; }
    }

    public class AtHomeChapterData
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("data")]
        public List<string>? Data { get; set; } // Danh sách tên file ảnh chất lượng cao

        [JsonPropertyName("dataSaver")]
        public List<string>? DataSaver { get; set; } // Danh sách tên file ảnh tiết kiệm dữ liệu
    }
}