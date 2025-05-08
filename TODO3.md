# TODO.md: Refactor MangaServices sử dụng DataProcessing Services (Phiên bản cập nhật)

Tài liệu này hướng dẫn các bước chi tiết để chuyển đổi các services trong thư mục `/Services/MangaServices/` sử dụng các service mới trong `/Services/MangaServices/DataProcessing`. Mục tiêu là tập trung logic trích xuất và ánh xạ dữ liệu vào các service chuyên biệt, giúp mã nguồn sạch sẽ và dễ bảo trì hơn.

**Lưu ý:** Các bước dưới đây giả định bạn đã tạo xong các interface và service trong thư mục `DataProcessing` theo hướng dẫn trước đó (`TODO2.md`).

## Bước 1: Cập nhật Dependencies trong `Program.cs`

Mở file `Program.cs` và thực hiện các thay đổi sau:

1.  **Đảm bảo đã đăng ký các Service DataProcessing:** Xác nhận rằng các dòng sau đã tồn tại và được đặt đúng vị trí (sau các dependencies mà chúng cần):

    ```csharp
    // Program.cs
    // ... (Đăng ký IUserService, IMangaFollowService, ICoverApiService, LocalizationService, MangaUtilityService...)

    // Đăng ký các Service DataProcessing mới
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.IMangaDataExtractor, MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaDataExtractorService>();
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.IMangaViewModelMapper, MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaViewModelMapperService>();

    // ... (Các đăng ký service khác)
    ```

2.  **Xóa các đăng ký Service cũ không còn cần thiết:** Tìm và **xóa bỏ hoàn toàn** các dòng đăng ký cho các service sau (logic của chúng đã được chuyển vào `DataProcessing`):

    ```csharp
    // Program.cs - XÓA CÁC DÒNG SAU
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaTitleService>();
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaTagService>();
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaRelationshipService>();
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaDescription>();
    builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterAttributeService>();
    // builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.JsonConversionService>(); // Chỉ xóa nếu không dùng ở đâu khác
    ```

    **Giải thích:**
    *   Các service trong `MangaInformation` và `ChapterAttributeService` đã được thay thế hoàn toàn bởi `IMangaDataExtractor` và `IMangaViewModelMapper`.
    *   `JsonConversionService`: Giữ lại nếu bạn còn sử dụng nó ở nơi khác, nếu không thì xóa. **Quyết định:** Giữ lại vì nó là utility chung.
    *   `LocalizationService`, `MangaUtilityService`, `IUserService`, `IMangaFollowService`, `ICoverApiService`: **Giữ lại** vì chúng được inject vào các service `DataProcessing` mới hoặc các service khác vẫn còn sử dụng.

## Bước 2: Refactor `MangaInfoService.cs`

1.  Mở file `Services/MangaServices/MangaInfoService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/MangaInfoService.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // using MangaReader.WebUI.Services.MangaServices.MangaInformation; // XÓA using này
    // using MangaReader.WebUI.Services.APIServices.Services; // XÓA using này nếu không dùng helper tĩnh CoverApiService
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaViewModelMapper` và xóa `MangaTitleService`, `ICoverApiService`.
    ```csharp
    // Services/MangaServices/MangaInfoService.cs
    public class MangaInfoService : IMangaInfoService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly IMangaViewModelMapper _mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
        private readonly ILogger<MangaInfoService> _logger;

        public MangaInfoService(
            IMangaApiService mangaApiService,
            IMangaViewModelMapper mangaViewModelMapper, // THÊM/Đảm bảo tồn tại
            ILogger<MangaInfoService> logger)
        {
            _mangaApiService = mangaApiService;
            _mangaViewModelMapper = mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
            _logger = logger;
        }
        // ...
    ```
