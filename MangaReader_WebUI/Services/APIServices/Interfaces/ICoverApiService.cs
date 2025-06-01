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
        /// Service sẽ tự động xử lý việc gọi nhiều trang API nếu cần thiết để lấy đủ dữ liệu.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="CoverList"/> chứa danh sách tất cả các <see cref="Cover"/> của manga đó,
        /// hoặc <c>null</c> nếu có lỗi xảy ra trong quá trình gọi API.
        /// </returns>
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);

        /// <summary>
        /// Tạo URL proxy cho ảnh bìa với kích thước tùy chọn thông qua Backend API.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="fileName">Tên file của ảnh bìa (ví dụ: 'cover.jpg').</param>
        /// <param name="size">Kích thước ảnh mong muốn (ví dụ: 512, 256). Kích thước này được MangaDex hỗ trợ.</param>
        /// <returns>URL đầy đủ của ảnh bìa đã được proxy bởi Backend API.</returns>
        string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512);
    }
} 