# TODO.md: Cập nhật Services/MangaServices để sử dụng API Services mới

## Mục tiêu

Refactor các service trong thư mục `Services/MangaServices` để:
1. Sử dụng các interface của API service mới (ví dụ: `IMangaApiService`, `IChapterApiService`) thay vì `MangaDexService` cũ hoặc các lời gọi API trực tiếp trả về `dynamic`.
2. Nhận dữ liệu dưới dạng các model C# strongly-typed từ `Models/Mangadex/`.
3. Thực hiện việc mapping dữ liệu từ các model mới (`Models/Mangadex/*`) sang các model cũ (`Models/MangaDexModels.cs`) mà UI đang sử dụng.

## Các bước chung cho mỗi Service

1.  **Inject Dependencies:** Đảm bảo các interface của API service mới cần thiết (ví dụ: `IMangaApiService`, `IChapterApiService`, `ICoverApiService`) được inject vào constructor của service đang được refactor.
2.  **Thay thế lời gọi API:** Tìm các phương thức đang gọi service cũ hoặc API trả về `dynamic`. Thay thế chúng bằng lời gọi đến các phương thức tương ứng của các interface API service mới.
3.  **Mapping Dữ liệu:** Xử lý kết quả trả về (là các model `Models/Mangadex/*`). Trích xuất dữ liệu cần thiết và tạo các đối tượng model cũ (`Models/MangaDexModels.cs`) để trả về cho Controller hoặc service gọi nó.
4.  **Sử dụng Helper Services:** Tận dụng các service tiện ích hiện có (trong `MangaInformation` và `UtilityServices`) để xử lý việc lấy tiêu đề, tags, description, status,... từ các model mới. Có thể cần điều chỉnh các helper service này một chút để nhận model mới làm tham số.

---

## Chi tiết cho từng Service

### 1. `Services/MangaServices/ChapterServices/ChapterAttributeService.cs`

*   **Hiện trạng:** Có vẻ đang sử dụng `HttpClient` trực tiếp hoặc `MangaDexService` cũ để gọi `/chapter/{id}`.
*   **Cần thay đổi:**
    *   Inject `IChapterApiService`.
    *   Trong `FetchChapterDataAsync`:
        *   Thay thế lời gọi `_httpClient.GetAsync` hoặc service cũ bằng `_chapterApiService.FetchChapterInfoAsync(chapterId)`.
        *   Xử lý kết quả `ChapterResponse?`. Nếu `Result == "ok"` và `Data` không null, trích xuất `Data.Attributes`.
        *   **Mapping:** Chuyển đổi `ChapterAttributes?` thành `Dictionary<string, object>` (hoặc trực tiếp sử dụng các thuộc tính).
    *   Các phương thức `GetChapterNumberAsync`, `GetChapterTitleAsync`, `GetPublishedAtAsync` sẽ sử dụng kết quả đã xử lý từ `FetchChapterDataAsync`.

```csharp
// File: Services/MangaServices/ChapterServices/ChapterAttributeService.cs

using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterAttributeService
    {
        private readonly IChapterApiService _chapterApiService; // Thay thế HttpClient/MangaDexService cũ
        private readonly JsonConversionService _jsonConversionService; // Giữ lại nếu cần convert sang Dict
        private readonly ILogger<ChapterAttributeService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChapterAttributeService(
            IChapterApiService chapterApiService, // Inject service mới
            JsonConversionService jsonConversionService,
            ILogger<ChapterAttributeService> logger)
        {
            _chapterApiService = chapterApiService;
            _jsonConversionService = jsonConversionService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task<ChapterAttributes?> FetchChapterAttributesAsync(string chapterId) // Đổi kiểu trả về
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi FetchChapterAttributesAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy thông tin Attributes cho Chapter: {chapterId}");

            // Gọi API service mới
            var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

            if (chapterResponse?.Result != "ok" || chapterResponse.Data?.Attributes == null)
            {
                _logger.LogError($"Không lấy được thông tin hoặc attributes cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                // Có thể throw exception hoặc trả về null tùy logic xử lý lỗi mong muốn
                return null;
            }

            return chapterResponse.Data.Attributes;
        }

        public async Task<string> GetChapterNumberAsync(string chapterId)
        {
            try
            {
                var attributes = await FetchChapterAttributesAsync(chapterId);
                if (attributes == null) return "?";

                string chapterNumber = attributes.ChapterNumber ?? "?"; // Truy cập trực tiếp thuộc tính

                _logger.LogInformation($"Đã lấy được số chapter: {chapterNumber} cho Chapter: {chapterId}");
                return chapterNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy số chapter cho {chapterId}");
                return "?";
            }
        }

        public async Task<string> GetChapterTitleAsync(string chapterId)
        {
            try
            {
                var attributes = await FetchChapterAttributesAsync(chapterId);
                 if (attributes == null) return "";

                string chapterTitle = attributes.Title ?? ""; // Truy cập trực tiếp thuộc tính

                _logger.LogInformation($"Đã lấy được tiêu đề chapter: '{chapterTitle}' cho Chapter: {chapterId}");
                return chapterTitle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy tiêu đề chapter cho {chapterId}");
                return "";
            }
        }

        public async Task<DateTime> GetPublishedAtAsync(string chapterId)
        {
             try
            {
                var attributes = await FetchChapterAttributesAsync(chapterId);
                if (attributes == null) return DateTime.MinValue;

                DateTime publishedAt = attributes.PublishAt.DateTime; // Lấy DateTime từ DateTimeOffset

                _logger.LogInformation($"Đã lấy được ngày xuất bản: {publishedAt} cho Chapter: {chapterId}");
                return publishedAt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy ngày xuất bản cho {chapterId}");
                return DateTime.MinValue;
            }
        }

        // CreateDisplayTitle không thay đổi
        public string CreateDisplayTitle(string chapterNumber, string chapterTitle)
        {
            if (string.IsNullOrEmpty(chapterNumber) || chapterNumber == "?")
            {
                return !string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot";
            }

            string displayTitle = $"Chương {chapterNumber}";
            if (!string.IsNullOrEmpty(chapterTitle) && chapterTitle != chapterNumber)
            {
                displayTitle += $": {chapterTitle}";
            }

            return displayTitle;
        }
    }
}
```

### 2. `Services/MangaServices/ChapterServices/ChapterInfoService.cs`

*   **Hiện trạng:** Đang sử dụng `ChapterAttributeService`.
*   **Cần thay đổi:** Không cần thay đổi lớn vì `ChapterAttributeService` đã được cập nhật. Đảm bảo `ChapterAttributeService` trả về đúng dữ liệu.

```csharp
// File: Services/MangaServices/ChapterServices/ChapterInfoService.cs
// Không cần thay đổi logic chính nếu ChapterAttributeService đã được cập nhật đúng.
// Chỉ cần đảm bảo ChapterAttributeService hoạt động chính xác với API service mới.
using MangaReader.WebUI.Services.MangaServices.Models;
using MangaReader.WebUI.Services.UtilityServices; // Giả sử cần nếu có xử lý JSON
using MangaReader.WebUI.Services.APIServices.Interfaces; // Có thể không cần trực tiếp ở đây

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterInfoService : IChapterInfoService
    {
        // Dependencies không đổi
        private readonly ChapterAttributeService _chapterAttributeService;
        private readonly ILogger<ChapterInfoService> _logger;

        public ChapterInfoService(
            ChapterAttributeService chapterAttributeService, // Đảm bảo instance này đã được cập nhật
            ILogger<ChapterInfoService> logger)
        {
            _chapterAttributeService = chapterAttributeService;
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

                // Sử dụng ChapterAttributeService đã được cập nhật
                string chapterNumber = await _chapterAttributeService.GetChapterNumberAsync(chapterId);
                string chapterTitle = await _chapterAttributeService.GetChapterTitleAsync(chapterId);
                DateTime publishedAt = await _chapterAttributeService.GetPublishedAtAsync(chapterId);

                // Tạo tiêu đề hiển thị sử dụng phương thức từ ChapterAttributeService
                string displayTitle = _chapterAttributeService.CreateDisplayTitle(chapterNumber, chapterTitle);

                return new ChapterInfo
                {
                    Id = chapterId,
                    Title = displayTitle, // Sử dụng tiêu đề đã format
                    PublishedAt = publishedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin chi tiết cho chapter ID: {chapterId}: {ex.Message}");
                return null; // Trả về null nếu có lỗi
            }
        }
    }
}
```

### 3. `Services/MangaServices/ChapterServices/ChapterLanguageServices.cs`

*   **Hiện trạng:** Đang sử dụng `HttpClient` trực tiếp.
*   **Cần thay đổi:**
    *   Inject `IChapterApiService`.
    *   Trong `GetChapterLanguageAsync`:
        *   Thay thế lời gọi `_httpClient.GetAsync` bằng `_chapterApiService.FetchChapterInfoAsync(chapterId)`.
        *   Xử lý kết quả `ChapterResponse?`. Nếu thành công, lấy `Data.Attributes.TranslatedLanguage`.

```csharp
// File: Services/MangaServices/ChapterServices/ChapterLanguageServices.cs

using MangaReader.WebUI.Services.APIServices.Interfaces; // Thêm using
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterLanguageServices
    {
        private readonly IChapterApiService _chapterApiService; // Thay thế HttpClient
        private readonly ILogger<ChapterLanguageServices> _logger;
        // _baseUrl và _jsonOptions không cần thiết nữa

        public ChapterLanguageServices(
            IChapterApiService chapterApiService, // Inject service mới
            ILogger<ChapterLanguageServices> logger)
        {
            _chapterApiService = chapterApiService;
            _logger = logger;
        }

        public async Task<string> GetChapterLanguageAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi GetChapterLanguageAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy thông tin ngôn ngữ cho Chapter: {chapterId}");

            try
            {
                // Gọi API service mới
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

                if (chapterResponse?.Result == "ok" && chapterResponse.Data?.Attributes?.TranslatedLanguage != null)
                {
                    string language = chapterResponse.Data.Attributes.TranslatedLanguage;
                    _logger.LogInformation($"Đã lấy được ngôn ngữ: {language} cho Chapter: {chapterId}");
                    return language;
                }
                else
                {
                    _logger.LogError($"Không lấy được thông tin ngôn ngữ cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                    // Có thể throw exception hoặc trả về giá trị mặc định/lỗi
                    throw new InvalidOperationException($"Không thể lấy ngôn ngữ cho chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, $"Lỗi khi lấy ngôn ngữ cho chapter {chapterId}");
                 throw; // Ném lại lỗi để lớp gọi xử lý
            }
        }
    }
}
```

