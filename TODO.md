# TODO: Cải thiện xử lý lưu tiến độ đọc và tải danh sách theo dõi/lịch sử

## Mục 1: Ngăn chặn gửi request `SaveReadingProgress` nếu người dùng chưa đăng nhập

### Bước 1.1: Cập nhật Client-side (`MangaReader_WebUI\wwwroot\js\modules\read-page.js`)

**Mục tiêu:** Kiểm tra trạng thái đăng nhập của người dùng trước khi gửi request `SaveReadingProgress`. Nếu chưa đăng nhập, không thực hiện request.

**Cách thực hiện:**

1.  Mở file `MangaReader_WebUI\wwwroot\js\modules\read-page.js`.
2.  Tìm đến hàm hoặc đoạn code chịu trách nhiệm gửi request `SaveReadingProgress`. Hiện tại, việc này được thực hiện thông qua một div ẩn với các thuộc tính `hx-post`.
3.  Chúng ta sẽ không sửa trực tiếp div đó, mà sẽ đảm bảo rằng `IUserService` trên server đã kiểm tra. Logic client hiện tại không có hàm `saveReadingProgress` tường minh để can thiệp. Phần xử lý chính sẽ ở server.
4.  Tuy nhiên, để cẩn thận hơn và có thể mở rộng trong tương lai, chúng ta có thể thêm một hàm kiểm tra trạng thái đăng nhập global trong `auth.js` để các module khác có thể sử dụng nếu cần.

**Cập nhật `MangaReader_WebUI\wwwroot\js\auth.js`:**

Thêm hàm `isUserAuthenticated` vào `auth.js` để các module khác có thể gọi và kiểm tra.

```javascript
// MangaReader_WebUI\wwwroot\js\auth.js
/**
 * auth.js - Xử lý xác thực và quản lý thông tin người dùng (Module ES6)
 */

// Biến cục bộ để lưu trạng thái đăng nhập, cập nhật bởi checkAuthState
let currentUserIsAuthenticated = false;
let currentUserData = null;

/**
 * Khởi tạo UI xác thực
 * Hàm này gọi checkAuthState để kiểm tra trạng thái đăng nhập khi được gọi
 */
export function initAuthUI() {
    console.log('Auth module: Khởi tạo UI xác thực');
    checkAuthState();
}

/**
 * Kiểm tra trạng thái đăng nhập và cập nhật giao diện
 */
function checkAuthState() {
    fetch('/Auth/GetCurrentUser')
        .then(response => response.json())
        .then(data => {
            currentUserIsAuthenticated = data.isAuthenticated; // Cập nhật biến cục bộ
            currentUserData = data.user || null; // Cập nhật dữ liệu người dùng
            updateUserInterface(data);
        })
        .catch(error => {
            console.error('Lỗi khi kiểm tra trạng thái đăng nhập:', error);
            currentUserIsAuthenticated = false;
            currentUserData = null;
            updateUserInterface({ isAuthenticated: false });
        });
}

/**
 * Trả về trạng thái đăng nhập hiện tại (đồng bộ)
 * Lưu ý: Hàm này trả về trạng thái đã được cache từ lần gọi checkAuthState gần nhất.
 * Để có trạng thái chính xác nhất, nên gọi checkAuthState() và đợi nó hoàn thành nếu cần.
 * @returns {boolean} True nếu người dùng đã đăng nhập, ngược lại False.
 */
export function isUserAuthenticated() {
    return currentUserIsAuthenticated;
}

/**
 * Lấy thông tin người dùng hiện tại (đồng bộ)
 * @returns {Object|null} Thông tin người dùng hoặc null.
 */
export function getCurrentUserData() {
    return currentUserData;
}

/**
 * Cập nhật giao diện dựa trên trạng thái đăng nhập
 * @param {Object} data - Dữ liệu người dùng từ API
 */
function updateUserInterface(data) {
    // ... (giữ nguyên phần còn lại của hàm updateUserInterface)
// ...
    const guestUserMenu = document.getElementById('guestUserMenu');
    const authenticatedUserMenu = document.getElementById('authenticatedUserMenu');
    const userNameDisplay = document.getElementById('userNameDisplay');
    const userDropdownToggle = document.getElementById('userDropdownToggle');
    
    if (data.isAuthenticated && data.user) {
        // Người dùng đã đăng nhập
        if (guestUserMenu) guestUserMenu.classList.add('d-none');
        if (authenticatedUserMenu) authenticatedUserMenu.classList.remove('d-none');
        
        // Hiển thị tên người dùng
        if (userNameDisplay) {
            userNameDisplay.textContent = data.user.displayName;
            userNameDisplay.classList.remove('d-none');
        }
        
        // Hiển thị icon người dùng 
        if (userDropdownToggle) {
            const icon = userDropdownToggle.querySelector('.user-icon');
            if (icon) icon.style.display = '';
        }
        
    } else {
        // Người dùng chưa đăng nhập
        if (guestUserMenu) guestUserMenu.classList.remove('d-none');
        if (authenticatedUserMenu) authenticatedUserMenu.classList.add('d-none');
        
        // Ẩn tên người dùng
        if (userNameDisplay) userNameDisplay.classList.add('d-none');
        
        // Đảm bảo hiển thị icon mặc định
        if (userDropdownToggle) {
            const icon = userDropdownToggle.querySelector('.user-icon');
            if (icon) icon.style.display = '';
        }
    }
}

```

