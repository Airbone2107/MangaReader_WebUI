using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Relationship
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("related")]
        public string? Related { get; set; } // Chỉ có khi type là manga_relation

        // Thuộc tính attributes sẽ được thêm khi có Reference Expansion
        // Kiểu dữ liệu có thể là MangaAttributes, AuthorAttributes, etc. tùy vào 'type'
        // Sử dụng object? để linh hoạt, hoặc tạo các lớp con nếu cần xử lý chi tiết
        [JsonPropertyName("attributes")]
        public object? Attributes { get; set; }
    }
} 