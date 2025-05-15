using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices
{
    public interface IMangaInfoService
    {
        /// <summary>
        /// Lấy thông tin cơ bản (Tiêu đề, Ảnh bìa) của manga dựa vào ID.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>MangaInfoViewModel chứa thông tin cơ bản hoặc null nếu có lỗi.</returns>
        Task<MangaInfoViewModel> GetMangaInfoAsync(string mangaId);
    }
} 