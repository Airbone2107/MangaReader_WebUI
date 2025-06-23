namespace MangaReader.WebUI.Models.ViewModels.Chapter
{
    public class ChapterInfoViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty; // Tiêu đề đã format (VD: Chương 10)
        public DateTime PublishedAt { get; set; }
    }
} 