using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaReaderLib.DTOs.Users
{
    public class CreateUserRequestDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public List<string> Roles { get; set; } = new List<string>();
    }
} 