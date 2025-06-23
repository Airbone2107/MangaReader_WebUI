using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for Chapter endpoints.
    /// </summary>
    public interface IChapterReader : IReadClient
    {
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
    }
} 