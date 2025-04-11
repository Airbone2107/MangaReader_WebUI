# TODO.md: Triển khai Tính năng Lịch sử Đọc Truyện (Phương án HTMX Trigger Riêng)

## Mục tiêu

Thêm chức năng cho phép người dùng xem lại lịch sử các chương truyện đã đọc, bao gồm thông tin về truyện, chương cuối cùng đọc, và thời gian đọc. Sử dụng trigger HTMX riêng để lưu tiến độ khi trang đọc truyện (`Read.cshtml`) được tải.

## Các Bước Thực Hiện Chi Tiết

### 1. Frontend: Kích Hoạt Lưu Tiến Độ Đọc Bằng HTMX Trigger

**File cần sửa:** `manga_reader_web\Views\Chapter\Read.cshtml`

**Công việc:**

1.  **Thêm thẻ `div` ẩn vào cuối file `Read.cshtml`:** Thẻ này sẽ tự động gửi request POST đến server khi trang được tải hoặc swap vào DOM bằng HTMX.
    *   Sử dụng `hx-post` để chỉ định Action Controller sẽ xử lý việc lưu tiến độ (ví dụ: `@Url.Action("SaveReadingProgress", "Chapter")`).
    *   Sử dụng `hx-trigger="load"` để request được gửi ngay khi phần tử này được tải.
    *   Sử dụng `hx-vals` để gửi kèm `mangaId` và `chapterId` lấy từ `@Model`. Ví dụ: `hx-vals='{"mangaId": "@Model.MangaId", "chapterId": "@Model.ChapterId"}'`.
    *   Sử dụng `hx-swap="none"` vì chúng ta không cần cập nhật giao diện từ response của request này.
    *   **Mã nguồn cần thêm:**
        ```html
        @* Thêm vào cuối file Views/Chapter/Read.cshtml *@
        <div hx-post="@Url.Action("SaveReadingProgress", "Chapter")"
             hx-trigger="load"
             hx-vals='{"mangaId": "@Model.MangaId", "chapterId": "@Model.ChapterId"}'
             hx-swap="none"
             aria-hidden="true"
             style="display: none;">
             <!-- Trigger lưu tiến độ đọc -->
        </div>
        ```

### 2. Backend (Frontend Project): Tạo Action Controller `SaveReadingProgress`

**File cần sửa:** `manga_reader_web\Controllers\ChapterController.cs`

**Công việc:**

1.  **Inject `IHttpClientFactory` và `IUserService`** vào `ChapterController` nếu chưa có.
    *   **Nhắc nhở:** Bạn đã inject `IHttpClientFactory` và `IUserService` trong `MangaController` và `AuthController`, bạn có thể tham khảo cách làm tương tự.
