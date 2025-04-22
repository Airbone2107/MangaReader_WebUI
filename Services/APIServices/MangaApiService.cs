using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MangaReader.WebUI.Services.APIServices
{
    public class MangaApiService : BaseApiService, IMangaApiService
    {
        public MangaApiService(HttpClient httpClient, ILogger<MangaApiService> logger, IConfiguration configuration)
            : base(httpClient, logger, configuration)
        {
        }

        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            Logger.LogInformation("Fetching manga list with models...");
            var queryParams = new Dictionary<string, List<string>>();

            if (limit.HasValue) AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());
            if (offset.HasValue) AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

            if (sortManga != null)
            {
                var parameters = sortManga.ToParams();
                foreach (var param in parameters)
                {
                    if (param.Key.EndsWith("[]") && param.Value is IEnumerable<string> values)
                    {
                        foreach (var value in values)
                        {
                            if (!string.IsNullOrEmpty(value)) AddOrUpdateParam(queryParams, param.Key, value);
                        }
                    }
                    else if (param.Value is string[] strArray)
                    {
                        foreach (var value in strArray)
                        {
                            if (!string.IsNullOrEmpty(value)) AddOrUpdateParam(queryParams, param.Key, value);
                        }
                    }
                    else if (param.Value is string strValue && !string.IsNullOrEmpty(strValue))
                    {
                        AddOrUpdateParam(queryParams, param.Key, strValue);
                    }
                    else if (param.Value != null)
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value.ToString() ?? string.Empty);
                    }
                }
            }
            else
            {
                AddOrUpdateParam(queryParams, "contentRating[]", "safe");
                AddOrUpdateParam(queryParams, "contentRating[]", "suggestive");
                AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
            }
            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "includes[]", "author");
            AddOrUpdateParam(queryParams, "includes[]", "artist");

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    var mangaList = await JsonSerializer.DeserializeAsync<MangaList>(contentStream, JsonOptions);
                    if (mangaList?.Data == null)
                    {
                        Logger.LogWarning("API response for manga list is successful but data is null or invalid.");
                        return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Limit = limit ?? 10, Offset = offset ?? 0, Total = 0 };
                    }
                    Logger.LogInformation($"Successfully fetched {mangaList.Data.Count} manga entries (Total: {mangaList.Total}).");
                    return mangaList;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchMangaAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchMangaAsync)} at {url}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchMangaAsync)} at {url}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchMangaAsync)} at {url}");
                return null;
            }
        }

        public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            Logger.LogInformation($"Fetching details for manga ID: {mangaId} with models...");
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "author", "artist", "cover_art", "tag" } }
            };
            var url = BuildUrlWithParams($"manga/{mangaId}", queryParams);
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    var mangaResponse = await JsonSerializer.DeserializeAsync<MangaResponse>(contentStream, JsonOptions);
                    if (mangaResponse?.Data == null)
                    {
                        Logger.LogWarning($"API response for manga details {mangaId} is successful but data is null.");
                        return null;
                    }
                    Logger.LogInformation($"Successfully fetched details for manga: {mangaResponse.Data.Attributes?.Title?.FirstOrDefault().Value ?? mangaId}");
                    return mangaResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchMangaDetailsAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchMangaDetailsAsync)} for manga ID: {mangaId}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchMangaDetailsAsync)} for manga ID: {mangaId}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchMangaDetailsAsync)} for manga ID: {mangaId}");
                return null;
            }
        }

        public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || !mangaIds.Any()) return new MangaList { Data = new List<Manga>() };

            Logger.LogInformation($"Fetching manga by IDs: {string.Join(", ", mangaIds)} with models...");
            var queryParams = new Dictionary<string, List<string>>();
            foreach (var id in mangaIds)
            {
                AddOrUpdateParam(queryParams, "ids[]", id);
            }
            AddOrUpdateParam(queryParams, "includes[]", "cover_art");

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    var mangaList = await JsonSerializer.DeserializeAsync<MangaList>(contentStream, JsonOptions);
                    if (mangaList?.Data == null)
                    {
                        Logger.LogWarning("API response for manga by IDs is successful but data is null.");
                        return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Limit = mangaIds.Count, Offset = 0, Total = 0 };
                    }
                    Logger.LogInformation($"Successfully fetched {mangaList.Data.Count} manga by IDs.");
                    return mangaList;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchMangaByIdsAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchMangaByIdsAsync)}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchMangaByIdsAsync)}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchMangaByIdsAsync)}");
                return null;
            }
        }
    }
} 