4.  **Refactor phương thức `GetMangaInfoAsync`:** Sử dụng `_mangaApiService` để lấy `Manga` và `_mangaViewModelMapper.MapToMangaInfoViewModel` để chuyển đổi.
    ```csharp
    // Services/MangaServices/MangaInfoService.cs
    public async Task<MangaInfoViewModel?> GetMangaInfoAsync(string mangaId) // Đảm bảo kiểu trả về là nullable
    {
        if (string.IsNullOrEmpty(mangaId))
        {
            _logger.LogWarning("MangaId không được cung cấp khi gọi GetMangaInfoAsync.");
            return null;
        }

        try
        {
            _logger.LogInformation($"Bắt đầu lấy thông tin cơ bản cho manga ID: {mangaId} (Sử dụng Mapper)");

            var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);

            if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
            {
                _logger.LogWarning($"Không thể lấy chi tiết manga {mangaId} trong MangaInfoService. Response: {mangaResponse?.Result}");
                 return null;
            }

            // Gọi Mapper để tạo ViewModel
            // Lưu ý: MapToMangaInfoViewModel không phải async trong định nghĩa interface trước đó
            var mangaInfoViewModel = _mangaViewModelMapper.MapToMangaInfoViewModel(mangaResponse.Data);

            if (mangaInfoViewModel == null)
            {
                 _logger.LogWarning($"Mapper trả về null cho MangaInfoViewModel, manga ID: {mangaId}");
                 return null;
            }


            _logger.LogInformation($"Lấy thông tin cơ bản thành công cho manga ID: {mangaId}");
            return mangaInfoViewModel;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi lấy thông tin cơ bản cho manga ID: {mangaId}");
            return null;
        }
    }
    ```

## Bước 3: Refactor `ChapterInfoService.cs`

1.  Mở file `Services/MangaServices/ChapterServices/ChapterInfoService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterInfoService.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // using MangaReader.WebUI.Services.UtilityServices; // XÓA using JsonConversionService nếu chỉ dùng ở đây
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaDataExtractor` và xóa `ChapterAttributeService`, `JsonConversionService`.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterInfoService.cs
    public class ChapterInfoService : IChapterInfoService
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly IMangaDataExtractor _mangaDataExtractor; // THÊM/Đảm bảo tồn tại
        private readonly ILogger<ChapterInfoService> _logger;

        public ChapterInfoService(
            IChapterApiService chapterApiService,
            IMangaDataExtractor mangaDataExtractor, // THÊM/Đảm bảo tồn tại
            ILogger<ChapterInfoService> logger)
        {
            _chapterApiService = chapterApiService;
            _mangaDataExtractor = mangaDataExtractor; // THÊM/Đảm bảo tồn tại
            _logger = logger;
        }
        // ...
    ```
4.  **Refactor phương thức `GetChapterInfoAsync`:** Sử dụng `_chapterApiService` để lấy `Chapter` và `_mangaDataExtractor` để trích xuất thông tin.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterInfoService.cs
    public async Task<ChapterInfo?> GetChapterInfoAsync(string chapterId) // Đảm bảo kiểu trả về là nullable
    {
        if (string.IsNullOrEmpty(chapterId))
        {
            _logger.LogWarning("ChapterId không được cung cấp khi gọi GetChapterInfoAsync.");
            return null;
        }

        try
        {
            _logger.LogInformation($"Đang lấy thông tin chi tiết cho chapter ID: {chapterId} (Sử dụng Extractor)");

            var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

            if (chapterResponse?.Result != "ok" || chapterResponse.Data?.Attributes == null)
            {
                _logger.LogError($"Không lấy được thông tin hoặc attributes cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                return null;
            }

            var attributes = chapterResponse.Data.Attributes;

            // Sử dụng Extractor để lấy thông tin
            string displayTitle = _mangaDataExtractor.ExtractChapterDisplayTitle(attributes);
            DateTime publishedAt = attributes.PublishAt.DateTime;

            return new ChapterInfo
            {
                Id = chapterId,
                Title = displayTitle,
                PublishedAt = publishedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi ngoại lệ không xác định khi lấy thông tin chi tiết cho chapter ID: {chapterId}: {ex.Message}");
            return null;
        }
    }
    ```

## Bước 4: Refactor `ChapterService.cs`

