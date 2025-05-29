using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Implementations
{
    public class ChapterPageClient : IChapterPageClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ChapterPageClient> _logger;

        public ChapterPageClient(IApiClient apiClient, ILogger<ChapterPageClient> logger)
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

        public async Task<LibApiResponse<CreateChapterPageEntryResponseDto>?> CreateChapterPageEntryAsync(
            Guid chapterId, LibCreateChapterPageEntryRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating chapter page entry for chapter ID: {ChapterId}, Page Number: {PageNumber}", 
                chapterId, request.PageNumber);
            return await _apiClient.PostAsync<LibCreateChapterPageEntryRequestDto, LibApiResponse<CreateChapterPageEntryResponseDto>>(
                $"Chapters/{chapterId}/pages/entry", request, cancellationToken);
        }

        public async Task<LibApiResponse<UploadChapterPageImageResponseDto>?> UploadChapterPageImageAsync(
            Guid pageId, Stream imageStream, string fileName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading image for page ID: {PageId}, Filename: {FileName}", pageId, fileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "file", fileName);
            
            return await _apiClient.PostAsync<LibApiResponse<UploadChapterPageImageResponseDto>>(
                $"chapterpages/{pageId}/image", content, cancellationToken);
        }

        public async Task<LibApiCollectionResponse<LibResourceObject<LibChapterPageAttributesDto>>?> GetChapterPagesAsync(
            Guid chapterId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting pages for chapter ID: {ChapterId}", chapterId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            
            string requestUri = BuildQueryString($"chapters/{chapterId}/pages", queryParams);
            return await _apiClient.GetAsync<LibApiCollectionResponse<LibResourceObject<LibChapterPageAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task UpdateChapterPageDetailsAsync(
            Guid pageId, LibUpdateChapterPageDetailsRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating page details for page ID: {PageId}", pageId);
            await _apiClient.PutAsync($"chapterpages/{pageId}/details", request, cancellationToken);
        }

        public async Task DeleteChapterPageAsync(Guid pageId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting page with ID: {PageId}", pageId);
            await _apiClient.DeleteAsync($"chapterpages/{pageId}", cancellationToken);
        }
    }
} 