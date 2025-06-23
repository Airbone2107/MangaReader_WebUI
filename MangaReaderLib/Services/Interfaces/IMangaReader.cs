using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for Manga endpoints.
    /// </summary>
    public interface IMangaReader : IReadClient
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
            List<PublicationDemographic>? publicationDemographicsFilter = null,
            string? originalLanguageFilter = null,
            int? yearFilter = null,
            List<Guid>? authors = null,
            List<Guid>? artists = null,
            List<string>? availableTranslatedLanguage = null,
            List<Guid>? includedTags = null,
            string? includedTagsMode = null,
            List<Guid>? excludedTags = null,
            string? excludedTagsMode = null,
            string? orderBy = null, 
            bool? ascending = null,
            List<string>? includes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một manga dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId,
            List<string>? includes = null,
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