**Lưu ý cho `MangaReader_WebUI\wwwroot\js\modules\read-page.js`:**

Hiện tại, div `SaveReadingProgress` đang tự động gửi request khi `load`. Chúng ta sẽ dựa vào kiểm tra ở server-side controller. Nếu sau này bạn muốn thêm logic client-side để *hoàn toàn* chặn request trước khi gửi (ví dụ, để không tạo request không cần thiết lên server), bạn sẽ cần:

1.  Bỏ thuộc tính `hx-trigger="load"` khỏi div đó.
2.  Trong `initReadPage()` hoặc một hàm tương tự của `read-page.js`:
    *   Import `isUserAuthenticated` từ `auth.js`.
    *   Kiểm tra `if (isUserAuthenticated()) { ... }`.
    *   Nếu `true`, thì dùng `htmx.trigger('#yourDivId', 'saveProgressTrigger');` (bạn cần định nghĩa `saveProgressTrigger` trong `hx-trigger` của div, ví dụ `hx-trigger="saveProgressTrigger"`) để kích hoạt request.

Tuy nhiên, với yêu cầu hiện tại, việc kiểm tra ở server là đủ và an toàn hơn.

### Bước 1.2: Cập nhật Server-side (`MangaReader_WebUI\Controllers\ChapterController.cs`)

**Mục tiêu:** Đảm bảo action `SaveReadingProgress` trong `ChapterController` kiểm tra trạng thái đăng nhập và trả về lỗi 401 nếu chưa đăng nhập.

**Cách thực hiện:**

1.  Mở file `MangaReader_WebUI\Controllers\ChapterController.cs`.
2.  Trong phương thức `SaveReadingProgress`, thêm kiểm tra `_userService.IsAuthenticated()`.

**File cập nhật:** `MangaReader_WebUI\Controllers\ChapterController.cs`

