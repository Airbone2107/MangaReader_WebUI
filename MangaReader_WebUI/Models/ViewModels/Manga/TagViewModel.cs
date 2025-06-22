namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class TagViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "tag";
        public TagAttributesViewModel Attributes { get; set; } = new();
    }

    public class TagAttributesViewModel
    {
        public Dictionary<string, string> Name { get; set; } = new();
        public Dictionary<string, string> Description { get; set; } = new();
        public string Group { get; set; } = "other";
        public int Version { get; set; } = 1;
    }
} 