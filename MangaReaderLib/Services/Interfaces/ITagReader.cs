using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for Tag endpoints.
    /// </summary>
    public interface ITagReader : IReadClient
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
    }
} 