1.  Mở file `Services/MangaServices/ChapterServices/ChapterService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterService.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // using MangaReader.WebUI.Services.UtilityServices; // XÓA using JsonConversionService nếu chỉ dùng ở đây
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaViewModelMapper` và xóa `JsonConversionService`.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterService.cs
    public class ChapterService
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly IMangaViewModelMapper _mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
        private readonly ILogger<ChapterService> _logger;
        private readonly string _backendBaseUrl;

        public ChapterService(
            IChapterApiService chapterApiService,
            IMangaViewModelMapper mangaViewModelMapper, // THÊM/Đảm bảo tồn tại
            IConfiguration configuration,
            ILogger<ChapterService> logger)
        {
            _chapterApiService = chapterApiService;
            _mangaViewModelMapper = mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
            _logger = logger;
            _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }
        // ...
    ```
4.  **Refactor `GetChaptersAsync`:** Sử dụng `_chapterApiService` để lấy danh sách `Chapter` và `_mangaViewModelMapper.MapToChapterViewModel` để chuyển đổi.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterService.cs
    public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string languages = "vi,en")
    {
        try
        {
            var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: null);
            var chapterViewModels = new List<ChapterViewModel>();

            if (chapterListResponse?.Data != null)
            {
                foreach (var chapter in chapterListResponse.Data)
                {
                    try
                    {
                        var chapterViewModel = _mangaViewModelMapper.MapToChapterViewModel(chapter); // Sử dụng Mapper
                        if (chapterViewModel != null)
                        {
                            chapterViewModels.Add(chapterViewModel);
                        } else {
                             _logger.LogWarning($"Mapper trả về null cho ChapterViewModel, chapter ID: {chapter?.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi map chapter ID: {chapter?.Id}");
                        continue;
                    }
                }
            }
            else
            {
                    _logger.LogWarning($"Không có dữ liệu chapter trả về cho manga {mangaId} với ngôn ngữ {languages}.");
            }

            return SortChaptersByNumberDescending(chapterViewModels); // Giữ lại logic sắp xếp nếu cần
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}");
            return new List<ChapterViewModel>();
        }
    }
    ```
5.  **Xóa các phương thức nội bộ không cần thiết:** Xóa `ProcessChapter`, `GetChapterDisplayInfo`, `ProcessChapterRelationships`. Logic này giờ nằm trong `IMangaViewModelMapper` và `IMangaDataExtractor`.
6.  **Refactor `GetChapterById`:** Sử dụng `_chapterApiService` để lấy `Chapter` và `_mangaViewModelMapper.MapToChapterViewModel`.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterService.cs
    public async Task<ChapterViewModel?> GetChapterById(string chapterId) // Đảm bảo kiểu trả về nullable
    {
        try
        {
            var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);
            if (chapterResponse?.Result != "ok" || chapterResponse.Data == null)
            {
                _logger.LogWarning($"Không tìm thấy chapter với ID: {chapterId} hoặc API lỗi.");
                return null;
            }

            var chapterViewModel = _mangaViewModelMapper.MapToChapterViewModel(chapterResponse.Data); // Sử dụng Mapper
            return chapterViewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi lấy thông tin chapter {chapterId}");
            return null;
        }
    }
    ```
7.  **Refactor `GetLatestChaptersAsync`:** Sử dụng `_chapterApiService` và `_mangaViewModelMapper.MapToSimpleChapterInfo`.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterService.cs
    public async Task<List<SimpleChapterInfo>> GetLatestChaptersAsync(string mangaId, int limit, string languages = "vi,en")
    {
        try
        {
            var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: limit);
            var simpleChapters = new List<SimpleChapterInfo>();

            if (chapterListResponse?.Data != null)
            {
                foreach (var chapter in chapterListResponse.Data)
                {
                    try
                    {
                        var simpleInfo = _mangaViewModelMapper.MapToSimpleChapterInfo(chapter); // Sử dụng Mapper
                        if (simpleInfo != null)
                        {
                            simpleChapters.Add(simpleInfo);
                        }
                         else {
                             _logger.LogWarning($"Mapper trả về null cho SimpleChapterInfo, chapter ID: {chapter?.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi map chapter ID: {chapter?.Id} thành SimpleChapterInfo");
                        continue;
                    }
                }
            }

            // API service đã giới hạn số lượng và thường sắp xếp sẵn,
            // nhưng vẫn sắp xếp lại ở đây để đảm bảo
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
    ```
