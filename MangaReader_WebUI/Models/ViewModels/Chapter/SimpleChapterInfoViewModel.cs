namespace MangaReader.WebUI.Models.ViewModels.Chapter
{
    public class SimpleChapterInfoViewModel
    {
        public string ChapterId { get; set; } = string.Empty;
        public string DisplayTitle { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
    }
} 