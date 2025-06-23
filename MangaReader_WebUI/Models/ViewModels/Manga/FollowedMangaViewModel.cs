using MangaReader.WebUI.Models.ViewModels.Chapter;
using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class FollowedMangaViewModel
    {
        public string MangaId { get; set; } = string.Empty;
        public string MangaTitle { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public List<SimpleChapterInfoViewModel> LatestChapters { get; set; } = new List<SimpleChapterInfoViewModel>();
    }
} 