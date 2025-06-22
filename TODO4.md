# TODO: Sửa Lỗi và Hoàn Tất Tái Cấu Trúc

Tài liệu này hướng dẫn sửa các lỗi biên dịch sau khi dọn dẹp mã nguồn và tái cấu trúc dự án.

## Bước 1: Tạo Lại Các File Interface và Service Đã Bị Xóa

Chúng ta sẽ tạo lại các file interface và service tiện ích cần thiết. Các file này không phụ thuộc vào nguồn dữ liệu cụ thể nào mà phục vụ cho logic của `ViewModel`.

### 1.1. Tạo lại Interface và Service cho `FollowedManga`

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaMapper\IFollowedMangaViewModelMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper
{
    /// <summary>
    /// Định nghĩa phương thức để chuyển đổi thông tin Manga và danh sách Chapter thành FollowedMangaViewModel.
    /// </summary>
    public interface IFollowedMangaViewModelMapper
    {
        /// <summary>
        /// Chuyển đổi thông tin Manga và danh sách Chapter thành FollowedMangaViewModel (dùng cho trang đang theo dõi).
        /// </summary>
        /// <param name="mangaInfo">Thông tin cơ bản của Manga.</param>
        /// <param name="latestChapters">Danh sách các chapter mới nhất (dạng SimpleChapterInfo).</param>
        /// <returns>FollowedMangaViewModel.</returns>
        FollowedMangaViewModel MapToFollowedMangaViewModel(MangaInfoViewModel mangaInfo, List<SimpleChapterInfoViewModel> latestChapters);
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaMapper\FollowedMangaViewModelMapperService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper
{
    /// <summary>
    /// Triển khai IFollowedMangaViewModelMapper, chịu trách nhiệm chuyển đổi thông tin Manga và Chapter thành FollowedMangaViewModel.
    /// </summary>
    public class FollowedMangaViewModelMapperService : IFollowedMangaViewModelMapper
    {
        public FollowedMangaViewModel MapToFollowedMangaViewModel(MangaInfoViewModel mangaInfo, List<SimpleChapterInfoViewModel> latestChapters)
        {
            Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành FollowedMangaViewModel.");
            Debug.Assert(latestChapters != null, "latestChapters không được null khi mapping thành FollowedMangaViewModel.");

            return new FollowedMangaViewModel
            {
                MangaId = mangaInfo.MangaId,
                MangaTitle = mangaInfo.MangaTitle,
                CoverUrl = mangaInfo.CoverUrl,
                LatestChapters = latestChapters
            };
        }
    }
}
```

### 1.2. Tạo lại Interface và Service cho `LastReadManga`

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaMapper\ILastReadMangaViewModelMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using System;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper
{
    /// <summary>
    /// Định nghĩa phương thức để chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel.
    /// </summary>
    public interface ILastReadMangaViewModelMapper
    {
        /// <summary>
        /// Chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel (dùng cho lịch sử đọc).
        /// </summary>
        /// <param name="mangaInfo">Thông tin cơ bản của Manga.</param>
        /// <param name="chapterInfo">Thông tin cơ bản của Chapter.</param>
        /// <param name="lastReadAt">Thời điểm đọc cuối.</param>
        /// <returns>LastReadMangaViewModel.</returns>
        LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfoViewModel chapterInfo, DateTime lastReadAt);
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaMapper\LastReadMangaViewModelMapperService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper
{
    /// <summary>
    /// Triển khai ILastReadMangaViewModelMapper, chịu trách nhiệm chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel.
    /// </summary>
    public class LastReadMangaViewModelMapperService : ILastReadMangaViewModelMapper
    {
        public LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfoViewModel chapterInfo, DateTime lastReadAt)
        {
            Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành LastReadMangaViewModel.");
            Debug.Assert(chapterInfo != null, "chapterInfo không được null khi mapping thành LastReadMangaViewModel.");

            return new LastReadMangaViewModel
            {
                MangaId = mangaInfo.MangaId,
                MangaTitle = mangaInfo.MangaTitle,
                CoverUrl = mangaInfo.CoverUrl,
                ChapterId = chapterInfo.Id,
                ChapterTitle = chapterInfo.Title,
                ChapterPublishedAt = chapterInfo.PublishedAt,
                LastReadAt = lastReadAt
            };
        }
    }
}
```

## Bước 2: Cập Nhật Các File Đang Gặp Lỗi

Bây giờ, chúng ta sẽ cập nhật toàn bộ nội dung của các file đã báo lỗi với mã nguồn đã được sửa.

### 2.1. Cập nhật `LocalizationService.cs`

<!-- MangaReader_WebUI\Services\UtilityServices\LocalizationService.cs -->
```csharp
using System.Diagnostics;
using System.Text.Json;

namespace MangaReader.WebUI.Services.UtilityServices
{
    public class LocalizationService
    {
        /// <summary>
        /// Lấy trạng thái đã dịch từ chuỗi status
        /// </summary>
        public string GetStatus(string? status)
        {
            if (string.IsNullOrEmpty(status)) return "Không rõ";
            
            return status.ToLowerInvariant() switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }
    }
}
```

### 2.2. Cập nhật `ReadingHistoryService.cs`

<!-- MangaReader_WebUI\Services\MangaServices\ReadingHistoryService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // Thêm using này
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices
{
    // Model để deserialize response từ backend /reading-history
    public class BackendHistoryItem
    {
        [JsonPropertyName("mangaId")]
        public string MangaId { get; set; }

        [JsonPropertyName("chapterId")]
        public string ChapterId { get; set; }

        [JsonPropertyName("lastReadAt")]
        public DateTime LastReadAt { get; set; }
    }

    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly ILogger<ReadingHistoryService> _logger;
        private readonly TimeSpan _rateLimitDelay;
        private readonly ILastReadMangaViewModelMapper _lastReadMapper;
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly IMangaReaderLibToChapterInfoMapper _chapterInfoMapper;
        private readonly IMangaReaderLibTranslatedMangaClient _translatedMangaClient;


        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IConfiguration configuration,
            ILogger<ReadingHistoryService> logger,
            ILastReadMangaViewModelMapper lastReadMapper,
            IMangaReaderLibChapterClient chapterClient,
            IMangaReaderLibToChapterInfoMapper chapterInfoMapper,
            IMangaReaderLibTranslatedMangaClient translatedMangaClient)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _logger = logger;
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 250));
            _lastReadMapper = lastReadMapper;
            _chapterClient = chapterClient;
            _chapterInfoMapper = chapterInfoMapper;
            _translatedMangaClient = translatedMangaClient;
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
                    _logger.LogError("Lỗi khi gọi API backend lấy lịch sử đọc. Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
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

                _logger.LogInformation("Nhận được {Count} mục lịch sử từ backend. Bắt đầu lấy chi tiết...", backendHistory.Count);

                foreach (var item in backendHistory)
                {
                    await Task.Delay(_rateLimitDelay);

                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning("Không thể lấy thông tin cho MangaId: {MangaId} trong lịch sử đọc. Bỏ qua mục này.", item.MangaId);
                        continue;
                    }

                    ChapterInfoViewModel? chapterInfoViewModel = null;
                    try
                    {
                        if (!Guid.TryParse(item.ChapterId, out var chapterGuid))
                        {
                             _logger.LogWarning("ChapterId không hợp lệ: {ChapterId}. Bỏ qua.", item.ChapterId);
                            continue;
                        }
                        
                        var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);
                        if (chapterResponse?.Data == null)
                        {
                            _logger.LogWarning("Không tìm thấy chapter với ID: {ChapterId} trong lịch sử đọc hoặc API lỗi. Bỏ qua mục này.", item.ChapterId);
                            continue;
                        }

                        var tmRel = chapterResponse.Data.Relationships?.FirstOrDefault(r => r.Type == "translated_manga");
                        string langKey = "en"; // Mặc định
                        if (tmRel != null && Guid.TryParse(tmRel.Id, out var tmGuid))
                        {
                            var tmResponse = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                            if (!string.IsNullOrEmpty(tmResponse?.Data?.Attributes?.LanguageKey))
                            {
                                langKey = tmResponse.Data.Attributes.LanguageKey;
                            }
                        }

                        chapterInfoViewModel = _chapterInfoMapper.MapToChapterInfo(chapterResponse.Data, langKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lấy thông tin chapter {ChapterId} trong lịch sử đọc. Bỏ qua mục này.", item.ChapterId);
                        continue;
                    }

                    if (chapterInfoViewModel == null)
                    {
                        _logger.LogWarning("Thông tin Chapter cho ChapterId: {ChapterId} vẫn null sau khi thử lấy. Bỏ qua mục lịch sử này.", item.ChapterId);
                        continue;
                    }

                    var historyViewModel = _lastReadMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfoViewModel, item.LastReadAt);
                    historyViewModels.Add(historyViewModel);

                    _logger.LogDebug("Đã xử lý xong mục lịch sử cho manga: {MangaTitle}, chapter: {ChapterTitle}", mangaInfo.MangaTitle, chapterInfoViewModel.Title);
                }

                _logger.LogInformation("Hoàn tất xử lý {Count} mục lịch sử đọc.", historyViewModels.Count);
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

