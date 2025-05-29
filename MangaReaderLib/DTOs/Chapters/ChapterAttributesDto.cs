using System;

namespace MangaReaderLib.DTOs.Chapters
{
    public class ChapterAttributesDto
    {
        public string? Volume { get; set; }
        public string? ChapterNumber { get; set; }
        public string? Title { get; set; }
        public int PagesCount { get; set; }
        public DateTime PublishAt { get; set; }
        public DateTime ReadableAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 