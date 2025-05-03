Chào bạn, tôi đã xem qua các file trong thư mục `/APIServices`. Tôi sẽ giúp bạn cập nhật các chú thích, tài liệu và loại bỏ code thừa đã được đánh dấu là lỗi thời (`[Obsolete]`).

**Lưu ý về tái sử dụng code:**

*   Bạn đang sử dụng tốt lớp `BaseApiService` để chứa các logic chung như tạo URL (`BuildUrlWithParams`, `AddOrUpdateParam`) và các thành phần dùng chung (`HttpClient`, `ILogger`, `JsonSerializerOptions`).
*   Lớp `ApiRequestHandler` giúp tập trung logic gọi API GET, xử lý lỗi và deserialize JSON, giúp các service khác gọn gàng hơn.
*   Hãy tiếp tục tận dụng các lớp cơ sở và helper này khi phát triển thêm các service API mới nhé.

Dưới đây là các bước chi tiết để thực hiện các thay đổi, được ghi trong file `TODO.md`:

```markdown
# TODO.md - Cập nhật APIServices

Hướng dẫn này mô tả các bước để cập nhật tài liệu và loại bỏ code lỗi thời trong thư mục `APIServices`.

## Mục lục

1.  [Cập nhật Interface `ICoverApiService`](#1-cập-nhật-interface-icoverapiservice)
2.  [Cập nhật Service `CoverApiService`](#2-cập-nhật-service-coverapiservice)
3.  [Cập nhật `Services/APIServices/README.md`](#3-cập-nhật-servicesapiservicesreadme)
4.  [Rà soát và cập nhật XML Comments cho các file còn lại](#4-rà-soát-và-cập-nhật-xml-comments-cho-các-file-còn-lại)

---

## 1. Cập nhật Interface `ICoverApiService`

**Mục tiêu:** Loại bỏ các phương thức đã đánh dấu `[Obsolete]` và cập nhật chú thích.

**File:** `Services/APIServices/Interfaces/ICoverApiService.cs`

**Các bước:**

1.  **Xóa bỏ các phương thức Obsolete:**
    *   Xóa phương thức `FetchRepresentativeCoverUrlsAsync`.
    *   Xóa phương thức `FetchCoverUrlAsync`.
2.  **Cập nhật XML Comments:**
    *   Đảm bảo chú thích cho `GetAllCoversForMangaAsync` và `GetProxiedCoverUrl` là chính xác và đầy đủ.

**Code thay đổi:**

```csharp
// Services/APIServices/Interfaces/ICoverApiService.cs
using MangaReader.WebUI.Models.Mangadex;
using System;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service tương tác với các endpoint API liên quan đến Cover Art.
    /// </summary>
    public interface ICoverApiService
    {
        /// <summary>
        /// Lấy TẤT CẢ ảnh bìa cho một manga cụ thể.
        /// Service sẽ tự động xử lý việc gọi nhiều trang API nếu cần thiết để lấy đủ dữ liệu.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng <see cref="CoverList"/> chứa danh sách tất cả các <see cref="Cover"/> của manga đó,
        /// hoặc <c>null</c> nếu có lỗi xảy ra trong quá trình gọi API.
        /// </returns>
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);

        // [PHẦN ĐÃ XÓA]
        // /// <summary>
        // /// [OBSOLETE] Lấy URL ảnh bìa ĐẠI DIỆN (thường là ảnh bìa mới nhất hoặc volume=null) cho một danh sách các manga.
        // /// </summary>
        // [Obsolete("Use cover art included in manga relationships instead.")]
        // Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds);
        //
        // /// <summary>
        // /// [OBSOLETE] Lấy URL ảnh bìa ĐẠI DIỆN cho một manga duy nhất.
        // /// </summary>
        // [Obsolete("Use cover art included in manga relationships instead.")]
        // Task<string> FetchCoverUrlAsync(string mangaId);
        // [/PHẦN ĐÃ XÓA]

        /// <summary>
        /// Tạo URL proxy cho ảnh bìa với kích thước tùy chọn thông qua Backend API.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="fileName">Tên file của ảnh bìa (ví dụ: 'cover.jpg').</param>
        /// <param name="size">Kích thước ảnh mong muốn (ví dụ: 512, 256). Kích thước này được MangaDex hỗ trợ.</param>
        /// <returns>URL đầy đủ của ảnh bìa đã được proxy bởi Backend API.</returns>
        string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512);
    }
}
```

---

## 2. Cập nhật Service `CoverApiService`

**Mục tiêu:** Loại bỏ các phương thức đã đánh dấu `[Obsolete]` và cập nhật chú thích.

**File:** `Services/APIServices/Services/CoverApiService.cs`

**Các bước:**

1.  **Xóa bỏ các phương thức Obsolete:**
    *   Xóa phương thức `FetchRepresentativeCoverUrlsAsync`.
    *   Xóa phương thức `FetchCoverUrlAsync`.
    *   Xóa phương thức `FetchCoversForMangaAsync` (cũng được đánh dấu obsolete).
2.  **Cập nhật XML Comments:**
    *   Xem lại chú thích cho hàm tạo (constructor).
    *   Xem lại chú thích cho `ExtractCoverFileNameFromRelationships` đảm bảo mô tả đúng về việc nó là `static` và cách sử dụng `ILogger`.
    *   Xem lại chú thích cho `GetAllCoversForMangaAsync` và `GetProxiedCoverUrl`.

**Code thay đổi:**

```csharp
// Services/APIServices/Services/CoverApiService.cs
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
            // ... (Giữ nguyên phần implementation của ExtractCoverFileNameFromRelationships)
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
             // ... (Giữ nguyên phần implementation của GetAllCoversForMangaAsync)
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

        // [PHẦN ĐÃ XÓA]
        // /// <inheritdoc/>
        // [Obsolete("Use cover art included in manga relationships instead.")]
        // public async Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)
        // { ... }
        //
        // /// <inheritdoc/>
        // [Obsolete("Use cover art included in manga relationships instead.")]
        // public async Task<string> FetchCoverUrlAsync(string mangaId)
        // { ... }
        //
        // /// <inheritdoc/>
        // [Obsolete("Use FetchCoverUrlAsync or FetchRepresentativeCoverUrlsAsync for better performance and representative covers.")]
        // public async Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10)
        // { ... }
        // [/PHẦN ĐÃ XÓA]

        /// <inheritdoc/>
        public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
        {
             // ... (Giữ nguyên phần implementation của GetProxiedCoverUrl)
            var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.{size}.jpg";
            return $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
        }
    }
}
```

---

## 3. Cập nhật `Services/APIServices/README.md`

**Mục tiêu:** Xóa bỏ mô tả về các phương thức đã lỗi thời trong phần `ICoverApiService` / `CoverApiService`.

**File:** `Services/APIServices/README.md`

**Các bước:**

1.  Mở file `Services/APIServices/README.md`.
2.  Tìm đến phần mô tả `ICoverApiService` / `CoverApiService`.
3.  Xóa các dòng mô tả về `FetchRepresentativeCoverUrlsAsync` và `FetchCoverUrlAsync`.
4.  Đảm bảo mô tả cho `GetAllCoversForMangaAsync` phản ánh đúng chức năng lấy *tất cả* ảnh bìa.
5.  Lưu file.

**Nội dung cần chỉnh sửa (ví dụ):**

```diff
---

