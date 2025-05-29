using System;

namespace MangaReaderLib.DTOs.TagGroups
{
    public class TagGroupAttributesDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 