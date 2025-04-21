namespace MangaReader.WebUI.Services.MangaServices.Models
{
    public class SimpleChapterInfo
    {
        public string ChapterId { get; set; }
        public string DisplayTitle { get; set; } // Tiêu đề đã được format (VD: "Chương 12: Tên chương")
        public DateTime PublishedAt { get; set; }
    }
} 