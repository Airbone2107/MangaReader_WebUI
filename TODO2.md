```markdown
# TODO.md: Cập nhật Logic Xử lý Cover Art

## Mục tiêu

Tối ưu hóa việc lấy và xử lý ảnh bìa (cover art) để đáp ứng các trường hợp sử dụng khác nhau và xử lý giới hạn phân trang của API MangaDex.

## Các bước thực hiện

### Bước 1: Cập nhật Interface `ICoverApiService.cs`

Định nghĩa lại các phương thức trong interface để phản ánh rõ ràng các use case và kiểu dữ liệu trả về mong muốn.

```csharp
// File: Services/APIServices/ICoverApiService.cs
using MangaReader.WebUI.Models.Mangadex;
using System.Collections.Generic; // Thêm using
using System.Threading.Tasks; // Thêm using

namespace MangaReader.WebUI.Services.APIServices
{
    public interface ICoverApiService
    {
        /// <summary>
        /// Lấy TẤT CẢ ảnh bìa cho một manga, xử lý pagination.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>Danh sách tất cả Cover của manga đó hoặc null nếu có lỗi.</returns>
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);

        /// <summary>
        /// Lấy URL ảnh bìa ĐẠI DIỆN (ưu tiên volume mới nhất) cho một danh sách manga.
        /// </summary>
        /// <param name="mangaIds">Danh sách ID của các manga.</param>
        /// <returns>Dictionary map từ MangaId sang URL ảnh bìa đại diện (thumbnail .512.jpg) hoặc null nếu có lỗi.</returns>
        Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds);

        /// <summary>
        /// Lấy URL ảnh bìa ĐẠI DIỆN cho một manga duy nhất.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>URL ảnh bìa đại diện (thumbnail .512.jpg) hoặc chuỗi rỗng nếu không tìm thấy/lỗi.</returns>
        Task<string> FetchCoverUrlAsync(string mangaId);

        // Các phương thức khác nếu có...
    }
}
```

### Bước 2: Implement Logic Pagination trong `CoverApiService.cs`

Triển khai phương thức `GetAllCoversForMangaAsync` để gọi API `/cover` lặp lại cho đến khi lấy hết dữ liệu.

```csharp
// File: Services/APIServices/CoverApiService.cs
using MangaReader.WebUI.Models.Mangadex;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic; // Thêm using
using System.Linq; // Thêm using
using System.Threading.Tasks; // Thêm using
using System; // Thêm using

namespace MangaReader.WebUI.Services.APIServices
{
    public class CoverApiService : BaseApiService, ICoverApiService
    {
        private readonly string _imageProxyBaseUrl;
        private readonly TimeSpan _apiDelay = TimeSpan.FromMilliseconds(250); // Delay giữa các lần gọi API pagination

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
            const int limit = 100; // Luôn lấy tối đa 100 mỗi lần
            int totalAvailable = 0;

            Logger.LogInformation($"Fetching ALL covers for manga ID: {mangaId} with pagination...");

