using MangaReader.WebUI.Models.Mangadex;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    /// <summary>
    /// Service xử lý các mối quan hệ (relationships) của manga
    /// </summary>
    public class MangaRelationshipService
    {
        private readonly ILogger<MangaRelationshipService> _logger;
        private readonly JsonSerializerOptions _jsonOptions; // Để deserialize attributes

        public MangaRelationshipService(ILogger<MangaRelationshipService> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        /// <summary>
        /// Lấy thông tin tác giả và họa sĩ từ relationships của manga
        /// </summary>
        /// <param name="relationships">Danh sách relationship của manga</param>
        /// <returns>Tuple chứa thông tin tác giả và họa sĩ</returns>
        public (string author, string artist) GetAuthorArtist(List<Relationship>? relationships)
        {
            string author = "Không rõ";
            string artist = "Không rõ";

            if (relationships == null) return (author, artist);

            try
            {
                foreach (var rel in relationships)
                {
                    if (rel == null) continue;

                    string relType = rel.Type;

                    // Chỉ xử lý author và artist
                    if (relType == "author" || relType == "artist")
                    {
                        // Kiểm tra xem attributes có được include không
                        if (rel.Attributes != null)
                        {
                            try
                            {
                                // Thử deserialize attributes thành AuthorAttributes
                                // Cần đảm bảo rằng API trả về cấu trúc này khi include author/artist
                                var attributesJson = JsonSerializer.Serialize(rel.Attributes); // Serialize lại object
                                var authorAttributes = JsonSerializer.Deserialize<AuthorAttributes>(attributesJson, _jsonOptions);

                                if (authorAttributes?.Name != null)
                                {
                                    if (relType == "author")
                                        author = authorAttributes.Name;
                                    else if (relType == "artist")
                                        artist = authorAttributes.Name;
                                }
                                else
                                {
                                     _logger.LogWarning($"Attributes của relationship {rel.Id} (type: {relType}) không chứa 'name'.");
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                 _logger.LogError(jsonEx, $"Lỗi deserialize attributes cho relationship {rel.Id} (type: {relType}). Attributes: {rel.Attributes}");
                            }
                            catch (Exception attrEx)
                            {
                                 _logger.LogError(attrEx, $"Lỗi không xác định khi xử lý attributes cho relationship {rel.Id} (type: {relType}).");
                            }
                        }
                        else
                        {
                             _logger.LogWarning($"Relationship {rel.Id} (type: {relType}) không có attributes. Đảm bảo có 'includes[]=author&includes[]=artist' trong lời gọi API.");
                             // Fallback: Có thể gọi API /author/{id} hoặc /artist/{id} nếu cần thiết, nhưng sẽ chậm
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý relationships.");
            }

            return (author, artist);
        }

        /// <summary>
        /// Lấy thông tin tác giả và họa sĩ từ relationships của manga (phương thức cũ để tương thích ngược)
        /// </summary>
        /// <param name="mangaDict">Dictionary chứa thông tin manga</param>
        /// <returns>Tuple chứa thông tin tác giả và họa sĩ</returns>
        public (string author, string artist) GetAuthorArtist(Dictionary<string, object> mangaDict)
        {
            string author = "Không rõ";
            string artist = "Không rõ";

            try
            {
                if (!mangaDict.ContainsKey("relationships") || mangaDict["relationships"] == null)
                    return (author, artist);

                var relationships = (List<object>)mangaDict["relationships"];
                
                foreach (var rel in relationships)
                {
                    if (rel is not Dictionary<string, object> relDict)
                        continue;

                    if (!relDict.ContainsKey("type") || !relDict.ContainsKey("id"))
                        continue;

                    string relType = relDict["type"].ToString();
                    string relId = relDict["id"].ToString();
                    
                    // Xử lý tác giả và họa sĩ từ relationships
                    if (relType == "author" || relType == "artist")
                    {
                        if (relDict.ContainsKey("attributes") && relDict["attributes"] != null)
                        {
                            var attrs = (Dictionary<string, object>)relDict["attributes"];
                            if (attrs.ContainsKey("name") && attrs["name"] != null)
                            {
                                if (relType == "author")
                                    author = attrs["name"].ToString();
                                else if (relType == "artist")
                                    artist = attrs["name"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý relationships từ Dictionary.");
            }
            
            return (author, artist);
        }
    }

    // Class AuthorAttributes để deserialize attributes của author/artist relationship
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

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