### 2.3. Cập nhật `FollowedMangaService.cs`

<!-- MangaReader_WebUI\Services\MangaServices\FollowedMangaService.cs -->
```csharp
using MangaReader.WebUI.Models.Auth;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

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

                _logger.LogInformation("Người dùng đang theo dõi {Count} manga. Bắt đầu lấy thông tin...", user.FollowingManga.Count);

                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        await Task.Delay(_rateLimitDelay);
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);

                        if (mangaInfo == null)
                        {
                             _logger.LogWarning("Không thể lấy thông tin cơ bản cho manga ID: {MangaId}. Bỏ qua.", mangaId);
                             continue; 
                        }

                        await Task.Delay(_rateLimitDelay);
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                        var followedMangaViewModel = _followedMangaMapper.MapToFollowedMangaViewModel(mangaInfo, latestChapters ?? new List<SimpleChapterInfoViewModel>());
                        followedMangaList.Add(followedMangaViewModel);
                        _logger.LogDebug("Đã xử lý xong manga: {MangaTitle}", mangaInfo.MangaTitle);

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, "Lỗi khi xử lý manga ID: {MangaId} trong danh sách theo dõi.", mangaId);
                    }
                }

                _logger.LogInformation("Hoàn tất lấy thông tin cho {Count} truyện đang theo dõi.", followedMangaList.Count);
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

## Bước 3: Cập nhật `Program.cs` để đăng ký các Service mới

Bây giờ chúng ta cần đăng ký các interface và service vừa tạo lại vào container DI.

<!-- MangaReader_WebUI\Program.cs -->
```csharp
using MangaReader.WebUI.Infrastructure;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaPageService;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc.Razor;
using MangaReaderLib.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Thêm cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Cấu hình logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Đảm bảo HttpContextAccessor được đăng ký
builder.Services.AddHttpContextAccessor();

