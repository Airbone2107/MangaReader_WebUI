# TODO.md: Tái cấu trúc Services\MangaServices để sử dụng DataProcessing

Mục tiêu: Cập nhật các service trong `Services\MangaServices` để tận dụng các service xử lý dữ liệu mới trong `Services\MangaServices\DataProcessing`, giúp mã nguồn rõ ràng và dễ bảo trì hơn.

## Bước 1: Đăng ký các Service mới trong `DataProcessing` vào `Program.cs`

1.  **Mở file `Program.cs`.**
2.  **Thêm các đăng ký service cho `DataProcessing`.** Đảm bảo bạn đã có các interface và class triển khai tương ứng trong thư mục `Services\MangaServices\DataProcessing`.

    ```csharp
    // Program.cs
    // ... các using statements ...
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

    var builder = WebApplication.CreateBuilder(args);

    // ... các services đã đăng ký khác ...

    // Đăng ký các service trong DataProcessing
    builder.Services.AddScoped<IMangaDataExtractor, MangaDataExtractorService>();
    builder.Services.AddScoped<IMangaToMangaViewModelMapper, MangaToMangaViewModelMapperService>();
    builder.Services.AddScoped<IChapterToChapterViewModelMapper, ChapterToChapterViewModelMapperService>();
    builder.Services.AddScoped<IMangaToDetailViewModelMapper, MangaToDetailViewModelMapperService>();
    builder.Services.AddScoped<IChapterToSimpleInfoMapper, ChapterToSimpleInfoMapperService>();
    builder.Services.AddScoped<IMangaToInfoViewModelMapper, MangaToInfoViewModelMapperService>();
    builder.Services.AddScoped<IFollowedMangaViewModelMapper, FollowedMangaViewModelMapperService>();
    builder.Services.AddScoped<ILastReadMangaViewModelMapper, LastReadMangaViewModelMapperService>();

    // ... phần còn lại của Program.cs ...

    var app = builder.Build();

    // ...
    ```

## Bước 2: Xác định và xóa các File Service cũ không còn cần thiết

Các file service sau đây có chức năng đã được bao gồm hoặc có thể được thay thế hoàn toàn bởi các service trong `DataProcessing`, đặc biệt là `MangaDataExtractorService` và các Mappers:

1.  `Services\MangaServices\MangaInformation\MangaDescription.cs`
2.  `Services\MangaServices\MangaInformation\MangaRelationshipService.cs` (Chức năng lấy author/artist)
3.  `Services\MangaServices\MangaInformation\MangaTagService.cs`
4.  `Services\MangaServices\MangaInformation\MangaTitleService.cs`

**Hành động:**

*   **Xóa các file trên khỏi project.**
*   **Loại bỏ đăng ký của chúng khỏi `Program.cs`:**

    ```csharp
    // Program.cs - Gỡ bỏ các dòng này (nếu có)
    // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaDescription>();
    // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaRelationshipService>();
    // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaTagService>();
    // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaTitleService>();
    ```

*Lưu ý:* `MangaUtilityService.cs` (nếu chỉ chứa logic giả như `GetMangaRating`) có thể tạm giữ lại hoặc tích hợp logic đó vào các mapper nếu phù hợp. Hiện tại, chúng ta tập trung vào các service trích xuất dữ liệu chính.

## Bước 3: Xác định và loại bỏ các Service nhỏ (phương thức/class) không cần thiết trong các File Service lớn

Một số class service lớn hơn chứa các phương thức mà chức năng của chúng giờ đây sẽ do `DataProcessing` đảm nhiệm.

1.  **`Services\MangaServices\ChapterServices\ChapterAttributeService.cs`**:
    *   Các phương thức `GetChapterNumberAsync`, `GetChapterTitleAsync`, `GetPublishedAtAsync`, `CreateDisplayTitle` sẽ được thay thế bằng cách gọi `IMangaDataExtractor`.
    *   Nếu sau khi refactor, class này không còn nhiều chức năng, có thể xem xét xóa bỏ hoàn toàn và tích hợp phần còn lại vào service khác hoặc trực tiếp sử dụng `IMangaDataExtractor`.
    *   **Hành động:** Tạm thời giữ lại class nhưng sẽ refactor bên trong. Nếu xóa, gỡ đăng ký:
        ```csharp
        // Program.cs - Gỡ bỏ nếu class bị xóa
        // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterAttributeService>();
        ```

2.  **`Services\MangaServices\ChapterServices\ChapterInfoService.cs`**:
    *   Toàn bộ class này có thể được thay thế bằng `IChapterToSimpleInfoMapper`.
    *   **Hành động:** Xóa file `ChapterInfoService.cs` và gỡ đăng ký:
        ```csharp
        // Program.cs - Gỡ bỏ
        // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.IChapterInfoService, MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterInfoService>();
        ```

## Bước 4: Cập nhật các Services đang sử dụng Services bị xóa/thay thế

Đây là phần chính của việc tái cấu trúc. Chúng ta sẽ đi qua từng service và cập nhật chúng.

### 4.1. Cập nhật `MangaInfoService.cs`

Service này tạo `MangaInfoViewModel`. Bây giờ nó sẽ sử dụng `IMangaToInfoViewModelMapper`.

