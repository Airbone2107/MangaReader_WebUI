using System;

namespace MangaReaderLib.DTOs.CoverArts
{
    public class CoverArtAttributesDto
    {
        public string? Volume { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 