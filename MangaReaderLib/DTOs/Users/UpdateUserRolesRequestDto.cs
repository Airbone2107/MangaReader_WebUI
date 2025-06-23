using System.Collections.Generic;

namespace MangaReaderLib.DTOs.Users
{
    public class UpdateUserRolesRequestDto
    {
        public List<string> Roles { get; set; } = new List<string>();
    }
} 