### 4. `Services/MangaServices/ChapterServices/ChapterReadingServices.cs`

*   **Hiện trạng:** Đang sử dụng `MangaDexService` hoặc `HttpClient` trực tiếp cho một số tác vụ, và các service chapter khác.
*   **Cần thay đổi:**
    *   Đảm bảo `IChapterApiService`, `IMangaApiService` được inject (nếu cần lấy title manga).
    *   Trong `GetChapterReadViewModel`:
        *   Gọi `_chapterApiService.FetchChapterPagesAsync(id)` để lấy thông tin trang.
            *   **Mapping:** Xử lý `AtHomeServerResponse?`. Nếu thành công, tạo danh sách URL đầy đủ từ `BaseUrl`, `Hash`, và `Data` (hoặc `DataSaver`). URL cần đi qua proxy ảnh của backend.
        *   Gọi `_mangaIdService.GetMangaIdFromChapterAsync(id)` (service này cần được cập nhật để dùng `IChapterApiService`).
        *   Gọi `_mangaTitleService.GetMangaTitleFromIdAsync(mangaId)` (service này cần được cập nhật để dùng `IMangaApiService`).
        *   Gọi `_chapterService.GetChaptersAsync(mangaId, currentChapterLanguage)` (service này cần được cập nhật để dùng `IChapterApiService`).
        *   **Mapping:** Kết hợp dữ liệu đã lấy để tạo `ChapterReadViewModel`.

```csharp
// File: Services/MangaServices/ChapterServices/ChapterReadingServices.cs

using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterReadingServices
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly MangaIdService _mangaIdService; // Đảm bảo đã cập nhật
        private readonly ChapterLanguageServices _chapterLanguageServices; // Đảm bảo đã cập nhật
        private readonly MangaTitleService _mangaTitleService; // Đảm bảo đã cập nhật
        private readonly ChapterService _chapterService; // Đảm bảo đã cập nhật
        private readonly ILogger<ChapterReadingServices> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _backendBaseUrl; // Lấy base URL của backend

        public ChapterReadingServices(
            IChapterApiService chapterApiService,
            MangaIdService mangaIdService,
            ChapterLanguageServices chapterLanguageServices,
            MangaTitleService mangaTitleService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration, // Inject IConfiguration
            ILogger<ChapterReadingServices> logger)
        {
            _chapterApiService = chapterApiService;
            _mangaIdService = mangaIdService;
            _chapterLanguageServices = chapterLanguageServices;
            _mangaTitleService = mangaTitleService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }

        public async Task<ChapterReadViewModel> GetChapterReadViewModel(string chapterId)
        {
            try
            {
                _logger.LogInformation($"Đang tải chapter {chapterId}");

                // 1. Tải thông tin server ảnh và tên file ảnh
                var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
                if (atHomeResponse == null || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null)
                {
                    _logger.LogError($"Không thể lấy thông tin trang ảnh cho chapter {chapterId}");
                    throw new Exception("Không thể tải trang ảnh cho chapter này.");
                }

                // 2. Mapping: Tạo danh sách URL ảnh đầy đủ qua proxy
                var pages = atHomeResponse.Chapter.Data
                    .Select(pageFile => $"{_backendBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"{atHomeResponse.BaseUrl}/data/{atHomeResponse.Chapter.Hash}/{pageFile}")}")
                    .ToList();
                _logger.LogInformation($"Đã tạo {pages.Count} URL ảnh cho chapter {chapterId}");

                // 3. Lấy mangaId (MangaIdService đã được cập nhật)
                string mangaId = await _mangaIdService.GetMangaIdFromChapterAsync(chapterId);
                _logger.LogInformation($"Đã xác định được mangaId: {mangaId} cho chapter {chapterId}");

                // 4. Lấy ngôn ngữ chapter hiện tại (ChapterLanguageServices đã được cập nhật)
                string currentChapterLanguage = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);
                 _logger.LogInformation($"Đã lấy được ngôn ngữ {currentChapterLanguage} từ API");

                 // 5. Lấy tiêu đề manga (MangaTitleService đã được cập nhật)
                string mangaTitle = await GetMangaTitleAsync(mangaId); // Sử dụng hàm helper bên dưới

                // 6. Lấy danh sách chapters (ChapterService đã được cập nhật)
                var chaptersList = await GetChaptersAsync(mangaId, currentChapterLanguage);

                // 7. Tìm chapter hiện tại và các chapter liền kề
                var (currentChapterViewModel, prevChapterId, nextChapterId) =
                    FindCurrentAndAdjacentChapters(chaptersList, chapterId, currentChapterLanguage);

                 // 8. Tạo view model
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = chapterId,
                    ChapterTitle = currentChapterViewModel?.Title ?? "Không xác định", // Xử lý null
                    ChapterNumber = currentChapterViewModel?.Number ?? "?", // Xử lý null
                    ChapterLanguage = currentChapterLanguage,
                    Pages = pages, // Danh sách URL đã xử lý
                    PrevChapterId = prevChapterId,
                    NextChapterId = nextChapterId,
                    SiblingChapters = chaptersList // Danh sách ViewModel đã xử lý
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải chapter {chapterId}");
                // Xem xét trả về một ViewModel lỗi hoặc ném lại exception
                throw;
            }
        }

        // --- Các hàm helper giữ nguyên logic session nhưng gọi service đã cập nhật ---

        public async Task<string> GetMangaTitleAsync(string mangaId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string sessionTitle = httpContext?.Session.GetString($"Manga_{mangaId}_Title");
            if (!string.IsNullOrEmpty(sessionTitle))
            {
                _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId} từ session: {sessionTitle}");
                return sessionTitle;
            }
            return await GetMangaTitleFromApiAsync(mangaId); // Gọi hàm lấy từ API (đã dùng service mới)
        }

        private async Task<string> GetMangaTitleFromApiAsync(string mangaId)
        {
            _logger.LogInformation($"Tiến hành lấy tiêu đề manga {mangaId} từ API...");
            string mangaTitle = await _mangaTitleService.GetMangaTitleFromIdAsync(mangaId); // MangaTitleService đã cập nhật
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && !string.IsNullOrEmpty(mangaTitle) && mangaTitle != "Không có tiêu đề")
            {
                httpContext.Session.SetString($"Manga_{mangaId}_Title", mangaTitle);
                _logger.LogInformation($"Đã lưu tiêu đề manga {mangaId} vào session: {mangaTitle}");
            }
            return mangaTitle;
        }

        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string language)
        {
             var httpContext = _httpContextAccessor.HttpContext;
             var sessionChaptersJson = httpContext?.Session.GetString($"Manga_{mangaId}_Chapters_{language}");

             if (!string.IsNullOrEmpty(sessionChaptersJson))
             {
                 try
                 {
                     var chaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(sessionChaptersJson);
                     if (chaptersList != null && chaptersList.Any())
                     {
                         _logger.LogInformation($"Đã lấy {chaptersList.Count} chapters ngôn ngữ {language} từ session");
                         return chaptersList;
                     }
                 }
                 catch (JsonException ex)
                 {
                      _logger.LogWarning(ex, $"Lỗi deserialize chapters từ session cho manga {mangaId}, ngôn ngữ {language}. Sẽ lấy lại từ API.");
                 }
             }
             return await GetChaptersFromApiAsync(mangaId, language); // Gọi hàm lấy từ API (đã dùng service mới)
        }

        private async Task<List<ChapterViewModel>> GetChaptersFromApiAsync(string mangaId, string language)
        {
            _logger.LogInformation($"Tiến hành lấy danh sách chapters cho manga {mangaId} với ngôn ngữ {language}");
            var allChapters = await _chapterService.GetChaptersAsync(mangaId, language); // ChapterService đã cập nhật
            _logger.LogInformation($"Đã lấy {allChapters.Count} chapters từ API");

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                 // Lưu danh sách tất cả chapters vào session (nếu cần)
                 // httpContext.Session.SetString($"Manga_{mangaId}_AllChapters", JsonSerializer.Serialize(allChapters));

                 // Phân loại và lưu theo ngôn ngữ
                 var chaptersByLanguage = _chapterService.GetChaptersByLanguage(allChapters);
                 if (chaptersByLanguage.TryGetValue(language, out var chaptersInLanguage))
                 {
                     httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                     _logger.LogInformation($"Đã lưu {chaptersInLanguage.Count} chapters ngôn ngữ {language} vào session");
                     return chaptersInLanguage;
                 }
            }
            return new List<ChapterViewModel>(); // Trả về rỗng nếu không tìm thấy ngôn ngữ
        }

        // FindCurrentAndAdjacentChapters không thay đổi logic
        private (ChapterViewModel currentChapter, string prevId, string nextId) FindCurrentAndAdjacentChapters(
            List<ChapterViewModel> chapters, string chapterId, string language)
        {
            // ... (Giữ nguyên logic tìm kiếm và sắp xếp) ...
             _logger.LogInformation($"Xác định chapter hiện tại và các chapter liền kề trong danh sách {chapters.Count} chapters");

            var currentChapter = chapters.FirstOrDefault(c => c.Id == chapterId);

            if (currentChapter == null)
            {
                _logger.LogWarning($"Không tìm thấy chapter {chapterId} trong danh sách chapters ngôn ngữ {language}");
                // Trả về ViewModel mặc định nếu không tìm thấy chapter hiện tại
                return (new ChapterViewModel { Id = chapterId, Title = "Chương không xác định", Number = "?", Language = language }, null, null);
            }

            // Sắp xếp danh sách chapters theo số chương tăng dần để xác định chương trước/sau
            // Cần xử lý trường hợp chapterNumber là null hoặc không phải số
            var sortedChapters = chapters
                .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                .OrderBy(c => c.Number ?? double.MaxValue) // Đẩy null/không phải số về cuối
                .Select(c => c.Chapter)
                .ToList();


            int index = sortedChapters.FindIndex(c => c.Id == chapterId);

            string prevId = (index > 0) ? sortedChapters[index - 1].Id : null;
            string nextId = (index >= 0 && index < sortedChapters.Count - 1) ? sortedChapters[index + 1].Id : null;

            _logger.LogInformation($"Chapter hiện tại: {currentChapter.Title}, Chapter trước: {(prevId != null ? "có" : "không có")}, Chapter sau: {(nextId != null ? "có" : "không có")}");

            return (currentChapter, prevId, nextId);
        }

         // Helper để parse số chapter, trả về null nếu không parse được
        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
```

