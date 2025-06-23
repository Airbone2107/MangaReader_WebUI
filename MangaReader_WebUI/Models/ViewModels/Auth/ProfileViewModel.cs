using MangaReader.WebUI.Models.Auth; // UserModel vẫn là DTO từ API

namespace MangaReader.WebUI.Models.ViewModels.Auth
{
    public class ProfileViewModel
    {
        public UserModel User { get; set; } = null!;
    }
} 