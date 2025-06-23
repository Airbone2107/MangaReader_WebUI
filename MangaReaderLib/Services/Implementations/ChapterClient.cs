using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaReaderLib.Services.Implementations
{
    public class ChapterClient : IChapterClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ChapterClient> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ChapterClient(IApiClient apiClient, ILogger<ChapterClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
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

        public async Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> CreateChapterAsync(
            CreateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new chapter for translated manga ID: {TranslatedMangaId}, Chapter: {ChapterNumber}", 
                request.TranslatedMangaId, request.ChapterNumber);
            return await _apiClient.PostAsync<CreateChapterRequestDto, ApiResponse<ResourceObject<ChapterAttributesDto>>>("Chapters", request, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(
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
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> GetChapterByIdAsync(
            Guid chapterId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting chapter by ID: {ChapterId}", chapterId);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<ChapterAttributesDto>>>($"Chapters/{chapterId}", cancellationToken);
        }

        public async Task UpdateChapterAsync(
            Guid chapterId, UpdateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating chapter with ID: {ChapterId}", chapterId);
            await _apiClient.PutAsync($"Chapters/{chapterId}", request, cancellationToken);
        }

        public async Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting chapter with ID: {ChapterId}", chapterId);
            await _apiClient.DeleteAsync($"Chapters/{chapterId}", cancellationToken);
        }

        public async Task<ApiResponse<List<ChapterPageAttributesDto>>?> BatchUploadChapterPagesAsync(
            Guid chapterId,
            IEnumerable<(Stream stream, string fileName, string contentType)> files,
            IEnumerable<int> pageNumbers,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Batch uploading pages for Chapter ID: {ChapterId}", chapterId);
            using var formData = new MultipartFormDataContent();

            if (files == null || !files.Any())
            {
                throw new ArgumentException("Files collection cannot be null or empty.", nameof(files));
            }
            if (pageNumbers == null || !pageNumbers.Any())
            {
                throw new ArgumentException("PageNumbers collection cannot be null or empty.", nameof(pageNumbers));
            }
            if (files.Count() != pageNumbers.Count())
            {
                throw new ArgumentException("The number of files must match the number of page numbers.");
            }

            foreach (var fileTuple in files)
            {
                var streamContent = new StreamContent(fileTuple.stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileTuple.contentType);
                formData.Add(streamContent, "files", fileTuple.fileName);
            }

            foreach (var pageNumber in pageNumbers)
            {
                formData.Add(new StringContent(pageNumber.ToString()), "pageNumbers");
            }

            return await _apiClient.PostAsync<ApiResponse<List<ChapterPageAttributesDto>>>(
                $"Chapters/{chapterId}/pages/batch", formData, cancellationToken);
        }

        public async Task<ApiResponse<List<ChapterPageAttributesDto>>?> SyncChapterPagesAsync(
            Guid chapterId,
            string pageOperationsJson, 
            IDictionary<string, (Stream stream, string fileName, string contentType)>? files,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Syncing pages for Chapter ID: {ChapterId}", chapterId);
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(pageOperationsJson, Encoding.UTF8, "application/json"), "pageOperationsJson");

            if (files != null)
            {
                foreach (var fileEntry in files)
                {
                    var fileIdentifier = fileEntry.Key;
                    var (stream, fileName, contentType) = fileEntry.Value;

                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    formData.Add(streamContent, fileIdentifier, fileName);
                }
            }
            
            return await _apiClient.PutAsync<ApiResponse<List<ChapterPageAttributesDto>>>(
                $"Chapters/{chapterId}/pages", formData, cancellationToken);
        }
    }
} 