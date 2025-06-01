namespace MangaReaderLib.DTOs.Chapters
{
    public class CreateChapterRequestDto
    {
        public Guid TranslatedMangaId { get; set; }
        public string? Volume { get; set; }
        public string? ChapterNumber { get; set; }
        public string? Title { get; set; }
        public DateTime PublishAt { get; set; }
        public DateTime ReadableAt { get; set; }
        public int UploadedByUserId { get; set; }
    }
} 