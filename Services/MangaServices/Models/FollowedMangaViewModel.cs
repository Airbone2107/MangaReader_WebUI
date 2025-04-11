using System;
using System.Collections.Generic;

namespace manga_reader_web.Services.MangaServices.Models
{
    public class FollowedMangaViewModel
    {
        public string MangaId { get; set; }
        public string MangaTitle { get; set; }
        public string CoverUrl { get; set; }
        public List<SimpleChapterInfo> LatestChapters { get; set; } = new List<SimpleChapterInfo>();
    }
} 