8.  **Giữ lại các phương thức helper còn lại:** `SortChaptersByNumberDescending`, `SortChaptersByNumberAscending`, `GetChaptersByLanguage`, `ParseChapterNumber` nếu chúng vẫn được sử dụng trong class này.

## Bước 5: Refactor `FollowedMangaService.cs`

1.  Mở file `Services/MangaServices/FollowedMangaService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/FollowedMangaService.cs
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IChapterApiService`, `IMangaViewModelMapper`, `IConfiguration` và xóa `ChapterService`.
    ```csharp
    // Services/MangaServices/FollowedMangaService.cs
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly IChapterApiService _chapterApiService; // THÊM
        private readonly IMangaViewModelMapper _mangaViewModelMapper; // THÊM
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay;

        public FollowedMangaService(
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IChapterApiService chapterApiService, // THÊM
            IMangaViewModelMapper mangaViewModelMapper, // THÊM
            ILogger<FollowedMangaService> logger,
            IConfiguration configuration) // THÊM
        {
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _chapterApiService = chapterApiService; // THÊM
            _mangaViewModelMapper = mangaViewModelMapper; // THÊM
            _logger = logger;
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 550)); // THÊM
        }
        // ...
    ```
4.  **Refactor `GetFollowedMangaListAsync`:** Sử dụng `_chapterApiService` và `_mangaViewModelMapper`.
    ```csharp
    // Services/MangaServices/FollowedMangaService.cs
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
                        _logger.LogWarning($"Không thể lấy thông tin cơ bản cho manga ID: {mangaId}. Bỏ qua.");
                        continue;
                    }

                    await Task.Delay(_rateLimitDelay);
                    var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, "vi,en", maxChapters: 3); // Lấy từ API Service
                    var latestChapters = new List<SimpleChapterInfo>();

                    if(chapterListResponse?.Data != null)
                    {
                        foreach (var chapter in chapterListResponse.Data)
                        {
                            try {
                                var simpleInfo = _mangaViewModelMapper.MapToSimpleChapterInfo(chapter); // Dùng Mapper
                                if(simpleInfo != null) latestChapters.Add(simpleInfo);
                            } catch (Exception mapEx){
                                _logger.LogError(mapEx, $"Lỗi khi map chapter {chapter?.Id} thành SimpleChapterInfo cho manga {mangaId}");
                            }
                        }
                        latestChapters = latestChapters.OrderByDescending(c => c.PublishedAt).ToList();
                    }

                    var followedManga = _mangaViewModelMapper.MapToFollowedMangaViewModel(mangaInfo, latestChapters); // Dùng Mapper
                    if(followedManga != null) followedMangaList.Add(followedManga);

                    _logger.LogDebug($"Đã xử lý xong manga (qua Mapper): {mangaInfo.MangaTitle}");
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
    ```

## Bước 6: Refactor `ReadingHistoryService.cs`

1.  Mở file `Services/MangaServices/ReadingHistoryService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/ReadingHistoryService.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaViewModelMapper`.
    ```csharp
    // Services/MangaServices/ReadingHistoryService.cs
    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly IChapterInfoService _chapterInfoService;
        private readonly IConfiguration _configuration;
        private readonly IMangaViewModelMapper _mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
        private readonly ILogger<ReadingHistoryService> _logger;
        private readonly TimeSpan _rateLimitDelay;

        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IChapterInfoService chapterInfoService,
            IConfiguration configuration,
            IMangaViewModelMapper mangaViewModelMapper, // THÊM/Đảm bảo tồn tại
            ILogger<ReadingHistoryService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _chapterInfoService = chapterInfoService;
            _configuration = configuration;
            _mangaViewModelMapper = mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
            _logger = logger;
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 550));
        }
        // ...
    ```