2.  **Tạo Action `SaveReadingProgress`:**
    *   Đánh dấu Action với `[HttpPost]`.
    *   Action nhận tham số `string mangaId`, `string chapterId` (HTMX sẽ gửi chúng từ `hx-vals`).
    *   **Kiểm tra xác thực:** Dùng `_userService.IsAuthenticated()` để kiểm tra người dùng đã đăng nhập chưa. Nếu chưa, trả về `Unauthorized()` hoặc `Forbid()`.
    *   **Lấy Token:** Lấy token xác thực từ `_userService.GetToken()`. `UserService` của bạn đã sử dụng Cookie, nên hàm này sẽ đọc từ Cookie.
    *   **Gọi Backend API:**
        *   Tạo `HttpClient` từ `_httpClientFactory.CreateClient("BackendApiClient")`.
        *   Thêm header `Authorization: Bearer <token>` vào request.
        *   Tạo request body JSON: `{ "mangaId": mangaId, "lastChapter": chapterId }`. **Lưu ý:** Backend API (`userRoutes.js`) đang mong đợi `lastChapter` chứ không phải `chapterId`.
        *   Gửi request `POST` đến endpoint backend `/api/users/reading-progress`.
        *   Log kết quả (thành công/thất bại).
    *   **Trả về kết quả:** Trả về `Ok()` hoặc `NoContent()` nếu thành công. Trả về `StatusCode(500)` hoặc mã lỗi phù hợp nếu gọi backend thất bại. **Quan trọng:** Không cần trả về nội dung HTML vì `hx-swap="none"`.
    *   **Mã nguồn cần thêm/sửa:**
        ```csharp
        using manga_reader_web.Services.AuthServices; // Thêm using này
        using System.Net.Http.Headers; // Thêm using này
        using System.Text; // Thêm using này
        using System.Text.Json; // Thêm using này

        namespace manga_reader_web.Controllers
        {
            public class ChapterController : Controller
            {
                private readonly ILogger<ChapterController> _logger;
                private readonly ChapterReadingServices _chapterReadingServices;
                private readonly ViewRenderService _viewRenderService;
                private readonly IHttpClientFactory _httpClientFactory; // Thêm injection
                private readonly IUserService _userService; // Thêm injection

                public ChapterController(
                    ChapterReadingServices chapterReadingServices,
                    ViewRenderService viewRenderService,
                    ILogger<ChapterController> logger,
                    IHttpClientFactory httpClientFactory, // Thêm vào constructor
                    IUserService userService) // Thêm vào constructor
                {
                    _chapterReadingServices = chapterReadingServices;
                    _viewRenderService = viewRenderService;
                    _logger = logger;
                    _httpClientFactory = httpClientFactory; // Gán giá trị
                    _userService = userService; // Gán giá trị
                }

                // ... các action khác ...

                [HttpPost]
                public async Task<IActionResult> SaveReadingProgress(string mangaId, string chapterId)
                {
                    _logger.LogInformation($"Nhận yêu cầu lưu tiến độ đọc: MangaId={mangaId}, ChapterId={chapterId}");

                    if (!_userService.IsAuthenticated())
                    {
                        _logger.LogWarning("Người dùng chưa đăng nhập, không thể lưu tiến độ.");
                        return Unauthorized(); // Hoặc Forbid() tùy theo chính sách
                    }

                    var token = _userService.GetToken();
                    if (string.IsNullOrEmpty(token))
                    {
                        _logger.LogError("Không thể lấy token người dùng đã đăng nhập.");
                        return Unauthorized(); // Token không có vì lý do nào đó
                    }

                    try
                    {
                        var client = _httpClientFactory.CreateClient("BackendApiClient");
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        // Backend mong đợi "lastChapter", không phải "chapterId"
                        var payload = new { mangaId = mangaId, lastChapter = chapterId };
                        var jsonPayload = JsonSerializer.Serialize(payload);
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("/api/users/reading-progress", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"Lưu tiến độ đọc thành công cho MangaId={mangaId}, ChapterId={chapterId}");
                            return Ok(); // Hoặc NoContent()
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError($"Lỗi khi gọi API backend để lưu tiến độ đọc. Status: {response.StatusCode}, Content: {errorContent}");
                            // Xử lý lỗi cụ thể nếu cần (ví dụ: 401 từ backend -> xóa token frontend)
                            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                _userService.RemoveToken();
                                return Unauthorized();
                            }
                            return StatusCode((int)response.StatusCode, $"Lỗi từ backend: {response.ReasonPhrase}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi ngoại lệ khi lưu tiến độ đọc cho MangaId={mangaId}, ChapterId={chapterId}");
                        return StatusCode(500, "Lỗi máy chủ nội bộ khi lưu tiến độ đọc.");
                    }
                }
            }
        }
        ```

### 3. Backend (Frontend Project): Tạo Service Lấy Thông Tin Chapter

**File cần tạo/sửa:** `manga_reader_web\Services\MangaServices\ChapterServices\ChapterInfoService.cs` (Tạo mới) và Interface `IChapterInfoService.cs`.

**Công việc:**

1.  **Định nghĩa Model `ChapterInfo`:** (Có thể đặt trong thư mục Models hoặc cùng file Service)
    ```csharp
    namespace manga_reader_web.Services.MangaServices.Models // Hoặc namespace phù hợp
    {
        public class ChapterInfo
        {
            public string Id { get; set; }
            public string Title { get; set; } // Tiêu đề đã format (VD: Chương 10)
            public DateTime PublishedAt { get; set; }
            // Thêm các thuộc tính khác nếu cần
        }
    }
    ```
2.  **Tạo Interface `IChapterInfoService.cs`:**
    ```csharp
    using manga_reader_web.Services.MangaServices.Models;
    using System.Threading.Tasks;

    namespace manga_reader_web.Services.MangaServices.ChapterServices
    {
        public interface IChapterInfoService
        {
            Task<ChapterInfo> GetChapterInfoAsync(string chapterId);
        }
    }
    ```
