using System;

namespace MangaReaderLib.DTOs.TranslatedMangas
{
    public class TranslatedMangaAttributesDto
    {
        public string LanguageKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 