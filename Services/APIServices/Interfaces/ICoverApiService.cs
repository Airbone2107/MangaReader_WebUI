using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service tương tác với các endpoint API liên quan đến Cover Art.
    /// </summary>
    public interface ICoverApiService
    {
        /// <summary>
        /// Lấy TẤT CẢ ảnh bìa cho một manga cụ thể.
        /// Service sẽ tự động xử lý việc gọi nhiều trang API nếu cần thiết.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="CoverList"/> chứa danh sách tất cả các <see cref="Cover"/> của manga đó,
        /// hoặc <c>null</c> nếu có lỗi xảy ra trong quá trình gọi API.
        /// </returns>
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);

        /// <summary>
        /// Lấy URL ảnh bìa ĐẠI DIỆN (thường là ảnh bìa mới nhất hoặc volume=null) cho một danh sách các manga.
        /// Hàm này tối ưu việc gọi API bằng cách fetch theo batch.
        /// </summary>
        /// <param name="mangaIds">Danh sách các ID manga cần lấy ảnh bìa đại diện.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một <see cref="Dictionary{TKey, TValue}"/>, trong đó key là MangaId và value là URL ảnh bìa thumbnail (phiên bản 512px) đã được proxy qua backend.
        /// Trả về <c>null</c> nếu có lỗi nghiêm trọng xảy ra. Nếu một manga không có ảnh bìa, nó sẽ không có trong dictionary.
        /// </returns>
        Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds);

        /// <summary>
        /// Lấy URL ảnh bìa ĐẠI DIỆN cho một manga duy nhất.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là URL ảnh bìa thumbnail (phiên bản 512px) đã được proxy qua backend.
        /// Trả về <see cref="string.Empty"/> nếu manga không có ảnh bìa hoặc có lỗi xảy ra.
        /// </returns>
        Task<string> FetchCoverUrlAsync(string mangaId);

        /// <summary>
        /// [Legacy] Lấy một số lượng giới hạn ảnh bìa cho một manga.
        /// Phương thức này có thể không lấy ảnh bìa mới nhất nếu manga có nhiều ảnh bìa.
        /// Nên ưu tiên sử dụng <see cref="FetchCoverUrlAsync"/> hoặc <see cref="FetchRepresentativeCoverUrlsAsync"/> để lấy ảnh đại diện.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="limit">Số lượng ảnh bìa tối đa cần lấy (mặc định là 10).</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="CoverList"/> chứa danh sách các <see cref="Cover"/> (có giới hạn số lượng),
        /// hoặc <c>null</c> nếu có lỗi xảy ra.
        /// </returns>
        Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10);
    }
} 