3.  **Tạo Service `ChapterInfoService.cs`:**
    *   Inject `MangaDexService`, `JsonConversionService`, `ILogger`.
    *   Triển khai phương thức `GetChapterInfoAsync(string chapterId)`:
        *   Gọi `_mangaDexService.FetchChapterInfoAsync(chapterId)`.
        *   Parse JSON response (sử dụng `JsonConversionService` nếu cần).
        *   Trích xuất thông tin cần thiết (ID, Title, PublishedAt). **Lưu ý:** Lấy `attributes.chapter` và `attributes.title` để tạo `Title` hiển thị.
        *   Xử lý lỗi cẩn thận.
    *   **Nhắc nhở:** Bạn đã có `MangaDexService` và `JsonConversionService`, hãy tái sử dụng chúng. Logic lấy `Title` có thể tham khảo từ `ChapterService.GetChapterDisplayInfo`.
    *   **Mã nguồn `ChapterInfoService.cs`:**
        ```csharp
        using manga_reader_web.Services.MangaServices.Models;
        using manga_reader_web.Services.UtilityServices;
        using Microsoft.Extensions.Logging;
        using System;
        using System.Collections.Generic;
        using System.Text.Json;
        using System.Threading.Tasks;

        namespace manga_reader_web.Services.MangaServices.ChapterServices
        {
            public class ChapterInfoService : IChapterInfoService
            {
                private readonly MangaDexService _mangaDexService;
                private readonly JsonConversionService _jsonConversionService;
                private readonly ILogger<ChapterInfoService> _logger;

                public ChapterInfoService(
                    MangaDexService mangaDexService,
                    JsonConversionService jsonConversionService,
                    ILogger<ChapterInfoService> logger)
                {
                    _mangaDexService = mangaDexService;
                    _jsonConversionService = jsonConversionService;
                    _logger = logger;
                }

                public async Task<ChapterInfo> GetChapterInfoAsync(string chapterId)
                {
                    if (string.IsNullOrEmpty(chapterId))
                    {
                        _logger.LogWarning("ChapterId không được cung cấp khi gọi GetChapterInfoAsync.");
                        return null;
                    }

                    try
                    {
                        _logger.LogInformation($"Đang lấy thông tin chi tiết cho chapter ID: {chapterId}");
                        var chapterData = await _mangaDexService.FetchChapterInfoAsync(chapterId);

                        if (chapterData == null)
                        {
                            _logger.LogWarning($"Không nhận được dữ liệu cho chapter ID: {chapterId}");
                            return null;
                        }

                        var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapterData.ToString());
                        var chapterDict = _jsonConversionService.ConvertJsonElementToDict(chapterElement);

                        if (!chapterDict.ContainsKey("attributes") || chapterDict["attributes"] == null)
                        {
                            _logger.LogWarning($"Chapter {chapterId} không có attributes.");
                            return null;
                        }

                        var attributesDict = (Dictionary<string, object>)chapterDict["attributes"];

                        // Lấy thông tin cần thiết
                        string chapterNumber = attributesDict.ContainsKey("chapter") && attributesDict["chapter"] != null ? attributesDict["chapter"].ToString() : "?";
                        string chapterTitleAttr = attributesDict.ContainsKey("title") && attributesDict["title"] != null ? attributesDict["title"].ToString() : "";
                        DateTime publishedAt = attributesDict.ContainsKey("publishAt") && attributesDict["publishAt"] != null && DateTime.TryParse(attributesDict["publishAt"].ToString(), out var date) ? date : DateTime.MinValue;

                        // Tạo tiêu đề hiển thị (Tương tự ChapterService)
                        string displayTitle = $"Chương {chapterNumber}";
                        if (!string.IsNullOrEmpty(chapterTitleAttr) && chapterTitleAttr != chapterNumber)
                        {
                            displayTitle += $": {chapterTitleAttr}";
                        }

                        return new ChapterInfo
                        {
                            Id = chapterId,
                            Title = displayTitle,
                            PublishedAt = publishedAt
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi lấy thông tin chi tiết cho chapter ID: {chapterId}");
                        return null; // Trả về null nếu có lỗi
                    }
                }
            }
        }
        ```
4.  **Đăng ký Service trong `Program.cs`:**
    ```csharp
    // Thêm dòng này vào phần đăng ký services
    builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterServices.IChapterInfoService, manga_reader_web.Services.MangaServices.ChapterServices.ChapterInfoService>();
    ```

### 4. Frontend: Tạo Model `LastReadMangaViewModel`

**File cần tạo:** `manga_reader_web\Services\MangaServices\Models\LastReadMangaViewModel.cs`

**Công việc:**

1.  Tạo class `LastReadMangaViewModel` với các thuộc tính: `MangaId`, `MangaTitle`, `CoverUrl`, `ChapterId`, `ChapterTitle`, `ChapterPublishedAt`, `LastReadAt`.
    *   **Mã nguồn:**
        ```csharp
        using System;

        namespace manga_reader_web.Services.MangaServices.Models
        {
            public class LastReadMangaViewModel
            {
                public string MangaId { get; set; }
                public string MangaTitle { get; set; }
                public string CoverUrl { get; set; }
                public string ChapterId { get; set; }
                public string ChapterTitle { get; set; } // Tiêu đề chương đã đọc cuối
                public DateTime ChapterPublishedAt { get; set; } // Ngày đăng chương đã đọc cuối
                public DateTime LastReadAt { get; set; } // Thời điểm đọc chương đó
            }
        }
        ```

### 5. Frontend: Tạo Service Lấy và Xử Lý Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\Services\MangaServices\ReadingHistoryService.cs` và Interface `IReadingHistoryService.cs`.

**Công việc:**
1.  **Định nghĩa Interface `IReadingHistoryService.cs`:**
    ```csharp
    using manga_reader_web.Services.MangaServices.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    namespace manga_reader_web.Services.MangaServices
    {
        public interface IReadingHistoryService
        {
            Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync();
        }
    }
    ```
