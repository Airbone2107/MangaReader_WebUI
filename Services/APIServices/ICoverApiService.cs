using MangaReader.WebUI.Models.Mangadex;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.APIServices
{
    public interface ICoverApiService
    {
        /// <summary>
        /// Lấy TẤT CẢ ảnh bìa cho một manga, xử lý pagination.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>Danh sách tất cả Cover của manga đó hoặc null nếu có lỗi.</returns>
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);

        /// <summary>
        /// Lấy URL ảnh bìa ĐẠI DIỆN (ưu tiên volume mới nhất) cho một danh sách manga.
        /// </summary>
        /// <param name="mangaIds">Danh sách ID của các manga.</param>
        /// <returns>Dictionary map từ MangaId sang URL ảnh bìa đại diện (thumbnail .512.jpg) hoặc null nếu có lỗi.</returns>
        Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds);

        /// <summary>
        /// Lấy URL ảnh bìa ĐẠI DIỆN cho một manga duy nhất.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>URL ảnh bìa đại diện (thumbnail .512.jpg) hoặc chuỗi rỗng nếu không tìm thấy/lỗi.</returns>
        Task<string> FetchCoverUrlAsync(string mangaId);

        /// <summary>
        /// [Legacy] Lấy các ảnh bìa cho một manga với giới hạn số lượng.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="limit">Số lượng tối đa ảnh bìa cần lấy.</param>
        /// <returns>Danh sách Cover có giới hạn của manga đó hoặc null nếu có lỗi.</returns>
        Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10);
    }
} 