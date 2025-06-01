using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Manga API endpoints
    /// </summary>
    public interface IMangaClient
    {
        /// <summary>
        /// Lấy danh sách manga với các tùy chọn lọc và phân trang
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, 
            int? limit = null, 
            string? titleFilter = null, 
            string? statusFilter = null, 
            string? contentRatingFilter = null, 
            string? demographicFilter = null, 
            string? originalLanguageFilter = null,
            int? yearFilter = null,
            List<Guid>? tagIdsFilter = null,
            List<Guid>? authorIdsFilter = null,
            string? orderBy = null, 
            bool? ascending = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một manga dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tạo một manga mới
        /// </summary>
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(
            CreateMangaRequestDto request, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Cập nhật thông tin của một manga
        /// </summary>
        Task UpdateMangaAsync(
            Guid mangaId, 
            UpdateMangaRequestDto request, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Xóa một manga
        /// </summary>
        Task DeleteMangaAsync(
            Guid mangaId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách bìa của một manga
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(
            Guid mangaId, 
            int? offset = null, 
            int? limit = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tải lên bìa mới cho một manga
        /// </summary>
        Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(
            Guid mangaId, 
            Stream imageStream, 
            string fileName, 
            string? volume = null, 
            string? description = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lấy danh sách các bản dịch của một manga
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(
            Guid mangaId, 
            int? offset = null, 
            int? limit = null,
            string? orderBy = null, 
            bool? ascending = null,
            CancellationToken cancellationToken = default);
    }
} 