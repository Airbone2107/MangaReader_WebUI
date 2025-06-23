using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for Tag endpoints.
    /// </summary>
    public interface ITagWriter : IWriteClient
    {
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