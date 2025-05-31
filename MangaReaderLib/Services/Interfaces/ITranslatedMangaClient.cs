using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với TranslatedManga API endpoints
    /// </summary>
    public interface ITranslatedMangaClient
    {
        /// <summary>
        /// Tạo một bản dịch mới cho manga
        /// </summary>
        Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> CreateTranslatedMangaAsync(
            CreateTranslatedMangaRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một bản dịch manga dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(
            Guid translatedMangaId,
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