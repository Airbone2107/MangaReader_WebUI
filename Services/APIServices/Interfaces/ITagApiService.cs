using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service tương tác với endpoint API lấy danh sách Tag (thể loại).
    /// </summary>
    public interface ITagApiService
    {
        /// <summary>
        /// Lấy danh sách tất cả các tag (thể loại) có sẵn từ API.
        /// </summary>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="TagListResponse"/> chứa danh sách tất cả các tag,
        /// hoặc một <see cref="TagListResponse"/> rỗng nếu có lỗi xảy ra trong quá trình gọi API.
        /// </returns>
        Task<TagListResponse?> FetchTagsAsync();
    }
} 