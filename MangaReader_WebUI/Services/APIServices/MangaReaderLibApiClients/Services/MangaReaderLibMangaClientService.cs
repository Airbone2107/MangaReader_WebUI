using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibMangaClientService : IMangaReaderLibMangaClient
    {
        private readonly MangaClient _innerClient;
        private readonly ILogger<MangaReaderLibMangaClientService> _wrapperLogger;

        public MangaReaderLibMangaClientService(
            IMangaReaderLibApiClient apiClient,
            ILogger<MangaClient> innerClientLogger,
            ILogger<MangaReaderLibMangaClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new MangaClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(CreateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Creating manga {Title}", request.Title);
            return await _innerClient.CreateMangaAsync(request, cancellationToken);
        }

        public async Task DeleteMangaAsync(Guid mangaId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Deleting manga {MangaId}", mangaId);
            await _innerClient.DeleteMangaAsync(mangaId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(Guid mangaId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting manga by ID {MangaId}", mangaId);
            return await _innerClient.GetMangaByIdAsync(mangaId, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(Guid mangaId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting covers for manga {MangaId}", mangaId);
            return await _innerClient.GetMangaCoversAsync(mangaId, offset, limit, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, int? limit = null, string? titleFilter = null, 
            string? statusFilter = null, string? contentRatingFilter = null, string? demographicFilter = null, 
            string? originalLanguageFilter = null, int? yearFilter = null, 
            List<Guid>? tagIdsFilter = null, List<Guid>? authorIdsFilter = null, 
            string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting mangas with title filter {TitleFilter}", titleFilter);
            return await _innerClient.GetMangasAsync(offset, limit, titleFilter, statusFilter, contentRatingFilter, demographicFilter, originalLanguageFilter, yearFilter, tagIdsFilter, authorIdsFilter, orderBy, ascending, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(Guid mangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting translations for manga {MangaId}", mangaId);
            return await _innerClient.GetMangaTranslationsAsync(mangaId, offset, limit, orderBy, ascending, cancellationToken);
        }

        public async Task UpdateMangaAsync(Guid mangaId, UpdateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Updating manga {MangaId}", mangaId);
            await _innerClient.UpdateMangaAsync(mangaId, request, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(Guid mangaId, Stream imageStream, string fileName, string? volume = null, string? description = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Uploading cover for manga {MangaId}", mangaId);
            return await _innerClient.UploadMangaCoverAsync(mangaId, imageStream, fileName, volume, description, cancellationToken);
        }
    }
} 