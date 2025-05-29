using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        Task<LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>?> CreateTranslatedMangaAsync(
            LibCreateTranslatedMangaRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một bản dịch manga dựa trên ID
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(
            Guid translatedMangaId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Cập nhật thông tin của một bản dịch manga
        /// </summary>
        Task UpdateTranslatedMangaAsync(
            Guid translatedMangaId, 
            LibUpdateTranslatedMangaRequestDto request, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Xóa một bản dịch manga
        /// </summary>
        Task DeleteTranslatedMangaAsync(
            Guid translatedMangaId, 
            CancellationToken cancellationToken = default);
    }
} 