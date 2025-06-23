using System.ComponentModel.DataAnnotations;

namespace MangaReaderLib.DTOs.Users
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
} 