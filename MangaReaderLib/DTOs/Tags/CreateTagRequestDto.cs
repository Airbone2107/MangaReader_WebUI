namespace MangaReaderLib.DTOs.Tags
{
    public class CreateTagRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid TagGroupId { get; set; }
    }
} 