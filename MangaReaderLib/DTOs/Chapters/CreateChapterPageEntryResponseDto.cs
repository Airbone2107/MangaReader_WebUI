using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Chapters
{
    public class CreateChapterPageEntryResponseDto
    {
        [JsonPropertyName("pageId")]
        public Guid PageId { get; set; }
    }
} 