            do
            {
                var queryParams = new Dictionary<string, List<string>>
                {
                    { "manga[]", new List<string> { mangaId } },
                    { "limit", new List<string> { limit.ToString() } },
                    { "offset", new List<string> { offset.ToString() } },
                    { "order[volume]", new List<string> { "asc" } } // Sắp xếp tăng dần để hiển thị
                    // Bạn có thể thêm các order khác nếu muốn, ví dụ order[createdAt]=asc
                };

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogInformation($"Fetching covers page: {url}");

                try
                {
                    var response = await HttpClient.GetAsync(url);

                    // Xử lý lỗi Rate Limit (429) - Chờ và thử lại
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Logger.LogWarning($"Rate limit hit when fetching covers for manga {mangaId}. Waiting and retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(5)); // Chờ 5 giây
                        response = await HttpClient.GetAsync(url); // Thử lại request
                    }

                    var contentStream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var coverListResponse = await JsonSerializer.DeserializeAsync<CoverList>(contentStream, JsonOptions);

                        if (coverListResponse?.Data == null) // Không cần !.Any() vì API có thể trả về mảng rỗng hợp lệ
                        {
                             Logger.LogInformation($"No more covers found or data is null/invalid for manga {mangaId} at offset {offset}.");
                             // Nếu là lần gọi đầu tiên và không có data, totalAvailable sẽ là 0
                             if (offset == 0) totalAvailable = coverListResponse?.Total ?? 0;
                             break; // Dừng vòng lặp
                        }


                        allCovers.AddRange(coverListResponse.Data);
                        if (totalAvailable == 0 && offset == 0) // Lấy total từ response đầu tiên
                        {
                            totalAvailable = coverListResponse.Total;
                        }
                        offset += coverListResponse.Data.Count; // Tăng offset dựa trên số lượng thực tế trả về

                        Logger.LogDebug($"Fetched {coverListResponse.Data.Count} covers. Offset now: {offset}. Total available: {totalAvailable}");

                        // Chỉ delay nếu còn trang tiếp theo
                        if (offset < totalAvailable && totalAvailable > 0)
                        {
                            await Task.Delay(_apiDelay);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        LogApiError(nameof(GetAllCoversForMangaAsync), response, errorContent);
                        return null; // Lỗi API
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
                catch (TaskCanceledException ex) // Xử lý timeout
                {
                    Logger.LogError(ex, $"Request timed out during cover pagination for manga ID: {mangaId}");
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Unexpected exception during cover pagination for manga ID: {mangaId}");
                    return null;
                }

            } while (offset < totalAvailable && totalAvailable > 0); // Tiếp tục nếu còn cover

            Logger.LogInformation($"Finished fetching. Total covers retrieved: {allCovers.Count} for manga ID: {mangaId}. API reported total: {totalAvailable}");

            return new CoverList
            {
                Result = "ok",
                Response = "collection",
                Data = allCovers,
                Limit = allCovers.Count, // Tổng số đã lấy
                Offset = 0,
                Total = totalAvailable
            };
        }

        // Implement FetchRepresentativeCoverUrlsAsync và FetchCoverUrlAsync ở bước tiếp theo
        // ... (code cũ của FetchCoverUrlAsync sẽ được thay thế) ...
        public async Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)
        {
           // Sẽ implement ở Bước 3
           throw new NotImplementedException();
        }

        public async Task<string> FetchCoverUrlAsync(string mangaId)
        {
            // Sẽ implement ở Bước 4
            throw new NotImplementedException();
        }
    }
}
```

### Bước 3: Implement `FetchRepresentativeCoverUrlsAsync` trong `CoverApiService.cs`

Phương thức này sẽ lấy cover đại diện cho một danh sách manga.

```csharp
// File: Services/APIServices/CoverApiService.cs
// ... (using và constructor giữ nguyên) ...

