using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using System;
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
        Task<LibApiResponse<LibResourceObject<LibChapterAttributesDto>>?> CreateChapterAsync(
            LibCreateChapterRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách các chapter của một bản dịch manga
        /// </summary>
        Task<LibApiCollectionResponse<LibResourceObject<LibChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(
            Guid translatedMangaId, 
            int? offset = null, 
            int? limit = null, 
            string? orderBy = null, 
            bool? ascending = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một chapter dựa trên ID
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibChapterAttributesDto>>?> GetChapterByIdAsync(
            Guid chapterId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Cập nhật thông tin của một chapter
        /// </summary>
        Task UpdateChapterAsync(
            Guid chapterId, 
            LibUpdateChapterRequestDto request, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Xóa một chapter
        /// </summary>
        Task DeleteChapterAsync(
            Guid chapterId, 
            CancellationToken cancellationToken = default);
    }
} 