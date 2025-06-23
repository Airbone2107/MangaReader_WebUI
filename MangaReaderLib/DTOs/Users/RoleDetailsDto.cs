using System.Collections.Generic;

namespace MangaReaderLib.DTOs.Users
{
    public class RoleDetailsDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
} 