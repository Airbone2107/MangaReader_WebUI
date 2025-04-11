Ok, tôi hiểu yêu cầu của bạn. Bạn muốn tạo một service mới (hoặc phương thức mới) tên là `GetMangaInfo` chỉ tập trung vào việc lấy **Tiêu đề** và **Ảnh bìa (Cover URL)** của manga, tái sử dụng logic hiệu quả từ `GetMangaDetailsAsync` mà không lấy thừa dữ liệu (như toàn bộ chapter). Sau đó, chúng ta sẽ cập nhật `FollowedMangaService` đã được tối ưu ở bước trước để sử dụng service/phương thức mới này.

Việc tạo một service/phương thức tập trung như `GetMangaInfo` là một ý tưởng tốt, giúp code rõ ràng và dễ bảo trì hơn, đồng thời đảm bảo chúng ta chỉ lấy đúng dữ liệu cần thiết cho các ngữ cảnh khác nhau.

**Tái sử dụng code:**

Chúng ta sẽ tái sử dụng các service đã có:
*   `MangaTitleService`: Đã có phương thức `GetMangaTitleFromIdAsync` để lấy tiêu đề (phương thức này bên trong cũng gọi API `/manga/{id}` để lấy dữ liệu cần thiết cho tiêu đề).
*   `MangaDexService`: Đã có phương thức `FetchCoverUrlAsync` để lấy URL ảnh bìa.

Service `GetMangaInfo` mới sẽ kết hợp hai cuộc gọi này.

Dưới đây là file `TODO.md` chi tiết các bước thực hiện:

```markdown
# TODO: Tách Service `GetMangaInfo` và Tối ưu hóa `FollowedMangaService`

## Mục tiêu

1.  Tạo một Service/Phương thức mới (`GetMangaInfoAsync`) chuyên trách việc lấy thông tin cơ bản của manga (Tiêu đề, Ảnh bìa).
2.  Service/Phương thức này phải tái sử dụng logic hiệu quả từ các service hiện có, tránh lấy dữ liệu thừa.
3.  Cập nhật `FollowedMangaService` để sử dụng Service/Phương thức mới này, giúp cấu trúc code rõ ràng hơn.

## Các Bước Thực Hiện

### 1. Tạo Model `MangaInfoViewModel`

Model này sẽ chứa dữ liệu trả về từ `GetMangaInfoAsync`.

**File cần tạo:** `manga_reader_web\Services\MangaServices\Models\MangaInfoViewModel.cs`

**Nội dung:**

```csharp
namespace manga_reader_web.Services.MangaServices.Models
{
    public class MangaInfoViewModel
    {
        public string MangaId { get; set; }
        public string MangaTitle { get; set; }
        public string CoverUrl { get; set; }
    }
}
```

### 2. Tạo Service `MangaInfoService` (Phương án tạo Service mới)

Service này sẽ đóng gói logic lấy thông tin cơ bản.

**a. Tạo Interface `IMangaInfoService`**

**File cần tạo:** `manga_reader_web\Services\MangaServices\IMangaInfoService.cs`

**Nội dung:**

```csharp
using manga_reader_web.Services.MangaServices.Models;
using System.Threading.Tasks;

