using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using MangaReaderLib.Enums;

namespace MangaReaderLib.Services.Implementations
{
    public class MangaClient : IMangaClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<MangaClient> _logger;

        public MangaClient(IApiClient apiClient, ILogger<MangaClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string BuildQueryString(string baseUri, Dictionary<string, List<string>> queryParams)
        {
            var queryString = new StringBuilder();
            if (queryParams != null && queryParams.Any())
            {
                bool firstParam = true;
                foreach (var param in queryParams)
                {
                    if (param.Value != null && param.Value.Any())
                    {
                        foreach (var value in param.Value)
                        {
                            if (string.IsNullOrEmpty(value)) continue;

                            if (firstParam)
                            {
                                queryString.Append("?");
                                firstParam = false;
                            }
                            else
                            {
                                queryString.Append("&");
                            }
                            queryString.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
                        }
                    }
                }
            }
            return $"{baseUri}{queryString}";
        }
        
        private void AddQueryParam(Dictionary<string, List<string>> queryParams, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].Add(value);
            }
        }

        private void AddListQueryParam<TValue>(Dictionary<string, List<string>> queryParams, string key, List<TValue>? values)
        {
            if (values != null && values.Any())
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].AddRange(values.Select(v => v?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        public async Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, int? limit = null, string? titleFilter = null,
            string? statusFilter = null, string? contentRatingFilter = null,
            List<PublicationDemographic>? publicationDemographicsFilter = null,
            string? originalLanguageFilter = null, int? yearFilter = null,
            List<Guid>? authorIdsFilter = null,
            List<Guid>? includedTags = null,
            string? includedTagsMode = null,
            List<Guid>? excludedTags = null,
            string? excludedTagsMode = null,
            string? orderBy = null, bool? ascending = null,
            List<string>? includes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mangas with various filters.");

            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "titleFilter", titleFilter);
            AddQueryParam(queryParams, "statusFilter", statusFilter);
            AddQueryParam(queryParams, "contentRatingFilter", contentRatingFilter);

            // Xử lý PublicationDemographicsFilter (List<Enum>)
            if (publicationDemographicsFilter != null && publicationDemographicsFilter.Any())
            {
                 AddListQueryParam(queryParams, "publicationDemographicsFilter", publicationDemographicsFilter.Select(e => e.ToString()).ToList());
            }
            
            AddQueryParam(queryParams, "originalLanguageFilter", originalLanguageFilter);
            AddQueryParam(queryParams, "yearFilter", yearFilter?.ToString());

            if (authorIdsFilter != null && authorIdsFilter.Any())
            {
                AddListQueryParam(queryParams, "authorIdsFilter", authorIdsFilter.Select(id => id.ToString()).ToList());
            }

            // THÊM FILTERS TAG NÂNG CAO
            if (includedTags != null && includedTags.Any())
            {
                AddListQueryParam(queryParams, "includedTags", includedTags.Select(id => id.ToString()).ToList());
                if (!string.IsNullOrEmpty(includedTagsMode))
                {
                    AddQueryParam(queryParams, "includedTagsMode", includedTagsMode);
                }
            }
            if (excludedTags != null && excludedTags.Any())
            {
                 AddListQueryParam(queryParams, "excludedTags", excludedTags.Select(id => id.ToString()).ToList());
                if (!string.IsNullOrEmpty(excludedTagsMode))
                {
                    AddQueryParam(queryParams, "excludedTagsMode", excludedTagsMode);
                }
            }

            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());

            // THAY ĐỔI: Sửa key từ "includes[]" thành "includes"
            if (includes != null && includes.Any())
            {
                AddListQueryParam(queryParams, "includes", includes);
            }

            string requestUri = BuildQueryString("Mangas", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId, 
            List<string>? includes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting manga by ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            if (includes != null && includes.Any())
            {
                AddListQueryParam(queryParams, "includes", includes);
            }
            string requestUri = BuildQueryString($"Mangas/{mangaId}", queryParams);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(CreateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new manga: {Title}", request.Title);
            return await _apiClient.PostAsync<CreateMangaRequestDto, ApiResponse<ResourceObject<MangaAttributesDto>>>("Mangas", request, cancellationToken);
        }

        public async Task UpdateMangaAsync(Guid mangaId, UpdateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating manga with ID: {MangaId}", mangaId);
            await _apiClient.PutAsync($"Mangas/{mangaId}", request, cancellationToken);
        }

        public async Task DeleteMangaAsync(Guid mangaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting manga with ID: {MangaId}", mangaId);
            await _apiClient.DeleteAsync($"Mangas/{mangaId}", cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(Guid mangaId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting covers for manga with ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            string requestUri = BuildQueryString($"mangas/{mangaId}/covers", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(Guid mangaId, Stream imageStream, string fileName, string? volume = null, string? description = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading cover for manga with ID: {MangaId}, Filename: {FileName}", mangaId, fileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "file", fileName);
            if (!string.IsNullOrEmpty(volume))
            {
                content.Add(new StringContent(volume), "volume");
            }
            if (!string.IsNullOrEmpty(description))
            {
                content.Add(new StringContent(description), "description");
            }
            
            return await _apiClient.PostAsync<ApiResponse<ResourceObject<CoverArtAttributesDto>>>($"mangas/{mangaId}/covers", content, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(Guid mangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting translations for manga with ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            string requestUri = BuildQueryString($"mangas/{mangaId}/translations", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>>(requestUri, cancellationToken);
        }
    }
} 