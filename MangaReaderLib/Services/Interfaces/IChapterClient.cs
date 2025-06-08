using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Chapter API endpoints
    /// </summary>
    public interface IChapterClient
    {
        /// <summary>
        /// Tạo một chapter mới
        /// </summary>
        Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> CreateChapterAsync(
            CreateChapterRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách các chapter của một bản dịch manga
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(
            Guid translatedMangaId,
            int? offset = null,
            int? limit = null,
            string? orderBy = null,
            bool? ascending = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một chapter dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> GetChapterByIdAsync(
            Guid chapterId,
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