2.  **Tạo Service `ReadingHistoryService.cs`:**
    *   Inject các dependency cần thiết (`IHttpClientFactory`, `IUserService`, `IMangaInfoService`, `IChapterInfoService`, `IConfiguration`, `ILogger`).
    *   **Nhắc nhở:** Bạn đã có `IHttpClientFactory`, `IUserService`, `IConfiguration`, `ILogger`. `IMangaInfoService` và `IChapterInfoService` đã được tạo ở bước 3.
    *   Triển khai `GetReadingHistoryAsync()`:
        *   Kiểm tra xác thực, lấy token (`_userService`).
        *   Gọi API backend `/api/users/reading-history` để lấy danh sách lịch sử cơ bản (`List<BackendHistoryItem>`). **Lưu ý:** Cần tạo một class `BackendHistoryItem` để deserialize response từ backend.
        *   Lặp qua từng item lịch sử:
            *   **Áp dụng `Task.Delay(_rateLimitDelay)`** trước mỗi *nhóm* gọi API MangaDex (ví dụ: trước khi gọi `GetMangaInfoAsync` và `GetChapterInfoAsync` cho mỗi item). Đặt `_rateLimitDelay` khoảng 550ms.
            *   Gọi `_mangaInfoService.GetMangaInfoAsync(item.MangaId)` để lấy Tên và Ảnh bìa manga.
            *   Gọi `_chapterInfoService.GetChapterInfoAsync(item.ChapterId)` để lấy Tên và Ngày đăng chương.
            *   Tạo `LastReadMangaViewModel` từ dữ liệu backend và dữ liệu lấy từ các service.
            *   Thêm vào danh sách kết quả.
        *   **Sắp xếp:** Backend API đã trả về danh sách sắp xếp theo `LastReadAt` giảm dần, không cần sắp xếp lại ở frontend.
        *   Trả về danh sách `List<LastReadMangaViewModel>`.
        *   Xử lý lỗi cẩn thận (API call errors, null results từ services).
    *   **Mã nguồn `ReadingHistoryService.cs`:**
        ```csharp
        using manga_reader_web.Services.AuthServices;
        using manga_reader_web.Services.MangaServices.ChapterServices;
        using manga_reader_web.Services.MangaServices.Models;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.Logging;
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Net.Http.Headers;
        using System.Text.Json;
        using System.Text.Json.Serialization;
        using System.Threading.Tasks;

        namespace manga_reader_web.Services.MangaServices
        {
            // Model để deserialize response từ backend /reading-history
            public class BackendHistoryItem
            {
                [JsonPropertyName("mangaId")]
                public string MangaId { get; set; }

                [JsonPropertyName("chapterId")] // Đảm bảo khớp với key JSON từ backend
                public string ChapterId { get; set; }

                [JsonPropertyName("lastReadAt")]
                public DateTime LastReadAt { get; set; }
            }

            public class ReadingHistoryService : IReadingHistoryService
            {
                private readonly IHttpClientFactory _httpClientFactory;
                private readonly IUserService _userService;
                private readonly IMangaInfoService _mangaInfoService;
                private readonly IChapterInfoService _chapterInfoService;
                private readonly IConfiguration _configuration;
                private readonly ILogger<ReadingHistoryService> _logger;
                private readonly TimeSpan _rateLimitDelay; // Delay giữa các nhóm API call

                public ReadingHistoryService(
                    IHttpClientFactory httpClientFactory,
                    IUserService userService,
                    IMangaInfoService mangaInfoService,
                    IChapterInfoService chapterInfoService,
                    IConfiguration configuration,
                    ILogger<ReadingHistoryService> logger)
                {
                    _httpClientFactory = httpClientFactory;
                    _userService = userService;
                    _mangaInfoService = mangaInfoService;
                    _chapterInfoService = chapterInfoService;
                    _configuration = configuration;
                    _logger = logger;
                    // Lấy giá trị delay từ config hoặc đặt mặc định (vd: 550ms)
                    _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 550));
                }

                public async Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync()
                {
                    var historyViewModels = new List<LastReadMangaViewModel>();

                    if (!_userService.IsAuthenticated())
                    {
                        _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy lịch sử đọc.");
                        return historyViewModels;
                    }

                    var token = _userService.GetToken();
                    if (string.IsNullOrEmpty(token))
                    {
                        _logger.LogError("Không thể lấy token người dùng đã đăng nhập.");
                        return historyViewModels;
                    }

                    try
                    {
                        var client = _httpClientFactory.CreateClient("BackendApiClient");
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        _logger.LogInformation("Đang gọi API backend /api/users/reading-history");
                        var response = await client.GetAsync("/api/users/reading-history");

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError($"Lỗi khi gọi API backend lấy lịch sử đọc. Status: {response.StatusCode}, Content: {errorContent}");
                            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                _userService.RemoveToken(); // Xóa token nếu backend báo Unauthorized
                            }
                            return historyViewModels; // Trả về rỗng nếu lỗi
                        }

                        var content = await response.Content.ReadAsStringAsync();
                        var backendHistory = JsonSerializer.Deserialize<List<BackendHistoryItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (backendHistory == null || !backendHistory.Any())
                        {
                            _logger.LogInformation("Không có lịch sử đọc nào từ backend.");
                            return historyViewModels;
                        }

                        _logger.LogInformation($"Nhận được {backendHistory.Count} mục lịch sử từ backend. Bắt đầu lấy chi tiết...");

                        foreach (var item in backendHistory)
                        {
                            // Áp dụng rate limit trước khi gọi API cho mỗi item
                            await Task.Delay(_rateLimitDelay);

                            // Lấy thông tin Manga (Title, Cover)
                            var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                            if (mangaInfo == null)
                            {
                                _logger.LogWarning($"Không thể lấy thông tin cho MangaId: {item.MangaId}. Bỏ qua mục lịch sử này.");
                                continue; // Bỏ qua nếu không lấy được info manga
                            }

                            // Lấy thông tin Chapter (Title, PublishedAt)
                            var chapterInfo = await _chapterInfoService.GetChapterInfoAsync(item.ChapterId);
                            if (chapterInfo == null)
                            {
                                _logger.LogWarning($"Không thể lấy thông tin cho ChapterId: {item.ChapterId}. Bỏ qua mục lịch sử này.");
                                continue; // Bỏ qua nếu không lấy được info chapter
                            }

                            // Tạo ViewModel
                            historyViewModels.Add(new LastReadMangaViewModel
                            {
                                MangaId = item.MangaId,
                                MangaTitle = mangaInfo.MangaTitle,
                                CoverUrl = mangaInfo.CoverUrl,
                                ChapterId = item.ChapterId,
                                ChapterTitle = chapterInfo.Title,
                                ChapterPublishedAt = chapterInfo.PublishedAt,
                                LastReadAt = item.LastReadAt
                            });
                             _logger.LogDebug($"Đã xử lý xong mục lịch sử cho manga: {mangaInfo.MangaTitle}, chapter: {chapterInfo.Title}");
                        }

                        _logger.LogInformation($"Hoàn tất xử lý {historyViewModels.Count} mục lịch sử đọc.");
                        // Backend đã sắp xếp, không cần sắp xếp lại ở đây
                        return historyViewModels;

                    }
                    catch (JsonException jsonEx)
                    {
                         _logger.LogError(jsonEx, "Lỗi khi deserialize lịch sử đọc từ backend.");
                         return historyViewModels; // Trả về rỗng nếu lỗi deserialize
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi ngoại lệ khi lấy và xử lý lịch sử đọc.");
                        return historyViewModels; // Trả về rỗng nếu có lỗi khác
                    }
                }
            }
        }
        ```
