namespace MangaReaderLib.DTOs.TranslatedMangas
{
    public class UpdateTranslatedMangaRequestDto
    {
        public string LanguageKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
} 