4.  **Refactor `GetReadingHistoryAsync`:** Sử dụng `_mangaViewModelMapper.MapToLastReadMangaViewModel`.
    ```csharp
    // Services/MangaServices/ReadingHistoryService.cs
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
                try
                {
                    await Task.Delay(_rateLimitDelay);
                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho MangaId: {item.MangaId}. Bỏ qua mục lịch sử này.");
                        continue;
                    }

                    await Task.Delay(_rateLimitDelay);
                    var chapterInfo = await _chapterInfoService.GetChapterInfoAsync(item.ChapterId);
                    if (chapterInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho ChapterId: {item.ChapterId}. Bỏ qua mục lịch sử này.");
                        continue;
                    }

                    // Gọi Mapper để tạo ViewModel
                    var historyViewModel = _mangaViewModelMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfo, item.LastReadAt);
                    if (historyViewModel != null)
                    {
                        historyViewModels.Add(historyViewModel);
                        _logger.LogDebug($"Đã xử lý xong mục lịch sử cho manga: {mangaInfo.MangaTitle}, chapter: {chapterInfo.Title}");
                    }
                     else {
                         _logger.LogWarning($"Mapper trả về null cho manga {item.MangaId}, chapter {item.ChapterId}");
                    }
                }
                catch (Exception itemEx)
                {
                        _logger.LogError(itemEx, $"Lỗi khi xử lý mục lịch sử manga {item.MangaId}, chapter {item.ChapterId}");
                }
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
    ```

## Bước 7: Refactor `MangaDetailsService.cs`

1.  Mở file `Services/MangaServices/MangaPageService/MangaDetailsService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/MangaPageService/MangaDetailsService.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // XÓA: using MangaReader.WebUI.Services.MangaServices.MangaInformation;
    // XÓA: using MangaReader.WebUI.Services.UtilityServices; // (LocalizationService, JsonConversionService)
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaViewModelMapper` và xóa các dependencies cũ như `MangaTitleService`, `MangaTagService`, `MangaRelationshipService`, `MangaDescription`, `LocalizationService`, `JsonConversionService`, `MangaUtilityService`. Giữ lại `IMangaFollowService`, `ChapterService`, `IHttpContextAccessor`.
    ```csharp
    // Services/MangaServices/MangaPageService/MangaDetailsService.cs
    public class MangaDetailsService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly IMangaFollowService _mangaFollowService; // Giữ lại
        private readonly ChapterService _chapterService; // Giữ lại
        private readonly IHttpContextAccessor _httpContextAccessor; // Giữ lại
        private readonly IMangaViewModelMapper _mangaViewModelMapper; // THÊM/Đảm bảo tồn tại

        public MangaDetailsService(
            IMangaApiService mangaApiService,
            ILogger<MangaDetailsService> logger,
            IMangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IMangaViewModelMapper mangaViewModelMapper // THÊM/Đảm bảo tồn tại
            )
        {
            _mangaApiService = mangaApiService;
            _logger = logger;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaViewModelMapper = mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
        }
        // ...
    ```