### 4. `ICoverApiService` / `CoverApiService`

*   **Mục đích:** Xử lý các lệnh gọi API liên quan đến Cover Art thông qua Backend API proxy.
*   **Phương thức chính:**
    *   `Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)`
        *   **Input:** `mangaId` (string): ID của manga.
        *   **Output:** `Task<CoverList?>` - Một đối tượng `CoverList` chứa **tất cả** các `Cover` của manga, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  **Xử lý Pagination Nội bộ:** Gọi API `GET /api/mangadex/cover` lặp lại với `limit=100`, `offset` tăng dần và tham số `manga[]={mangaId}`.
            2.  Tích lũy kết quả và trả về `CoverList` tổng hợp.
-   *   `Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)`
-       *   **Input:** `mangaIds` (List<string>): Danh sách ID manga.
-       *   **Output:** `Task<Dictionary<string, string>?>` - Dictionary map từ `MangaId` sang URL ảnh bìa thumbnail (qua proxy `/mangadex/proxy-image`), hoặc `null` nếu lỗi.
-       *   **Luồng xử lý:**
-           1.  **Tối ưu hóa:** Gọi API `GET /api/mangadex/cover` một lần (hoặc theo batch) với nhiều tham số `manga[]`.
-           2.  Sử dụng `order[volume]=desc` để ưu tiên cover mới nhất.
-           3.  Lọc kết quả và tạo URL ảnh thumbnail qua proxy `/mangadex/proxy-image`.
-   *   `Task<string> FetchCoverUrlAsync(string mangaId)`
-       *   **Input:** `mangaId` (string): ID của một manga.
-       *   **Output:** `Task<string>` - URL ảnh bìa thumbnail (qua proxy `/mangadex/proxy-image`) hoặc chuỗi rỗng.
-       *   **Luồng xử lý:** Gọi `FetchRepresentativeCoverUrlsAsync` với danh sách chỉ chứa `mangaId`.
+   *   `string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)`
+       *   **Input:**
+           *   `mangaId` (string): ID của manga.
+           *   `fileName` (string): Tên file của ảnh bìa.
+           *   `size` (int): Kích thước ảnh mong muốn (mặc định 512).
+       *   **Output:** `string` - URL của ảnh bìa đã được proxy bởi Backend API.
+       *   **Luồng xử lý:** Tạo URL đến MangaDex uploads và bọc nó trong URL proxy của Backend API (`/mangadex/proxy-image?url=...`).