### 5. `Services/MangaServices/ChapterServices/ChapterService.cs`

*   **Hiện trạng:** Đang sử dụng `MangaDexService` hoặc `HttpClient` trực tiếp.
*   **Cần thay đổi:**
    *   Inject `IChapterApiService`.
    *   Trong `GetChaptersAsync`:
        *   Thay thế lời gọi service cũ bằng `_chapterApiService.FetchChaptersAsync(mangaId, languages)`.
        *   Xử lý kết quả `ChapterList?`. Lặp qua `Data` (là `List<Chapter>`).
    *   Trong `ProcessChapter`:
        *   Nhận tham số là `Models.Mangadex.Chapter` thay vì `Dictionary<string, object>`.
        *   Truy cập trực tiếp các thuộc tính của `chapter.Attributes` và `chapter.Relationships`.
        *   **Mapping:** Tạo `ChapterViewModel` từ dữ liệu `Chapter`.
    *   Trong `GetChapterById`:
        *   Gọi `_chapterApiService.FetchChapterInfoAsync(chapterId)`.
        *   **Mapping:** Chuyển đổi `ChapterResponse?.Data` thành `ChapterViewModel` bằng `ProcessChapter`.
    *   Trong `GetChapterPages`:
        *   Gọi `_chapterApiService.FetchChapterPagesAsync(chapterId)`.
        *   **Mapping:** Xử lý `AtHomeServerResponse?` để tạo danh sách URL ảnh đầy đủ (qua proxy).
    *   Trong `GetLatestChaptersAsync`:
        *   Gọi `_chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: limit)`.
        *   **Mapping:** Lặp qua `ChapterList?.Data` và tạo `SimpleChapterInfo`.

```csharp
// File: Services/MangaServices/ChapterServices/ChapterService.cs

using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.MangaServices.Models;
using MangaReader.WebUI.Services.UtilityServices; // Vẫn cần nếu dùng JsonConversionService
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly IChapterApiService _chapterApiService; // Thay thế MangaDexService/HttpClient
        private readonly JsonConversionService _jsonConversionService; // Có thể không cần nữa nếu ProcessChapter nhận model
        private readonly ILogger<ChapterService> _logger;
        private readonly string _backendBaseUrl; // Lấy base URL của backend

        public ChapterService(
            IChapterApiService chapterApiService, // Inject service mới
            JsonConversionService jsonConversionService,
            IConfiguration configuration, // Inject IConfiguration
            ILogger<ChapterService> logger)
        {
            _chapterApiService = chapterApiService;
            _jsonConversionService = jsonConversionService;
            _logger = logger;
             _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }

        public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string languages = "vi,en")
        {
            try
            {
                // Gọi API service mới, yêu cầu lấy hết chapters (maxChapters = null)
                var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: null);
                var chapterViewModels = new List<ChapterViewModel>();

                if (chapterListResponse?.Data != null)
                {
                    foreach (var chapter in chapterListResponse.Data) // Lặp qua List<Chapter>
                    {
                        try
                        {
                            // Xử lý chapter thành ChapterViewModel
                            var chapterViewModel = ProcessChapter(chapter); // Truyền thẳng model Chapter
                            if (chapterViewModel != null)
                            {
                                chapterViewModels.Add(chapterViewModel);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapter?.Id}");
                            continue; // Bỏ qua chapter này và tiếp tục
                        }
                    }
                }
                else
                {
                     _logger.LogWarning($"Không có dữ liệu chapter trả về cho manga {mangaId} với ngôn ngữ {languages}.");
                }

                // Sắp xếp chapters theo thứ tự giảm dần
                return SortChaptersByNumberDescending(chapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}");
                return new List<ChapterViewModel>();
            }
        }

        // Thay đổi tham số thành model Chapter
        private ChapterViewModel ProcessChapter(Chapter chapter)
        {
            try
            {
                if (chapter?.Attributes == null)
                {
                    _logger.LogWarning($"Chapter {chapter?.Id} không có attributes, bỏ qua");
                    return null;
                }

                var attributes = chapter.Attributes;

                // Lấy thông tin hiển thị (số chương, tiêu đề)
                var (displayTitle, chapterNumber) = GetChapterDisplayInfo(attributes);

                // Lấy các thông tin khác
                var language = attributes.TranslatedLanguage ?? "unknown";
                var publishedAt = attributes.PublishAt.DateTime; // Lấy DateTime từ DateTimeOffset

                // Xử lý relationships
                var relationships = ProcessChapterRelationships(chapter.Relationships);

                return new ChapterViewModel
                {
                    Id = chapter.Id.ToString(),
                    Title = displayTitle,
                    Number = chapterNumber,
                    Language = language,
                    PublishedAt = publishedAt,
                    Relationships = relationships
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapter?.Id}");
                return null;
            }
        }

        // Thay đổi tham số thành ChapterAttributes
        private (string displayTitle, string chapterNumber) GetChapterDisplayInfo(ChapterAttributes attributes)
        {
            string chapterNumber = attributes.ChapterNumber; // Có thể null
            string chapterTitle = attributes.Title; // Có thể null

            if (string.IsNullOrEmpty(chapterNumber))
            {
                 return (!string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot", chapterNumber);
            }

            var displayTitle = string.IsNullOrEmpty(chapterTitle) || chapterTitle == chapterNumber
                ? $"Chương {chapterNumber}"
                : $"Chương {chapterNumber}: {chapterTitle}";

            return (displayTitle, chapterNumber);
        }

        // Thay đổi tham số thành List<Relationship>
        private List<ChapterRelationship> ProcessChapterRelationships(List<Relationship>? relationships)
        {
            var result = new List<ChapterRelationship>();
            if (relationships == null) return result;

            foreach (var relationship in relationships)
            {
                if (relationship != null && !string.IsNullOrEmpty(relationship.Type))
                {
                    result.Add(new ChapterRelationship
                    {
                        Id = relationship.Id.ToString(),
                        Type = relationship.Type
                    });
                }
            }
            return result;
        }

        // SortChaptersByNumberDescending và SortChaptersByNumberAscending không đổi

        // GetChaptersByLanguage không đổi logic, chỉ cần đảm bảo list đầu vào đúng

        public async Task<ChapterViewModel> GetChapterById(string chapterId)
        {
            try
            {
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);
                if (chapterResponse?.Result != "ok" || chapterResponse.Data == null)
                {
                    _logger.LogWarning($"Không tìm thấy chapter với ID: {chapterId} hoặc API lỗi.");
                    return null;
                }

                var chapterViewModel = ProcessChapter(chapterResponse.Data); // Xử lý model Chapter
                return chapterViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin chapter {chapterId}");
                return null;
            }
        }

        public async Task<List<string>> GetChapterPages(string chapterId)
        {
            try
            {
                var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
                if (atHomeResponse == null || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null)
                {
                    _logger.LogWarning($"Không thể lấy thông tin trang ảnh cho chapter {chapterId}");
                    return new List<string>();
                }

                 // Mapping: Tạo danh sách URL ảnh đầy đủ qua proxy
                var pages = atHomeResponse.Chapter.Data
                    .Select(pageFile => $"{_backendBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"{atHomeResponse.BaseUrl}/data/{atHomeResponse.Chapter.Hash}/{pageFile}")}")
                    .ToList();

                return pages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy trang chapter {chapterId}");
                return new List<string>();
            }
        }

        public async Task<List<SimpleChapterInfo>> GetLatestChaptersAsync(string mangaId, int limit, string languages = "vi,en")
        {
            try
            {
                // Gọi API service mới
                var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: limit);
                var simpleChapters = new List<SimpleChapterInfo>();

                if (chapterListResponse?.Data != null)
                {
                    foreach (var chapter in chapterListResponse.Data)
                    {
                        try
                        {
                             if (chapter?.Attributes == null) continue;

                            var attributes = chapter.Attributes;
                            var (displayTitle, _) = GetChapterDisplayInfo(attributes); // Chỉ cần displayTitle
                            var publishedAt = attributes.PublishAt.DateTime;

                            simpleChapters.Add(new SimpleChapterInfo
                            {
                                ChapterId = chapter.Id.ToString(),
                                DisplayTitle = displayTitle,
                                PublishedAt = publishedAt
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapter?.Id} trong GetLatestChaptersAsync");
                            continue;
                        }
                    }
                }

                // API service đã giới hạn số lượng, không cần Take(limit) ở đây nữa
                // Sắp xếp theo ngày xuất bản giảm dần (API service nên làm điều này, nhưng kiểm tra lại)
                return simpleChapters
                    .OrderByDescending(c => c.PublishedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chapters mới nhất cho manga {mangaId}");
                return new List<SimpleChapterInfo>();
            }
        }

        // --- Các hàm helper còn lại (Sort, GetChaptersByLanguage) giữ nguyên ---
         private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
        {
            return chapters
                .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                .OrderByDescending(c => c.Number ?? double.MinValue) // Đẩy null/không phải số về đầu khi giảm dần
                .Select(c => c.Chapter)
                .ToList();
        }

        private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
        {
             return chapters
                .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                .OrderBy(c => c.Number ?? double.MaxValue) // Đẩy null/không phải số về cuối khi tăng dần
                .Select(c => c.Chapter)
                .ToList();
        }

        public Dictionary<string, List<ChapterViewModel>> GetChaptersByLanguage(List<ChapterViewModel> chapters)
        {
            var chaptersByLanguage = chapters.GroupBy(c => c.Language)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var language in chaptersByLanguage.Keys)
            {
                chaptersByLanguage[language] = SortChaptersByNumberAscending(chaptersByLanguage[language]);
            }

            return chaptersByLanguage;
        }

         private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
```

### 6. `Services/MangaServices/ChapterServices/MangaIdService.cs`

*   **Hiện trạng:** Đang sử dụng `HttpClient` trực tiếp.
*   **Cần thay đổi:**
    *   Inject `IChapterApiService`.
    *   Trong `GetMangaIdFromChapterAsync`:
        *   Thay thế lời gọi `_httpClient.GetAsync` bằng `_chapterApiService.FetchChapterInfoAsync(chapterId)`.
        *   Xử lý kết quả `ChapterResponse?`. Nếu thành công, tìm relationship có `Type == "manga"` trong `Data.Relationships` và lấy `Id`.