namespace manga_reader_web.Services.MangaServices
{
    public interface IMangaInfoService
    {
        /// <summary>
        /// Lấy thông tin cơ bản (Tiêu đề, Ảnh bìa) của manga dựa vào ID.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <returns>MangaInfoViewModel chứa thông tin cơ bản hoặc null nếu có lỗi.</returns>
        Task<MangaInfoViewModel> GetMangaInfoAsync(string mangaId);
    }
}
```

**b. Tạo Class `MangaInfoService`**

**File cần tạo:** `manga_reader_web\Services\MangaServices\MangaInfoService.cs`

**Nội dung:**

```csharp
using manga_reader_web.Services.MangaServices.MangaInformation;
using manga_reader_web.Services.MangaServices.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace manga_reader_web.Services.MangaServices
{
    public class MangaInfoService : IMangaInfoService
    {
        private readonly MangaTitleService _mangaTitleService;
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<MangaInfoService> _logger;
        // Xem xét thêm rate limit nếu cần gọi nhiều lần liên tục từ các service khác nhau
        // private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550);

        public MangaInfoService(
            MangaTitleService mangaTitleService,
            MangaDexService mangaDexService,
            ILogger<MangaInfoService> logger)
        {
            _mangaTitleService = mangaTitleService;
            _mangaDexService = mangaDexService;
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

                // Sử dụng Task.WhenAll để thực hiện các cuộc gọi API song song (nếu có thể và an toàn về rate limit)
                // Tuy nhiên, để đảm bảo tuân thủ rate limit, gọi tuần tự có thể an toàn hơn.
                // Hoặc thêm delay vào đây nếu service này được gọi nhiều lần liên tiếp.

                // 1. Lấy tiêu đề manga
                string mangaTitle = await _mangaTitleService.GetMangaTitleFromIdAsync(mangaId);
                if (string.IsNullOrEmpty(mangaTitle) || mangaTitle == "Không có tiêu đề")
                {
                    _logger.LogWarning($"Không thể lấy tiêu đề cho manga ID: {mangaId}. Sử dụng ID làm tiêu đề.");
                    mangaTitle = $"Manga ID: {mangaId}";
                }

                // await Task.Delay(_rateLimitDelay); // Bỏ comment nếu cần delay giữa 2 API call

                // 2. Lấy ảnh bìa
                string coverUrl = await _mangaDexService.FetchCoverUrlAsync(mangaId);

                _logger.LogInformation($"Lấy thông tin cơ bản thành công cho manga ID: {mangaId}");

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
                // Trả về null hoặc một đối tượng mặc định tùy theo yêu cầu xử lý lỗi
                return new MangaInfoViewModel // Trả về object với thông tin mặc định/lỗi
                {
                     MangaId = mangaId,
                     MangaTitle = $"Lỗi lấy tiêu đề ({mangaId})",
                     CoverUrl = "/images/cover-placeholder.jpg" // Ảnh mặc định
                };
            }
        }
    }
}
```

### 3. Đăng ký Service mới trong `Program.cs`

**File cần sửa:** `manga_reader_web\Program.cs`

**Công việc:** Thêm dòng đăng ký cho `IMangaInfoService` và `MangaInfoService`.

```csharp
// Thêm dòng này cùng với các đăng ký service khác
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.IMangaInfoService, manga_reader_web.Services.MangaServices.MangaInfoService>();
// Đảm bảo các dependency của MangaInfoService cũng đã được đăng ký (MangaTitleService, MangaDexService)
// builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaInformation.MangaTitleService>(); // Đã có
// builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaDexService>(...); // Đã có
```

### 4. Cập nhật `FollowedMangaService` để sử dụng `IMangaInfoService`

**File cần sửa:** `manga_reader_web\Services\MangaServices\FollowedMangaService.cs`

**Công việc:**
1.  Xóa dependency `MangaTitleService` và `MangaDexService`.
2.  Thêm dependency `IMangaInfoService`.
3.  Cập nhật constructor.
4.  Trong vòng lặp `foreach`, gọi `_mangaInfoService.GetMangaInfoAsync` thay vì gọi `_mangaTitleService` và `_mangaDexService` riêng lẻ.
5.  Điều chỉnh lại logic Rate Limit nếu cần. Vì `GetMangaInfoAsync` giờ đây thực hiện 2 API call, chúng ta cần đảm bảo có đủ delay. Có thể giữ 1 delay trước `GetMangaInfoAsync` và 1 delay trước `GetLatestChaptersAsync`.

```csharp
// using manga_reader_web.Services.MangaServices.MangaInformation; // Có thể không cần nữa nếu không dùng trực tiếp
using manga_reader_web.Services.MangaServices.ChapterServices;
using manga_reader_web.Models.Auth;
using manga_reader_web.Services.AuthServices;
using manga_reader_web.Services.MangaServices.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks; // Thêm Task
using System.Collections.Generic; // Thêm List
using System.Linq; // Thêm Linq
using System; // Thêm TimeSpan