---
```

---

## 4. Rà soát và cập nhật XML Comments cho các file còn lại

**Mục tiêu:** Đảm bảo các chú thích XML (`/// <summary>...`) trong các file còn lại của thư mục `Services/APIServices` là chính xác, đầy đủ và nhất quán.

**Các file cần rà soát:**

*   `Services/APIServices/BaseApiService.cs`
*   `Services/APIServices/Interfaces/IApiRequestHandler.cs`
*   `Services/APIServices/Interfaces/IApiStatusService.cs`
*   `Services/APIServices/Interfaces/IChapterApiService.cs`
*   `Services/APIServices/Interfaces/IMangaApiService.cs`
*   `Services/APIServices/Interfaces/ITagApiService.cs`
*   `Services/APIServices/Services/ApiRequestHandler.cs`
*   `Services/APIServices/Services/ApiStatusService.cs`
*   `Services/APIServices/Services/ChapterApiService.cs`
*   `Services/APIServices/Services/MangaApiService.cs`
*   `Services/APIServices/Services/TagApiService.cs`

**Các bước:**

1.  Mở từng file trong danh sách trên.
2.  Đọc các chú thích XML hiện có cho lớp và từng phương thức/thuộc tính công khai (public).
3.  **Đối chiếu với code:** Kiểm tra xem chú thích có còn phù hợp với logic hiện tại của code hay không, đặc biệt là sau các thay đổi gần đây của bạn.
4.  **Bổ sung/Chỉnh sửa:**
    *   Thêm chú thích cho các thành phần public chưa có.
    *   Sửa lại nội dung chú thích cho rõ ràng, chính xác hơn. Mô tả rõ mục đích, tham số (`<param>`), giá trị trả về (`<returns>`), và các trường hợp ngoại lệ có thể xảy ra (`<exception>`) nếu cần.
    *   Đảm bảo văn phong nhất quán (tiếng Việt, mô tả ngắn gọn, dễ hiểu).
5.  Lưu lại các thay đổi.

**Ví dụ (minh họa):**

Giả sử trong `BaseApiService.cs`, bạn muốn làm rõ hơn về `BuildUrlWithParams`:

```csharp
// File: Services/APIServices/BaseApiService.cs

// ...

        /// <summary>
        /// Xây dựng URL đầy đủ với các tham số truy vấn từ một Dictionary.
        /// Hỗ trợ các tham số có nhiều giá trị (ví dụ: `includes[]=a&includes[]=b` bằng cách lặp qua danh sách giá trị của key).
        /// Tự động mã hóa (URL encode) tên và giá trị tham số.
        /// </summary>
        /// <param name="endpointPath">Đường dẫn tương đối của endpoint (ví dụ: "manga", "chapter/{id}/feed"). Sẽ được nối với BaseUrl.</param>
        /// <param name="parameters">Dictionary chứa các tham số truy vấn. Key là tên tham số (ví dụ: "limit", "includes[]"), Value là danh sách các giá trị cho tham số đó.</param>
        /// <returns>URL đầy đủ đã bao gồm các tham số truy vấn đã được mã hóa.</returns>
        protected string BuildUrlWithParams(string endpointPath, Dictionary<string, List<string>>? parameters = null)
        {
           // ... implementation ...
        }

// ...
```

Hãy thực hiện các bước trên để hoàn tất việc cập nhật. Chúc bạn thành công!
```