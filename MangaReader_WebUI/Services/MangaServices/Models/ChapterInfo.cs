namespace MangaReader.WebUI.Services.MangaServices.Models
{
    public class ChapterInfo
    {
        public string Id { get; set; }
        public string Title { get; set; } // Tiêu đề đã format (VD: Chương 10)
        public DateTime PublishedAt { get; set; }
        // Thêm các thuộc tính khác nếu cần
    }
} 