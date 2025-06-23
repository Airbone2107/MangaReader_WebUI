using MangaReader.WebUI.Models.ViewModels.Chapter;
using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class MangaDetailViewModel
    {
        public MangaViewModel Manga { get; set; } = new MangaViewModel();
        public List<ChapterViewModel> Chapters { get; set; } = new List<ChapterViewModel>();
        public Dictionary<string, List<string>> AlternativeTitlesByLanguage { get; set; } = new Dictionary<string, List<string>>();
    }
} 