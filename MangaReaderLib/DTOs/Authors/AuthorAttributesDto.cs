namespace MangaReaderLib.DTOs.Authors
{
    public class AuthorAttributesDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 