```csharp
// File: Services/MangaServices/ChapterServices/MangaIdService.cs

using MangaReader.WebUI.Services.APIServices.Interfaces; // Thêm using
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class MangaIdService
    {
        private readonly IChapterApiService _chapterApiService; // Thay thế HttpClient
        private readonly ILogger<MangaIdService> _logger;
        // _baseUrl và _jsonOptions không cần thiết nữa

        public MangaIdService(
            IChapterApiService chapterApiService, // Inject service mới
            ILogger<MangaIdService> logger)
        {
            _chapterApiService = chapterApiService;
            _logger = logger;
        }

        public async Task<string> GetMangaIdFromChapterAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi GetMangaIdFromChapterAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy MangaID cho Chapter: {chapterId}");

            try
            {
                // Gọi API service mới
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

                if (chapterResponse?.Result == "ok" && chapterResponse.Data?.Relationships != null)
                {
                    // Tìm relationship là manga
                    var mangaRelationship = chapterResponse.Data.Relationships
                                                .FirstOrDefault(r => r.Type == "manga");

                    if (mangaRelationship != null)
                    {
                        string mangaId = mangaRelationship.Id.ToString();
                        _logger.LogInformation($"Đã tìm thấy MangaID: {mangaId} cho Chapter: {chapterId}");
                        return mangaId;
                    }
                    else
                    {
                        _logger.LogWarning($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                        throw new KeyNotFoundException($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                    }
                }
                else
                {
                    _logger.LogError($"Không lấy được thông tin hoặc relationships cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                    throw new InvalidOperationException($"Không thể lấy thông tin relationships cho chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy MangaID cho chapter {chapterId}");
                throw; // Ném lại lỗi
            }
        }
    }
}
```

### 7. `Services/MangaServices/MangaInformation/MangaDescription.cs`

*   **Hiện trạng:** Đang nhận `Dictionary<string, object>` làm tham số.
*   **Cần thay đổi:**
    *   Thay đổi phương thức `GetDescription` để nhận `MangaAttributes?` làm tham số.
    *   Truy cập trực tiếp thuộc tính `Description` (là `Dictionary<string, string>?`).
    *   Sử dụng `LocalizationService` hoặc logic tương tự để lấy mô tả ưu tiên ("vi", "en").

```csharp
// File: Services/MangaServices/MangaInformation/MangaDescription.cs

using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaDescription
    {
        private readonly ILogger<MangaDescription> _logger;
        private readonly LocalizationService _localizationService; // Giữ lại nếu cần

        public MangaDescription(
            ILogger<MangaDescription> logger,
            LocalizationService localizationService)
        {
            _logger = logger;
            _localizationService = localizationService;
        }

        // Thay đổi tham số thành MangaAttributes?
        public string GetDescription(MangaAttributes? attributes)
        {
            if (attributes?.Description == null || !attributes.Description.Any())
            {
                return "";
            }

            try
            {
                // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                if (attributes.Description.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
                {
                    return viDesc;
                }
                if (attributes.Description.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
                {
                    return enDesc;
                }

                // Lấy giá trị đầu tiên nếu không có vi/en
                return attributes.Description.FirstOrDefault().Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý description manga từ MangaAttributes.");
                return "";
            }
        }

        // Có thể giữ lại phương thức cũ để tương thích tạm thời hoặc xóa đi
        // public string GetDescription(Dictionary<string, object> attributesDict) { ... }
    }
}
```

### 8. `Services/MangaServices/MangaInformation/MangaRelationshipService.cs`

*   **Hiện trạng:** Đang nhận `Dictionary<string, object>` làm tham số.
*   **Cần thay đổi:**
    *   Thay đổi phương thức `GetAuthorArtist` để nhận `List<Relationship>?` làm tham số.
    *   Lặp qua danh sách `Relationship`.
    *   Kiểm tra `relationship.Type` là "author" hoặc "artist".
    *   **Quan trọng:** Cần lấy tên từ `relationship.Attributes`. Do `Attributes` là `object?`, cần ép kiểu cẩn thận sang `AuthorAttributes` hoặc `ArtistAttributes` (nếu có model riêng) hoặc `Dictionary<string, object>` để lấy `name`. **Lưu ý:** API hiện tại có thể không trả về `attributes` trong `Relationship` trừ khi có `includes[]` tương ứng trong lời gọi API gốc. Cần đảm bảo `IMangaApiService.FetchMangaDetailsAsync` có `includes[]=author&includes[]=artist`.

```csharp
// File: Services/MangaServices/MangaInformation/MangaRelationshipService.cs

using MangaReader.WebUI.Models.Mangadex; // Thêm using
using System.Text.Json; // Thêm using

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaRelationshipService
    {
        private readonly ILogger<MangaRelationshipService> _logger;
        private readonly JsonSerializerOptions _jsonOptions; // Để deserialize attributes

        public MangaRelationshipService(ILogger<MangaRelationshipService> logger)
        {
            _logger = logger;
             _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // Thay đổi tham số thành List<Relationship>?
        public (string author, string artist) GetAuthorArtist(List<Relationship>? relationships)
        {
            string author = "Không rõ";
            string artist = "Không rõ";

            if (relationships == null) return (author, artist);

            try
            {
                foreach (var rel in relationships)
                {
                    if (rel == null) continue;

                    string relType = rel.Type;

                    // Chỉ xử lý author và artist
                    if (relType == "author" || relType == "artist")
                    {
                        // Kiểm tra xem attributes có được include không
                        if (rel.Attributes != null)
                        {
                            try
                            {
                                // Thử deserialize attributes thành AuthorAttributes
                                // Cần đảm bảo rằng API trả về cấu trúc này khi include author/artist
                                var attributesJson = JsonSerializer.Serialize(rel.Attributes); // Serialize lại object
                                var authorAttributes = JsonSerializer.Deserialize<AuthorAttributes>(attributesJson, _jsonOptions);

                                if (authorAttributes?.Name != null)
                                {
                                    if (relType == "author")
                                        author = authorAttributes.Name;
                                    else if (relType == "artist")
                                        artist = authorAttributes.Name;
                                }
                                else
                                {
                                     _logger.LogWarning($"Attributes của relationship {rel.Id} (type: {relType}) không chứa 'name'.");
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                 _logger.LogError(jsonEx, $"Lỗi deserialize attributes cho relationship {rel.Id} (type: {relType}). Attributes: {rel.Attributes}");
                            }
                            catch (Exception attrEx)
                            {
                                 _logger.LogError(attrEx, $"Lỗi không xác định khi xử lý attributes cho relationship {rel.Id} (type: {relType}).");
                            }
                        }
                        else
                        {
                             _logger.LogWarning($"Relationship {rel.Id} (type: {relType}) không có attributes. Đảm bảo có 'includes[]=author&includes[]=artist' trong lời gọi API.");
                             // Fallback: Có thể gọi API /author/{id} hoặc /artist/{id} nếu cần thiết, nhưng sẽ chậm
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý relationships.");
            }

            return (author, artist);
        }

         // Có thể giữ lại phương thức cũ để tương thích tạm thời hoặc xóa đi
        // public (string author, string artist) GetAuthorArtist(Dictionary<string, object> mangaDict) { ... }
    }
}
```

### 9. `Services/MangaServices/MangaInformation/MangaTagService.cs`

*   **Hiện trạng:** Đang nhận `Dictionary<string, object>` làm tham số.
*   **Cần thay đổi:**
    *   Thay đổi phương thức `GetMangaTags` để nhận `MangaAttributes?` làm tham số.
    *   Truy cập trực tiếp thuộc tính `Tags` (là `List<Tag>?`).
    *   Trong `GetTagsFromAttributes` (đổi tên thành `GetTagsFromModel` hoặc tương tự):
        *   Lặp qua `List<Tag>`.
        *   Gọi `ExtractTagName` với `tag.Attributes`.
    *   Trong `ExtractTagName`:
        *   Nhận `TagAttributes?` làm tham số.
        *   Truy cập `tagAttributes.Name` (là `Dictionary<string, string>?`) để lấy tên tiếng Anh.

