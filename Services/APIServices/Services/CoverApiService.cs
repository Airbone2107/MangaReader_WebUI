using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="ICoverApiService"/>.
    /// Tương tác với các endpoint API liên quan đến Cover Art của MangaDex thông qua Backend API proxy.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency và gọi constructor lớp cơ sở.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình để gọi Backend API.</param>
    /// <param name="logger">Logger dành riêng cho CoverApiService.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình, đặc biệt là BaseUrl.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API chung.</param>
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
        /// Khoảng thời gian delay (ms) giữa các lần gọi API liên tiếp trong quá trình phân trang để tránh rate limit.
        /// Giá trị được lấy từ cấu hình "ApiRateLimitDelayMs" hoặc mặc định là 250ms.
        /// </summary>
        private readonly TimeSpan _apiDelay = TimeSpan.FromMilliseconds(configuration?.GetValue<int>("ApiRateLimitDelayMs", 250) ?? 250);

        /// <summary>
        /// Helper tĩnh để trích xuất filename của cover art chính từ danh sách relationships của một đối tượng Manga.
        /// Phương thức này tìm relationship có type 'cover_art' đầu tiên và trích xuất thuộc tính 'fileName' từ 'attributes' của nó (nếu có và được include trong lời gọi API gốc).
        /// </summary>
        /// <param name="relationships">Danh sách relationships từ đối tượng Manga.</param>
        /// <param name="logger">Logger (tùy chọn) để ghi log trong quá trình trích xuất (ví dụ: logger của service gọi hàm này).</param>
        /// <returns>Filename của cover art (ví dụ: 'abc.jpg') hoặc null nếu không tìm thấy relationship 'cover_art' hợp lệ hoặc không có thuộc tính 'fileName'.</returns>
        public static string? ExtractCoverFileNameFromRelationships(List<Relationship>? relationships, ILogger? logger = null)
        {
            if (relationships == null || !relationships.Any())
            {
                logger?.LogDebug("ExtractCoverFileName: Danh sách relationships rỗng hoặc null."); // Đổi thành Debug vì đây là trường hợp phổ biến
                return null;
            }

            // Tìm relationship cover_art đầu tiên
            var coverRelationship = relationships.FirstOrDefault(r => r != null && r.Type == "cover_art");

            if (coverRelationship == null)
            {
                logger?.LogDebug("ExtractCoverFileName: Không tìm thấy relationship có type 'cover_art'."); // Đổi thành Debug
                return null;
            }

            // Kiểm tra xem attributes có được include không và có phải là JsonElement không
            if (coverRelationship.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    // Thử lấy thuộc tính 'fileName'
                    if (attributesElement.TryGetProperty("fileName", out var fileNameElement) && fileNameElement.ValueKind == JsonValueKind.String)
                    {
                        var fileName = fileNameElement.GetString();
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            logger?.LogDebug("ExtractCoverFileName: Đã trích xuất filename '{FileName}' từ relationship {RelationshipId}.", fileName, coverRelationship.Id);
                            return fileName;
                        }
                        else
                        {
                            logger?.LogWarning("ExtractCoverFileName: Thuộc tính 'fileName' rỗng trong attributes của relationship {RelationshipId}.", coverRelationship.Id);
                        }
                    }
                    else
                    {
                        logger?.LogWarning("ExtractCoverFileName: Thuộc tính 'fileName' không tồn tại hoặc không phải string trong attributes của relationship {RelationshipId}.", coverRelationship.Id);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "ExtractCoverFileName: Lỗi khi đọc attributes của relationship {RelationshipId}.", coverRelationship.Id);
                }
            }
            else
            {
                logger?.LogWarning("ExtractCoverFileName: Relationship 'cover_art' {RelationshipId} không có attributes hoặc attributes không phải là object. Đảm bảo 'includes[]=cover_art' được sử dụng. Type of Attributes: {AttributeType}",
                    coverRelationship.Id, coverRelationship.Attributes?.GetType().Name ?? "null");
            }

            logger?.LogWarning("ExtractCoverFileName: Không thể trích xuất filename từ attributes của cover art {RelationshipId}.", coverRelationship.Id);
            return null;
        }

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
        public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
        {
            var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.{size}.jpg";
            return $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
        }
    }
} 