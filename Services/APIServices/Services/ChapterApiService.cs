using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="IChapterApiService"/>.
    /// Tương tác với các endpoint API liên quan đến Chapter của MangaDex thông qua Backend API proxy.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency và gọi constructor lớp cơ sở.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình.</param>
    /// <param name="logger">Logger cho ChapterApiService.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API.</param>
    public class ChapterApiService(
        HttpClient httpClient,
        ILogger<ChapterApiService> logger,
        IConfiguration configuration,
        IApiRequestHandler apiRequestHandler)
        : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
          IChapterApiService
    {
        /// <summary>
        /// URL cơ sở của Backend API (không bao gồm /mangadex) để xây dựng URL proxy ảnh.
        /// </summary>
        private readonly string _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                      ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");

        /// <inheritdoc/>
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

                #if DEBUG
                Debug.Assert(chapterListResponse.Result == "ok", $"[ChapterApiService] FetchChaptersAsync({mangaId}) - API returned error: {chapterListResponse.Result} at offset {offset}.");
                Debug.Assert(chapterListResponse.Data != null, $"[ChapterApiService] FetchChaptersAsync({mangaId}) - API returned null Data despite ok result at offset {offset}.");
                #endif

                if (chapterListResponse.Result != "ok" || chapterListResponse.Data == null || !chapterListResponse.Data.Any())
                {
                    Logger.LogInformation("No more chapters found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                    if (totalAvailable == 0 && offset == 0) totalAvailable = chapterListResponse.Total;
                    break;
                }

                allChapters.AddRange(chapterListResponse.Data);
                totalFetched += chapterListResponse.Data.Count;
                if (totalAvailable == 0) totalAvailable = chapterListResponse.Total; // Get total from first successful response
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

        /// <inheritdoc/>
        public async Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "scanlation_group", "manga", "user" } }
            };
            var url = BuildUrlWithParams($"chapter/{chapterId}", queryParams);
            Logger.LogInformation("Fetching info for chapter ID: {ChapterId}", chapterId);

            var chapterResponse = await GetApiAsync<ChapterResponse>(url);
            if (chapterResponse == null) return null;

            #if DEBUG
            Debug.Assert(chapterResponse.Result == "ok", $"[ChapterApiService] FetchChapterInfoAsync({chapterId}) - API returned error: {chapterResponse.Result}.");
            Debug.Assert(chapterResponse.Data != null, $"[ChapterApiService] FetchChapterInfoAsync({chapterId}) - API returned null Data despite ok result.");
            #endif

            if (chapterResponse.Result != "ok" || chapterResponse.Data == null)
            {
                Logger.LogWarning("API response for chapter info {ChapterId} has invalid format or missing data. Result: {Result}, HasData: {HasData}",
                    chapterId, chapterResponse.Result, chapterResponse.Data != null);
                return null;
            }

            Logger.LogInformation("Successfully fetched info for chapter ID: {ChapterId}", chapterId);
            return chapterResponse;
        }

        /// <inheritdoc/>
        public async Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            var url = BuildUrlWithParams($"at-home/server/{chapterId}");
            Logger.LogInformation("Fetching chapter pages info for chapter ID: {ChapterId}", chapterId);

            var atHomeResponse = await GetApiAsync<AtHomeServerResponse>(url);
            if (atHomeResponse == null) return null;

            #if DEBUG
            Debug.Assert(atHomeResponse.Result == "ok", $"[ChapterApiService] FetchChapterPagesAsync({chapterId}) - API returned error: {atHomeResponse.Result}.");
            Debug.Assert(!string.IsNullOrEmpty(atHomeResponse.BaseUrl), $"[ChapterApiService] FetchChapterPagesAsync({chapterId}) - API returned empty/null BaseUrl.");
            Debug.Assert(atHomeResponse.Chapter?.Data != null, $"[ChapterApiService] FetchChapterPagesAsync({chapterId}) - API returned null Chapter.Data.");
            Debug.Assert(atHomeResponse.Chapter?.Data?.Any() ?? false, $"[ChapterApiService] FetchChapterPagesAsync({chapterId}) - API returned empty Chapter.Data.");
            #endif

            if (atHomeResponse.Result != "ok" || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null || !atHomeResponse.Chapter.Data.Any())
            {
                Logger.LogWarning("API response for chapter pages {ChapterId} has invalid format or missing data. Result: {Result}, BaseUrl: {HasBaseUrl}, ChapterData: {HasChapterData}, HasPages: {HasPages}",
                    chapterId, atHomeResponse.Result, !string.IsNullOrEmpty(atHomeResponse.BaseUrl), atHomeResponse.Chapter?.Data != null, atHomeResponse.Chapter?.Data?.Any() ?? false);
                return null;
            }

            Logger.LogInformation("Successfully fetched page info for chapter ID: {ChapterId}", chapterId);
            return atHomeResponse;
        }

        /// <summary>
        /// Tạo URL proxy để tải ảnh chapter qua Backend API.
        /// </summary>
        /// <param name="baseUrl">URL cơ sở của server MangaDex@Home.</param>
        /// <param name="chapterId">ID của chapter (không thực sự dùng trong URL này, nhưng có thể hữu ích để log).</param>
        /// <param name="hash">Hash của chapter.</param>
        /// <param name="fileName">Tên file ảnh.</param>
        /// <param name="isDataSaver">Chỉ định có sử dụng ảnh tiết kiệm dữ liệu không.</param>
        /// <returns>URL đầy đủ của ảnh đã được proxy.</returns>
        public string GetProxyImageUrl(string baseUrl, string chapterId, string hash, string fileName, bool isDataSaver = false)
        {
            var folder = isDataSaver ? "data-saver" : "data";
            var originalImageUrl = $"{baseUrl}/{folder}/{hash}/{fileName}";
            var proxiedUrl = $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
            return proxiedUrl;
        }

        /// <summary>
        /// [Helper] Lấy danh sách chapter của một manga với phân trang cơ bản.
        /// Hàm này không xử lý pagination tự động để lấy hết.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="limit">Số lượng tối đa.</param>
        /// <param name="offset">Vị trí bắt đầu.</param>
        /// <param name="order">Thứ tự sắp xếp.</param>
        /// <returns>Đối tượng ChapterList hoặc list rỗng nếu lỗi.</returns>
        public async Task<ChapterList?> FetchChaptersByMangaIdAsync(string mangaId, int limit = 100, int offset = 0, string order = "desc")
        {
            var queryParams = new Dictionary<string, List<string>>();
            AddOrUpdateParam(queryParams, "limit", limit.ToString());
            AddOrUpdateParam(queryParams, "offset", offset.ToString());
            AddOrUpdateParam(queryParams, "order[chapter]", order);
            AddOrUpdateParam(queryParams, "order[volume]", order);
            AddOrUpdateParam(queryParams, "includes[]", "scanlation_group");
            AddOrUpdateParam(queryParams, "includes[]", "user");

            var url = BuildUrlWithParams($"manga/{mangaId}/feed", queryParams);
            Logger.LogInformation("Fetching chapters by manga ID {MangaId} (Limit: {Limit}, Offset: {Offset})", mangaId, limit, offset);

            var chapterList = await GetApiAsync<ChapterList>(url);
            if (chapterList == null) return new ChapterList { Data = new List<Chapter>(), Result = "error" };

            #if DEBUG
            Debug.Assert(chapterList.Result == "ok", $"[ChapterApiService] FetchChaptersByMangaIdAsync({mangaId}) - API returned error: {chapterList.Result}.");
            Debug.Assert(chapterList.Data != null, $"[ChapterApiService] FetchChaptersByMangaIdAsync({mangaId}) - API returned null Data despite ok result.");
            #endif

            if (chapterList.Result != "ok" || chapterList.Data == null)
            {
                Logger.LogWarning("API response for chapters by manga ID {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}",
                    mangaId, chapterList.Result, chapterList.Data != null);
                return new ChapterList { Data = new List<Chapter>(), Result = chapterList.Result ?? "error" };
            }

            Logger.LogInformation("Successfully fetched {Count} chapters for manga ID: {MangaId}.", chapterList.Data.Count, mangaId);
            return chapterList;
        }

        /// <summary>
        /// [Helper] Lấy danh sách chapter của một manga theo ngôn ngữ cụ thể với phân trang cơ bản.
        /// Hàm này không xử lý pagination tự động để lấy hết.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="language">Mã ngôn ngữ.</param>
        /// <param name="limit">Số lượng tối đa.</param>
        /// <param name="offset">Vị trí bắt đầu.</param>
        /// <param name="order">Thứ tự sắp xếp.</param>
        /// <returns>Đối tượng ChapterList hoặc list rỗng nếu lỗi.</returns>
        public async Task<ChapterList?> FetchChaptersByLanguageAsync(string mangaId, string language, int limit = 100, int offset = 0, string order = "desc")
        {
            if (string.IsNullOrEmpty(language))
            {
                Logger.LogWarning("Language is null or empty for FetchChaptersByLanguageAsync");
                return new ChapterList { Data = new List<Chapter>(), Result = "error" };
            }

            var queryParams = new Dictionary<string, List<string>>();
            AddOrUpdateParam(queryParams, "limit", limit.ToString());
            AddOrUpdateParam(queryParams, "offset", offset.ToString());
            AddOrUpdateParam(queryParams, "order[chapter]", order);
            AddOrUpdateParam(queryParams, "order[volume]", order);
            AddOrUpdateParam(queryParams, "translatedLanguage[]", language);
            AddOrUpdateParam(queryParams, "includes[]", "scanlation_group");
            AddOrUpdateParam(queryParams, "includes[]", "user");

            var url = BuildUrlWithParams($"manga/{mangaId}/feed", queryParams);
            Logger.LogInformation("Fetching chapters by language {Language} for manga ID {MangaId} (Limit: {Limit}, Offset: {Offset})", language, mangaId, limit, offset);

            var chapterList = await GetApiAsync<ChapterList>(url);
            if (chapterList == null) return new ChapterList { Data = new List<Chapter>(), Result = "error" };

            #if DEBUG
            Debug.Assert(chapterList.Result == "ok", $"[ChapterApiService] FetchChaptersByLanguageAsync({mangaId}, {language}) - API returned error: {chapterList.Result}.");
            Debug.Assert(chapterList.Data != null, $"[ChapterApiService] FetchChaptersByLanguageAsync({mangaId}, {language}) - API returned null Data despite ok result.");
            #endif

            if (chapterList.Result != "ok" || chapterList.Data == null)
            {
                Logger.LogWarning("API response for chapters by language {Language} for manga ID {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}",
                    language, mangaId, chapterList.Result, chapterList.Data != null);
                return new ChapterList { Data = new List<Chapter>(), Result = chapterList.Result ?? "error" };
            }

            Logger.LogInformation("Successfully fetched {Count} chapters in language {Language} for manga ID: {MangaId}.", chapterList.Data.Count, language, mangaId);
            return chapterList;
        }
    }
} 