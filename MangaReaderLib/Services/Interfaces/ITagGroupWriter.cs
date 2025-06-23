using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TagGroups;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for TagGroup endpoints.
    /// </summary>
    public interface ITagGroupWriter : IWriteClient
    {
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