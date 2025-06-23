using MangaReaderLib.DTOs.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReaderLib.Extensions
{
    /// <summary>
    /// Cung cấp các phương thức mở rộng tiện ích cho lớp RelationshipObject.
    /// </summary>
    public static class RelationshipExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Giải tuần tự thuộc tính 'Attributes' của một RelationshipObject thành một kiểu đối tượng cụ thể.
        /// </summary>
        /// <typeparam name="T">Kiểu DTO mong muốn (ví dụ: AuthorAttributesDto, CoverArtAttributesDto).</typeparam>
        /// <param name="relationship">Đối tượng RelationshipObject.</param>
        /// <param name="options">Tùy chọn cho JsonSerializer. Nếu để null, sẽ sử dụng cài đặt mặc định.</param>
        /// <returns>Một đối tượng kiểu T nếu giải tuần tự thành công, ngược lại trả về null.</returns>
        public static T? GetAttributesAs<T>(this RelationshipObject relationship, JsonSerializerOptions? options = null) where T : class
        {
            // THAY ĐỔI: Bây giờ chúng ta làm việc trực tiếp với JsonElement?
            if (!relationship.Attributes.HasValue)
            {
                return null;
            }

            try
            {
                // Lấy JsonElement từ thuộc tính nullable
                var jsonElement = relationship.Attributes.Value;
                // Thực hiện deserialize
                return jsonElement.Deserialize<T>(options ?? DefaultJsonOptions);
            }
            catch (JsonException ex)
            {
                // Ghi log lỗi nếu cần để debug
                // Console.WriteLine($"JsonException during deserialization in GetAttributesAs: {ex.Message}");
                return null;
            }
        }
    }
} 