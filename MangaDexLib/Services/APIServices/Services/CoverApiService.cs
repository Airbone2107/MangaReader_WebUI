using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaDexLib.Services.APIServices.Services
{
    public class CoverApiService : BaseApiService, ICoverApiService
    {
        private readonly string _imageProxyBaseUrl;
        private readonly TimeSpan _apiDelay;

        public CoverApiService(
            HttpClient httpClient,
            ILogger<CoverApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
            _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
            _apiDelay = TimeSpan.FromMilliseconds(configuration?.GetValue<int>("ApiRateLimitDelayMs", 250) ?? 250);
        }

        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            var allCovers = new List<Cover>();
            int offset = 0;
            const int limit = 100;
            int totalAvailable = 0;

            Logger.LogInformation("Fetching ALL covers for manga ID: {MangaId} with pagination...", mangaId);

            do
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "manga[]", mangaId);
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[volume]", "asc");

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogDebug("Fetching covers page: {Url}", url);

                try
                {
                    var coverListResponse = await GetApiAsync<CoverList>(url);
                    
                    if (coverListResponse == null)
                    {
                        Logger.LogWarning("Error fetching covers for manga {MangaId} at offset {Offset}. Retrying...", mangaId, offset);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        coverListResponse = await GetApiAsync<CoverList>(url);

                        if (coverListResponse == null)
                        {
                            Logger.LogError("Failed to fetch covers for manga {MangaId} at offset {Offset} after retry. Stopping pagination.", mangaId, offset);
                            break;
                        }
                    }

                    if (coverListResponse.Data == null || !coverListResponse.Data.Any())
                    {
                        Logger.LogInformation("No more covers found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                        if (totalAvailable == 0 && offset == 0) totalAvailable = coverListResponse.Total;
                        break;
                    }

                    allCovers.AddRange(coverListResponse.Data);
                    if (totalAvailable == 0) totalAvailable = coverListResponse.Total;
                    offset += limit;
                    Logger.LogDebug("Fetched {Count} covers. Offset now: {Offset}. Total available: {TotalAvailable}",
                        coverListResponse.Data.Count, offset, totalAvailable);

                    if (offset < totalAvailable && totalAvailable > 0)
                    {
                        await Task.Delay(_apiDelay);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unexpected exception during cover pagination for manga ID: {MangaId}", mangaId);
                    return null;
                }

            } while (offset < totalAvailable && totalAvailable > 0);

            Logger.LogInformation("Finished fetching. Total covers retrieved: {RetrievedCount} for manga ID: {MangaId}. API reported total: {ApiTotal}",
                allCovers.Count, mangaId, totalAvailable);

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

        public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
        {
            var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.{size}.jpg";
            return $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
        }
    }
} 