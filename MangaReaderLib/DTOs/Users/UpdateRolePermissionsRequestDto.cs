using System.Collections.Generic;

namespace MangaReaderLib.DTOs.Users
{
    public class UpdateRolePermissionsRequestDto
    {
        public List<string> Permissions { get; set; } = new List<string>();
    }
} 