4.  **Refactor `GetMangaDetailsAsync`:** Sử dụng `_mangaApiService` để lấy `Manga` và `_mangaViewModelMapper.MapToMangaDetailViewModelAsync`.
    ```csharp
    // Services/MangaServices/MangaPageService/MangaDetailsService.cs
    public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
    {
        try
        {
            _logger.LogInformation($"Đang lấy chi tiết manga ID: {id} (Sử dụng Mapper)");
            var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);

            if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
            {
                _logger.LogError($"Không thể lấy chi tiết manga {id}. Response: {mangaResponse?.Result}");
                return new MangaDetailViewModel {
                    Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" },
                    Chapters = new List<ChapterViewModel>(),
                    AlternativeTitlesByLanguage = new Dictionary<string, List<string>>()
                };
            }

            var mangaData = mangaResponse.Data;

            // Lấy danh sách chapters (giữ nguyên logic cũ)
            var chapterViewModels = await GetChaptersAsync(id);

            // Gọi Mapper để tạo MangaDetailViewModel
            var viewModel = await _mangaViewModelMapper.MapToMangaDetailViewModelAsync(mangaData, chapterViewModels);

            // Lưu title vào session (nếu cần)
             var httpContext = _httpContextAccessor.HttpContext;
             if (httpContext != null && viewModel.Manga != null && !string.IsNullOrEmpty(viewModel.Manga.Title))
             {
                 httpContext.Session.SetString($"Manga_{id}_Title", viewModel.Manga.Title);
                 _logger.LogInformation($"Đã lưu tiêu đề manga {id} vào session: {viewModel.Manga.Title}");
             }

            return viewModel;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chi tiết manga {id}: {jsonEx.Message}");
             return new MangaDetailViewModel {
                Manga = new MangaViewModel { Id = id, Title = "Lỗi định dạng dữ liệu" },
                Chapters = new List<ChapterViewModel>(),
                AlternativeTitlesByLanguage = new Dictionary<string, List<string>>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi nghiêm trọng khi lấy chi tiết manga {id}: {ex.Message}");
             return new MangaDetailViewModel {
                Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" },
                Chapters = new List<ChapterViewModel>(),
                AlternativeTitlesByLanguage = new Dictionary<string, List<string>>()
            };
        }
    }
    ```
5.  **Xóa phương thức `CreateMangaViewModelAsync`:** Logic này đã được chuyển vào `MangaViewModelMapperService`.
6.  **Giữ lại phương thức `GetChaptersAsync`:** Phương thức này vẫn cần thiết để lấy danh sách chapter và lưu vào session.

## Bước 8: Refactor `MangaSearchService.cs`

1.  Mở file `Services/MangaServices/MangaPageService/MangaSearchService.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/MangaPageService/MangaSearchService.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // XÓA: using MangaReader.WebUI.Services.MangaServices.MangaInformation;
    // XÓA: using MangaReader.WebUI.Services.UtilityServices;
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaViewModelMapper` và xóa các dependencies cũ.
    ```csharp
    // Services/MangaServices/MangaPageService/MangaSearchService.cs
    public class MangaSearchService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ILogger<MangaSearchService> _logger;
        private readonly IMangaViewModelMapper _mangaViewModelMapper; // THÊM/Đảm bảo tồn tại

        public MangaSearchService(
            IMangaApiService mangaApiService,
            ILogger<MangaSearchService> logger,
            IMangaViewModelMapper mangaViewModelMapper // THÊM/Đảm bảo tồn tại
            )
        {
            _mangaApiService = mangaApiService;
            _logger = logger;
            _mangaViewModelMapper = mangaViewModelMapper; // THÊM/Đảm bảo tồn tại
        }
        // ...
    ```
4.  **Refactor `SearchMangaAsync`:** Gọi Mapper trong `ConvertToMangaViewModelsAsync`.
    ```csharp
    // Services/MangaServices/MangaPageService/MangaSearchService.cs
    public async Task<MangaListViewModel> SearchMangaAsync(
        int page,
        int pageSize,
        SortManga sortManga)
    {
        try
        {
            // ... (phần xử lý giới hạn API và gọi _mangaApiService.FetchMangaAsync giữ nguyên)
            var result = await _mangaApiService.FetchMangaAsync(limit: limit, offset: offset, sortManga: sortManga);
            // ... (phần tính toán totalCount và maxPages giữ nguyên)

            // Chuyển danh sách Manga thành MangaViewModel SỬ DỤNG MAPPER
            var mangaViewModels = await ConvertToMangaViewModelsAsync(result?.Data); // Đổi tên hàm

            // ... (return ViewModel giữ nguyên)
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
            // ... (xử lý lỗi giữ nguyên)
            _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}\nStack trace: {ex.StackTrace}");
            return new MangaListViewModel
            {
                Mangas = new List<MangaViewModel>(),
                CurrentPage = 1,
                PageSize = pageSize,
                TotalCount = 0,
                MaxPages = 0,
                SortOptions = sortManga
            };
        }
    }
    ```
