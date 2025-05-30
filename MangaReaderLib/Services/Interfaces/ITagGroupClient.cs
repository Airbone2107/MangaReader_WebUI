using MangaReaderLib.DTOs.TagGroups;
using MangaReaderLib.DTOs.Common;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với TagGroup API endpoints
    /// </summary>
    public interface ITagGroupClient
    {
        /// <summary>
        /// Lấy danh sách các nhóm tag với các tùy chọn lọc và phân trang
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupsAsync(
            int? offset = null,
            int? limit = null,
            string? nameFilter = null,
            string? orderBy = null,
            bool? ascending = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một nhóm tag dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupByIdAsync(
            Guid tagGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo một nhóm tag mới
        /// </summary>
        Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> CreateTagGroupAsync(
            CreateTagGroupRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin của một nhóm tag
        /// </summary>
        Task UpdateTagGroupAsync(
            Guid tagGroupId,
            UpdateTagGroupRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một nhóm tag
        /// </summary>
        Task DeleteTagGroupAsync(
            Guid tagGroupId,
            CancellationToken cancellationToken = default);
    }
} 