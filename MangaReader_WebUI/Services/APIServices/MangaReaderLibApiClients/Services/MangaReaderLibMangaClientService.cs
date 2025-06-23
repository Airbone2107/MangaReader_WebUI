using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Enums;
using MangaReaderLib.Services.Interfaces;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibMangaClientService : IMangaReaderLibMangaClient
    {
        private readonly IMangaReader _innerReader;
        private readonly ILogger<MangaReaderLibMangaClientService> _wrapperLogger;

        public MangaReaderLibMangaClientService(
            IMangaReader mangaReader,
            ILogger<MangaReaderLibMangaClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = mangaReader;
        }

        public Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId, 
            List<string>? includes = null,
            CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting manga by ID {MangaId}", mangaId);
            return _innerReader.GetMangaByIdAsync(mangaId, includes, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(Guid mangaId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting covers for manga {MangaId}", mangaId);
            return _innerReader.GetMangaCoversAsync(mangaId, offset, limit, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, 
            int? limit = null, 
            string? titleFilter = null, 
            string? statusFilter = null, 
            string? contentRatingFilter = null,
            List<PublicationDemographic>? publicationDemographicsFilter = null,
            string? originalLanguageFilter = null, 
            int? yearFilter = null,
            List<Guid>? authorIdsFilter = null,
            List<Guid>? includedTags = null,
            string? includedTagsMode = null,
            List<Guid>? excludedTags = null,
            string? excludedTagsMode = null,
            string? orderBy = null, 
            bool? ascending = null,
            List<string>? includes = null,
            CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting mangas with title filter {TitleFilter}", titleFilter);
            return _innerReader.GetMangasAsync(
                offset, limit, titleFilter, statusFilter, contentRatingFilter, 
                publicationDemographicsFilter, originalLanguageFilter, yearFilter, 
                authorIdsFilter, includedTags, includedTagsMode, excludedTags, excludedTagsMode,
                orderBy, ascending, includes, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(Guid mangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibMangaClientService (Wrapper): Getting translations for manga {MangaId}", mangaId);
            return _innerReader.GetMangaTranslationsAsync(mangaId, offset, limit, orderBy, ascending, cancellationToken);
        }
    }
} 