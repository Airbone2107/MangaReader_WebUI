using MangaReader.WebUI.Models.Mangadex;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MangaReader.WebUI.Services.APIServices
{
    public class CoverApiService : BaseApiService, ICoverApiService
    {
        private readonly string _imageProxyBaseUrl;
        private readonly TimeSpan _apiDelay = TimeSpan.FromMilliseconds(250);

        public CoverApiService(HttpClient httpClient, ILogger<CoverApiService> logger, IConfiguration configuration)
            : base(httpClient, logger, configuration)
        {
            _imageProxyBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/') 
                              ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }

        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            var allCovers = new List<Cover>();
            int offset = 0;
            const int limit = 100;
            int totalAvailable = 0;

            Logger.LogInformation($"Fetching ALL covers for manga ID: {mangaId} with pagination...");

            do
            {
                var queryParams = new Dictionary<string, List<string>>
                {
                    { "manga[]", new List<string> { mangaId } },
                    { "limit", new List<string> { limit.ToString() } },
                    { "offset", new List<string> { offset.ToString() } },
                    { "order[volume]", new List<string> { "asc" } }
                };

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogInformation($"Fetching covers page: {url}");

                try
                {
                    var response = await HttpClient.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Logger.LogWarning($"Rate limit hit when fetching covers for manga {mangaId}. Waiting and retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        response = await HttpClient.GetAsync(url);
                    }

                    var contentStream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var coverListResponse = await JsonSerializer.DeserializeAsync<CoverList>(contentStream, JsonOptions);

                        if (coverListResponse?.Data == null)
                        {
                             Logger.LogInformation($"No more covers found or data is null/invalid for manga {mangaId} at offset {offset}.");
                             if (offset == 0) totalAvailable = coverListResponse?.Total ?? 0;
                             break;
                        }

                        allCovers.AddRange(coverListResponse.Data);
                        if (totalAvailable == 0 && offset == 0)
                        {
                            totalAvailable = coverListResponse.Total;
                        }
                        offset += coverListResponse.Data.Count;

                        Logger.LogDebug($"Fetched {coverListResponse.Data.Count} covers. Offset now: {offset}. Total available: {totalAvailable}");

                        if (offset < totalAvailable && totalAvailable > 0)
                        {
                            await Task.Delay(_apiDelay);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        LogApiError(nameof(GetAllCoversForMangaAsync), response, errorContent);
                        return null;
                    }
                }
                catch (JsonException jsonEx)
                {
                    Logger.LogError(jsonEx, $"JSON Deserialization error during cover pagination for manga ID: {mangaId}");
                    return null;
                }
                catch (HttpRequestException httpEx)
                {
                     Logger.LogError(httpEx, $"HTTP Request error during cover pagination for manga ID: {mangaId}");
                     return null;
                }
                catch (TaskCanceledException ex)
                {
                    Logger.LogError(ex, $"Request timed out during cover pagination for manga ID: {mangaId}");
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Unexpected exception during cover pagination for manga ID: {mangaId}");
                    return null;
                }

            } while (offset < totalAvailable && totalAvailable > 0);

            Logger.LogInformation($"Finished fetching. Total covers retrieved: {allCovers.Count} for manga ID: {mangaId}. API reported total: {totalAvailable}");

            return new CoverList
            {
                Result = "ok",
                Response = "collection",
                Data = allCovers,
                Limit = allCovers.Count,
                Offset = 0,
                Total = totalAvailable
            };
        }

        public async Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || !mangaIds.Any())
            {
                return new Dictionary<string, string>();
            }

            Logger.LogInformation($"Fetching representative covers for {mangaIds.Count} manga IDs...");
            var resultCovers = new Dictionary<string, string>();
            var mangaIdsToProcess = new HashSet<string>(mangaIds);

            const int batchSize = 100;
            for (int i = 0; i < mangaIds.Count; i += batchSize)
            {
                var currentBatchIds = mangaIds.Skip(i).Take(batchSize).ToList();
                if (!currentBatchIds.Any()) continue;

                var queryParams = new Dictionary<string, List<string>>
                {
                    { "order[volume]", new List<string> { "desc" } },
                    { "order[createdAt]", new List<string> { "desc" } },
                    { "limit", new List<string> { currentBatchIds.Count.ToString() } }
                };

                foreach (var mangaId in currentBatchIds)
                {
                    AddOrUpdateParam(queryParams, "manga[]", mangaId);
                }

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogInformation($"Fetching representative covers batch: {url}");

                try
                {
                    var response = await HttpClient.GetAsync(url);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Logger.LogWarning($"Rate limit hit fetching representative covers. Waiting and retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        response = await HttpClient.GetAsync(url);
                    }

                    var contentStream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var coverListResponse = await JsonSerializer.DeserializeAsync<CoverList>(contentStream, JsonOptions);

                        if (coverListResponse?.Data != null && coverListResponse.Data.Any())
                        {
                            foreach (var cover in coverListResponse.Data)
                            {
                                string? relatedMangaId = cover.Relationships?
                                    .FirstOrDefault(r => r.Type == "manga")?.Id.ToString();

                                if (relatedMangaId != null && mangaIdsToProcess.Contains(relatedMangaId) && !resultCovers.ContainsKey(relatedMangaId))
                                {
                                    if (cover.Attributes?.FileName != null)
                                    {
                                        string fileName = cover.Attributes.FileName;
                                        string thumbnailUrl = $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"https://uploads.mangadex.org/covers/{relatedMangaId}/{fileName}.512.jpg")}";
                                        resultCovers.Add(relatedMangaId, thumbnailUrl);
                                        Logger.LogDebug($"Found representative cover for {relatedMangaId}: {thumbnailUrl}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        LogApiError(nameof(FetchRepresentativeCoverUrlsAsync), response, errorContent);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error fetching representative covers batch.");
                }

                if (i + batchSize < mangaIds.Count)
                {
                    await Task.Delay(_apiDelay);
                }
            }

            Logger.LogInformation($"Finished fetching representative covers. Found {resultCovers.Count} covers for {mangaIds.Count} requested manga IDs.");
            return resultCovers;
        }

        public async Task<string> FetchCoverUrlAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                Logger.LogWarning("MangaId is null or empty in FetchCoverUrlAsync.");
                return string.Empty;
            }

            Logger.LogInformation($"Fetching single representative cover URL for manga ID: {mangaId}...");

            try
            {
                var coversDict = await FetchRepresentativeCoverUrlsAsync(new List<string> { mangaId });

                if (coversDict != null && coversDict.TryGetValue(mangaId, out var coverUrl) && !string.IsNullOrEmpty(coverUrl))
                {
                    Logger.LogInformation($"Successfully fetched single cover URL for {mangaId}.");
                    return coverUrl;
                }
                else
                {
                    Logger.LogWarning($"Could not find representative cover URL for manga ID: {mangaId} after calling FetchRepresentativeCoverUrlsAsync.");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception in FetchCoverUrlAsync for manga ID: {mangaId}");
                return string.Empty;
            }
        }

        public async Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10)
        {
            Logger.LogInformation($"Fetching covers for manga ID: {mangaId} with models...");
            var queryParams = new Dictionary<string, List<string>>
            {
                { "manga[]", new List<string> { mangaId } },
                { "limit", new List<string> { limit.ToString() } },
                { "order[volume]", new List<string> { "asc" } }
            };

            var url = BuildUrlWithParams("cover", queryParams);
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    var coverList = await JsonSerializer.DeserializeAsync<CoverList>(contentStream, JsonOptions);
                    if (coverList?.Data == null)
                    {
                         Logger.LogWarning($"API response for covers {mangaId} is successful but data is null.");
                         return new CoverList { Result = "ok", Response = "collection", Data = new List<Cover>(), Limit = limit, Offset = 0, Total = 0 };
                    }
                    Logger.LogInformation($"Successfully fetched {coverList.Data.Count} covers for manga ID: {mangaId}.");
                    return coverList;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchCoversForMangaAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchCoversForMangaAsync)} for manga ID: {mangaId}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchCoversForMangaAsync)} for manga ID: {mangaId}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchCoversForMangaAsync)} for manga ID: {mangaId}");
                return null;
            }
        }
    }
} 