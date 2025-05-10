using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="IMangaApiService"/>.
    /// Tương tác với các endpoint API liên quan đến Manga của MangaDex thông qua Backend API proxy.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency và gọi constructor lớp cơ sở.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình.</param>
    /// <param name="logger">Logger cho MangaApiService.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API.</param>
    public class MangaApiService(
        HttpClient httpClient,
        ILogger<MangaApiService> logger,
        IConfiguration configuration,
        IApiRequestHandler apiRequestHandler)
        : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
          IMangaApiService
    {
        /// <inheritdoc/>
        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            Logger.LogInformation("Fetching manga list with parameters: Limit={Limit}, Offset={Offset}, SortOptions={@SortOptions}",
                limit, offset, sortManga);
            var queryParams = new Dictionary<string, List<string>>();

            if (limit.HasValue) AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());
            if (offset.HasValue) AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

            // Áp dụng các tham số từ SortManga nếu có
            if (sortManga != null)
            {
                var sortParams = sortManga.ToParams(); // Lấy dictionary từ SortManga
                foreach (var param in sortParams)
                {
                    // Xử lý các tham số dạng mảng (kết thúc bằng [])
                    if (param.Key.EndsWith("[]") && param.Value is IEnumerable<string> values)
                    {
                        foreach (var value in values)
                        {
                            if (!string.IsNullOrEmpty(value)) AddOrUpdateParam(queryParams, param.Key, value);
                        }
                    }
                    // Xử lý các tham số order (dạng order[key]=value)
                    else if (param.Key.StartsWith("order["))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value?.ToString() ?? string.Empty);
                    }
                    // Xử lý các tham số đơn lẻ khác
                    else if (param.Value != null && !string.IsNullOrEmpty(param.Value.ToString()))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value.ToString()!);
                    }
                }
            }
            else // Tham số mặc định nếu không có SortManga
            {
                AddOrUpdateParam(queryParams, "contentRating[]", "safe");
                AddOrUpdateParam(queryParams, "contentRating[]", "suggestive");
                AddOrUpdateParam(queryParams, "contentRating[]", "erotica");
                AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
            }

            // Luôn bao gồm các relationship cần thiết
            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "includes[]", "author");
            AddOrUpdateParam(queryParams, "includes[]", "artist");

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            // Trả về list rỗng nếu lỗi, thay vì null, để giảm kiểm tra null ở lớp gọi
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga list failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = limit ?? 10, Offset = offset ?? 0, Total = 0 };
            }

            #if DEBUG
            Debug.Assert(mangaList.Result == "ok", $"[MangaApiService] FetchMangaAsync - API returned error: {mangaList.Result}. URL: {url}");
            Debug.Assert(mangaList.Data != null, $"[MangaApiService] FetchMangaAsync - API returned null Data despite ok result. URL: {url}");
            #endif

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga list has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                // Trả về list rỗng nhưng giữ nguyên trạng thái lỗi nếu có
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga entries (Total: {Total}).", mangaList.Data.Count, mangaList.Total);
            return mangaList;
        }

        /// <inheritdoc/>
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
                 return null; // Trả về null nếu lỗi
            }

            #if DEBUG
            Debug.Assert(mangaResponse.Result == "ok", $"[MangaApiService] FetchMangaDetailsAsync({mangaId}) - API returned error: {mangaResponse.Result}. URL: {url}");
            Debug.Assert(mangaResponse.Data != null, $"[MangaApiService] FetchMangaDetailsAsync({mangaId}) - API returned null Data despite ok result. URL: {url}");
            #endif

            if (mangaResponse.Result != "ok" || mangaResponse.Data == null)
            {
                Logger.LogWarning("API response for manga details {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaId, mangaResponse.Result, mangaResponse.Data != null, url);
                return null; // Trả về null nếu dữ liệu không hợp lệ
            }

            Logger.LogInformation("Successfully fetched details for manga: {MangaTitle} ({MangaId})",
                mangaResponse.Data.Attributes?.Title?.FirstOrDefault().Value ?? "N/A", mangaId);
            return mangaResponse;
        }

        /// <inheritdoc/>
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
            AddOrUpdateParam(queryParams, "includes[]", "cover_art"); // Chỉ cần cover_art cho danh sách
            AddOrUpdateParam(queryParams, "limit", mangaIds.Count.ToString()); // Đảm bảo lấy đủ

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch by IDs URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            // Trả về list rỗng nếu lỗi
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga by IDs failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = mangaIds.Count, Offset = 0, Total = 0 };
            }

            #if DEBUG
            Debug.Assert(mangaList.Result == "ok", $"[MangaApiService] FetchMangaByIdsAsync - API returned error: {mangaList.Result}. URL: {url}");
            Debug.Assert(mangaList.Data != null, $"[MangaApiService] FetchMangaByIdsAsync - API returned null Data despite ok result. URL: {url}");
            #endif

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