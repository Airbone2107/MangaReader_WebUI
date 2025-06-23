using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for TranslatedManga endpoints.
    /// </summary>
    public interface ITranslatedMangaWriter : IWriteClient
    {
        /// <summary>
        /// Tạo một bản dịch mới cho manga
        /// </summary>
        Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> CreateTranslatedMangaAsync(
            CreateTranslatedMangaRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin của một bản dịch manga
        /// </summary>
        Task UpdateTranslatedMangaAsync(
            Guid translatedMangaId,
            UpdateTranslatedMangaRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một bản dịch manga
        /// </summary>
        Task DeleteTranslatedMangaAsync(
            Guid translatedMangaId,
            CancellationToken cancellationToken = default);
    }
} 