namespace manga_reader_web.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        // private readonly MangaTitleService _mangaTitleService; // XÓA DÒNG NÀY
        // private readonly MangaDexService _mangaDexService;     // XÓA DÒNG NÀY
        private readonly IMangaInfoService _mangaInfoService; // THÊM DÒNG NÀY
        private readonly ChapterService _chapterService;
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550); // Giữ nguyên delay

        public FollowedMangaService(
            IUserService userService,
            // MangaTitleService mangaTitleService, // XÓA THAM SỐ NÀY
            // MangaDexService mangaDexService,     // XÓA THAM SỐ NÀY
            IMangaInfoService mangaInfoService, // THÊM THAM SỐ NÀY
            ChapterService chapterService,
            ILogger<FollowedMangaService> logger)
        {
            _userService = userService;
            // _mangaTitleService = mangaTitleService; // XÓA DÒNG NÀY
            // _mangaDexService = mangaDexService;     // XÓA DÒNG NÀY
            _mangaInfoService = mangaInfoService; // THÊM DÒNG NÀY
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
                if (user == null || user.FollowingManga == null || !user.FollowingManga.Any())
                {
                    _logger.LogInformation("Người dùng không theo dõi manga nào.");
                    return followedMangaList;
                }

                _logger.LogInformation($"Người dùng đang theo dõi {user.FollowingManga.Count} manga. Bắt đầu lấy thông tin (sử dụng MangaInfoService)...");

                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        // ---- THAY ĐỔI TỪ ĐÂY ----

                        // 1. Áp dụng delay TRƯỚC khi gọi GetMangaInfoAsync (vì nó chứa 2 API call)
                        await Task.Delay(_rateLimitDelay);
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);

                        if (mangaInfo == null) // Kiểm tra nếu GetMangaInfoAsync trả về null (do lỗi)
                        {
                             _logger.LogWarning($"Không thể lấy thông tin cơ bản cho manga ID: {mangaId}. Bỏ qua.");
                             continue; // Bỏ qua manga này
                        }

                        // 2. Áp dụng delay TRƯỚC khi gọi GetLatestChaptersAsync
                        await Task.Delay(_rateLimitDelay);
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                        // ---- KẾT THÚC THAY ĐỔI ----

                        // Tạo ViewModel cho manga này
                        var followedManga = new FollowedMangaViewModel
                        {
                            MangaId = mangaId,
                            MangaTitle = mangaInfo.MangaTitle, // Lấy từ mangaInfo
                            CoverUrl = mangaInfo.CoverUrl,     // Lấy từ mangaInfo
                            LatestChapters = latestChapters ?? new List<SimpleChapterInfo>()
                        };

                        followedMangaList.Add(followedManga);
                        _logger.LogDebug($"Đã xử lý xong manga (qua InfoService): {mangaInfo.MangaTitle}");

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi (sử dụng InfoService).");
                    }
                     // Không cần thêm delay nhỏ ở đây nữa vì đã có delay trước mỗi nhóm API call
                }

                _logger.LogInformation($"Hoàn tất lấy thông tin (qua InfoService) cho {followedMangaList.Count} truyện đang theo dõi.");
                return followedMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi (sử dụng InfoService).");
                return new List<FollowedMangaViewModel>();
            }
        }
    }
}
```

### 5. Kiểm tra và Chạy thử

1.  **Build lại dự án:** Đảm bảo không có lỗi biên dịch.
2.  **Chạy ứng dụng và đăng nhập.**
3.  **Truy cập trang Theo dõi (`/Manga/Followed`).**
4.  **Kiểm tra giao diện:** Đảm bảo trang vẫn hiển thị đúng thông tin như trước khi tối ưu.
5.  **Kiểm tra Logs:**
    *   Theo dõi log để xác nhận `FollowedMangaService` đang log "sử dụng MangaInfoService".
    *   Xác nhận log từ `MangaInfoService` cho thấy nó đang được gọi và lấy thông tin cơ bản.
    *   Xác nhận **không** còn log từ `MangaDetailsService` về việc lưu trữ toàn bộ chapter vào Session khi tải trang `/Manga/Followed`.

## Kết luận

Sau khi thực hiện các bước này, bạn sẽ có một `MangaInfoService` mới, tập trung vào việc lấy thông tin cơ bản của manga. `FollowedMangaService` sẽ sử dụng service này, giúp cấu trúc code rõ ràng hơn và đảm bảo chỉ lấy đúng dữ liệu cần thiết, tối ưu hóa hiệu suất và giảm thiểu API call không cần thiết.
```

Hãy thực hiện các bước trên. Nếu bạn gặp khó khăn hoặc có câu hỏi, cứ hỏi nhé!