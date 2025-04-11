using manga_reader_web.Services.MangaServices.Models;
using System.Threading.Tasks;

namespace manga_reader_web.Services.MangaServices.ChapterServices
{
    public interface IChapterInfoService
    {
        /// <summary>
        /// Lấy thông tin chi tiết của chapter dựa vào ID
        /// </summary>
        /// <param name="chapterId">ID của chapter</param>
        /// <returns>ChapterInfo chứa thông tin chi tiết hoặc null nếu có lỗi</returns>
        Task<ChapterInfo> GetChapterInfoAsync(string chapterId);
    }
} 