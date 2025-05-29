using System;

namespace MangaReaderLib.DTOs.TranslatedMangas
{
    public class CreateTranslatedMangaRequestDto
    {
        public Guid MangaId { get; set; }
        public string LanguageKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
} 