```csharp
// MangaReader_WebUI\Controllers\ChapterController.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Controllers
{
    public class ChapterController : Controller
    {
        private readonly ILogger<ChapterController> _logger;
        private readonly ChapterReadingServices _chapterReadingServices;
        private readonly ViewRenderService _viewRenderService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;

        public ChapterController(
            ChapterReadingServices chapterReadingServices,
            ViewRenderService viewRenderService,
            ILogger<ChapterController> logger,
            IHttpClientFactory httpClientFactory,
            IUserService userService)
        {
            _chapterReadingServices = chapterReadingServices;
            _viewRenderService = viewRenderService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
        }

        // GET: Chapter/Read/5
        public async Task<IActionResult> Read(string id)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xử lý yêu cầu đọc chapter {id}");
                
                var viewModel = await _chapterReadingServices.GetChapterReadViewModel(id);
                
                // Sử dụng ViewRenderService để trả về view phù hợp với loại request
                return _viewRenderService.RenderViewBasedOnRequest(this, "Read", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}");
                ViewBag.ErrorMessage = $"Không thể tải chapter. Lỗi: {ex.Message}";
                return View("Read", new ChapterReadViewModel());
            }
        }
        
        // GET: Chapter/GetChapterImagesPartial/5
        public async Task<IActionResult> GetChapterImagesPartial(string id)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xử lý yêu cầu lấy ảnh cho chapter {id}");
                
                var viewModel = await _chapterReadingServices.GetChapterReadViewModel(id);
                
                return PartialView("_ChapterImagesPartial", viewModel.Pages);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải ảnh chapter: {ex.Message}");
                return PartialView("_ChapterImagesPartial", new List<string>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveReadingProgress(string mangaId, string chapterId)
        {
            _logger.LogInformation($"Nhận yêu cầu lưu tiến độ đọc: MangaId={mangaId}, ChapterId={chapterId}");

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lưu tiến độ.");
                // Trả về lỗi 401 Unauthorized nếu người dùng chưa đăng nhập
                // HTMX sẽ xử lý lỗi này dựa trên cấu hình của nó (mặc định là không làm gì)
                // hoặc có thể cấu hình hx-on::after-request="if(event.detail.failed && event.detail.xhr.status === 401) { // Xử lý ở đây }"
                return Unauthorized(new { message = "Vui lòng đăng nhập để lưu tiến độ đọc." });
            }

            var token = _userService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Không thể lấy token người dùng đã đăng nhập.");
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { mangaId = mangaId, lastChapter = chapterId };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/users/reading-progress", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Lưu tiến độ đọc thành công cho MangaId={mangaId}, ChapterId={chapterId}");
                    return Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi khi gọi API backend để lưu tiến độ đọc. Status: {response.StatusCode}, Content: {errorContent}");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken(); // Xóa token không hợp lệ
                        return Unauthorized(new { message = "Phiên đăng nhập hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại." });
                    }
                    return StatusCode((int)response.StatusCode, $"Lỗi từ backend: {response.ReasonPhrase} - {errorContent}");
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

## Mục 2: Bỏ qua truyện không tìm thấy trong Theo dõi/Lịch sử

### Bước 2.1: Cập nhật `FollowedMangaService.cs`

**Mục tiêu:** Khi lấy danh sách truyện đang theo dõi, nếu không tìm thấy thông tin của một manga (ví dụ, manga đã bị xóa ở nguồn), thì bỏ qua manga đó và tiếp tục xử lý các manga khác, không dừng lại hoặc báo lỗi toàn bộ.

**Cách thực hiện:**

1.  Mở file `MangaReader_WebUI\Services\MangaServices\FollowedMangaService.cs`.
2.  Trong vòng lặp `foreach (var mangaId in user.FollowingManga)`, sau khi gọi `_mangaInfoService.GetMangaInfoAsync(mangaId)`, kiểm tra xem `mangaInfo` có `null` không. Nếu `null`, ghi log cảnh báo và sử dụng `continue` để chuyển sang manga tiếp theo.

**File cập nhật:** `MangaReader_WebUI\Services\MangaServices\FollowedMangaService.cs`

```csharp
// MangaReader_WebUI\Services\MangaServices\FollowedMangaService.cs
using MangaReader.WebUI.Models.Auth;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService; 
        private readonly ChapterService _chapterService; 
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550); 
        private readonly IFollowedMangaViewModelMapper _followedMangaMapper;

        public FollowedMangaService(
            IUserService userService,
            IMangaInfoService mangaInfoService, 
            ChapterService chapterService,
            ILogger<FollowedMangaService> logger,
            IFollowedMangaViewModelMapper followedMangaMapper)
        {
            _userService = userService;
            _mangaInfoService = mangaInfoService; 
            _chapterService = chapterService;
            _logger = logger;
            // _rateLimitDelay = TimeSpan.FromMilliseconds(550); // Đã có sẵn
            _followedMangaMapper = followedMangaMapper;
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
                if (user == null || user.FollowingManga == null || !user.FollowingManga.Any())
                {
                    _logger.LogInformation("Người dùng không theo dõi manga nào.");
                    return followedMangaList;
                }

                _logger.LogInformation($"Người dùng đang theo dõi {user.FollowingManga.Count} manga. Bắt đầu lấy thông tin...");

                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        await Task.Delay(_rateLimitDelay);
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);

                        if (mangaInfo == null) 
                        {
                             _logger.LogWarning($"Không thể lấy thông tin cơ bản cho manga ID: {mangaId} trong danh sách theo dõi. Bỏ qua manga này.");
                             continue; // Bỏ qua manga này và tiếp tục vòng lặp
                        }

                        await Task.Delay(_rateLimitDelay);
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                        var followedMangaViewModel = _followedMangaMapper.MapToFollowedMangaViewModel(mangaInfo, latestChapters ?? new List<SimpleChapterInfo>());
                        followedMangaList.Add(followedMangaViewModel);
                        _logger.LogDebug($"Đã xử lý xong manga trong danh sách theo dõi: {mangaInfo.MangaTitle}");

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi.");
                        // Bỏ qua manga bị lỗi và tiếp tục với manga tiếp theo
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

### Bước 2.2: Cập nhật `ReadingHistoryService.cs`

