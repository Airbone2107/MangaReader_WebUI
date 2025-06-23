using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for ChapterPage endpoints.
    /// </summary>
    public interface IChapterPageReader : IReadClient
    {
        /// <summary>
        /// Lấy danh sách trang của một chapter
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>>?> GetChapterPagesAsync(
            Guid chapterId,
            int? offset = null,
            int? limit = null,
            CancellationToken cancellationToken = default);
    }
} 