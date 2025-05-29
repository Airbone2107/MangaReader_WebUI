using System;

namespace MangaReaderLib.DTOs.Tags
{
    public class UpdateTagRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid TagGroupId { get; set; }
    }
} 