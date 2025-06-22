using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;

namespace MangaReader.WebUI.Models.ViewModels.History
{
    public class LastReadMangaViewModel
    {
        public string MangaId { get; set; } = string.Empty;
        public string MangaTitle { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public string ChapterId { get; set; } = string.Empty;
        public string ChapterTitle { get; set; } = string.Empty; 
        public DateTime ChapterPublishedAt { get; set; } 
        public DateTime LastReadAt { get; set; }
    }
} 