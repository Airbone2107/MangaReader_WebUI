using System;
using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class MangaViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Author { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string OriginalLanguage { get; set; } = string.Empty;
        public string PublicationDemographic { get; set; } = string.Empty;
        public string ContentRating { get; set; } = string.Empty;
        public string AlternativeTitles { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
        public bool IsFollowing { get; set; }
        public string LatestChapter { get; set; } = string.Empty;
    }
} 