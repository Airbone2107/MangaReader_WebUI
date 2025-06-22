using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class MangaListViewModel
    {
        public List<MangaViewModel> Mangas { get; set; } = new List<MangaViewModel>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int MaxPages { get; set; } 
        public SortManga SortOptions { get; set; } = new SortManga();
    }
} 