*   **Mở file `Services\MangaServices\MangaInfoService.cs`**
*   **Thay đổi constructor và phương thức `GetMangaInfoAsync`:**

    ```csharp
    // Services\MangaServices\MangaInfoService.cs
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.MangaServices.Models;
    // using MangaReader.WebUI.Services.MangaServices.MangaInformation; // Không cần nữa
    // using MangaReader.WebUI.Services.APIServices.Services; // Không cần CoverApiService trực tiếp ở đây nữa
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // THÊM MỚI

    namespace MangaReader.WebUI.Services.MangaServices
    {
        public class MangaInfoService : IMangaInfoService
        {
            // private readonly MangaTitleService _mangaTitleService; // XÓA
            private readonly IMangaApiService _mangaApiService;
            // private readonly ICoverApiService _coverApiService; // XÓA
            private readonly ILogger<MangaInfoService> _logger;
            private readonly IMangaToInfoViewModelMapper _mangaToInfoViewModelMapper; // THÊM MỚI

            public MangaInfoService(
                // MangaTitleService mangaTitleService, // XÓA
                IMangaApiService mangaApiService,
                // ICoverApiService coverApiService, // XÓA
                ILogger<MangaInfoService> logger,
                IMangaToInfoViewModelMapper mangaToInfoViewModelMapper // THÊM MỚI
                )
            {
                // _mangaTitleService = mangaTitleService; // XÓA
                _mangaApiService = mangaApiService;
                // _coverApiService = coverApiService; // XÓA
                _logger = logger;
                _mangaToInfoViewModelMapper = mangaToInfoViewModelMapper; // THÊM MỚI
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

                    var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);

                    if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                    {
                        _logger.LogWarning($"Không thể lấy chi tiết manga {mangaId} trong MangaInfoService. Response: {mangaResponse?.Result}");
                        return new MangaInfoViewModel
                        {
                            MangaId = mangaId,
                            MangaTitle = $"Lỗi tải tiêu đề ({mangaId})",
                            CoverUrl = "/images/cover-placeholder.jpg"
                        };
                    }

                    // Sử dụng mapper mới
                    var mangaInfoViewModel = _mangaToInfoViewModelMapper.MapToMangaInfoViewModel(mangaResponse.Data);
                    
                    _logger.LogInformation($"Lấy thông tin cơ bản thành công cho manga ID: {mangaId}");
                    return mangaInfoViewModel;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi lấy thông tin cơ bản cho manga ID: {mangaId}");
                    return new MangaInfoViewModel
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

### 4.2. Cập nhật `ChapterService.cs`

Service này tạo `List<ChapterViewModel>` và `List<SimpleChapterInfo>`.

*   **Mở file `Services\MangaServices\ChapterServices\ChapterService.cs`**
*   **Thay đổi constructor và các phương thức liên quan:**

    ```csharp
    // Services\MangaServices\ChapterServices\ChapterService.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReader.WebUI.Services.MangaServices.Models;
    // using MangaReader.WebUI.Services.UtilityServices; // JsonConversionService không còn cần thiết trực tiếp ở đây
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // THÊM MỚI
    using System.Text.Json; // Vẫn cần cho Deserialize

    namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
    {
        public class ChapterService
        {
            private readonly IChapterApiService _chapterApiService;
            // private readonly JsonConversionService _jsonConversionService; // XÓA
            private readonly ILogger<ChapterService> _logger;
            private readonly string _backendBaseUrl;
            private readonly IChapterToChapterViewModelMapper _chapterViewModelMapper; // THÊM MỚI
            private readonly IChapterToSimpleInfoMapper _simpleChapterInfoMapper; // THÊM MỚI

            public ChapterService(
                IChapterApiService chapterApiService,
                // JsonConversionService jsonConversionService, // XÓA
                IConfiguration configuration,
                ILogger<ChapterService> logger,
                IChapterToChapterViewModelMapper chapterViewModelMapper, // THÊM MỚI
                IChapterToSimpleInfoMapper simpleChapterInfoMapper // THÊM MỚI
                )
            {
                _chapterApiService = chapterApiService;
                // _jsonConversionService = jsonConversionService; // XÓA
                _logger = logger;
                _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                                 ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
                _chapterViewModelMapper = chapterViewModelMapper; // THÊM MỚI
                _simpleChapterInfoMapper = simpleChapterInfoMapper; // THÊM MỚI
            }

            public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string languages = "vi,en")
            {
                try
                {
                    var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: null);
                    var chapterViewModels = new List<ChapterViewModel>();

                    if (chapterListResponse?.Data != null)
                    {
                        foreach (var chapterData in chapterListResponse.Data)
                        {
                            try
                            {
                                // Sử dụng mapper mới
                                var chapterViewModel = _chapterViewModelMapper.MapToChapterViewModel(chapterData);
                                if (chapterViewModel != null)
                                {
                                    chapterViewModels.Add(chapterViewModel);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapterData?.Id}");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Không có dữ liệu chapter trả về cho manga {mangaId} với ngôn ngữ {languages}.");
                    }
                    return SortChaptersByNumberDescending(chapterViewModels);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}");
                    return new List<ChapterViewModel>();
                }
            }

            // XÓA BỎ: private ChapterViewModel ProcessChapter(MangaReader.WebUI.Models.Mangadex.Chapter chapter)
            // XÓA BỎ: private (string displayTitle, string chapterNumber) GetChapterDisplayInfo(ChapterAttributes attributes)
            // XÓA BỎ: private List<ChapterRelationship> ProcessChapterRelationships(List<Relationship>? relationships)
            // Các phương thức này giờ đã được xử lý trong MangaDataExtractorService và ChapterToChapterViewModelMapperService

            private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
            {
                // ... (giữ nguyên logic này)
                return chapters
                    .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                    .OrderByDescending(c => c.Number ?? double.MinValue) 
                    .Select(c => c.Chapter)
                    .ToList();
            }
            
            private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
            {
                // ... (giữ nguyên logic này)
                 return chapters
                    .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                    .OrderBy(c => c.Number ?? double.MaxValue) 
                    .Select(c => c.Chapter)
                    .ToList();
            }

            public Dictionary<string, List<ChapterViewModel>> GetChaptersByLanguage(List<ChapterViewModel> chapters)
            {
                // ... (giữ nguyên logic này)
                var chaptersByLanguage = chapters.GroupBy(c => c.Language)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var language in chaptersByLanguage.Keys)
                {
                    chaptersByLanguage[language] = SortChaptersByNumberAscending(chaptersByLanguage[language]);
                }

                return chaptersByLanguage;
            }

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
                    // Sử dụng mapper mới
                    return _chapterViewModelMapper.MapToChapterViewModel(chapterResponse.Data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi lấy thông tin chapter {chapterId}");
                    return null;
                }
            }

            public async Task<List<string>> GetChapterPages(string chapterId)
            {
                // ... (giữ nguyên logic này, nó không map data mà chỉ tạo URL)
                try
                {
                    var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
                    if (atHomeResponse == null || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin trang ảnh cho chapter {chapterId}");
                        return new List<string>();
                    }

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
                    var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: limit);
                    var simpleChapters = new List<SimpleChapterInfo>();

                    if (chapterListResponse?.Data != null)
                    {
                        foreach (var chapterData in chapterListResponse.Data)
                        {
                            try
                            {
                                // Sử dụng mapper mới
                                var simpleInfo = _simpleChapterInfoMapper.MapToSimpleChapterInfo(chapterData);
                                simpleChapters.Add(simpleInfo);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapterData?.Id} trong GetLatestChaptersAsync");
                                continue;
                            }
                        }
                    }
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

            private double? ParseChapterNumber(string chapterNumber)
            {
                // ... (giữ nguyên logic này)
                if (double.TryParse(chapterNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
                {
                    return number;
                }
                return null;
            }
        }
    }
    ```

### 4.3. Cập nhật `MangaSearchService.cs`

Service này tạo `MangaListViewModel` (chứa `List<MangaViewModel>`).

*   **Mở file `Services\MangaServices\MangaPageService\MangaSearchService.cs`**
*   **Thay đổi constructor và phương thức `ConvertToMangaViewModels` (hoặc xóa và map trực tiếp):**

    ```csharp
    // Services\MangaServices\MangaPageService\MangaSearchService.cs
    using MangaReader.WebUI.Models;
    // ...
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    // using MangaReader.WebUI.Services.APIServices.Services; // CoverApiService không cần trực tiếp
    // using MangaReader.WebUI.Services.MangaServices.MangaInformation; // Các service info cũ không cần
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // THÊM MỚI

    namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
    {
        public class MangaSearchService
        {
            private readonly IMangaApiService _mangaApiService;
            // private readonly ICoverApiService _coverApiService; // XÓA
            private readonly ILogger<MangaSearchService> _logger;
            // private readonly LocalizationService _localizationService; // XÓA
            // private readonly JsonConversionService _jsonConversionService; // XÓA
            // private readonly MangaTitleService _mangaTitleService; // XÓA
            // private readonly MangaTagService _mangaTagService; // XÓA
            // private readonly MangaDescription _mangaDescriptionService; // XÓA
            // private readonly MangaRelationshipService _mangaRelationshipService; // XÓA
            private readonly IMangaToMangaViewModelMapper _mangaViewModelMapper; // THÊM MỚI

            public MangaSearchService(
                IMangaApiService mangaApiService,
                // ICoverApiService coverApiService, // XÓA
                ILogger<MangaSearchService> logger,
                // LocalizationService localizationService, // XÓA
                // JsonConversionService jsonConversionService, // XÓA
                // MangaTitleService mangaTitleService, // XÓA
                // MangaTagService mangaTagService, // XÓA
                // MangaDescription mangaDescriptionService, // XÓA
                // MangaRelationshipService mangaRelationshipService, // XÓA
                IMangaToMangaViewModelMapper mangaViewModelMapper // THÊM MỚI
                )
            {
                _mangaApiService = mangaApiService;
                // _coverApiService = coverApiService; // XÓA
                _logger = logger;
                // _localizationService = localizationService; // XÓA
                // _jsonConversionService = jsonConversionService; // XÓA
                // _mangaTitleService = mangaTitleService; // XÓA
                // _mangaTagService = mangaTagService; // XÓA
                // _mangaDescriptionService = mangaDescriptionService; // XÓA
                // _mangaRelationshipService = mangaRelationshipService; // XÓA
                _mangaViewModelMapper = mangaViewModelMapper; // THÊM MỚI
            }

            // ... CreateSortMangaFromParameters giữ nguyên ...

            public async Task<MangaListViewModel> SearchMangaAsync(
                int page,
                int pageSize,
                SortManga sortManga)
            {
                try
                {
                    // ... (logic phân trang và gọi API giữ nguyên) ...
                    const int MAX_API_RESULTS = 10000;
                    int offset = (page - 1) * pageSize;
                    int limit = pageSize;

                    if (offset + limit > MAX_API_RESULTS)
                    {
                        if (offset < MAX_API_RESULTS) limit = MAX_API_RESULTS - offset;
                        else limit = 0;
                    }

                    if (limit == 0) // ...
                    {
                         return new MangaListViewModel { /* ... */ };
                    }

                    var result = await _mangaApiService.FetchMangaAsync(limit: limit, offset: offset, sortManga: sortManga);
                    
                    int totalCount = result?.Total ?? 0;
                    // ... (logic tính totalCount và maxPages giữ nguyên) ...
                    int maxPages = (int)Math.Ceiling(Math.Min(totalCount, MAX_API_RESULTS) / (double)pageSize);


                    // Sử dụng mapper mới
                    var mangaViewModels = new List<MangaViewModel>();
                    if (result?.Data != null)
                    {
                        foreach (var mangaData in result.Data)
                        {
                            if (mangaData != null) // Kiểm tra null cho mangaData
                            {
                                mangaViewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaData));
                            }
                        }
                    }
                    
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
                    // ... (xử lý lỗi giữ nguyên) ...
                    _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}\nStack trace: {ex.StackTrace}");
                    return new MangaListViewModel { /* ... */ };
                }
            }

            // XÓA BỎ: private List<MangaViewModel> ConvertToMangaViewModels(List<MangaReader.WebUI.Models.Mangadex.Manga>? mangaList)
        }
    }
    ```

### 4.4. Cập nhật `MangaDetailsService.cs` (trong `MangaPageService`)

Service này tạo `MangaDetailViewModel`.

*   **Mở file `Services\MangaServices\MangaPageService\MangaDetailsService.cs`**
*   **Thay đổi constructor và phương thức `GetMangaDetailsAsync`:**

    ```csharp
    // Services\MangaServices\MangaPageService\MangaDetailsService.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReader.WebUI.Services.MangaServices.ChapterServices;
    // using MangaReader.WebUI.Services.MangaServices.MangaInformation; // Không cần nhiều service con nữa
    // using MangaReader.WebUI.Services.UtilityServices; // LocalizationService, JsonConversionService không cần trực tiếp
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    // using MangaReader.WebUI.Services.APIServices.Services; // CoverApiService không cần trực tiếp
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // THÊM MỚI
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // THÊM MỚI cho IMangaDataExtractor


    namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
    {
        public class MangaDetailsService
        {
            private readonly IMangaApiService _mangaApiService;
            // private readonly ICoverApiService _coverApiService; // XÓA
            private readonly ILogger<MangaDetailsService> _logger;
            // private readonly LocalizationService _localizationService; // XÓA
            // private readonly JsonConversionService _jsonConversionService; // XÓA
            // private readonly MangaUtilityService _mangaUtilityService; // XÓA nếu logic giả không cần
            // private readonly MangaTitleService _mangaTitleService; // XÓA
            // private readonly MangaTagService _mangaTagService; // XÓA
            // private readonly MangaRelationshipService _mangaRelationshipService; // XÓA
            // private readonly IMangaFollowService _mangaFollowService; // Vẫn cần nếu IsFollowing set ở đây
            private readonly ChapterService _chapterService; // Vẫn cần để lấy danh sách chapter
            private readonly IHttpContextAccessor _httpContextAccessor;
            // private readonly MangaDescription _mangaDescription; // XÓA
            private readonly IMangaToDetailViewModelMapper _mangaDetailViewModelMapper; // THÊM MỚI
            private readonly IMangaToMangaViewModelMapper _mangaViewModelMapper; // Thêm nếu IMangaToDetailViewModelMapper chưa có
            private readonly IMangaDataExtractor _mangaDataExtractor; // Thêm nếu IMangaToDetailViewModelMapper chưa có

            public MangaDetailsService(
                IMangaApiService mangaApiService,
                // ICoverApiService coverApiService, // XÓA
                ILogger<MangaDetailsService> logger,
                // LocalizationService localizationService, // XÓA
                // JsonConversionService jsonConversionService, // XÓA
                // MangaUtilityService mangaUtilityService, // XÓA
                // MangaTitleService mangaTitleService, // XÓA
                // MangaTagService mangaTagService, // XÓA
                // MangaRelationshipService mangaRelationshipService, // XÓA
                // IMangaFollowService mangaFollowService,
                ChapterService chapterService,
                IHttpContextAccessor httpContextAccessor,
                // MangaDescription mangaDescription, // XÓA
                IMangaToDetailViewModelMapper mangaDetailViewModelMapper, // THÊM MỚI
                IMangaToMangaViewModelMapper mangaViewModelMapper, // Thêm nếu mapper chi tiết chưa có
                IMangaDataExtractor mangaDataExtractor // Thêm nếu mapper chi tiết chưa có
                )
            {
                _mangaApiService = mangaApiService;
                // _coverApiService = coverApiService; // XÓA
                _logger = logger;
                // _localizationService = localizationService; // XÓA
                // _jsonConversionService = jsonConversionService; // XÓA
                // _mangaUtilityService = mangaUtilityService; // XÓA
                // _mangaTitleService = mangaTitleService; // XÓA
                // _mangaTagService = mangaTagService; // XÓA
                // _mangaRelationshipService = mangaRelationshipService; // XÓA
                // _mangaFollowService = mangaFollowService;
                _chapterService = chapterService;
                _httpContextAccessor = httpContextAccessor;
                // _mangaDescription = mangaDescription; // XÓA
                _mangaDetailViewModelMapper = mangaDetailViewModelMapper; // THÊM MỚI
                _mangaViewModelMapper = mangaViewModelMapper;
                _mangaDataExtractor = mangaDataExtractor;
            }

            public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
            {
                try
                {
                    _logger.LogInformation($"Đang lấy chi tiết manga ID: {id}");
                    var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);

                    if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                    {
                        // ... (xử lý lỗi giữ nguyên) ...
                        _logger.LogError($"Không thể lấy chi tiết manga {id}. Response: {mangaResponse?.Result}");
                        return new MangaDetailViewModel { /* ... */ };
                    }

                    var mangaData = mangaResponse.Data;
                    var chapterViewModels = await GetChaptersAsync(id); // Lấy chapter như cũ

                    // Sử dụng mapper mới
                    // Lưu ý: IMangaToDetailViewModelMapper cần được inject IMangaFollowService và IUserService nếu muốn tự xử lý IsFollowing.
                    // Hoặc bạn có thể truyền IsFollowing vào mapper.
                    // Hiện tại, chúng ta sẽ giữ logic IsFollowing ở Controller hoặc service gọi nó.
                    var mangaDetailViewModel = await _mangaDetailViewModelMapper.MapToMangaDetailViewModelAsync(mangaData, chapterViewModels);
                    
                    // Lưu title vào session (nếu cần, có thể mapper tự làm hoặc service này làm)
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null && mangaDetailViewModel.Manga != null && !string.IsNullOrEmpty(mangaDetailViewModel.Manga.Title))
                    {
                        httpContext.Session.SetString($"Manga_{id}_Title", mangaDetailViewModel.Manga.Title);
                    }

                    return mangaDetailViewModel;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chi tiết manga {id}: {jsonEx.Message}");
                    return new MangaDetailViewModel { /* ... */ };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi nghiêm trọng khi lấy chi tiết manga {id}: {ex.Message}");
                    return new MangaDetailViewModel { /* ... */ };
                }
            }

            // XÓA BỎ: private async Task<MangaViewModel> CreateMangaViewModelAsync(Manga? mangaData)
            // Chức năng này giờ đã nằm trong IMangaToMangaViewModelMapper và được IMangaToDetailViewModelMapper sử dụng.

            private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId)
            {
                 // ... (giữ nguyên logic này, vì ChapterService đã được refactor)
                try
                {
                    var chapterViewModels = await _chapterService.GetChaptersAsync(mangaId, "vi,en");
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null && chapterViewModels.Any())
                    {
                        var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                        foreach (var kvp in chaptersByLanguage)
                        {
                            httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{kvp.Key}", JsonSerializer.Serialize(kvp.Value));
                        }
                    }
                    return chapterViewModels;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}: {ex.Message}");
                    return new List<ChapterViewModel>();
                }
            }
        }
    }
    ```
    **Lưu ý quan trọng cho `MangaDetailsService`:** Nếu `IMangaToDetailViewModelMapper` của bạn chưa tự lấy `IsFollowing`, bạn cần lấy nó ở đây (giống như trong `MangaController`) và gán vào `mangaDetailViewModel.Manga.IsFollowing` sau khi map.

### 4.5. Cập nhật `FollowedMangaService.cs`

Service này tạo `List<FollowedMangaViewModel>`.

*   **Mở file `Services\MangaServices\FollowedMangaService.cs`**
*   **Thay đổi constructor và phương thức `GetFollowedMangaListAsync`:**

    ```csharp
    // Services\MangaServices\FollowedMangaService.cs
    using MangaReader.WebUI.Services.AuthServices;
    using MangaReader.WebUI.Services.MangaServices.ChapterServices;
    using MangaReader.WebUI.Services.MangaServices.Models;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // THÊM MỚI

    namespace MangaReader.WebUI.Services.MangaServices
    {
        public class FollowedMangaService : IFollowedMangaService
        {
            private readonly IUserService _userService;
            private readonly IMangaInfoService _mangaInfoService; // Vẫn dùng để lấy MangaInfoViewModel
            private readonly ChapterService _chapterService;
            private readonly ILogger<FollowedMangaService> _logger;
            private readonly TimeSpan _rateLimitDelay;
            private readonly IFollowedMangaViewModelMapper _followedMangaMapper; // THÊM MỚI

            public FollowedMangaService(
                IUserService userService,
                IMangaInfoService mangaInfoService,
                ChapterService chapterService,
                ILogger<FollowedMangaService> logger,
                IFollowedMangaViewModelMapper followedMangaMapper) // THÊM MỚI
            {
                _userService = userService;
                _mangaInfoService = mangaInfoService;
                _chapterService = chapterService;
                _logger = logger;
                _rateLimitDelay = TimeSpan.FromMilliseconds(550);
                _followedMangaMapper = followedMangaMapper; // THÊM MỚI
            }

            public async Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync()
            {
                var followedMangaList = new List<FollowedMangaViewModel>();
                // ... (logic kiểm tra user, lấy user.FollowingManga giữ nguyên) ...
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
                    // ...
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
                            var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                            // Sử dụng mapper mới
                            var followedMangaViewModel = _followedMangaMapper.MapToFollowedMangaViewModel(mangaInfo, latestChapters ?? new List<SimpleChapterInfo>());
                            followedMangaList.Add(followedMangaViewModel);

                            _logger.LogDebug($"Đã xử lý xong manga (qua InfoService): {mangaInfo.MangaTitle}");
                        }
                        // ... (catch giữ nguyên) ...
                        catch (Exception mangaEx)
                        {
                            _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi (sử dụng InfoService).");
                        }
                    }
                    // ... (log cuối giữ nguyên) ...
                     _logger.LogInformation($"Hoàn tất lấy thông tin (qua InfoService) cho {followedMangaList.Count} truyện đang theo dõi.");
                    return followedMangaList;
                }
                // ... (catch giữ nguyên) ...
                catch (Exception ex)
                {
                     _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi (sử dụng InfoService).");
                    return new List<FollowedMangaViewModel>();
                }
            }
        }
    }
    ```

### 4.6. Cập nhật `ReadingHistoryService.cs`

Service này tạo `List<LastReadMangaViewModel>`.

*   **Mở file `Services\MangaServices\ReadingHistoryService.cs`**
*   **Thay đổi constructor và phương thức `GetReadingHistoryAsync`:**

    ```csharp
    // Services\MangaServices\ReadingHistoryService.cs
    using MangaReader.WebUI.Services.AuthServices;
    using MangaReader.WebUI.Services.MangaServices.ChapterServices; // Vẫn cần IChapterInfoService
    using MangaReader.WebUI.Services.MangaServices.Models;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // THÊM MỚI

    namespace MangaReader.WebUI.Services.MangaServices
    {
        // ... (BackendHistoryItem giữ nguyên) ...

        public class ReadingHistoryService : IReadingHistoryService
        {
            private readonly IHttpClientFactory _httpClientFactory;
            private readonly IUserService _userService;
            private readonly IMangaInfoService _mangaInfoService; // Vẫn cần để lấy MangaInfoViewModel
            private readonly IChapterInfoService _chapterInfoService; // Thay thế bằng IChapterToSimpleInfoMapper hoặc giữ nếu ChapterInfo phức tạp
            private readonly IConfiguration _configuration;
            private readonly ILogger<ReadingHistoryService> _logger;
            private readonly TimeSpan _rateLimitDelay;
            private readonly ILastReadMangaViewModelMapper _lastReadMapper; // THÊM MỚI
            private readonly IChapterToSimpleInfoMapper _chapterSimpleInfoMapper; // Thêm nếu ChapterInfo đơn giản

            public ReadingHistoryService(
                IHttpClientFactory httpClientFactory,
                IUserService userService,
                IMangaInfoService mangaInfoService,
                IChapterInfoService chapterInfoService, // Cân nhắc thay thế
                IConfiguration configuration,
                ILogger<ReadingHistoryService> logger,
                ILastReadMangaViewModelMapper lastReadMapper, // THÊM MỚI
                IChapterToSimpleInfoMapper chapterSimpleInfoMapper // Thêm nếu ChapterInfo đơn giản
                )
            {
                _httpClientFactory = httpClientFactory;
                _userService = userService;
                _mangaInfoService = mangaInfoService;
                _chapterInfoService = chapterInfoService; // Cân nhắc
                _configuration = configuration;
                _logger = logger;
                _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 550));
                _lastReadMapper = lastReadMapper; // THÊM MỚI
                _chapterSimpleInfoMapper = chapterSimpleInfoMapper; // Thêm
            }

            public async Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync()
            {
                // ... (logic lấy backendHistory giữ nguyên) ...
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
                    var response = await client.GetAsync("/api/users/reading-history");
                    // ... xử lý response ...
                    if (!response.IsSuccessStatusCode)
                    {
                        // ...
                        return historyViewModels;
                    }
                    var content = await response.Content.ReadAsStringAsync();
                    var backendHistory = JsonSerializer.Deserialize<List<BackendHistoryItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (backendHistory == null || !backendHistory.Any())
                    {
                        _logger.LogInformation("Không có lịch sử đọc nào từ backend.");
                        return historyViewModels;
                    }
                // ...
                foreach (var item in backendHistory)
                {
                    await Task.Delay(_rateLimitDelay);
                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho MangaId: {item.MangaId}. Bỏ qua mục lịch sử này.");
                        continue;
                    }

                    // Lấy thông tin Chapter. Nếu ChapterInfoService đã bị xóa:
                    // Bạn cần một cách để lấy ChapterInfo (có thể là SimpleChapterInfo)
                    // Ví dụ: gọi _chapterApiService.FetchChapterInfoAsync rồi map bằng _chapterSimpleInfoMapper
                    // Hoặc nếu _chapterInfoService vẫn còn (chưa xóa ở bước 3), thì giữ nguyên:
                    var chapterInfo = await _chapterInfoService.GetChapterInfoAsync(item.ChapterId);
                    // Nếu đã xóa ChapterInfoService và dùng IChapterToSimpleInfoMapper:
                    // var chapterApiResponse = await _chapterApiService.FetchChapterInfoAsync(item.ChapterId);
                    // ChapterInfo chapterInfo = null;
                    // if (chapterApiResponse?.Data != null) {
                    //      var simpleInfo = _chapterSimpleInfoMapper.MapToSimpleChapterInfo(chapterApiResponse.Data);
                    //      chapterInfo = new ChapterInfo { Id = simpleInfo.ChapterId, Title = simpleInfo.DisplayTitle, PublishedAt = simpleInfo.PublishedAt };
                    // }

                    if (chapterInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho ChapterId: {item.ChapterId}. Bỏ qua mục lịch sử này.");
                        continue;
                    }

                    // Sử dụng mapper mới
                    var historyViewModel = _lastReadMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfo, item.LastReadAt);
                    historyViewModels.Add(historyViewModel);
                    
                    _logger.LogDebug($"Đã xử lý xong mục lịch sử cho manga: {mangaInfo.MangaTitle}, chapter: {chapterInfo.Title}");
                }
                // ... (log và return giữ nguyên) ...
                _logger.LogInformation($"Hoàn tất xử lý {historyViewModels.Count} mục lịch sử đọc.");
                return historyViewModels;
                }
                // ... (catch giữ nguyên) ...
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi ngoại lệ khi lấy và xử lý lịch sử đọc.");
                    return historyViewModels;
                }
            }
        }
    }
    ```

### 4.7. Cập nhật `ChapterReadingServices.cs`

Service này tạo `ChapterReadViewModel`.

*   **Mở file `Services\MangaServices\ChapterServices\ChapterReadingServices.cs`**
*   **Thay đổi constructor và phương thức liên quan:**

    ```csharp
    // Services\MangaServices\ChapterServices\ChapterReadingServices.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Models.Mangadex;
    // using MangaReader.WebUI.Services.MangaServices.MangaInformation; // MangaTitleService không cần trực tiếp
    using System.Text.Json;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // THÊM MỚI

    namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
    {
        public class ChapterReadingServices
        {
            private readonly IChapterApiService _chapterApiService;
            private readonly MangaIdService _mangaIdService;
            private readonly ChapterLanguageServices _chapterLanguageServices;
            // private readonly MangaTitleService _mangaTitleService; // XÓA
            private readonly ChapterService _chapterService; // Vẫn cần để lấy SiblingChapters
            private readonly ILogger<ChapterReadingServices> _logger;
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly string _backendBaseUrl;
            private readonly IMangaDataExtractor _mangaDataExtractor; // THÊM MỚI
            private readonly IMangaApiService _mangaApiService; // Thêm để lấy manga title

            public ChapterReadingServices(
                IChapterApiService chapterApiService,
                MangaIdService mangaIdService,
                ChapterLanguageServices chapterLanguageServices,
                // MangaTitleService mangaTitleService, // XÓA
                ChapterService chapterService,
                IHttpContextAccessor httpContextAccessor,
                IConfiguration configuration,
                ILogger<ChapterReadingServices> logger,
                IMangaDataExtractor mangaDataExtractor, // THÊM MỚI
                IMangaApiService mangaApiService // Thêm
                )
            {
                _chapterApiService = chapterApiService;
                _mangaIdService = mangaIdService;
                _chapterLanguageServices = chapterLanguageServices;
                // _mangaTitleService = mangaTitleService; // XÓA
                _chapterService = chapterService;
                _httpContextAccessor = httpContextAccessor;
                _logger = logger;
                _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                                 ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
                _mangaDataExtractor = mangaDataExtractor; // THÊM MỚI
                _mangaApiService = mangaApiService; // Thêm
            }

            public async Task<ChapterReadViewModel> GetChapterReadViewModel(string chapterId)
            {
                try
                {
                    // ... (lấy atHomeResponse, pages, mangaId, currentChapterLanguage giữ nguyên) ...
                    var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
                    if (atHomeResponse == null /* ... */) { /* ... */ throw new Exception("..."); }
                    var pages = atHomeResponse.Chapter.Data.Select(f => $"{_backendBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"{atHomeResponse.BaseUrl}/data/{atHomeResponse.Chapter.Hash}/{f}")}").ToList();
                    string mangaId = await _mangaIdService.GetMangaIdFromChapterAsync(chapterId);
                    string currentChapterLanguage = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);


                    // Lấy Manga Title bằng cách gọi API Manga rồi dùng Extractor
                    string mangaTitle = await GetMangaTitleAsync(mangaId);

                    var chaptersList = await GetChaptersAsync(mangaId, currentChapterLanguage);

                    var (currentChapterViewModel, prevChapterId, nextChapterId) =
                        FindCurrentAndAdjacentChapters(chaptersList, chapterId, currentChapterLanguage);
                    
                    // Lấy thông tin chapter hiện tại để trích xuất title và number
                    ChapterAttributes currentChapterAttributes = null;
                    var currentChapterDataResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);
                    if (currentChapterDataResponse?.Data?.Attributes != null)
                    {
                        currentChapterAttributes = currentChapterDataResponse.Data.Attributes;
                    }
                    
                    string displayChapterTitle = "Không xác định";
                    string displayChapterNumber = "?";

                    if (currentChapterAttributes != null)
                    {
                        displayChapterTitle = _mangaDataExtractor.ExtractChapterDisplayTitle(currentChapterAttributes);
                        displayChapterNumber = _mangaDataExtractor.ExtractChapterNumber(currentChapterAttributes);
                    } 
                    else if (currentChapterViewModel != null) // Fallback nếu API lỗi, dùng từ Sibling
                    {
                        displayChapterTitle = currentChapterViewModel.Title;
                        displayChapterNumber = currentChapterViewModel.Number;
                    }


                    var viewModel = new ChapterReadViewModel
                    {
                        MangaId = mangaId,
                        MangaTitle = mangaTitle,
                        ChapterId = chapterId,
                        ChapterTitle = displayChapterTitle, 
                        ChapterNumber = displayChapterNumber, 
                        ChapterLanguage = currentChapterLanguage,
                        Pages = pages,
                        PrevChapterId = prevChapterId,
                        NextChapterId = nextChapterId,
                        SiblingChapters = chaptersList
                    };

                    return viewModel;
                }
                // ... (catch giữ nguyên) ...
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi tải chapter {chapterId}");
                    throw;
                }
            }
            
            public async Task<string> GetMangaTitleAsync(string mangaId)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                string sessionTitle = httpContext?.Session.GetString($"Manga_{mangaId}_Title");
                if (!string.IsNullOrEmpty(sessionTitle))
                {
                    return sessionTitle;
                }
                // Lấy từ API và dùng extractor
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);
                if (mangaResponse?.Data?.Attributes != null)
                {
                    string title = _mangaDataExtractor.ExtractMangaTitle(mangaResponse.Data.Attributes.Title, mangaResponse.Data.Attributes.AltTitles);
                     if (httpContext != null && !string.IsNullOrEmpty(title) && title != "Không có tiêu đề")
                    {
                        httpContext.Session.SetString($"Manga_{mangaId}_Title", title);
                    }
                    return title;
                }
                return "Không có tiêu đề";
            }

            // ... (GetChaptersAsync, FindCurrentAndAdjacentChapters, ParseChapterNumber giữ nguyên)
            private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string language)
            {
                 var httpContext = _httpContextAccessor.HttpContext;
                 var sessionChaptersJson = httpContext?.Session.GetString($"Manga_{mangaId}_Chapters_{language}");

                 if (!string.IsNullOrEmpty(sessionChaptersJson))
                 {
                     try
                     {
                         var chaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(sessionChaptersJson);
                         if (chaptersList != null && chaptersList.Any()) return chaptersList;
                     }
                     catch (JsonException ex)
                     {
                          _logger.LogWarning(ex, $"Lỗi deserialize chapters từ session cho manga {mangaId}, ngôn ngữ {language}. Sẽ lấy lại từ API.");
                     }
                 }
                 return await GetChaptersFromApiAsync(mangaId, language);
            }
        
            private async Task<List<ChapterViewModel>> GetChaptersFromApiAsync(string mangaId, string language)
            {
                var allChapters = await _chapterService.GetChaptersAsync(mangaId, language); // ChapterService đã refactor
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                     var chaptersByLanguage = _chapterService.GetChaptersByLanguage(allChapters);
                     if (chaptersByLanguage.TryGetValue(language, out var chaptersInLanguage))
                     {
                         httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                         return chaptersInLanguage;
                     }
                }
                return new List<ChapterViewModel>();
            }
            
            private (ChapterViewModel currentChapter, string prevId, string nextId) FindCurrentAndAdjacentChapters(
                List<ChapterViewModel> chapters, string chapterId, string language)
            {
                var currentChapter = chapters.FirstOrDefault(c => c.Id == chapterId);
                if (currentChapter == null)
                {
                    return (new ChapterViewModel { Id = chapterId, Title = "Chương không xác định", Number = "?", Language = language }, null, null);
                }
                var sortedChapters = chapters
                    .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                    .OrderBy(c => c.Number ?? double.MaxValue)
                    .Select(c => c.Chapter)
                    .ToList();
                int index = sortedChapters.FindIndex(c => c.Id == chapterId);
                string prevId = (index > 0) ? sortedChapters[index - 1].Id : null;
                string nextId = (index >= 0 && index < sortedChapters.Count - 1) ? sortedChapters[index + 1].Id : null;
                return (currentChapter, prevId, nextId);
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

### 4.8. Kiểm tra và cập nhật `ChapterAttributeService.cs` (nếu còn giữ lại)

Nếu bạn quyết định giữ lại `ChapterAttributeService.cs` cho một số chức năng cụ thể không thuộc `IMangaDataExtractor` (ví dụ, nếu nó có logic phức tạp riêng), hãy đảm bảo nó không còn phụ thuộc vào các service đã bị xóa. Tuy nhiên, khả năng cao là các phương thức của nó đã được `IMangaDataExtractor` bao phủ.

**Hành động khuyến nghị:**
*   Xóa file `Services\MangaServices\ChapterServices\ChapterAttributeService.cs`.
*   Gỡ đăng ký khỏi `Program.cs`:
    ```csharp
    // Program.cs - Gỡ bỏ
    // builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterAttributeService>();
    ```

## Bước 5: Kiểm tra lại `Program.cs`

Sau khi thực hiện các thay đổi, hãy kiểm tra lại `Program.cs` một lần nữa để đảm bảo:
*   Tất cả các service mới trong `DataProcessing` đã được đăng ký.
*   Tất cả các service cũ đã bị xóa hoặc các service nhỏ không cần thiết đã được gỡ bỏ đăng ký.
*   Không còn đăng ký nào trỏ đến các file đã bị xóa.

Ví dụ, phần đăng ký service của bạn bây giờ sẽ trông gọn gàng hơn cho các service liên quan đến thông tin manga.