// Cấu hình HttpClient để gọi Backend API (CHỈ DÙNG CHO XÁC THỰC)
builder.Services.AddHttpClient("BackendApiClient", client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Thêm HttpClient cho MangaReaderLib API
builder.Services.AddHttpClient("MangaReaderLibApiClient", client =>
{
    var baseUrl = builder.Configuration["MangaReaderApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Đăng ký các MangaReaderLib Mappers
builder.Services.AddScoped<IMangaReaderLibToMangaViewModelMapper, MangaReaderLibToMangaViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaDetailViewModelMapper, MangaReaderLibToMangaDetailViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterViewModelMapper, MangaReaderLibToChapterViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToSimpleChapterInfoMapper, MangaReaderLibToSimpleChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaInfoViewModelMapper, MangaReaderLibToMangaInfoViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterInfoMapper, MangaReaderLibToChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToTagListResponseMapper, MangaReaderLibToTagListResponseMapper>();
builder.Services.AddScoped<IMangaReaderLibToAtHomeServerResponseMapper, MangaReaderLibToAtHomeServerResponseMapper>();

// Đăng ký các Mappers cấp cao (ViewModel-to-ViewModel)
builder.Services.AddScoped<IFollowedMangaViewModelMapper, FollowedMangaViewModelMapperService>();
builder.Services.AddScoped<ILastReadMangaViewModelMapper, LastReadMangaViewModelMapperService>();


// Đăng ký MangaDataExtractor
builder.Services.AddScoped<IMangaDataExtractor, MangaDataExtractorService>();

// Đăng ký các MangaReaderLib API Clients
builder.Services.AddScoped<IMangaReaderLibApiClient, MangaReaderLibApiClientService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("MangaReaderLibApiClient");
    var innerClientLogger = provider.GetRequiredService<ILogger<ApiClient>>();
    var wrapperLoggerForApiClient = provider.GetRequiredService<ILogger<MangaReaderLibApiClientService>>();
    return new MangaReaderLibApiClientService(httpClient, innerClientLogger, wrapperLoggerForApiClient);
});
builder.Services.AddScoped<IMangaReaderLibAuthorClient, MangaReaderLibAuthorClientService>();
builder.Services.AddScoped<IMangaReaderLibChapterClient, MangaReaderLibChapterClientService>();
builder.Services.AddScoped<IMangaReaderLibChapterPageClient, MangaReaderLibChapterPageClientService>();
builder.Services.AddScoped<IMangaReaderLibCoverApiService, MangaReaderLibCoverApiService>();
builder.Services.AddScoped<IMangaReaderLibMangaClient, MangaReaderLibMangaClientService>();
builder.Services.AddScoped<IMangaReaderLibTagClient, MangaReaderLibTagClientService>();
builder.Services.AddScoped<IMangaReaderLibTagGroupClient, MangaReaderLibTagGroupClientService>();
builder.Services.AddScoped<IMangaReaderLibTranslatedMangaClient, MangaReaderLibTranslatedMangaClientService>();

// Đăng ký các service liên quan đến xác thực
builder.Services.AddScoped<IUserService, UserService>();

// Đăng ký các service tiện ích
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<JsonConversionService>();
builder.Services.AddScoped<ViewRenderService>();

// Đăng ký các service tầng cao của ứng dụng
builder.Services.AddScoped<IMangaFollowService, MangaFollowService>();
builder.Services.AddScoped<IFollowedMangaService, FollowedMangaService>();
builder.Services.AddScoped<IMangaInfoService, MangaInfoService>();
builder.Services.AddScoped<IReadingHistoryService, ReadingHistoryService>();
builder.Services.AddScoped<ChapterService>();
builder.Services.AddScoped<MangaIdService>();
builder.Services.AddScoped<ChapterLanguageServices>();
builder.Services.AddScoped<ChapterReadingServices>();
builder.Services.AddScoped<MangaDetailsService>();
builder.Services.AddScoped<MangaSearchService>();

// Cấu hình Razor View Engine để sử dụng View Location Expander tùy chỉnh
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "auth_callback",
    pattern: "auth/callback",
    defaults: new { controller = "Auth", action = "Callback" });

app.Run();
```