```csharp
// File: Services/MangaServices/MangaInformation/MangaTagService.cs

using MangaReader.WebUI.Models.Mangadex; // Thêm using

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaTagService
    {
        private readonly ILogger<MangaTagService> _logger;
        private readonly Dictionary<string, string> _tagTranslations;

        public MangaTagService(
            ILogger<MangaTagService> logger)
        {
            _logger = logger;
            _tagTranslations = InitializeTagTranslations();
        }

        private Dictionary<string, string> InitializeTagTranslations()
        {
            // ... (Giữ nguyên) ...
             return new Dictionary<string, string>
            {
                { "Oneshot", "Oneshot" }, { "Thriller", "Hồi hộp" }, { "Award Winning", "Đạt giải thưởng" },
                { "Reincarnation", "Chuyển sinh" }, { "Sci-Fi", "Khoa học viễn tưởng" }, { "Time Travel", "Du hành thời gian" },
                { "Genderswap", "Chuyển giới" }, { "Loli", "Loli" }, { "Traditional Games", "Trò chơi truyền thống" },
                { "Official Colored", "Bản màu chính thức" }, { "Historical", "Lịch sử" }, { "Monsters", "Quái vật" },
                { "Action", "Hành động" }, { "Demons", "Ác quỷ" }, { "Psychological", "Tâm lý" }, { "Ghosts", "Ma" },
                { "Animals", "Động vật" }, { "Long Strip", "Truyện dài" }, { "Romance", "Lãng mạn" }, { "Ninja", "Ninja" },
                { "Comedy", "Hài hước" }, { "Mecha", "Robot" }, { "Anthology", "Tuyển tập" }, { "Boys' Love", "Tình yêu nam giới" },
                { "Incest", "Loạn luân" }, { "Crime", "Tội phạm" }, { "Survival", "Sinh tồn" }, { "Zombies", "Zombie" },
                { "Reverse Harem", "Harem đảo" }, { "Sports", "Thể thao" }, { "Superhero", "Siêu anh hùng" },
                { "Martial Arts", "Võ thuật" }, { "Fan Colored", "Bản màu fanmade" }, { "Samurai", "Samurai" },
                { "Magical Girls", "Ma pháp thiếu nữ" }, { "Mafia", "Mafia" }, { "Adventure", "Phiêu lưu" },
                { "Self-Published", "Tự xuất bản" }, { "Virtual Reality", "Thực tế ảo" }, { "Office Workers", "Nhân viên văn phòng" },
                { "Video Games", "Trò chơi điện tử" }, { "Post-Apocalyptic", "Hậu tận thế" }, { "Sexual Violence", "Bạo lực tình dục" },
                { "Crossdressing", "Giả trang khác giới" }, { "Magic", "Phép thuật" }, { "Girls' Love", "Tình yêu nữ giới" },
                { "Harem", "Harem" }, { "Military", "Quân đội" }, { "Wuxia", "Võ hiệp" }, { "Isekai", "Dị giới" },
                { "4-Koma", "4-Koma" }, { "Doujinshi", "Doujinshi" }, { "Philosophical", "Triết học" }, { "Gore", "Bạo lực" },
                { "Drama", "Kịch tính" }, { "Medical", "Y học" }, { "School Life", "Học đường" }, { "Horror", "Kinh dị" },
                { "Fantasy", "Kỳ ảo" }, { "Villainess", "Nữ phản diện" }, { "Vampires", "Ma cà rồng" },
                { "Delinquents", "Học sinh cá biệt" }, { "Monster Girls", "Monster Girls" }, { "Shota", "Shota" },
                { "Police", "Cảnh sát" }, { "Web Comic", "Web Comic" }, { "Slice of Life", "Đời thường" },
                { "Aliens", "Người ngoài hành tinh" }, { "Cooking", "Nấu ăn" }, { "Supernatural", "Siêu nhiên" },
                { "Mystery", "Bí ẩn" }, { "Adaptation", "Chuyển thể" }, { "Music", "Âm nhạc" }, { "Full Color", "Bản màu đầy đủ" },
                { "Tragedy", "Bi kịch" }, { "Gyaru", "Gyaru" }
            };
        }

        // Thay đổi tham số thành MangaAttributes?
        public List<string> GetMangaTags(MangaAttributes? attributes)
        {
            var tags = new List<string>();
            if (attributes?.Tags == null) return tags;

            try
            {
                // Lấy tags tiếng Anh từ model
                var englishTags = GetTagsFromModel(attributes.Tags);

                // Dịch sang tiếng Việt
                tags = TranslateTagsToVietnamese(englishTags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tags từ MangaAttributes.");
            }

            // Sắp xếp tags theo thứ tự alphabet cho dễ đọc
            return tags.Distinct().OrderBy(t => t).ToList();
        }

        private List<string> TranslateTagsToVietnamese(List<string> englishTags)
        {
            // ... (Giữ nguyên) ...
            var vietnameseTags = new List<string>();
            foreach (var tag in englishTags)
            {
                if (_tagTranslations.TryGetValue(tag, out var translation))
                {
                    vietnameseTags.Add(translation);
                }
                else
                {
                    vietnameseTags.Add(tag); // Giữ nguyên nếu không có bản dịch
                    _logger.LogInformation($"Không tìm thấy bản dịch cho tag: {tag}");
                }
            }
            return vietnameseTags;
        }

        // Thay đổi tham số thành List<Tag>?
        private List<string> GetTagsFromModel(List<Tag>? tagsList)
        {
            var tags = new List<string>();
            if (tagsList == null) return tags;

            try
            {
                foreach (var tag in tagsList)
                {
                    if (tag?.Attributes != null)
                    {
                        var tagName = ExtractTagName(tag.Attributes);
                        if (!string.IsNullOrEmpty(tagName) && tagName != "Không rõ")
                        {
                             tags.Add(tagName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tags từ List<Tag>.");
            }

            return tags;
        }

        // Thay đổi tham số thành TagAttributes?
        private string ExtractTagName(TagAttributes? tagAttributes)
        {
             if (tagAttributes?.Name == null) return "Không rõ";

            try
            {
                // Ưu tiên lấy tên tiếng Anh
                if (tagAttributes.Name.TryGetValue("en", out var enName) && !string.IsNullOrEmpty(enName))
                {
                    return enName;
                }
                // Nếu không có tiếng Anh, lấy tên đầu tiên tìm thấy
                return tagAttributes.Name.FirstOrDefault().Value ?? "Không rõ";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất tên tag từ TagAttributes.");
            }
            return "Không rõ";
        }

         // Có thể giữ lại phương thức cũ để tương thích tạm thời hoặc xóa đi
        // public List<string> GetMangaTags(Dictionary<string, object> mangaDict) { ... }
    }
}
```

### 10. `Services/MangaServices/MangaInformation/MangaTitleService.cs`

*   **Hiện trạng:** Đang nhận `Dictionary<string, object>` làm tham số.
*   **Cần thay đổi:**
    *   Thay đổi các phương thức `GetDefaultMangaTitle`, `GetMangaTitle`, `GetAlternativeTitles` để nhận `MangaAttributes?` hoặc các thuộc tính liên quan (`Dictionary<string, string>?`, `List<Dictionary<string, string>>?`) làm tham số.
    *   Truy cập trực tiếp các thuộc tính `Title` và `AltTitles` từ `MangaAttributes`.
    *   Trong `GetMangaTitleFromIdAsync`:
        *   Inject `IMangaApiService`.
        *   Thay thế lời gọi `_httpClient.GetAsync` bằng `_mangaApiService.FetchMangaDetailsAsync(mangaId)`.
        *   Xử lý kết quả `MangaResponse?`. Nếu thành công, lấy `Data.Attributes`.
        *   Gọi các phương thức đã cập nhật (`GetMangaTitle`, `GetDefaultMangaTitle`) với `attributes`.

```csharp
// File: Services/MangaServices/MangaInformation/MangaTitleService.cs

using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.APIServices.Interfaces; // Thêm using
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaTitleService
    {
        private readonly ILogger<MangaTitleService> _logger;
        private readonly IMangaApiService _mangaApiService; // Thay thế HttpClient
        private readonly JsonConversionService _jsonConversionService; // Có thể không cần nữa
        // _baseUrl không cần thiết nữa

        public MangaTitleService(
            ILogger<MangaTitleService> logger,
            IMangaApiService mangaApiService, // Inject service mới
            JsonConversionService jsonConversionService)
        {
            _logger = logger;
            _mangaApiService = mangaApiService;
            _jsonConversionService = jsonConversionService;
        }

        // Thay đổi tham số thành Dictionary<string, string>?
        public string GetDefaultMangaTitle(Dictionary<string, string>? titleDict)
        {
            if (titleDict == null || !titleDict.Any())
                return "Không có tiêu đề";

            try
            {
                // Ưu tiên 'en' nếu có
                if (titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle)) return enTitle;
                // Lấy giá trị đầu tiên
                return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề mặc định từ Dictionary.");
                return "Không có tiêu đề";
            }
        }

        // Thay đổi tham số thành các thuộc tính từ MangaAttributes
        public string GetMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
        {
            try
            {
                // Lấy danh sách tiêu đề thay thế
                var altTitlesDictionary = GetAlternativeTitles(altTitlesList);

                // Kiểm tra xem có tên tiếng Việt (vi) không
                if (altTitlesDictionary.TryGetValue("vi", out var viTitles) && viTitles.Any())
                {
                    return viTitles.First(); // Lấy tiêu đề tiếng Việt đầu tiên
                }

                // Nếu không có tên tiếng Việt, sử dụng tên mặc định
                return GetDefaultMangaTitle(titleDict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề ưu tiên.");
                return GetDefaultMangaTitle(titleDict); // Fallback về tiêu đề mặc định
            }
        }

        // Thay đổi tham số thành List<Dictionary<string, string>>?
        public Dictionary<string, List<string>> GetAlternativeTitles(List<Dictionary<string, string>>? altTitlesList)
        {
            var altTitlesDictionary = new Dictionary<string, List<string>>();
            if (altTitlesList == null) return altTitlesDictionary;

            try
            {
                foreach (var altTitleDict in altTitlesList)
                {
                    if (altTitleDict != null && altTitleDict.Any())
                    {
                        var langKey = altTitleDict.Keys.First();
                        var titleText = altTitleDict[langKey];

                        if (!string.IsNullOrEmpty(titleText))
                        {
                            if (!altTitlesDictionary.ContainsKey(langKey))
                            {
                                altTitlesDictionary[langKey] = new List<string>();
                            }
                            altTitlesDictionary[langKey].Add(titleText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề thay thế từ List.");
            }

            return altTitlesDictionary;
        }

        // GetPreferredAlternativeTitle không đổi

        public async Task<string> GetMangaTitleFromIdAsync(string mangaId)
        {
            try
            {
                if (string.IsNullOrEmpty(mangaId))
                {
                    _logger.LogWarning("MangaId không được cung cấp khi gọi GetMangaTitleFromIdAsync");
                    throw new ArgumentNullException(nameof(mangaId), "MangaId không được để trống");
                }

                _logger.LogInformation($"Đang lấy tiêu đề cho manga: {mangaId}");

                // Gọi API service mới
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);

                if (mangaResponse?.Result != "ok" || mangaResponse.Data?.Attributes == null)
                {
                     _logger.LogError($"Không lấy được thông tin hoặc attributes cho manga {mangaId}. Response: {mangaResponse?.Result}");
                     return "Không có tiêu đề"; // Trả về giá trị mặc định nếu lỗi
                }

                var attributes = mangaResponse.Data.Attributes;

                // Sử dụng các phương thức đã cập nhật để lấy tiêu đề
                string title = GetMangaTitle(attributes.Title, attributes.AltTitles);
                _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId}: {title}");
                return title;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy tiêu đề manga {mangaId}");
                return "Không có tiêu đề";
            }
        }

        // --- Giữ lại các phương thức cũ nhận object để tương thích tạm thời nếu cần ---
        public string GetDefaultMangaTitle(object titleObj)
        {
             if (titleObj is Dictionary<string, string> titleDict)
             {
                 return GetDefaultMangaTitle(titleDict);
             }
             // Fallback hoặc xử lý lỗi nếu kiểu không đúng
             _logger.LogWarning($"GetDefaultMangaTitle(object): Kiểu dữ liệu không mong đợi: {titleObj?.GetType()}");
             return "Không có tiêu đề";
        }

        public string GetMangaTitle(object titleObj, object altTitlesObj)
        {
             Dictionary<string, string>? titleDict = titleObj as Dictionary<string, string>;
             List<Dictionary<string, string>>? altTitlesList = null;

             // Cần xử lý altTitlesObj cẩn thận hơn vì nó là List<Dictionary<string, object>> từ json
             if (altTitlesObj is List<object> altObjList)
             {
                 altTitlesList = altObjList.OfType<Dictionary<string, object>>()
                                          .Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""))
                                          .ToList();
             }
             else if (altTitlesObj is List<Dictionary<string, string>> altDictList) // Trường hợp đã đúng kiểu
             {
                 altTitlesList = altDictList;
             }

             return GetMangaTitle(titleDict, altTitlesList);
        }

         public Dictionary<string, List<string>> GetAlternativeTitles(object altTitlesObj)
         {
             List<Dictionary<string, string>>? altTitlesList = null;
             if (altTitlesObj is List<object> altObjList)
             {
                 altTitlesList = altObjList.OfType<Dictionary<string, object>>()
                                          .Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""))
                                          .ToList();
             }
             else if (altTitlesObj is List<Dictionary<string, string>> altDictList)
             {
                 altTitlesList = altDictList;
             }
             return GetAlternativeTitles(altTitlesList);
         }

         public string GetPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary)
        {
            if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
            if (altTitlesDictionary.TryGetValue("jp", out var jpTitles) && jpTitles.Any()) return jpTitles.First(); // Thường là romaji
            if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First(); // Romaji
            return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
        }
    }
}
```

