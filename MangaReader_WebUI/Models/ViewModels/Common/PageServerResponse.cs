namespace MangaReader.WebUI.Models.ViewModels.Common
{
    public class PageServerResponse
    {
        public string Result { get; set; } = "ok";
        public string BaseUrl { get; set; } = string.Empty; // Sẽ rỗng cho MangaReaderLib
        public PageChapterData Chapter { get; set; } = new();
    }

    public class PageChapterData
    {
        public string Hash { get; set; } = string.Empty; // Sẽ là ChapterId
        public List<string> Data { get; set; } = new(); // Sẽ chứa URL ảnh đầy đủ
        public List<string> DataSaver { get; set; } = new();
    }
} 