5.  **Refactor `ConvertToMangaViewModels` thành `ConvertToMangaViewModelsAsync`:** Sử dụng `_mangaViewModelMapper.MapToMangaViewModelAsync`.
    ```csharp
    // Services/MangaServices/MangaPageService/MangaSearchService.cs
    private async Task<List<MangaViewModel>> ConvertToMangaViewModelsAsync(List<MangaReader.WebUI.Models.Mangadex.Manga>? mangaList) // Đổi tên và thêm async
    {
        var mangaViewModels = new List<MangaViewModel>();
        if (mangaList == null || !mangaList.Any())
        {
            return mangaViewModels;
        }

        foreach (var manga in mangaList)
        {
            try
            {
                if (manga.Attributes == null)
                {
                    _logger.LogWarning($"Manga ID: {manga.Id} không có thuộc tính Attributes");
                    continue;
                }

                // Gọi Mapper để tạo ViewModel
                var viewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(manga); // Gọi mapper async
                if(viewModel != null) {
                     mangaViewModels.Add(viewModel);
                } else {
                     _logger.LogWarning($"Mapper trả về null cho manga ID: {manga.Id} trong SearchService.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi manga ID: {manga?.Id} trong SearchService.");
                continue;
            }
        }

        return mangaViewModels;
    }
    ```

## Bước 9: Refactor `ChapterReadingServices.cs`

1.  Mở file `Services/MangaServices/ChapterServices/ChapterReadingServices.cs`.
2.  **Cập nhật using:**
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterReadingServices.cs
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Đảm bảo using này tồn tại
    // ...
    ```
3.  **Cập nhật Constructor:** Inject `IMangaDataExtractor` và `IMangaInfoService`, xóa `MangaTitleService`.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterReadingServices.cs
    public class ChapterReadingServices
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly MangaIdService _mangaIdService;
        private readonly ChapterLanguageServices _chapterLanguageServices;
        private readonly ChapterService _chapterService; // Giữ lại
        private readonly IMangaInfoService _mangaInfoService; // THÊM/Đảm bảo tồn tại
        private readonly IMangaDataExtractor _mangaDataExtractor; // THÊM/Đảm bảo tồn tại
        private readonly ILogger<ChapterReadingServices> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _backendBaseUrl;

        public ChapterReadingServices(
            IChapterApiService chapterApiService,
            MangaIdService mangaIdService,
            ChapterLanguageServices chapterLanguageServices,
            ChapterService chapterService, // Giữ lại
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IMangaInfoService mangaInfoService, // THÊM/Đảm bảo tồn tại
            IMangaDataExtractor mangaDataExtractor, // THÊM/Đảm bảo tồn tại
            ILogger<ChapterReadingServices> logger)
        {
            _chapterApiService = chapterApiService;
            _mangaIdService = mangaIdService;
            _chapterLanguageServices = chapterLanguageServices;
            _chapterService = chapterService; // Giữ lại
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
            _mangaInfoService = mangaInfoService; // THÊM/Đảm bảo tồn tại
            _mangaDataExtractor = mangaDataExtractor; // THÊM/Đảm bảo tồn tại
        }
        // ...
    ```
