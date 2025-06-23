using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for ChapterPage endpoints.
    /// </summary>
    public interface IChapterPageWriter : IWriteClient
    {
        /// <summary>
        /// Tạo một entry cho trang mới trong chapter (trước khi upload ảnh)
        /// </summary>
        Task<ApiResponse<CreateChapterPageEntryResponseDto>?> CreateChapterPageEntryAsync(
            Guid chapterId,
            CreateChapterPageEntryRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tải lên ảnh cho trang
        /// </summary>
        Task<ApiResponse<UploadChapterPageImageResponseDto>?> UploadChapterPageImageAsync(
            Guid pageId,
            Stream imageStream,
            string fileName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin chi tiết của một trang
        /// </summary>
        Task UpdateChapterPageDetailsAsync(
            Guid pageId,
            UpdateChapterPageDetailsRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một trang
        /// </summary>
        Task DeleteChapterPageAsync(
            Guid pageId,
            CancellationToken cancellationToken = default);
    }
} 