using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for TranslatedManga endpoints.
    /// </summary>
    public interface ITranslatedMangaReader : IReadClient
    {
        /// <summary>
        /// Lấy thông tin chi tiết của một bản dịch manga dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(
            Guid translatedMangaId,
            CancellationToken cancellationToken = default);
    }
} 