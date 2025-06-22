using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaDexLib.Services.APIServices.Services
{
    public class MangaApiService : BaseApiService, IMangaApiService
    {
        public MangaApiService(
            HttpClient httpClient,
            ILogger<MangaApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
        }

        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            Logger.LogInformation("Fetching manga list with parameters: Limit={Limit}, Offset={Offset}, SortOptions={@SortOptions}",
                limit, offset, sortManga);
            var queryParams = new Dictionary<string, List<string>>();

            if (limit.HasValue) AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());
            if (offset.HasValue) AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

            if (sortManga != null)
            {
                var sortParams = sortManga.ToParams();
                foreach (var param in sortParams)
                {
                    if (param.Key.EndsWith("[]") && param.Value is IEnumerable<string> values)
                    {
                        if (!queryParams.ContainsKey(param.Key))
                        {
                            queryParams[param.Key] = new List<string>();
                        }
                        foreach (var val in values)
                        {
                            if (!string.IsNullOrEmpty(val)) queryParams[param.Key].Add(val);
                        }
                    }
                    else if (param.Key.StartsWith("order["))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value?.ToString() ?? string.Empty);
                    }
                    else if (param.Value != null && !string.IsNullOrEmpty(param.Value.ToString()))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value.ToString()!);
                    }
                }
                if (sortManga.ContentRating != null && sortManga.ContentRating.Any())
                {
                    if (!queryParams.ContainsKey("contentRating[]"))
                    {
                        queryParams["contentRating[]"] = new List<string>();
                    }
                    queryParams["contentRating[]"].AddRange(sortManga.ContentRating);
                }
            }
            else
            {
                AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
            }

            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "includes[]", "author");
            AddOrUpdateParam(queryParams, "includes[]", "artist");

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga list failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = limit ?? 10, Offset = offset ?? 0, Total = 0 };
            }

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga list has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga entries (Total: {Total}).", mangaList.Data.Count, mangaList.Total);
            return mangaList;
        }

        public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            Logger.LogInformation("Fetching details for manga ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "author", "artist", "cover_art", "tag" } }
            };
            var url = BuildUrlWithParams($"manga/{mangaId}", queryParams);
            Logger.LogInformation("Constructed manga details fetch URL: {Url}", url);

            var mangaResponse = await GetApiAsync<MangaResponse>(url);
            if (mangaResponse == null)
            {
                Logger.LogWarning("Fetching manga details for {MangaId} failed.", mangaId);
                return null;
            }

            if (mangaResponse.Result != "ok" || mangaResponse.Data == null)
            {
                Logger.LogWarning("API response for manga details {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaId, mangaResponse.Result, mangaResponse.Data != null, url);
                return null;
            }

            Logger.LogInformation("Successfully fetched details for manga: {MangaTitle} ({MangaId})",
                mangaResponse.Data.Attributes?.Title?.FirstOrDefault().Value ?? "N/A", mangaId);
            return mangaResponse;
        }

        public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || !mangaIds.Any())
            {
                Logger.LogWarning("FetchMangaByIdsAsync called with empty or null list of IDs.");
                return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Total = 0 };
            }

            Logger.LogInformation("Fetching manga by IDs: [{MangaIds}]", string.Join(", ", mangaIds));
            var queryParams = new Dictionary<string, List<string>>();
            foreach (var id in mangaIds)
            {
                AddOrUpdateParam(queryParams, "ids[]", id);
            }
            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "limit", mangaIds.Count.ToString());

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch by IDs URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga by IDs failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = mangaIds.Count, Offset = 0, Total = 0 };
            }

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga by IDs has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga by IDs.", mangaList.Data.Count);
            return mangaList;
        }
    }
} 