### 11. `Services/MangaServices/MangaPageService/MangaDetailsService.cs`

*   **Hiện trạng:** Đang sử dụng `MangaDexService` và các helper service.
*   **Cần thay đổi:**
    *   Inject `IMangaApiService`, `ICoverApiService`, `IChapterApiService`.
    *   Trong `GetMangaDetailsAsync`:
        *   Gọi `_mangaApiService.FetchMangaDetailsAsync(id)`.
        *   Xử lý `MangaResponse?`. Nếu thành công, lấy `Data`.
        *   Gọi `CreateMangaViewModelAsync` với `manga.Data`.
        *   Gọi `GetChaptersAsync` (đã được cập nhật để dùng `IChapterApiService`).
    *   Trong `CreateMangaViewModelAsync`:
        *   Nhận tham số là `Models.Mangadex.Manga?` thay vì `Dictionary`.
        *   Truy cập trực tiếp thuộc tính từ `manga.Attributes` và `manga.Relationships`.
        *   Gọi các helper service (`MangaTitleService`, `MangaTagService`, `MangaRelationshipService`, `MangaDescription`) với các thuộc tính tương ứng từ model `Manga`.
        *   Gọi `_coverApiService.FetchCoverUrlAsync(id)` để lấy ảnh bìa.
        *   Gọi `_mangaFollowService.IsFollowingMangaAsync(id)` (service này không đổi).
        *   **Mapping:** Tạo `MangaViewModel`.
    *   Trong `GetChaptersAsync`: Đã được xử lý bởi `ChapterService`.

```csharp
// File: Services/MangaServices/MangaPageService/MangaDetailsService.cs

using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        // IChapterApiService không cần trực tiếp ở đây vì đã dùng ChapterService
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly LocalizationService _localizationService;
        // JsonConversionService có thể không cần nữa
        private readonly MangaUtilityService _mangaUtilityService;
        private readonly MangaTitleService _mangaTitleService;
        private readonly MangaTagService _mangaTagService;
        private readonly MangaRelationshipService _mangaRelationshipService;
        private readonly IMangaFollowService _mangaFollowService; // Sử dụng Interface
        private readonly ChapterService _chapterService; // Đã được cập nhật
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MangaDescription _mangaDescription;

        public MangaDetailsService(
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<MangaDetailsService> logger,
            LocalizationService localizationService,
            // JsonConversionService jsonConversionService, // Bỏ đi nếu không dùng
            MangaUtilityService mangaUtilityService,
            MangaTitleService mangaTitleService,
            MangaTagService mangaTagService,
            MangaRelationshipService mangaRelationshipService,
            IMangaFollowService mangaFollowService, // Inject Interface
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            MangaDescription mangaDescription)
        {
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
            _logger = logger;
            _localizationService = localizationService;
            // _jsonConversionService = jsonConversionService;
            _mangaUtilityService = mangaUtilityService;
            _mangaTitleService = mangaTitleService;
            _mangaTagService = mangaTagService;
            _mangaRelationshipService = mangaRelationshipService;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaDescription = mangaDescription;
        }

        public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
        {
            try
            {
                _logger.LogInformation($"Đang lấy chi tiết manga ID: {id}");
                // Gọi API service mới để lấy chi tiết manga
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);

                if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                {
                    _logger.LogError($"Không thể lấy chi tiết manga {id}. Response: {mangaResponse?.Result}");
                    // Trả về ViewModel rỗng hoặc throw exception
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, Chapters = new List<ChapterViewModel>() };
                }

                var mangaData = mangaResponse.Data; // Model Manga

                // Tạo MangaViewModel từ dữ liệu Manga
                var mangaViewModel = await CreateMangaViewModelAsync(mangaData);

                // Lấy danh sách chapters (ChapterService đã được cập nhật)
                var chapterViewModels = await GetChaptersAsync(id);

                return new MangaDetailViewModel
                {
                    Manga = mangaViewModel,
                    Chapters = chapterViewModels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi nghiêm trọng khi lấy chi tiết manga {id}");
                // Trả về ViewModel lỗi
                 return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, Chapters = new List<ChapterViewModel>() };
            }
        }

        public async Task<Dictionary<string, List<string>>> GetAlternativeTitlesByLanguageAsync(string id)
        {
            try
            {
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);
                if (mangaResponse?.Result == "ok" && mangaResponse.Data?.Attributes?.AltTitles != null)
                {
                    // Gọi helper service với dữ liệu từ model mới
                    return _mangaTitleService.GetAlternativeTitles(mangaResponse.Data.Attributes.AltTitles);
                }
                return new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy tiêu đề thay thế cho manga {id}");
                return new Dictionary<string, List<string>>();
            }
        }

        // Thay đổi tham số thành model Manga
        private async Task<MangaViewModel> CreateMangaViewModelAsync(Manga mangaData)
        {
            if (mangaData?.Attributes == null)
            {
                 _logger.LogWarning($"Dữ liệu manga hoặc attributes bị null khi tạo ViewModel cho ID: {mangaData?.Id}");
                 return new MangaViewModel { Id = mangaData?.Id.ToString() ?? "unknown", Title = "Lỗi dữ liệu" };
            }

            string id = mangaData.Id.ToString();
            var attributes = mangaData.Attributes;

            try
            {
                // Lấy title (MangaTitleService đã cập nhật)
                string mangaTitle = _mangaTitleService.GetMangaTitle(attributes.Title, attributes.AltTitles);

                // Lưu title vào session
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && !string.IsNullOrEmpty(mangaTitle))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaTitle);
                    _logger.LogInformation($"Đã lưu tiêu đề manga {id} vào session: {mangaTitle}");
                }

                // Lấy description (MangaDescription đã cập nhật)
                string description = _mangaDescription.GetDescription(attributes);

                // Lấy tags (MangaTagService đã cập nhật)
                var tags = _mangaTagService.GetMangaTags(attributes);

                // Lấy author/artist (MangaRelationshipService đã cập nhật)
                var (author, artist) = _mangaRelationshipService.GetAuthorArtist(mangaData.Relationships);

                // Lấy ảnh bìa (ICoverApiService)
                string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);
                if (string.IsNullOrEmpty(coverUrl))
                {
                    coverUrl = "/images/cover-placeholder.jpg"; // Ảnh mặc định
                }

                // Lấy trạng thái (LocalizationService)
                string status = _localizationService.GetStatus(attributes); // Cần cập nhật LocalizationService

                // Lấy các thuộc tính khác trực tiếp
                string originalLanguage = attributes.OriginalLanguage ?? "";
                string publicationDemographic = attributes.PublicationDemographic ?? "";
                string contentRating = attributes.ContentRating ?? "";
                DateTime? lastUpdated = attributes.UpdatedAt.DateTime;
                string alternativeTitles = _mangaTitleService.GetPreferredAlternativeTitle(
                                            _mangaTitleService.GetAlternativeTitles(attributes.AltTitles));


                // Kiểm tra trạng thái follow (IMangaFollowService)
                bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);

                return new MangaViewModel
                {
                    Id = id,
                    Title = mangaTitle,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = status,
                    Tags = tags,
                    Author = author,
                    Artist = artist,
                    OriginalLanguage = originalLanguage,
                    PublicationDemographic = publicationDemographic,
                    ContentRating = contentRating,
                    AlternativeTitles = alternativeTitles,
                    LastUpdated = lastUpdated,
                    IsFollowing = isFollowing,
                    Rating = _mangaUtilityService.GetMangaRating(id), // Giữ nguyên logic giả
                    Views = 0 // Giữ nguyên
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tạo MangaViewModel cho ID: {id}");
                // Trả về ViewModel lỗi
                return new MangaViewModel { Id = id, Title = "Lỗi tạo ViewModel" };
            }
        }

        // GetChaptersAsync không đổi, vẫn dùng ChapterService đã cập nhật
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId)
        {
            try
            {
                var chapterViewModels = await _chapterService.GetChaptersAsync(mangaId, "vi,en");

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && chapterViewModels.Any()) // Chỉ lưu nếu có chapter
                {
                    // Lưu tất cả chapters (nếu cần)
                    // httpContext.Session.SetString($"Manga_{mangaId}_AllChapters", JsonSerializer.Serialize(chapterViewModels));

                    // Phân loại và lưu theo ngôn ngữ
                    var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                    foreach (var kvp in chaptersByLanguage)
                    {
                        httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{kvp.Key}", JsonSerializer.Serialize(kvp.Value));
                        _logger.LogInformation($"Đã lưu {kvp.Value.Count} chapters ngôn ngữ {kvp.Key} của manga {mangaId} vào session");
                    }
                }

                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}");
                return new List<ChapterViewModel>();
            }
        }
    }
}

// Cần cập nhật LocalizationService.GetStatus để nhận MangaAttributes
namespace MangaReader.WebUI.Services.UtilityServices
{
    public class LocalizationService
    {
        private readonly ILogger<LocalizationService> _logger;

        public LocalizationService(ILogger<LocalizationService> logger)
        {
            _logger = logger;
        }

        // Cập nhật phương thức này
        public string GetStatus(MangaAttributes? attributes)
        {
            if (attributes == null || string.IsNullOrEmpty(attributes.Status)) return "Không rõ";

            return attributes.Status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }

         // Giữ lại phương thức cũ để tương thích tạm thời hoặc xóa đi
        public string GetStatus(Dictionary<string, object> attributesDict)
        {
            string status = attributesDict.ContainsKey("status") ? attributesDict["status"]?.ToString() ?? "unknown" : "unknown";
             return status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }

        // Các phương thức khác giữ nguyên
        public string GetLocalizedTitle(string titleJson) { /* ... giữ nguyên ... */
             try
            {
                if (string.IsNullOrEmpty(titleJson)) return "Không có tiêu đề";
                var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                if (titles == null || !titles.Any()) return "Không có tiêu đề";
                if (titles.TryGetValue("vi", out var viTitle) && !string.IsNullOrEmpty(viTitle)) return viTitle;
                if (titles.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle)) return enTitle;
                return titles.FirstOrDefault().Value ?? "Không có tiêu đề";
            }
            catch (JsonException jEx)
            {
                 _logger.LogError(jEx, $"Lỗi JSON khi parse title: {titleJson}");
                 return "Không có tiêu đề";
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý tiêu đề truyện: {titleJson}");
                return "Không có tiêu đề";
            }
        }
        public string GetLocalizedDescription(string descriptionJson) { /* ... giữ nguyên ... */
             try
            {
                 if (string.IsNullOrEmpty(descriptionJson)) return "";
                 var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);
                 if (descriptions == null || !descriptions.Any()) return "";
                 if (descriptions.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc)) return viDesc;
                 if (descriptions.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc)) return enDesc;
                 return descriptions.FirstOrDefault().Value ?? "";
            }
             catch (JsonException jEx)
            {
                 _logger.LogError(jEx, $"Lỗi JSON khi parse description: {descriptionJson}");
                 return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý mô tả truyện: {descriptionJson}");
                return "";
            }
        }
    }
}
```

