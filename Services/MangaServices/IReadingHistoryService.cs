using manga_reader_web.Services.MangaServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace manga_reader_web.Services.MangaServices
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