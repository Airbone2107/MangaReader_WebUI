using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Auth
{
    public class UserModel
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("googleId")]
        public string GoogleId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("photoURL")]
        public string PhotoURL { get; set; }

        [JsonPropertyName("followingManga")]
        public List<string> FollowingManga { get; set; } = new List<string>();

        [JsonPropertyName("readingManga")]
        public List<ReadingMangaInfo> ReadingManga { get; set; } = new List<ReadingMangaInfo>();
    }

    public class ReadingMangaInfo
    {
        [JsonPropertyName("mangaId")]
        public string MangaId { get; set; }

        [JsonPropertyName("lastChapter")]
        public string LastChapter { get; set; }

        [JsonPropertyName("lastReadAt")]
        public DateTime LastReadAt { get; set; }
    }
} 