### 12. `Services/MangaServices/MangaPageService/MangaSearchService.cs`

*   **Hiện trạng:** Đang sử dụng `MangaDexService` và các helper service.
*   **Cần thay đổi:**
    *   Inject `IMangaApiService`, `ICoverApiService`.
    *   Trong `SearchMangaAsync`:
        *   Gọi `_mangaApiService.FetchMangaAsync(limit, offset, sortManga)`.
        *   Xử lý `MangaList?`.
        *   Gọi `ConvertToMangaViewModelsAsync` với `result.Data`.
    *   Trong `ConvertToMangaViewModelsAsync`:
        *   Nhận tham số là `List<Manga>?`.
        *   Lặp qua danh sách `Manga`.
        *   **Mapping:** Tạo `MangaViewModel` từ mỗi `Manga`.
            *   Gọi các helper service (`MangaTitleService`, `MangaTagService`, `MangaRelationshipService`, `MangaDescription`, `LocalizationService`) với `manga.Attributes` hoặc `manga.Relationships`.
            *   **Tối ưu Cover:** Thay vì gọi `FetchCoverUrlAsync` cho từng manga, gọi `_coverApiService.FetchRepresentativeCoverUrlsAsync(mangaIds)` một lần cho cả batch kết quả tìm kiếm. Sau đó lấy URL từ dictionary trả về.

```csharp
// File: Services/MangaServices/MangaPageService/MangaSearchService.cs

using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex; // Thêm using
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaSearchService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        private readonly ILogger<MangaSearchService> _logger;
        private readonly LocalizationService _localizationService; // Đã cập nhật
        // JsonConversionService có thể không cần nữa
        private readonly MangaTitleService _mangaTitleService; // Đã cập nhật
        private readonly MangaTagService _mangaTagService; // Đã cập nhật
        private readonly MangaDescription _mangaDescriptionService; // Đã cập nhật
        private readonly MangaRelationshipService _mangaRelationshipService; // Đã cập nhật

        public MangaSearchService(
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<MangaSearchService> logger,
            LocalizationService localizationService,
            // JsonConversionService jsonConversionService, // Bỏ đi
            MangaTitleService mangaTitleService,
            MangaTagService mangaTagService,
            MangaDescription mangaDescriptionService,
            MangaRelationshipService mangaRelationshipService)
        {
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
            _logger = logger;
            _localizationService = localizationService;
            // _jsonConversionService = jsonConversionService;
            _mangaTitleService = mangaTitleService;
            _mangaTagService = mangaTagService;
            _mangaDescriptionService = mangaDescriptionService;
            _mangaRelationshipService = mangaRelationshipService;
        }

        // CreateSortMangaFromParameters không thay đổi

        public async Task<MangaListViewModel> SearchMangaAsync(
            int page,
            int pageSize,
            SortManga sortManga)
        {
            try
            {
                const int MAX_API_RESULTS = 10000;
                int offset = (page - 1) * pageSize;
                int limit = pageSize;

                if (offset + limit > MAX_API_RESULTS)
                {
                    limit = (offset < MAX_API_RESULTS) ? MAX_API_RESULTS - offset : 0;
                }

                if (limit <= 0)
                {
                     _logger.LogWarning($"Yêu cầu tìm kiếm vượt quá giới hạn {MAX_API_RESULTS} kết quả (page: {page}, pageSize: {pageSize}).");
                    return new MangaListViewModel { Mangas = new List<MangaViewModel>(), CurrentPage = page, PageSize = pageSize, TotalCount = MAX_API_RESULTS, MaxPages = (int)Math.Ceiling(MAX_API_RESULTS / (double)pageSize), SortOptions = sortManga };
                }

                // Gọi API service mới
                var mangaListResponse = await _mangaApiService.FetchMangaAsync(limit: limit, offset: offset, sortManga: sortManga);

                if (mangaListResponse?.Result != "ok")
                {
                    _logger.LogError($"API FetchMangaAsync trả về lỗi hoặc null. Result: {mangaListResponse?.Result}");
                     return new MangaListViewModel { Mangas = new List<MangaViewModel>(), CurrentPage = page, PageSize = pageSize, TotalCount = 0, MaxPages = 0, SortOptions = sortManga };
                }

                int totalCount = mangaListResponse.Total;
                int maxPages = (int)Math.Ceiling(Math.Min(totalCount, MAX_API_RESULTS) / (double)pageSize);

                // Chuyển đổi List<Manga> sang List<MangaViewModel>
                var mangaViewModels = await ConvertToMangaViewModelsAsync(mangaListResponse.Data);

                return new MangaListViewModel
                {
                    Mangas = mangaViewModels,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    MaxPages = maxPages,
                    SortOptions = sortManga
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tìm kiếm manga");
                return new MangaListViewModel { Mangas = new List<MangaViewModel>(), CurrentPage = page, PageSize = pageSize, TotalCount = 0, MaxPages = 0, SortOptions = sortManga };
            }
        }

        // Thay đổi tham số thành List<Manga>?
        private async Task<List<MangaViewModel>> ConvertToMangaViewModelsAsync(List<Manga>? mangaList)
        {
            var mangaViewModels = new List<MangaViewModel>();
            if (mangaList == null || !mangaList.Any()) return mangaViewModels;

            // Lấy danh sách ID để fetch cover một lần
            var mangaIds = mangaList.Select(m => m.Id.ToString()).ToList();
            var coverUrls = await _coverApiService.FetchRepresentativeCoverUrlsAsync(mangaIds) ?? new Dictionary<string, string>();

            foreach (var manga in mangaList)
            {
                 if (manga?.Attributes == null) continue; // Bỏ qua nếu thiếu dữ liệu

                string id = manga.Id.ToString();
                var attributes = manga.Attributes;

                try
                {
                    // Lấy title (MangaTitleService đã cập nhật)
                    string title = _mangaTitleService.GetMangaTitle(attributes.Title, attributes.AltTitles);

                    // Lấy author/artist (MangaRelationshipService đã cập nhật)
                    var (author, artist) = _mangaRelationshipService.GetAuthorArtist(manga.Relationships);

                    // Lấy description (MangaDescription đã cập nhật)
                    string description = _mangaDescriptionService.GetDescription(attributes);

                    // Lấy status (LocalizationService đã cập nhật)
                    string status = _localizationService.GetStatus(attributes);

                    // Lấy tags (MangaTagService đã cập nhật)
                    var tags = _mangaTagService.GetMangaTags(attributes);

                    // Lấy cover URL từ dictionary đã fetch
                    string coverUrl = coverUrls.TryGetValue(id, out var url) ? url : "/images/cover-placeholder.jpg";

                    DateTime? lastUpdated = attributes.UpdatedAt.DateTime;

                    var viewModel = new MangaViewModel
                    {
                        Id = id,
                        Title = title,
                        Author = author,
                        Artist = artist,
                        Description = description,
                        CoverUrl = coverUrl,
                        Status = status,
                        LastUpdated = lastUpdated,
                        Tags = tags
                        // Các trường khác như Rating, Views giữ nguyên logic cũ hoặc bỏ đi nếu không có dữ liệu
                    };
                    mangaViewModels.Add(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi chuyển đổi manga ID: {id} sang ViewModel.");
                    // Có thể thêm một ViewModel lỗi vào danh sách nếu muốn
                    mangaViewModels.Add(new MangaViewModel { Id = id, Title = "Lỗi xử lý dữ liệu", CoverUrl = "/images/cover-placeholder.jpg" });
                }
            }

            return mangaViewModels;
        }

         // CreateSortMangaFromParameters không đổi
         public SortManga CreateSortMangaFromParameters(
            string title = "", List<string>? status = null, string sortBy = "latest",
            string authors = "", string artists = "", int? year = null,
            List<string>? availableTranslatedLanguage = null, List<string>? publicationDemographic = null,
            List<string>? contentRating = null, string includedTagsMode = "AND",
            string excludedTagsMode = "OR", List<string>? genres = null,
            string includedTagsStr = "", string excludedTagsStr = "")
        {
             var sortManga = new SortManga
            {
                Title = title,
                Status = status ?? new List<string>(),
                SortBy = sortBy ?? "latest",
                Year = year,
                Demographic = publicationDemographic ?? new List<string>(),
                IncludedTagsMode = includedTagsMode ?? "AND",
                ExcludedTagsMode = excludedTagsMode ?? "OR",
                Genres = genres ?? new List<string>() // Khởi tạo nếu null
            };

            if (!string.IsNullOrEmpty(authors))
                sortManga.Authors = authors.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();

            if (!string.IsNullOrEmpty(artists))
                sortManga.Artists = artists.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();

            if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
                sortManga.Languages = availableTranslatedLanguage;

            if (contentRating != null && contentRating.Any())
                sortManga.ContentRating = contentRating;
            else
                sortManga.ContentRating = new List<string> { "safe", "suggestive", "erotica" }; // Mặc định

            if (!string.IsNullOrEmpty(includedTagsStr))
                sortManga.IncludedTags = includedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

            if (!string.IsNullOrEmpty(excludedTagsStr))
                sortManga.ExcludedTags = excludedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

            return sortManga;
        }
    }
}
```

