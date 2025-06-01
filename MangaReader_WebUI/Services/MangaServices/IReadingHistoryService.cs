using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices
{
    public interface IReadingHistoryService
    {
        /// <summary>
        /// Lấy danh sách lịch sử đọc truyện của người dùng
        /// </summary>
        /// <returns>Danh sách các manga đã đọc gần đây</returns>
        Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync();
    }
} 