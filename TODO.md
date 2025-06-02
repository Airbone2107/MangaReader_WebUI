Để điều chỉnh bộ lọc mặc định cho `contentRating` dựa trên nguồn truyện và trang hiện tại, chúng ta cần thực hiện các thay đổi ở cả phía backend (Controller và Service của WebUI) và frontend (JavaScript).

Dưới đây là các bước chi tiết:

**Bước 1: Cập nhật Model `SortManga`**

Gỡ bỏ giá trị mặc định của `ContentRating` trong constructor để việc quyết định giá trị mặc định được thực hiện ở tầng Controller/Service.

```csharp
// MangaReader_WebUI\Models\MangaDexModels.cs
// ...
    public class SortManga
    {
        // ...
        public List<string> ContentRating { get; set; } = new List<string>(); // Khởi tạo rỗng
        // ...

        public SortManga()
        {
            // Giá trị mặc định
            Title = "";
            Status = new List<string>();
            Safety = "";
            Demographic = new List<string>();
            SortBy = "latest";
            IncludedTags = new List<string>();
            ExcludedTags = new List<string>();
            Languages = new List<string>();
            Genres = new List<string>();
            // ContentRating = new List<string>() { "safe", "suggestive", "erotica"}; // Gỡ bỏ dòng này
            Authors = new List<string>();
            Artists = new List<string>();
            OriginalLanguage = new List<string>();
            ExcludedOriginalLanguage = new List<string>();
        }
        // ...
    }
// ...
```

**Bước 2: Điều chỉnh `HomeController` cho trang chủ**

Khi lấy truyện mới nhất cho trang chủ, chúng ta sẽ áp dụng logic sau:
*   Nếu nguồn là `MangaDex`, `ContentRating` = `{"safe"}`.
*   Nếu nguồn là `MangaReaderLib`, `ContentRating` sẽ là danh sách rỗng (không lọc).

```csharp
// MangaReader_WebUI\Controllers\HomeController.cs
// ...
using MangaReader.WebUI.Enums; // Thêm using này

namespace MangaReader.WebUI.Controllers
{
    public class HomeController : Controller
    {
        // ... (giữ nguyên các dependencies và constructor)

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["PageType"] = "home";
                bool isConnected = await _apiStatusService.TestConnectionAsync();
                ViewBag.IsConnected = isConnected;

                if (!isConnected)
                {
                    _logger.LogWarning("Không thể kết nối đến API");
                    ViewBag.ErrorMessage = "Không thể kết nối đến API. Vui lòng thử lại sau.";
                    return View("Index", new List<MangaViewModel>());
                }
                
                var sortOptions = new SortManga
                {
                    SortBy = "Mới cập nhật",
                    Languages = new List<string> { "vi", "en" }
                };

                // Lấy nguồn truyện hiện tại từ cookie
                var currentSource = HttpContext.Request.Cookies.TryGetValue("MangaSource", out var sourceString) &&
                                    Enum.TryParse(sourceString, true, out MangaSource sourceEnum)
                                    ? sourceEnum
                                    : MangaSource.MangaDex; // Mặc định là MangaDex

                _logger.LogInformation("Trang chủ: Nguồn truyện hiện tại là {MangaSource}", currentSource);

                if (currentSource == MangaSource.MangaDex)
                {
                    sortOptions.ContentRating = new List<string> { "safe" };
                    _logger.LogInformation("Trang chủ (MangaDex): Áp dụng ContentRating = 'safe'");
                }
                else // MangaSource.MangaReaderLib
                {
                    sortOptions.ContentRating = new List<string>(); // Không áp dụng filter content rating
                    _logger.LogInformation("Trang chủ (MangaReaderLib): Không áp dụng ContentRating filter");
                }
                
                var recentMangaResponse = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);

                if (recentMangaResponse?.Data == null || !recentMangaResponse.Data.Any())
                {
                    _logger.LogWarning("API đã kết nối nhưng không trả về dữ liệu manga cho trang chủ");
                    ViewBag.ErrorMessage = "Không có dữ liệu manga. Vui lòng thử lại sau.";
                    return View("Index", new List<MangaViewModel>());
                }

                var viewModels = new List<MangaViewModel>();
                foreach (var manga in recentMangaResponse.Data)
                {
                    try
                    {
                        var viewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(manga);
                        viewModels.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi map manga ID: {manga?.Id} trên trang chủ.");
                    }
                }

                if (viewModels.Count == 0)
                {
                    ViewBag.ErrorMessage = "Không thể hiển thị dữ liệu manga. Định dạng dữ liệu không hợp lệ.";
                }

                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("Index", viewModels);
                }
                return View("Index", viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang chủ");
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View("Index", new List<MangaViewModel>());
            }
        }

        // ... (các actions khác giữ nguyên)
    }
}
```