### 13. `Services/MangaServices/FollowedMangaService.cs`

*   **Hiện trạng:** Đang sử dụng `MangaDetailsService` (cần thay bằng `IMangaInfoService`) và `ChapterService`.
*   **Cần thay đổi:**
    *   Inject `IMangaInfoService`.
    *   Trong `GetFollowedMangaListAsync`:
        *   Gọi `_mangaInfoService.GetMangaInfoAsync(mangaId)` để lấy thông tin cơ bản (title, cover).
        *   Gọi `_chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en")` (ChapterService đã được cập nhật).
        *   **Mapping:** Tạo `FollowedMangaViewModel` từ kết quả của `MangaInfoViewModel` và `List<SimpleChapterInfo>`.

```csharp
// File: Services/MangaServices/FollowedMangaService.cs

using MangaReader.WebUI.Models.Auth;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices; // Cần ChapterService
using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService; // Đã inject Interface
        private readonly ChapterService _chapterService; // Đã cập nhật
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550);

        public FollowedMangaService(
            IUserService userService,
            IMangaInfoService mangaInfoService, // Inject Interface
            ChapterService chapterService, // Inject ChapterService đã cập nhật
            ILogger<FollowedMangaService> logger)
        {
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _chapterService = chapterService;
            _logger = logger;
        }

        public async Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync()
        {
            var followedMangaList = new List<FollowedMangaViewModel>();

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy danh sách theo dõi.");
                return followedMangaList;
            }

            try
            {
                UserModel user = await _userService.GetUserInfoAsync();
                if (user?.FollowingManga == null || !user.FollowingManga.Any())
                {
                    _logger.LogInformation("Người dùng không theo dõi manga nào.");
                    return followedMangaList;
                }

                _logger.LogInformation($"Người dùng đang theo dõi {user.FollowingManga.Count} manga. Bắt đầu lấy thông tin...");

                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        // Áp dụng delay trước khi gọi API
                        await Task.Delay(_rateLimitDelay);

                        // 1. Lấy thông tin cơ bản (đã dùng IMangaInfoService)
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);
                        if (mangaInfo == null)
                        {
                             _logger.LogWarning($"Không thể lấy thông tin cơ bản cho manga ID: {mangaId}. Bỏ qua.");
                             continue;
                        }

                        // Áp dụng delay trước khi gọi API tiếp theo
                        await Task.Delay(_rateLimitDelay);

                        // 2. Lấy chapter mới nhất (ChapterService đã cập nhật)
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                        // 3. Mapping: Tạo ViewModel
                        var followedManga = new FollowedMangaViewModel
                        {
                            MangaId = mangaId,
                            MangaTitle = mangaInfo.MangaTitle,
                            CoverUrl = mangaInfo.CoverUrl,
                            LatestChapters = latestChapters ?? new List<SimpleChapterInfo>()
                        };

                        followedMangaList.Add(followedManga);
                        _logger.LogDebug($"Đã xử lý xong manga: {mangaInfo.MangaTitle}");
                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi.");
                    }
                }

                _logger.LogInformation($"Hoàn tất lấy thông tin cho {followedMangaList.Count} truyện đang theo dõi.");
                return followedMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi.");
                return new List<FollowedMangaViewModel>();
            }
        }
    }
}
```

### 14. `Services/MangaServices/MangaInfoService.cs`

*   **Hiện trạng:** Đang sử dụng `MangaTitleService` và `MangaDexService`.
*   **Cần thay đổi:**
    *   Inject `ICoverApiService`.
    *   Trong `GetMangaInfoAsync`:
        *   Gọi `_mangaTitleService.GetMangaTitleFromIdAsync(mangaId)` (đã được cập nhật).
        *   Gọi `_coverApiService.FetchCoverUrlAsync(mangaId)`.
        *   **Mapping:** Tạo `MangaInfoViewModel`.

```csharp
// File: Services/MangaServices/MangaInfoService.cs

using MangaReader.WebUI.Services.APIServices.Interfaces; // Thêm using ICoverApiService
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class MangaInfoService : IMangaInfoService
    {
        private readonly MangaTitleService _mangaTitleService; // Đã cập nhật
        private readonly ICoverApiService _coverApiService; // Thay thế MangaDexService
        private readonly ILogger<MangaInfoService> _logger;

        public MangaInfoService(
            MangaTitleService mangaTitleService,
            ICoverApiService coverApiService, // Inject service mới
            ILogger<MangaInfoService> logger)
        {
            _mangaTitleService = mangaTitleService;
            _coverApiService = coverApiService;
            _logger = logger;
        }

        public async Task<MangaInfoViewModel> GetMangaInfoAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("MangaId không được cung cấp khi gọi GetMangaInfoAsync.");
                return null;
            }

            try
            {
                _logger.LogInformation($"Bắt đầu lấy thông tin cơ bản cho manga ID: {mangaId}");

                // 1. Lấy tiêu đề manga (MangaTitleService đã cập nhật)
                string mangaTitle = await _mangaTitleService.GetMangaTitleFromIdAsync(mangaId);
                if (string.IsNullOrEmpty(mangaTitle) || mangaTitle == "Không có tiêu đề")
                {
                    _logger.LogWarning($"Không thể lấy tiêu đề cho manga ID: {mangaId}. Sử dụng ID làm tiêu đề.");
                    mangaTitle = $"Manga ID: {mangaId}";
                }

                // 2. Lấy ảnh bìa (ICoverApiService)
                string coverUrl = await _coverApiService.FetchCoverUrlAsync(mangaId);
                 if (string.IsNullOrEmpty(coverUrl))
                {
                    coverUrl = "/images/cover-placeholder.jpg"; // Ảnh mặc định
                }


                _logger.LogInformation($"Lấy thông tin cơ bản thành công cho manga ID: {mangaId}");

                // 3. Mapping: Tạo ViewModel
                return new MangaInfoViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    CoverUrl = coverUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin cơ bản cho manga ID: {mangaId}");
                return new MangaInfoViewModel // Trả về object với thông tin mặc định/lỗi
                {
                     MangaId = mangaId,
                     MangaTitle = $"Lỗi lấy tiêu đề ({mangaId})",
                     CoverUrl = "/images/cover-placeholder.jpg"
                };
            }
        }
    }
}
```

### 15. `Services/MangaServices/ReadingHistoryService.cs`

*   **Hiện trạng:** Đang sử dụng `IMangaInfoService` và `IChapterInfoService`.
*   **Cần thay đổi:** Không cần thay đổi lớn nếu `IMangaInfoService` và `IChapterInfoService` đã được cập nhật đúng cách để sử dụng các API service mới. Đảm bảo rằng các service này trả về dữ liệu chính xác.

```csharp
// File: Services/MangaServices/ReadingHistoryService.cs
// Logic chính không cần thay đổi nhiều nếu các dependency service (IMangaInfoService, IChapterInfoService) đã được cập nhật.
// Chỉ cần đảm bảo các service đó hoạt động đúng.

using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices; // Cần IChapterInfoService
using MangaReader.WebUI.Services.MangaServices.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Services.MangaServices
{
    // Model BackendHistoryItem giữ nguyên

    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService; // Đảm bảo đã cập nhật
        private readonly IChapterInfoService _chapterInfoService; // Đảm bảo đã cập nhật
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReadingHistoryService> _logger;
        private readonly TimeSpan _rateLimitDelay;

        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService, // Inject Interface đã cập nhật
            IChapterInfoService chapterInfoService, // Inject Interface đã cập nhật
            IConfiguration configuration,
            ILogger<ReadingHistoryService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _chapterInfoService = chapterInfoService;
            _configuration = configuration;
            _logger = logger;
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
                    // ... (Xử lý lỗi giữ nguyên) ...
                     var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi khi gọi API backend lấy lịch sử đọc. Status: {response.StatusCode}, Content: {errorContent}");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) _userService.RemoveToken();
                    return historyViewModels;
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
                    await Task.Delay(_rateLimitDelay); // Delay

                    // Gọi service đã cập nhật
                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho MangaId: {item.MangaId}. Bỏ qua mục lịch sử này.");
                        continue;
                    }

                    await Task.Delay(_rateLimitDelay); // Delay

                    // Gọi service đã cập nhật
                    var chapterInfo = await _chapterInfoService.GetChapterInfoAsync(item.ChapterId);
                    if (chapterInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho ChapterId: {item.ChapterId}. Bỏ qua mục lịch sử này.");
                        continue;
                    }

                    // Mapping: Tạo ViewModel (logic không đổi)
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
                return historyViewModels;
            }
            catch (JsonException jsonEx)
            {
                 _logger.LogError(jsonEx, "Lỗi khi deserialize lịch sử đọc từ backend.");
                 return historyViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ngoại lệ khi lấy và xử lý lịch sử đọc.");
                return historyViewModels;
            }
        }
    }
     // Model BackendHistoryItem giữ nguyên
    public class BackendHistoryItem
    {
        [JsonPropertyName("mangaId")]
        public string MangaId { get; set; } = default!;

        [JsonPropertyName("chapterId")]
        public string ChapterId { get; set; } = default!;

        [JsonPropertyName("lastReadAt")]
        public DateTime LastReadAt { get; set; }
    }
}
```

### 16. Các Service khác

*   **`MangaFollowService.cs`:** Service này chủ yếu tương tác với API backend (`/api/users/follow`, `/api/users/unfollow`, `/api/users/user/following/{mangaId}`). Không cần thay đổi vì nó không trực tiếp gọi API MangaDex hoặc xử lý model MangaDex.
*   **`MangaUtilityService.cs`:** Chứa logic giả lập rating, không tương tác API, không cần thay đổi.
*   **`UtilityServices/`:** Các service này (`JsonConversionService`, `LocalizationService`, `ViewRenderService`) là các tiện ích chung, không gọi API MangaDex trực tiếp. `LocalizationService` đã được cập nhật trong bước của `MangaDetailsService`.

---

