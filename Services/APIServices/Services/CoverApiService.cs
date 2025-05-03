using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="ICoverApiService"/>.
    /// Tương tác với các endpoint API liên quan đến Cover Art của MangaDex thông qua Backend API proxy.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency và gọi constructor lớp cơ sở.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình.</param>
    /// <param name="logger">Logger cho CoverApiService.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API.</param>
    public class CoverApiService(
        HttpClient httpClient,
        ILogger<CoverApiService> logger,
        IConfiguration configuration,
        IApiRequestHandler apiRequestHandler)
        : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
          ICoverApiService
    {
        /// <summary>
        /// URL cơ sở của Backend API (không bao gồm /mangadex) để xây dựng URL proxy ảnh.
        /// </summary>
        private readonly string _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                      ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");

        /// <summary>
        /// Khoảng thời gian delay giữa các lần gọi API để tránh rate limit.
        /// </summary>
        private readonly TimeSpan _apiDelay = TimeSpan.FromMilliseconds(configuration?.GetValue<int>("ApiRateLimitDelayMs", 250) ?? 250);

        /// <inheritdoc/>
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
                AddOrUpdateParam(queryParams, "order[volume]", "asc"); // Sắp xếp theo volume tăng dần

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogDebug("Fetching covers page: {Url}", url);

                try
                {
                    var coverListResponse = await GetApiAsync<CoverList>(url);

                    // Xử lý lỗi API hoặc response null
                    if (coverListResponse == null)
                    {
                        Logger.LogWarning("Error fetching covers for manga {MangaId} at offset {Offset}. Retrying...", mangaId, offset);
                        await Task.Delay(TimeSpan.FromSeconds(1)); // Delay trước khi retry
                        coverListResponse = await GetApiAsync<CoverList>(url);

                        if (coverListResponse == null)
                        {
                            Logger.LogError("Failed to fetch covers for manga {MangaId} at offset {Offset} after retry. Stopping pagination.", mangaId, offset);
                            break; // Dừng nếu retry cũng lỗi
                        }
                    }

                    if (coverListResponse.Data == null || !coverListResponse.Data.Any())
                    {
                        Logger.LogInformation("No more covers found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                        if (totalAvailable == 0 && offset == 0) totalAvailable = coverListResponse.Total;
                        break; // Dừng nếu không còn dữ liệu
                    }

                    allCovers.AddRange(coverListResponse.Data);
                    if (totalAvailable == 0) totalAvailable = coverListResponse.Total;
                    offset += limit;
                    Logger.LogDebug("Fetched {Count} covers. Offset now: {Offset}. Total available: {TotalAvailable}",
                        coverListResponse.Data.Count, offset, totalAvailable);

                    // Delay giữa các lần gọi API nếu còn trang tiếp theo
                    if (offset < totalAvailable && totalAvailable > 0)
                    {
                        await Task.Delay(_apiDelay);
                    }
                }
                catch (Exception ex) // Bắt lỗi không mong muốn trong vòng lặp
                {
                    Logger.LogError(ex, "Unexpected exception during cover pagination for manga ID: {MangaId}", mangaId);
                    return null; // Trả về null nếu có lỗi nghiêm trọng
                }

            } while (offset < totalAvailable && totalAvailable > 0);

            Logger.LogInformation("Finished fetching. Total covers retrieved: {RetrievedCount} for manga ID: {MangaId}. API reported total: {ApiTotal}",
                allCovers.Count, mangaId, totalAvailable);

            // Trả về đối tượng CoverList tổng hợp
            return new CoverList
            {
                Result = "ok",
                Response = "collection",
                Data = allCovers,
                Limit = allCovers.Count, // Giới hạn là tổng số đã lấy
                Offset = 0,
                Total = totalAvailable // Tổng số thực tế từ API
            };
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || !mangaIds.Any())
            {
                return new Dictionary<string, string>();
            }

            Logger.LogInformation("Fetching representative covers for {Count} manga IDs...", mangaIds.Count);
            var resultCovers = new Dictionary<string, string>();
            var processedMangaIds = new HashSet<string>(); // Để tránh xử lý trùng lặp

            const int batchSize = 100; // Gọi API theo batch 100 manga ID
            for (int i = 0; i < mangaIds.Count; i += batchSize)
            {
                var currentBatchIds = mangaIds.Skip(i).Take(batchSize).Distinct().ToList(); // Lấy batch và loại bỏ ID trùng
                if (!currentBatchIds.Any()) continue;

                var queryParams = new Dictionary<string, List<string>>();
                // Sắp xếp ưu tiên volume null (thường là cover chính), sau đó đến volume giảm dần
                AddOrUpdateParam(queryParams, "order[volume]", "desc");
                AddOrUpdateParam(queryParams, "order[createdAt]", "desc"); // Ưu tiên cover mới nhất nếu volume giống nhau
                AddOrUpdateParam(queryParams, "limit", currentBatchIds.Count.ToString()); // Lấy tối đa 1 cover cho mỗi manga trong batch

                foreach (var mangaId in currentBatchIds)
                {
                    AddOrUpdateParam(queryParams, "manga[]", mangaId);
                }

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogDebug("Fetching representative covers batch: {Url}", url);

                try
                {
                    var coverListResponse = await GetApiAsync<CoverList>(url);

                    if (coverListResponse == null)
                    {
                        Logger.LogWarning("Error fetching representative covers batch. Retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        coverListResponse = await GetApiAsync<CoverList>(url);

                        if (coverListResponse == null)
                        {
                            Logger.LogError("Failed to fetch representative covers batch after retry.");
                            continue; // Bỏ qua batch này nếu retry lỗi
                        }
                    }

                    if (coverListResponse.Data != null && coverListResponse.Data.Any())
                    {
                        // Nhóm các cover theo mangaId để chọn cover đại diện tốt nhất cho mỗi manga
                        var coversByManga = coverListResponse.Data
                            .Where(c => c.Relationships != null)
                            .SelectMany(c => c.Relationships! // Dùng ! vì đã kiểm tra null
                                .Where(r => r.Type == "manga")
                                .Select(r => new { MangaId = r.Id.ToString(), Cover = c }))
                            .GroupBy(x => x.MangaId);

                        foreach (var group in coversByManga)
                        {
                            var mangaId = group.Key;
                            // Chỉ xử lý nếu chưa có cover cho manga này
                            if (!resultCovers.ContainsKey(mangaId) && !processedMangaIds.Contains(mangaId))
                            {
                                // Chọn cover đầu tiên trong group (đã được sắp xếp theo volume/createdAt từ API)
                                var representativeCover = group.First().Cover;

                                if (representativeCover.Attributes?.FileName != null)
                                {
                                    var thumbnailUrl = GetProxiedCoverUrl(mangaId, representativeCover.Attributes.FileName);
                                    resultCovers.Add(mangaId, thumbnailUrl);
                                    processedMangaIds.Add(mangaId); // Đánh dấu đã xử lý
                                    Logger.LogDebug("Found representative cover for {MangaId}: {Url}", mangaId, thumbnailUrl);
                                }
                                else
                                {
                                     Logger.LogWarning("Representative cover found for {MangaId} but filename is null.", mangaId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing representative covers batch.");
                }

                // Delay giữa các batch
                if (i + batchSize < mangaIds.Count)
                {
                    await Task.Delay(_apiDelay);
                }
            }

            Logger.LogInformation("Retrieved {FoundCount} representative covers out of {RequestedCount} requested manga IDs.", resultCovers.Count, mangaIds.Count);
            return resultCovers;
        }

        /// <inheritdoc/>
        public async Task<string> FetchCoverUrlAsync(string mangaId)
        {
            Logger.LogInformation("Fetching single representative cover URL for manga ID: {MangaId}", mangaId);
            // Gọi hàm xử lý batch với danh sách chỉ chứa 1 ID
            var resultDict = await FetchRepresentativeCoverUrlsAsync(new List<string> { mangaId });

            if (resultDict != null && resultDict.TryGetValue(mangaId, out var url))
            {
                Logger.LogInformation("Found cover URL for manga {MangaId}: {Url}", mangaId, url);
                return url;
            }

            Logger.LogWarning("No representative cover URL found for manga ID: {MangaId}", mangaId);
            return string.Empty; // Trả về chuỗi rỗng nếu không tìm thấy hoặc lỗi
        }

        /// <inheritdoc/>
        [Obsolete("Use FetchCoverUrlAsync or FetchRepresentativeCoverUrlsAsync for better performance and representative covers.")]
        public async Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10)
        {
            var queryParams = new Dictionary<string, List<string>>();
            AddOrUpdateParam(queryParams, "manga[]", mangaId);
            AddOrUpdateParam(queryParams, "limit", limit.ToString());
            AddOrUpdateParam(queryParams, "order[volume]", "desc"); // Lấy các volume mới nhất trước

            var url = BuildUrlWithParams("cover", queryParams);
            Logger.LogInformation("[Legacy] Fetching covers for manga ID: {MangaId} (Limit: {Limit})", mangaId, limit);

            var coverList = await GetApiAsync<CoverList>(url);
            if (coverList == null) return new CoverList { Data = new List<Cover>(), Result = "error" };

            #if DEBUG
            Debug.Assert(coverList.Result == "ok", $"[CoverApiService] FetchCoversForMangaAsync({mangaId}) - API returned error: {coverList.Result}.");
            Debug.Assert(coverList.Data != null, $"[CoverApiService] FetchCoversForMangaAsync({mangaId}) - API returned null Data despite ok result.");
            #endif

            if (coverList.Result != "ok" || coverList.Data == null)
            {
                Logger.LogWarning("[Legacy] API response for covers by manga ID {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}",
                    mangaId, coverList.Result, coverList.Data != null);
                return new CoverList { Data = new List<Cover>(), Result = coverList.Result ?? "error" };
            }

            Logger.LogInformation("[Legacy] Successfully fetched {Count} covers for manga ID: {MangaId}.", coverList.Data.Count, mangaId);
            return coverList;
        }

        /// <summary>
        /// Helper tạo URL proxy cho ảnh bìa với kích thước tùy chọn.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="fileName">Tên file ảnh bìa.</param>
        /// <param name="size">Kích thước ảnh (ví dụ: 512, 256).</param>
        /// <returns>URL đầy đủ của ảnh đã được proxy.</returns>
        public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
        {
            var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.{size}.jpg";
            return $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
        }
    }
} 