3.  **Đăng ký Service trong `Program.cs`:**
    ```csharp
    // Thêm dòng này vào phần đăng ký services
    builder.Services.AddScoped<manga_reader_web.Services.MangaServices.IReadingHistoryService, manga_reader_web.Services.MangaServices.ReadingHistoryService>();
    ```

### 6. Frontend: Tạo Partial View cho Mục Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\Views\Shared\_ReadingHistoryItemPartial.cshtml`

**Công việc:**

1.  **Nhắc nhở:** Bạn có thể copy nội dung từ `_FollowedMangaItemPartial.cshtml` (đã tồn tại) làm mẫu.
2.  Đổi `@model` thành `manga_reader_web.Services.MangaServices.Models.LastReadMangaViewModel`.
3.  **Đổi tên các class CSS** để tránh xung đột với CSS của followed item (ví dụ: `custom-followed-manga-container` -> `custom-history-manga-container`, `followed-cover` -> `history-cover`, etc.).
4.  Cập nhật các thẻ để hiển thị đúng dữ liệu từ `Model`:
    *   Hiển thị `Model.MangaTitle`, `Model.CoverUrl`.
    *   Thay vì danh sách chapter mới nhất, hiển thị thông tin chapter đã đọc cuối cùng:
        *   Link đến chapter: `@Url.Action("Read", "Chapter", new { id = Model.ChapterId })`
        *   Tiêu đề chapter: `@Model.ChapterTitle`
        *   Ngày đăng chapter: `@Model.ChapterPublishedAt.ToString("dd/MM/yyyy")`
    *   Thêm phần hiển thị thời gian đọc lần cuối: `@Model.LastReadAt.ToString("dd/MM/yyyy HH:mm")`.
    *   **Mã nguồn gợi ý (dựa trên _FollowedMangaItemPartial):**
        ```html
        @model manga_reader_web.Services.MangaServices.Models.LastReadMangaViewModel

        <div class="custom-history-manga-container mb-4"> @* Đổi class *@
            <!-- Container ảnh bìa -->
            <div class="History-Image-Container"> @* Đổi class *@
                <a asp-action="Details" asp-controller="Manga" asp-route-id="@Model.MangaId"
                   hx-get="@Url.Action("Details", "Manga", new { id = Model.MangaId })"
                   hx-target="#main-content"
                   hx-push-url="true">
                    <img src="@(string.IsNullOrEmpty(Model.CoverUrl) ? "/images/cover-placeholder.jpg" : Model.CoverUrl)"
                         class="history-cover" @* Đổi class *@
                         alt="@Model.MangaTitle" loading="lazy"
                         onerror="this.onerror=null; this.src='/images/cover-placeholder.jpg';">
                </a>
            </div>

            <!-- Thông tin và chapter đã đọc -->
            <div class="custom-history-chapter-container"> @* Đổi class *@
                <!-- Tên manga -->
                <div class="mb-2">
                    <h5 class="history-title mb-0"> @* Đổi class *@
                        <a asp-action="Details" asp-controller="Manga" asp-route-id="@Model.MangaId"
                           hx-get="@Url.Action("Details", "Manga", new { id = Model.MangaId })"
                           hx-target="#main-content"
                           hx-push-url="true"
                           class="text-decoration-none">
                            @Model.MangaTitle
                        </a>
                    </h5>
                </div>

                <!-- Thông tin chapter đã đọc cuối -->
                <div class="last-read-chapter-info"> @* Class mới *@
                    <a asp-controller="Chapter" asp-action="Read"
                       asp-route-id="@Model.ChapterId"
                       class="custom-chapter-item chapter-link" @* Giữ lại class nếu style phù hợp *@
                       hx-get="@Url.Action("Read", "Chapter", new { id = Model.ChapterId })"
                       hx-target="#main-content"
                       hx-push-url="true">
                        <div class="custom-chapter-info">
                            <h6 class="mb-0 chapter-item-title">@Model.ChapterTitle</h6>
                            <small class="text-muted chapter-item-date">Đăng: @Model.ChapterPublishedAt.ToString("dd/MM/yyyy")</small>
                        </div>
                        <div class="custom-chapter-actions ms-auto">
                            <i class="bi bi-chevron-right"></i>
                        </div>
                    </a>
                    <div class="last-read-time text-muted small mt-1"> @* Class mới *@
                        <i class="bi bi-clock-history me-1"></i> Đọc lần cuối: @Model.LastReadAt.ToString("dd/MM/yyyy HH:mm")
                    </div>
                </div>
            </div>
        </div>
        ```

