using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service tương tác với các endpoint API liên quan đến Manga.
    /// </summary>
    public interface IMangaApiService
    {
        /// <summary>
        /// Lấy danh sách manga từ API, hỗ trợ phân trang và lọc/sắp xếp.
        /// </summary>
        /// <param name="limit">Số lượng manga tối đa mỗi trang (mặc định theo API).</param>
        /// <param name="offset">Vị trí bắt đầu lấy dữ liệu (mặc định là 0).</param>
        /// <param name="sortManga">Đối tượng chứa các tùy chọn lọc và sắp xếp.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="MangaList"/> chứa danh sách manga và thông tin phân trang,
        /// hoặc một <see cref="MangaList"/> rỗng nếu có lỗi hoặc không tìm thấy kết quả.
        /// </returns>
        Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null);

        /// <summary>
        /// Lấy thông tin chi tiết của nhiều manga dựa trên danh sách ID.
        /// </summary>
        /// <param name="mangaIds">Danh sách các ID manga cần lấy thông tin.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="MangaList"/> chứa danh sách các manga tương ứng,
        /// hoặc một <see cref="MangaList"/> rỗng nếu có lỗi hoặc không tìm thấy manga nào.
        /// </returns>
        Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds);

        /// <summary>
        /// Lấy thông tin chi tiết của một manga cụ thể dựa vào ID.
        /// Bao gồm các thông tin liên quan như tác giả, họa sĩ, ảnh bìa và tags.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="MangaResponse"/> chứa thông tin chi tiết của manga,
        /// hoặc <c>null</c> nếu manga không tồn tại hoặc có lỗi xảy ra.
        /// </returns>
        Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId);
    }
} 