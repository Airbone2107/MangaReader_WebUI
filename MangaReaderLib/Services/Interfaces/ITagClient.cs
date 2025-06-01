using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Tag API endpoints
    /// </summary>
    public interface ITagClient
    {
        /// <summary>
        /// Lấy danh sách các tag với các tùy chọn lọc và phân trang
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<TagAttributesDto>>?> GetTagsAsync(
            int? offset = null,
            int? limit = null,
            Guid? tagGroupId = null,
            string? nameFilter = null,
            string? orderBy = null,
            bool? ascending = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một tag dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<TagAttributesDto>>?> GetTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo một tag mới
        /// </summary>
        Task<ApiResponse<ResourceObject<TagAttributesDto>>?> CreateTagAsync(
            CreateTagRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin của một tag
        /// </summary>
        Task UpdateTagAsync(
            Guid tagId,
            UpdateTagRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một tag
        /// </summary>
        Task DeleteTagAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);
    }
} 