using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Chapters
{
    public class UploadChapterPageImageResponseDto
    {
        [JsonPropertyName("publicId")]
        public string PublicId { get; set; } = string.Empty;
    }
} 