### 7. Frontend: Tạo CSS Mới cho Mục Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\wwwroot\css\pages\history\reading-history-item.css`

**Công việc:**

1.  Tạo thư mục `history` trong `wwwroot/css/pages` nếu chưa có.
2.  Tạo file `reading-history-item.css`.
3.  **Nhắc nhở:** Copy nội dung từ `wwwroot\css\pages\followed-item.css` (đã tồn tại).
4.  **Tìm và thay thế** các class CSS đã đổi tên ở Bước 6 (ví dụ: `.custom-followed-manga-container` thành `.custom-history-manga-container`, `.followed-cover` thành `.history-cover`, ...).
5.  Thêm style cho các phần tử mới (ví dụ: `.last-read-chapter-info`, `.last-read-time`). Điều chỉnh padding, margin, font-size nếu cần.
6.  **Import file CSS mới vào `main.css`:**
    **File cần sửa:** `manga_reader_web\wwwroot\css\main.css`
    **Mã nguồn cần thêm (vào cuối phần `PAGE-SPECIFIC STYLES`):**
    ```css
    /* ... các import khác ... */
    @import url("./pages/followed-item.css"); /* Style cho từng item truyện đang theo dõi */
    @import url("./pages/history/reading-history-item.css"); /* THÊM DÒNG NÀY: Style cho từng item lịch sử đọc */
    ```

### 8. Frontend: Tạo Controller Action và View cho Trang Lịch Sử

**File cần sửa:** `manga_reader_web\Controllers\MangaController.cs`
**File cần tạo:**
*   `manga_reader_web\Views\Manga\History.cshtml`
*   `manga_reader_web\Views\Shared\_ReadingHistoryListPartial.cshtml`

**Công việc:**

