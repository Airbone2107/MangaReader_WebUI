namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class TagListViewModel
    {
        public string Result { get; set; } = "ok";
        public string Response { get; set; } = "collection";
        public List<TagViewModel> Data { get; set; } = new();
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int Total { get; set; }
    }
} 