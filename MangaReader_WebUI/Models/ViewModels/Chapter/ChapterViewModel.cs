using System;
using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Chapter
{
    public class ChapterViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Volume { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public List<ChapterRelationshipViewModel> Relationships { get; set; } = new List<ChapterRelationshipViewModel>();
    }
} 