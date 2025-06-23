using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaDexLib.Services.APIServices.Services
{
    public class ChapterApiService : BaseApiService, IChapterApiService
    {
        private readonly string _imageProxyBaseUrl;

        public ChapterApiService(
            HttpClient httpClient,
            ILogger<ChapterApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
            _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }

        public async Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            var allChapters = new List<Chapter>();
            int offset = 0;
            int limit = 100;
            int totalFetched = 0;
            int totalAvailable = 0;

            var validLanguages = languages.Split(',')
                                          .Select(l => l.Trim())
                                          .Where(l => !string.IsNullOrEmpty(l))
                                          .ToList();

            if (!validLanguages.Any())
            {
                Logger.LogWarning("No valid languages specified for fetching chapters.");
                return new ChapterList { Data = new List<Chapter>(), Result = "ok", Response = "collection" };
            }

            Logger.LogInformation("Fetching chapters for manga {MangaId} with languages [{Languages}]. Max chapters: {MaxChapters}",
                mangaId, string.Join(", ", validLanguages), maxChapters?.ToString() ?? "All");

            do
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[chapter]", order);
                AddOrUpdateParam(queryParams, "order[volume]", order);
                AddOrUpdateParam(queryParams, "includes[]", "scanlation_group");
                AddOrUpdateParam(queryParams, "includes[]", "user");

                foreach (var lang in validLanguages)
                {
                    AddOrUpdateParam(queryParams, "translatedLanguage[]", lang);
                }

                var url = BuildUrlWithParams($"manga/{mangaId}/feed", queryParams);
                Logger.LogDebug("Fetching chapter page: {Url}", url);

                var chapterListResponse = await GetApiAsync<ChapterList>(url);

                if (chapterListResponse == null)
                {
                    Logger.LogWarning("API call failed when fetching chapters for manga {MangaId} at offset {Offset}. Stopping pagination.", mangaId, offset);
                    break;
                }
                
                if (chapterListResponse.Result != "ok" || chapterListResponse.Data == null || !chapterListResponse.Data.Any())
                {
                    Logger.LogInformation("No more chapters found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                    if (totalAvailable == 0 && offset == 0) totalAvailable = chapterListResponse.Total;
                    break;
                }

                allChapters.AddRange(chapterListResponse.Data);
                totalFetched += chapterListResponse.Data.Count;
                if (totalAvailable == 0) totalAvailable = chapterListResponse.Total; 
                offset += limit;

                Logger.LogDebug("Fetched {Count} chapters. Total fetched: {TotalFetched}. Total available: {TotalAvailable}. Next offset: {NextOffset}",
                    chapterListResponse.Data.Count, totalFetched, totalAvailable, offset);

                if ((maxChapters.HasValue && totalFetched >= maxChapters.Value) || totalFetched >= totalAvailable)
                {
                    Logger.LogInformation("Reached chapter limit or fetched all available chapters for manga {MangaId}. Stopping.", mangaId);
                    break;
                }

            } while (offset < totalAvailable);

            if (maxChapters.HasValue && allChapters.Count > maxChapters.Value)
            {
                allChapters = allChapters.Take(maxChapters.Value).ToList();
                Logger.LogInformation("Truncated chapter list to {MaxChapters} for manga {MangaId}.", maxChapters.Value, mangaId);
            }

            Logger.LogInformation("Successfully fetched {Count} chapters for manga ID: {MangaId}.", allChapters.Count, mangaId);
            return new ChapterList
            {
                Result = "ok",
                Response = "collection",
                Data = allChapters,
                Limit = limit,
                Offset = 0,
                Total = totalAvailable
            };
        }
        
        public async Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "scanlation_group", "manga", "user" } }
            };
            var url = BuildUrlWithParams($"chapter/{chapterId}", queryParams);
            Logger.LogInformation("Fetching info for chapter ID: {ChapterId}", chapterId);

            var chapterResponse = await GetApiAsync<ChapterResponse>(url);
            if (chapterResponse == null) return null;
            
            if (chapterResponse.Result != "ok" || chapterResponse.Data == null)
            {
                Logger.LogWarning("API response for chapter info {ChapterId} has invalid format or missing data. Result: {Result}, HasData: {HasData}",
                    chapterId, chapterResponse.Result, chapterResponse.Data != null);
                return null;
            }

            Logger.LogInformation("Successfully fetched info for chapter ID: {ChapterId}", chapterId);
            return chapterResponse;
        }

        public async Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            var url = BuildUrlWithParams($"at-home/server/{chapterId}");
            Logger.LogInformation("Fetching chapter pages info for chapter ID: {ChapterId}", chapterId);

            var atHomeResponse = await GetApiAsync<AtHomeServerResponse>(url);
            if (atHomeResponse == null) return null;
            
            if (atHomeResponse.Result != "ok" || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null || !atHomeResponse.Chapter.Data.Any())
            {
                Logger.LogWarning("API response for chapter pages {ChapterId} has invalid format or missing data. Result: {Result}, BaseUrl: {HasBaseUrl}, ChapterData: {HasChapterData}, HasPages: {HasPages}",
                    chapterId, atHomeResponse.Result, !string.IsNullOrEmpty(atHomeResponse.BaseUrl), atHomeResponse.Chapter?.Data != null, atHomeResponse.Chapter?.Data?.Any() ?? false);
                return null;
            }

            Logger.LogInformation("Successfully fetched page info for chapter ID: {ChapterId}", chapterId);
            return atHomeResponse;
        }
    }
} 