public async Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)
{
    if (mangaIds == null || !mangaIds.Any())
    {
        return new Dictionary<string, string>();
    }

    Logger.LogInformation($"Fetching representative covers for {mangaIds.Count} manga IDs...");
    var resultCovers = new Dictionary<string, string>();
    var mangaIdsToProcess = new HashSet<string>(mangaIds); // Dùng HashSet để kiểm tra nhanh

    // Chia nhỏ danh sách mangaIds nếu cần (ví dụ: mỗi lần 100 ID)
    const int batchSize = 100;
    for (int i = 0; i < mangaIds.Count; i += batchSize)
    {
        var currentBatchIds = mangaIds.Skip(i).Take(batchSize).ToList();
        if (!currentBatchIds.Any()) continue;

        var queryParams = new Dictionary<string, List<string>>
        {
            // Sắp xếp ưu tiên volume mới nhất, sau đó đến ngày tạo mới nhất
            { "order[volume]", new List<string> { "desc" } },
            { "order[createdAt]", new List<string> { "desc" } },
            { "limit", new List<string> { currentBatchIds.Count.ToString() } } // Lấy tối đa số lượng ID trong batch
        };

        // Thêm các manga ID vào queryParams
        foreach (var mangaId in currentBatchIds)
        {
            AddOrUpdateParam(queryParams, "manga[]", mangaId);
        }

        var url = BuildUrlWithParams("cover", queryParams);
        Logger.LogInformation($"Fetching representative covers batch: {url}");

        try
        {
            var response = await HttpClient.GetAsync(url);
             // Xử lý lỗi Rate Limit (429) - Chờ và thử lại
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
                        // Tìm mangaId từ relationship của cover
                        string? relatedMangaId = cover.Relationships?
                            .FirstOrDefault(r => r.Type == "manga")?.Id.ToString();

                        // Nếu cover này thuộc về một manga trong batch VÀ manga đó chưa có cover trong kết quả
                        if (relatedMangaId != null && mangaIdsToProcess.Contains(relatedMangaId) && !resultCovers.ContainsKey(relatedMangaId))
                        {
                            if (cover.Attributes?.FileName != null)
                            {
                                string fileName = cover.Attributes.FileName;
                                // Tạo URL thumbnail (.512.jpg) qua proxy
                                string thumbnailUrl = $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"https://uploads.mangadex.org/covers/{relatedMangaId}/{fileName}.512.jpg")}";
                                resultCovers.Add(relatedMangaId, thumbnailUrl);
                                Logger.LogDebug($"Found representative cover for {relatedMangaId}: {thumbnailUrl}");
                            }
                        }

                        // Nếu đã tìm đủ cover cho batch này thì dừng sớm
                        if (resultCovers.Count >= currentBatchIds.Count) break;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchRepresentativeCoverUrlsAsync), response, errorContent);
                    // Không trả về null ngay, tiếp tục với batch khác nếu có
                }
            }
            // ... catch exceptions ...
            catch (Exception ex)
            {
                 Logger.LogError(ex, $"Error fetching representative covers batch.");
                 // Không trả về null ngay, tiếp tục với batch khác nếu có
            }

            // Delay nhỏ giữa các batch để tránh rate limit
            if (i + batchSize < mangaIds.Count)
            {
                await Task.Delay(_apiDelay);
            }
        }

    Logger.LogInformation($"Finished fetching representative covers. Found {resultCovers.Count} covers for {mangaIds.Count} requested manga IDs.");
    return resultCovers;
}

// ... (GetAllCoversForMangaAsync đã implement ở Bước 2) ...

// Implement FetchCoverUrlAsync ở bước tiếp theo
public async Task<string> FetchCoverUrlAsync(string mangaId)
{
    // Sẽ implement ở Bước 4
    throw new NotImplementedException();
}
```

### Bước 4: Cập nhật `FetchCoverUrlAsync` trong `CoverApiService.cs`

Phương thức này giờ sẽ gọi `FetchRepresentativeCoverUrlsAsync` cho một manga duy nhất.

```csharp
// File: Services/APIServices/CoverApiService.cs
// ... (using, constructor, GetAllCoversForMangaAsync, FetchRepresentativeCoverUrlsAsync giữ nguyên) ...

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
        // Gọi phương thức lấy cover đại diện cho list chỉ chứa 1 mangaId
        var coversDict = await FetchRepresentativeCoverUrlsAsync(new List<string> { mangaId });

        // Lấy URL từ dictionary trả về
        if (coversDict != null && coversDict.TryGetValue(mangaId, out var coverUrl) && !string.IsNullOrEmpty(coverUrl))
        {
            Logger.LogInformation($"Successfully fetched single cover URL for {mangaId}.");
            return coverUrl;
        }
        else
        {
            Logger.LogWarning($"Could not find representative cover URL for manga ID: {mangaId} after calling FetchRepresentativeCoverUrlsAsync.");
            return string.Empty; // Trả về rỗng nếu không tìm thấy
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, $"Exception in FetchCoverUrlAsync for manga ID: {mangaId}");
        return string.Empty; // Trả về rỗng nếu có lỗi
    }
}
```