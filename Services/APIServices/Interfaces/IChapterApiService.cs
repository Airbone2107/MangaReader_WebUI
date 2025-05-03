using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service tương tác với các endpoint API liên quan đến Chapter.
    /// </summary>
    public interface IChapterApiService
    {
        /// <summary>
        /// Lấy danh sách các chapter của một manga cụ thể, hỗ trợ phân trang và lọc theo ngôn ngữ.
        /// Tự động xử lý việc gọi nhiều trang API nếu cần để lấy đủ số lượng chapter yêu cầu hoặc toàn bộ.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="languages">Chuỗi các mã ngôn ngữ cần lọc (ví dụ: "vi,en"), phân tách bằng dấu phẩy.</param>
        /// <param name="order">Thứ tự sắp xếp chapter (mặc định là "desc" - mới nhất trước).</param>
        /// <param name="maxChapters">Số lượng chapter tối đa cần lấy. Nếu là null, sẽ lấy tất cả chapter phù hợp.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="ChapterList"/> chứa danh sách các chapter đã được tổng hợp,
        /// hoặc <c>null</c> nếu có lỗi xảy ra trong quá trình gọi API.
        /// </returns>
        Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null);

        /// <summary>
        /// Lấy thông tin chi tiết của một chapter cụ thể dựa vào ID.
        /// </summary>
        /// <param name="chapterId">ID của chapter.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="ChapterResponse"/> chứa thông tin chi tiết của chapter,
        /// hoặc <c>null</c> nếu chapter không tồn tại hoặc có lỗi xảy ra.
        /// </returns>
        Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId);

        /// <summary>
        /// Lấy thông tin server MangaDex@Home để tải các trang ảnh của một chapter.
        /// </summary>
        /// <param name="chapterId">ID của chapter.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="AtHomeServerResponse"/> chứa URL cơ sở và danh sách tên file ảnh,
        /// hoặc <c>null</c> nếu có lỗi xảy ra hoặc chapter không có trang ảnh.
        /// </returns>
        Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId);
        // Có thể thêm các phương thức khác liên quan đến Chapter nếu cần
    }
} 