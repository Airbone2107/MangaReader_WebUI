namespace MangaReaderLib.DTOs.Tags
{
    public class TagAttributesDto
    {
        public string Name { get; set; } = string.Empty;
        public string TagGroupName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 