**Mục tiêu:** Tương tự như `FollowedMangaService`, khi lấy lịch sử đọc, nếu không tìm thấy thông tin của manga hoặc chapter, bỏ qua mục đó và tiếp tục.

**Cách thực hiện:**

1.  Mở file `MangaReader_WebUI\Services\MangaServices\ReadingHistoryService.cs`.
2.  Trong vòng lặp `foreach (var item in backendHistory)`:
    *   Sau khi gọi `_mangaInfoService.GetMangaInfoAsync(item.MangaId)`, kiểm tra `mangaInfo == null`. Nếu `true`, log warning và `continue`.
    *   Sau khi gọi `_chapterApiService.FetchChapterInfoAsync(item.ChapterId)` (hoặc logic lấy thông tin chapter tương ứng), kiểm tra kết quả. Nếu không lấy được thông tin chapter, log warning và `continue`.

**File cập nhật:** `MangaReader_WebUI\Services\MangaServices\ReadingHistoryService.cs`

```csharp
// MangaReader_WebUI\Services\MangaServices\ReadingHistoryService.cs
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Services.MangaServices
{
    // Model để deserialize response từ backend /reading-history
    // Đã có sẵn
    // public class BackendHistoryItem
    // {
    //     [JsonPropertyName("mangaId")]
    //     public string MangaId { get; set; }

    //     [JsonPropertyName("chapterId")]
    //     public string ChapterId { get; set; }

    //     [JsonPropertyName("lastReadAt")]
    //     public DateTime LastReadAt { get; set; }
    // }

    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReadingHistoryService> _logger;
        private readonly TimeSpan _rateLimitDelay; 
        private readonly ILastReadMangaViewModelMapper _lastReadMapper;
        private readonly IChapterToSimpleInfoMapper _chapterSimpleInfoMapper; // Giữ lại để map chapter
        private readonly IMangaDataExtractor _mangaDataExtractor; // Giữ lại nếu _chapterSimpleInfoMapper cần
        private readonly IChapterApiService _chapterApiService; // Để lấy chi tiết chapter

        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IConfiguration configuration,
            ILogger<ReadingHistoryService> logger,
            ILastReadMangaViewModelMapper lastReadMapper,
            IChapterToSimpleInfoMapper chapterSimpleInfoMapper, // Giữ lại
            IMangaDataExtractor mangaDataExtractor, // Giữ lại
            IChapterApiService chapterApiService) // Thêm
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _configuration = configuration;
            _logger = logger;
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 550));
            _lastReadMapper = lastReadMapper;
            _chapterSimpleInfoMapper = chapterSimpleInfoMapper; // Giữ lại
            _mangaDataExtractor = mangaDataExtractor; // Giữ lại
            _chapterApiService = chapterApiService; // Gán
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
                        _userService.RemoveToken(); 
                    }
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
                    await Task.Delay(_rateLimitDelay);

                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho MangaId: {item.MangaId} trong lịch sử đọc. Bỏ qua mục này.");
                        continue; 
                    }

                    ChapterInfo chapterInfo = null;
                    try 
                    {
                        var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(item.ChapterId);
                        if (chapterResponse?.Result != "ok" || chapterResponse.Data == null)
                        {
                            _logger.LogWarning($"Không tìm thấy chapter với ID: {item.ChapterId} trong lịch sử đọc hoặc API lỗi. Bỏ qua mục này.");
                            continue; 
                        }
                        
                        // Sử dụng _chapterSimpleInfoMapper để lấy thông tin đơn giản
                        // Hoặc trực tiếp map từ chapterResponse.Data.Attributes nếu cần
                        var simpleChapter = _chapterSimpleInfoMapper.MapToSimpleChapterInfo(chapterResponse.Data);
                        chapterInfo = new ChapterInfo
                        {
                            Id = item.ChapterId,
                            Title = simpleChapter.DisplayTitle, // DisplayTitle đã được format
                            PublishedAt = simpleChapter.PublishedAt
                        };
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, $"Lỗi khi lấy thông tin chapter {item.ChapterId} trong lịch sử đọc. Bỏ qua mục này.");
                        continue; 
                    }
                    
                    if (chapterInfo == null) // Kiểm tra lại sau try-catch
                    {
                        _logger.LogWarning($"Thông tin Chapter cho ChapterId: {item.ChapterId} vẫn null sau khi thử lấy. Bỏ qua mục lịch sử này.");
                        continue; 
                    }

                    var historyViewModel = _lastReadMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfo, item.LastReadAt);
                    historyViewModels.Add(historyViewModel);
                    
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
}
```