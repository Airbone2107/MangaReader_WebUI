using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Implementations
{
    public class ChapterClient : IChapterClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ChapterClient> _logger;

        public ChapterClient(IApiClient apiClient, ILogger<ChapterClient> logger)
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

        public async Task<LibApiResponse<LibResourceObject<LibChapterAttributesDto>>?> CreateChapterAsync(
            LibCreateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new chapter for translated manga ID: {TranslatedMangaId}, Chapter: {ChapterNumber}", 
                request.TranslatedMangaId, request.ChapterNumber);
            return await _apiClient.PostAsync<LibCreateChapterRequestDto, LibApiResponse<LibResourceObject<LibChapterAttributesDto>>>("Chapters", request, cancellationToken);
        }

        public async Task<LibApiCollectionResponse<LibResourceObject<LibChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(
            Guid translatedMangaId, int? offset = null, int? limit = null, 
            string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting chapters for translated manga ID: {TranslatedMangaId}", translatedMangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            
            string requestUri = BuildQueryString($"translatedmangas/{translatedMangaId}/chapters", queryParams);
            return await _apiClient.GetAsync<LibApiCollectionResponse<LibResourceObject<LibChapterAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<LibApiResponse<LibResourceObject<LibChapterAttributesDto>>?> GetChapterByIdAsync(
            Guid chapterId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting chapter by ID: {ChapterId}", chapterId);
            return await _apiClient.GetAsync<LibApiResponse<LibResourceObject<LibChapterAttributesDto>>>($"Chapters/{chapterId}", cancellationToken);
        }

        public async Task UpdateChapterAsync(
            Guid chapterId, LibUpdateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating chapter with ID: {ChapterId}", chapterId);
            await _apiClient.PutAsync($"Chapters/{chapterId}", request, cancellationToken);
        }

        public async Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting chapter with ID: {ChapterId}", chapterId);
            await _apiClient.DeleteAsync($"Chapters/{chapterId}", cancellationToken);
        }
    }
} 