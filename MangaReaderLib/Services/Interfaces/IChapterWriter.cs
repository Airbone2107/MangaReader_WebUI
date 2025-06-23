using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for Chapter endpoints.
    /// </summary>
    public interface IChapterWriter : IWriteClient
    {
        /// <summary>
        /// Tạo một chapter mới
        /// </summary>
        Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> CreateChapterAsync(
            CreateChapterRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin của một chapter
        /// </summary>
        Task UpdateChapterAsync(
            Guid chapterId,
            UpdateChapterRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một chapter
        /// </summary>
        Task DeleteChapterAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Upload hàng loạt các trang cho một chapter
        /// </summary>
        Task<ApiResponse<List<ChapterPageAttributesDto>>?> BatchUploadChapterPagesAsync(
            Guid chapterId,
            IEnumerable<(Stream stream, string fileName, string contentType)> files,
            IEnumerable<int> pageNumbers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Đồng bộ hóa các trang của một chapter
        /// </summary>
        Task<ApiResponse<List<ChapterPageAttributesDto>>?> SyncChapterPagesAsync(
            Guid chapterId,
            string pageOperationsJson, 
            IDictionary<string, (Stream stream, string fileName, string contentType)>? files,
            CancellationToken cancellationToken = default);
    }
} 