1.  **Trong `MangaController.cs`:**
    *   Inject `IReadingHistoryService`.
    *   Tạo Action `History()`:
        *   Kiểm tra xác thực (`_userService.IsAuthenticated()`). Nếu chưa đăng nhập, chuyển hướng đến trang Login (`RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("History", "Manga") })`).
        *   Trả về `View()`.
    *   Tạo Action `GetReadingHistoryPartial()`:
        *   Đánh dấu `[HttpGet]`.
        *   Kiểm tra xác thực. Nếu chưa đăng nhập, trả về `Unauthorized()` hoặc một PartialView thông báo lỗi/yêu cầu đăng nhập.
        *   Gọi `_readingHistoryService.GetReadingHistoryAsync()`.
        *   Trả về `PartialView("_ReadingHistoryListPartial", history)`.
    *   **Mã nguồn cần thêm/sửa trong `MangaController.cs`:**
        ```csharp
        // Thêm using nếu cần
        using manga_reader_web.Services.MangaServices; // Namespace của IReadingHistoryService

        public class MangaController : Controller
        {
            // ... các injections khác ...
            private readonly IReadingHistoryService _readingHistoryService; // Thêm injection

            public MangaController(
                // ... các tham số khác ...
                IReadingHistoryService readingHistoryService // Thêm vào constructor
                )
            {
                // ... gán các giá trị khác ...
                _readingHistoryService = readingHistoryService; // Gán giá trị
            }

            // ... các actions khác ...

            // GET: /Manga/History
            public IActionResult History()
            {
                // Kiểm tra xác thực trước khi hiển thị trang
                if (!_userService.IsAuthenticated())
                {
                    _logger.LogWarning("Người dùng chưa đăng nhập truy cập trang Lịch sử.");
                    // Chuyển hướng đến trang đăng nhập, lưu lại trang muốn quay về
                    return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("History", "Manga") });
                }

                _logger.LogInformation("Hiển thị trang Lịch sử đọc.");
                // Chỉ cần trả về View trống, HTMX sẽ load nội dung
                return View();
            }

            // GET: /Manga/GetReadingHistoryPartial
            [HttpGet]
            public async Task<IActionResult> GetReadingHistoryPartial()
            {
                _logger.LogInformation("Nhận yêu cầu lấy partial view Lịch sử đọc.");
                if (!_userService.IsAuthenticated())
                {
                    _logger.LogWarning("Yêu cầu lấy partial Lịch sử đọc nhưng chưa đăng nhập.");
                    // Trả về Unauthorized để HTMX có thể xử lý (ví dụ: chuyển hướng) hoặc trả về partial báo lỗi
                    // return Unauthorized();
                    return PartialView("_UnauthorizedPartial"); // Tạo partial này nếu muốn hiển thị thông báo
                }

                try
                {
                    var history = await _readingHistoryService.GetReadingHistoryAsync();
                    _logger.LogInformation($"Lấy được {history.Count} mục lịch sử đọc.");
                    return PartialView("_ReadingHistoryListPartial", history);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lấy dữ liệu lịch sử đọc cho partial view.");
                    // Trả về partial view báo lỗi
                    ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                    return PartialView("_ErrorPartial"); // Tạo partial này để hiển thị lỗi
                }
            }
        }
        ```
2.  **Tạo View `History.cshtml`:**
    *   Chứa container `div#reading-history-container` với các thuộc tính HTMX `hx-get`, `hx-trigger="load"`, `hx-swap="innerHTML"` để tải partial view.
    *   Thêm tiêu đề trang.
    *   **Mã nguồn `History.cshtml`:**
        ```html
        @{
            ViewData["Title"] = "Lịch sử đọc truyện";
        }

        <div class="container mt-4">
            <h1 class="mb-4"><i class="bi bi-clock-history me-2"></i>Lịch sử đọc truyện</h1>

            <div id="reading-history-container"
                 hx-get="@Url.Action("GetReadingHistoryPartial", "Manga")"
                 hx-trigger="load"
                 hx-swap="innerHTML"
                 class="htmx-indicator-parent"> @* Thêm class để chứa indicator *@

                @* Loading Indicator - Sẽ bị thay thế bởi nội dung partial *@
                <div class="text-center py-5 htmx-indicator"> @* Thêm class htmx-indicator *@
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Đang tải lịch sử...</span>
                    </div>
                    <p class="mt-2">Đang tải lịch sử đọc của bạn...</p>
                </div>
            </div>
        </div>
        ```
3.  **Tạo Partial View `_ReadingHistoryListPartial.cshtml`:**
    *   Model là `List<LastReadMangaViewModel>`.
    *   Hiển thị danh sách lịch sử đọc bằng cách lặp qua Model và render `_ReadingHistoryItemPartial` cho mỗi mục.
    *   Hiển thị thông báo nếu danh sách trống (`Model == null || !Model.Any()`).
    *   **Mã nguồn `_ReadingHistoryListPartial.cshtml`:**
        ```html
        @model List<manga_reader_web.Services.MangaServices.Models.LastReadMangaViewModel>

        @if (Model == null || !Model.Any())
        {
            <div class="text-center py-5">
                <i class="bi bi-journal-x" style="font-size: 3rem;"></i>
                <h5 class="mt-3">Lịch sử đọc của bạn trống</h5>
                <p class="text-muted">Hãy bắt đầu đọc truyện để lưu lại tiến trình của bạn!</p>
                <a asp-controller="Manga" asp-action="Search" class="btn btn-primary"
                   hx-get="@Url.Action("Search", "Manga")" hx-target="#main-content" hx-push-url="true">
                    <i class="bi bi-search me-1"></i> Tìm truyện
                </a>
            </div>
        }
        else
        {
            <div class="list-group reading-history-list"> @* Thêm class để style nếu cần *@
                @foreach (var item in Model)
                {
                    @Html.Partial("_ReadingHistoryItemPartial", item)
                }
            </div>
            @* Optional: Thêm nút "Xem thêm" hoặc phân trang nếu cần sau này *@
        }
        ```

### 9. Frontend: Thêm Link vào Sidebar

**File cần sửa:** `manga_reader_web\Views\Shared\_Layout.cshtml`

**Công việc:**