4.  **Refactor `GetChapterReadViewModel`:** Sử dụng `IMangaInfoService` để lấy tiêu đề manga và `IMangaDataExtractor` để lấy tiêu đề và số chương hiện tại.
    ```csharp
    // Services/MangaServices/ChapterServices/ChapterReadingServices.cs
    public async Task<ChapterReadViewModel> GetChapterReadViewModel(string chapterId)
    {
        try
        {
            _logger.LogInformation($"Đang tải chapter {chapterId}");

            // ... (phần 1, 2, 3, 4 giữ nguyên)
            var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
            // ... (kiểm tra lỗi)
             var pages = atHomeResponse.Chapter.Data
                .Select(pageFile => $"{_backendBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"{atHomeResponse.BaseUrl}/data/{atHomeResponse.Chapter.Hash}/{pageFile}")}")
                .ToList();
            string mangaId = await _mangaIdService.GetMangaIdFromChapterAsync(chapterId);
            string currentChapterLanguage = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);

            // 5. Lấy tiêu đề manga SỬ DỤNG MangaInfoService
            var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);
            string mangaTitle = mangaInfo?.MangaTitle ?? "Không rõ tiêu đề manga";

            // Lưu title vào session (nếu cần)
             var httpContext = _httpContextAccessor.HttpContext;
             if (httpContext != null && !string.IsNullOrEmpty(mangaTitle) && mangaTitle != "Không rõ tiêu đề manga")
             {
                 httpContext.Session.SetString($"Manga_{mangaId}_Title", mangaTitle);
                 _logger.LogInformation($"Đã lưu tiêu đề manga {mangaId} vào session: {mangaTitle}");
             }

            // 6. Lấy danh sách chapters (Sibling chapters - giữ nguyên)
            var chaptersList = await GetChaptersAsync(mangaId, currentChapterLanguage);

            // 7. Tìm chapter hiện tại và các chapter liền kề (Sử dụng Extractor)
             var currentChapterDetails = await _chapterApiService.FetchChapterInfoAsync(chapterId);
             string currentChapterDisplayTitle = "Không xác định";
             string currentChapterNumber = "?";

             if (currentChapterDetails?.Data?.Attributes != null)
             {
                 currentChapterDisplayTitle = _mangaDataExtractor.ExtractChapterDisplayTitle(currentChapterDetails.Data.Attributes);
                 currentChapterNumber = _mangaDataExtractor.ExtractChapterNumber(currentChapterDetails.Data.Attributes);
             }
             else
             {
                 _logger.LogWarning($"Không lấy được attributes cho chapter hiện tại {chapterId}");
             }

             var (_, prevChapterId, nextChapterId) = FindCurrentAndAdjacentChapters(chaptersList, chapterId, currentChapterLanguage); // Giữ nguyên logic tìm

            // 8. Tạo view model
            var viewModel = new ChapterReadViewModel
            {
                MangaId = mangaId,
                MangaTitle = mangaTitle,
                ChapterId = chapterId,
                ChapterTitle = currentChapterDisplayTitle, // Dùng title đã trích xuất
                ChapterNumber = currentChapterNumber, // Dùng number đã trích xuất
                ChapterLanguage = currentChapterLanguage,
                Pages = pages,
                PrevChapterId = prevChapterId,
                NextChapterId = nextChapterId,
                SiblingChapters = chaptersList
            };

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi tải chapter {chapterId}");
            throw;
        }
    }
    ```
5.  **Xóa các phương thức `GetMangaTitleFromApiAsync` và `GetMangaTitleAsync`:** Logic này đã chuyển vào `MangaInfoService`.
6.  **Giữ lại các phương thức còn lại:** `GetChaptersAsync`, `GetChaptersFromApiAsync`, `FindCurrentAndAdjacentChapters`, `ParseChapterNumber`.

## Bước 10: Dọn dẹp cuối cùng

1.  **Xóa Files:** Xóa các file service đã liệt kê ở **Bước 1** mục 2 khỏi dự án.
2.  **Xóa Usings:** Mở các file đã refactor (`MangaInfoService.cs`, `ChapterInfoService.cs`, `ChapterService.cs`, `FollowedMangaService.cs`, `ReadingHistoryService.cs`, `MangaDetailsService.cs`, `MangaSearchService.cs`, `ChapterReadingServices.cs`) và xóa các `using` không cần thiết, đặc biệt là `using MangaReader.WebUI.Services.MangaServices.MangaInformation;` và `using MangaReader.WebUI.Services.UtilityServices;` (chỉ xóa nếu không còn dùng `LocalizationService` hoặc `JsonConversionService` trực tiếp).
3.  **Build và Chạy thử:** Build lại dự án và kiểm tra kỹ lưỡng các chức năng liên quan để đảm bảo mọi thứ hoạt động đúng như mong đợi.

Chúc bạn hoàn thành tốt việc refactor!