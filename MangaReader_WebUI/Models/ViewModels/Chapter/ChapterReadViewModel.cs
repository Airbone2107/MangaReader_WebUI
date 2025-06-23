using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Chapter
{
    public class ChapterReadViewModel
    {
        public string MangaId { get; set; } = string.Empty;
        public string MangaTitle { get; set; } = string.Empty;
        public string ChapterId { get; set; } = string.Empty;
        public string ChapterTitle { get; set; } = string.Empty;
        public string ChapterNumber { get; set; } = string.Empty;
        public string ChapterLanguage { get; set; } = string.Empty;
        public List<string> Pages { get; set; } = new List<string>();
        public string? PrevChapterId { get; set; }
        public string? NextChapterId { get; set; }
        public List<ChapterViewModel> SiblingChapters { get; set; } = new List<ChapterViewModel>();
    }
} 