1.  Thêm một `<li>` và `<a>` mới vào `ul.navbar-nav` trong sidebar, dưới mục "Truyện đang theo dõi".
2.  Thẻ `<a>` trỏ đến `Manga/History` và sử dụng các thuộc tính HTMX (`hx-get`, `hx-target`, `hx-push-url`) để tải nội dung vào `#main-content`.
    *   **Mã nguồn cần thêm (vào trong `ul.navbar-nav`):**
        ```html
        @* ... các li khác ... *@
        <li class="nav-item mb-2">
            <a class="nav-link sidebar-nav-link p-2" asp-area="" asp-controller="Manga" asp-action="Followed"
               hx-get="@Url.Action("Followed", "Manga")" hx-target="#main-content" hx-push-url="true">
                <i class="bi bi-bookmark-heart me-2"></i>Truyện đang theo dõi
            </a>
        </li>
        @* THÊM LI MỚI DƯỚI ĐÂY *@
        <li class="nav-item mb-2">
            <a class="nav-link sidebar-nav-link p-2" asp-area="" asp-controller="Manga" asp-action="History"
               hx-get="@Url.Action("History", "Manga")" hx-target="#main-content" hx-push-url="true">
                <i class="bi bi-clock-history me-2"></i>Lịch sử đọc
            </a>
        </li>
        @* ... các li khác nếu có ... *@
        ```

### 10. Cập nhật `UserService` (Kiểm tra lại)

**File cần kiểm tra:** `manga_reader_web\Services\AuthServices\UserService.cs`

**Công việc:**

1.  **Kiểm tra:** `UserService` của bạn **đã** được cấu hình để sử dụng HttpOnly Cookie (`TOKEN_COOKIE_KEY`).
2.  **Xác nhận:**
    *   `GetToken()`: Đọc giá trị token từ `HttpContext.Request.Cookies[TOKEN_COOKIE_KEY]`. (Đã đúng)
    *   `IsAuthenticated()`: Kiểm tra sự tồn tại của token bằng cách gọi `GetToken()`. (Đã đúng)
    *   `SaveToken()`: Ghi token vào cookie với `HttpOnly = true`, `Secure = true`, `SameSite = SameSiteMode.Lax`, và `Expires`. (Đã đúng)
    *   `RemoveToken()`: Xóa cookie bằng cách ghi đè với giá trị rỗng và ngày hết hạn trong quá khứ. (Đã đúng)
3.  **Kết luận:** Bước này không yêu cầu thay đổi code trong `UserService.cs` vì nó đã hoạt động đúng với cơ chế Cookie.

### 11. Kiểm Tra và Hoàn Thiện

**Công việc:**

1.  Chạy ứng dụng (`dotnet run`).
2.  Đăng nhập vào tài khoản Google của bạn.
3.  Mở một truyện bất kỳ và đọc một vài chương.
4.  **Kiểm tra Network:** Mở Developer Tools (F12), chuyển sang tab Network. Khi bạn tải trang đọc chapter (`/Chapter/Read/...`), kiểm tra xem có request `POST` đến `/Chapter/SaveReadingProgress` được gửi đi không. Xem chi tiết request (Headers, Payload) và response (Status Code).
5.  **Kiểm tra Log:** Xem log của ứng dụng frontend (console output) và log của backend API (nếu có thể) để xác nhận việc lưu tiến độ thành công.
6.  **Truy cập Trang Lịch Sử:** Click vào link "Lịch sử đọc" trên sidebar.
7.  **Kiểm tra Hiển thị:**
    *   Trang Lịch sử có hiển thị đúng các truyện bạn vừa đọc không?
    *   Thông tin (Ảnh bìa, Tên truyện, Tên chương cuối, Ngày đăng chương, Thời gian đọc) có chính xác không?
    *   Thứ tự hiển thị có đúng là truyện/chương đọc gần nhất lên đầu không?
    *   CSS có hiển thị đúng và đẹp mắt không?
    *   Các link (đến trang chi tiết truyện, đến trang đọc chương) có hoạt động đúng không?
8.  **Kiểm tra HTMX:** Click vào các link trên trang Lịch sử (tên truyện, tên chương) và các link trên sidebar. Đảm bảo nội dung được tải vào `#main-content` mà không load lại toàn bộ trang. Kiểm tra URL trên thanh địa chỉ có cập nhật đúng không (`hx-push-url="true"`).
9.  **Kiểm tra Tốc độ:** Trang Lịch sử có tải nhanh không? Lưu ý rằng việc gọi API để lấy chi tiết cho từng mục lịch sử có thể làm chậm trang nếu có nhiều mục. Cân nhắc thêm cơ chế caching hoặc tải dần (lazy loading) nếu cần.
10. **Kiểm tra Responsive:** Thay đổi kích thước cửa sổ trình duyệt hoặc dùng chế độ mô phỏng thiết bị di động để đảm bảo trang Lịch sử hiển thị tốt trên các kích thước màn hình khác nhau.

Chúc bạn thực hiện thành công!