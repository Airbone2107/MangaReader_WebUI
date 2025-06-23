using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TagGroups;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for TagGroup endpoints.
    /// </summary>
    public interface ITagGroupReader : IReadClient
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
    }
} 