**Bước 3: Điều chỉnh `MangaSearchService` cho trang tìm kiếm**

Trang tìm kiếm sẽ luôn mặc định là "safe" cho cả hai nguồn nếu người dùng không chọn gì.

```csharp
// MangaReader_WebUI\Services\MangaServices\MangaPageService\MangaSearchService.cs
// ...
        public SortManga CreateSortMangaFromParameters(
            string title = "", 
            List<string> status = null, 
            string sortBy = "latest", // Mặc định là "latest" ở đây
            string authors = "",
            string artists = "",
            int? year = null,
            List<string> availableTranslatedLanguage = null,
            List<string> publicationDemographic = null,
            List<string> contentRating = null, // Nhận contentRating từ controller
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            List<string> genres = null,
            string includedTagsStr = "",
            string excludedTagsStr = "")
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
                Genres = genres
            };

            // ... (xử lý authors, artists, languages giữ nguyên)

            // Xử lý danh sách đánh giá nội dung
            if (contentRating != null && contentRating.Any())
            {
                sortManga.ContentRating = contentRating;
                _logger.LogInformation($"Tìm kiếm với mức độ nội dung người dùng chọn: {string.Join(", ", sortManga.ContentRating)}");
            }
            else
            {
                // Mặc định của trang Search: chỉ "safe" cho cả hai nguồn
                sortManga.ContentRating = new List<string> { "safe" };
                _logger.LogInformation($"Tìm kiếm với mức độ nội dung mặc định (Search Page): safe");
            }
            
            // ... (xử lý includedTags, excludedTags giữ nguyên)

            return sortManga;
        }
// ...
```

**Bước 4: Cập nhật `MangaApiService` (cho MangaDex) để không tự thêm `contentRating`**

`MangaApiService` không nên tự thêm `contentRating` mặc định nữa, vì việc này đã được xử lý ở `HomeController` hoặc `MangaSearchService`.

```csharp
// MangaReader_WebUI\Services\APIServices\Services\MangaApiService.cs
// ...
        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            // ... (logging giữ nguyên)
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
                        // Sửa chỗ này để AddOrUpdateParam nhận List<string>
                        if (!queryParams.ContainsKey(param.Key))
                        {
                            queryParams[param.Key] = new List<string>();
                        }
                        foreach(var val in values)
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
                 // Chỉ thêm contentRating[] nếu nó được cung cấp trong sortManga
                if (sortManga.ContentRating != null && sortManga.ContentRating.Any())
                {
                    if (!queryParams.ContainsKey("contentRating[]"))
                    {
                        queryParams["contentRating[]"] = new List<string>();
                    }
                    queryParams["contentRating[]"].AddRange(sortManga.ContentRating);
                }
            }
            else // Nếu không có sortManga (ví dụ: gọi từ nơi khác không có filter UI)
            {
                // Chỉ thêm order mặc định, không thêm contentRating
                 AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
            }

            // Luôn bao gồm các relationship cần thiết
            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "includes[]", "author");
            AddOrUpdateParam(queryParams, "includes[]", "artist");

            var url = BuildUrlWithParams("manga", queryParams);
            // ... (phần còn lại của hàm giữ nguyên)
            var mangaList = await GetApiAsync<MangaList>(url);
            // ...
            return mangaList;
        }
// ...
```
Lưu ý: Hàm `AddOrUpdateParam` trong `BaseApiService` cần được điều chỉnh để có thể nhận `List<string>` cho `value` hoặc bạn cần lặp qua `sortManga.ContentRating` và gọi `AddOrUpdateParam` cho từng item nếu `param.Key` là `contentRating[]`. Cách đơn giản hơn là xử lý trực tiếp trong `MangaApiService` như trên.