namespace MangaReader.WebUI.Services.MangaServices.Models
{
    public class LastReadMangaViewModel
    {
        public string MangaId { get; set; }
        public string MangaTitle { get; set; }
        public string CoverUrl { get; set; }
        public string ChapterId { get; set; }
        public string ChapterTitle { get; set; } // Tiêu đề chương đã đọc cuối
        public DateTime ChapterPublishedAt { get; set; } // Ngày đăng chương đã đọc cuối
        public DateTime LastReadAt { get; set; } // Thời điểm đọc chương đó
    }
} 