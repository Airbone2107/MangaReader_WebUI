using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Mangadex
{
    public class Author
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!; // "author" hoặc "artist"

        [JsonPropertyName("attributes")]
        public AuthorAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class AuthorAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("biography")]
        public Dictionary<string, string>? Biography { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("pixiv")]
        public string? Pixiv { get; set; }

        [JsonPropertyName("melonBook")]
        public string? MelonBook { get; set; }

        [JsonPropertyName("fanBox")]
        public string? FanBox { get; set; }

        [JsonPropertyName("booth")]
        public string? Booth { get; set; }

        [JsonPropertyName("nicoVideo")]
        public string? NicoVideo { get; set; }

        [JsonPropertyName("skeb")]
        public string? Skeb { get; set; }

        [JsonPropertyName("fantia")]
        public string? Fantia { get; set; }

        [JsonPropertyName("tumblr")]
        public string? Tumblr { get; set; }

        [JsonPropertyName("youtube")]
        public string? Youtube { get; set; }

        [JsonPropertyName("weibo")]
        public string? Weibo { get; set; }

        [JsonPropertyName("naver")]
        public string? Naver { get; set; }

        [JsonPropertyName("namicomi")]
        public string? Namicomi { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    // Các lớp Response
    public class AuthorResponse : BaseEntityResponse<Author> { }
    public class AuthorList : BaseListResponse<Author> { }
}