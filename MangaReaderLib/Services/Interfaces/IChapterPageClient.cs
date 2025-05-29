using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với ChapterPage API endpoints
    /// </summary>
    public interface IChapterPageClient
    {
        /// <summary>
        /// Tạo một entry cho trang mới trong chapter (trước khi upload ảnh)
        /// </summary>
        Task<LibApiResponse<CreateChapterPageEntryResponseDto>?> CreateChapterPageEntryAsync(
            Guid chapterId, 
            LibCreateChapterPageEntryRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tải lên ảnh cho trang
        /// </summary>
        Task<LibApiResponse<UploadChapterPageImageResponseDto>?> UploadChapterPageImageAsync(
            Guid pageId, 
            Stream imageStream, 
            string fileName, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách trang của một chapter
        /// </summary>
        Task<LibApiCollectionResponse<LibResourceObject<LibChapterPageAttributesDto>>?> GetChapterPagesAsync(
            Guid chapterId, 
            int? offset = null, 
            int? limit = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Cập nhật thông tin chi tiết của một trang
        /// </summary>
        Task UpdateChapterPageDetailsAsync(
            Guid pageId, 
            LibUpdateChapterPageDetailsRequestDto request, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Xóa một trang
        /// </summary>
        Task DeleteChapterPageAsync(
            Guid pageId, 
            CancellationToken cancellationToken = default);
    }
} 