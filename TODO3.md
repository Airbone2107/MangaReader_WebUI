# TODO: Tối ưu hóa Luồng Lấy Ảnh Bìa Manga

**Mục tiêu:** Thay đổi cách lấy ảnh bìa cho các danh sách manga (Trang chủ, Tìm kiếm, Theo dõi, Lịch sử) để sử dụng thông tin Cover Art được trả về trực tiếp từ API Manga (thông qua `includes[]=cover_art`), thay vì gọi API lấy ảnh bìa riêng lẻ. Giữ nguyên cách lấy tất cả ảnh bìa cho trang chi tiết Manga.

**Các bước thực hiện:**

## Bước 1: Cập nhật API Services để Luôn Include Cover Art

Mục đích: Đảm bảo các lệnh gọi API lấy danh sách manga luôn kèm theo thông tin về cover art.

1.  **Sửa `Services\APIServices\Services\MangaApiService.cs`:**
    *   Trong phương thức `FetchMangaAsync`: Thêm `AddOrUpdateParam(queryParams, "includes[]", "cover_art");` để luôn yêu cầu dữ liệu cover.
    *   Trong phương thức `FetchMangaByIdsAsync`: Đảm bảo `includes[]=cover_art` đã có hoặc thêm vào.

    ```csharp
    // Services\APIServices\Services\MangaApiService.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using System.Diagnostics;

    namespace MangaReader.WebUI.Services.APIServices.Services
    {
        // ... (using statements)
        public class MangaApiService(
            HttpClient httpClient,
            ILogger<MangaApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
              IMangaApiService
        {
            /// <inheritdoc/>
            public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
            {
                // ... (logging and existing parameter setup)
                var queryParams = new Dictionary<string, List<string>>();

                if (limit.HasValue) AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());
                if (offset.HasValue) AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

                // ... (existing SortManga parameter handling)

                // Luôn bao gồm các relationship cần thiết
                AddOrUpdateParam(queryParams, "includes[]", "author"); // Giữ nguyên
                AddOrUpdateParam(queryParams, "includes[]", "artist"); // Giữ nguyên
                AddOrUpdateParam(queryParams, "includes[]", "cover_art"); // *** LUÔN THÊM COVER ART ***

                var url = BuildUrlWithParams("manga", queryParams);
                Logger.LogInformation("Constructed manga fetch URL: {Url}", url);

                // ... (rest of the method)
                var mangaList = await GetApiAsync<MangaList>(url);
                // ... (error handling)
                return mangaList;
            }

            /// <inheritdoc/>
            public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
            {
                Logger.LogInformation("Fetching details for manga ID: {MangaId}", mangaId);
                var queryParams = new Dictionary<string, List<string>> {
                    // Đảm bảo cover_art luôn có trong danh sách includes cho details
                    { "includes[]", new List<string> { "author", "artist", "cover_art", "tag" } }
                };
                var url = BuildUrlWithParams($"manga/{mangaId}", queryParams);
                Logger.LogInformation("Constructed manga details fetch URL: {Url}", url);

                // ... (rest of the method)
                 var mangaResponse = await GetApiAsync<MangaResponse>(url);
                 // ... (error handling)
                 return mangaResponse;
            }

            /// <inheritdoc/>
            public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
            {
                 // ... (check mangaIds)
                Logger.LogInformation("Fetching manga by IDs: [{MangaIds}]", string.Join(", ", mangaIds));
                var queryParams = new Dictionary<string, List<string>>();
                foreach (var id in mangaIds)
                {
                    AddOrUpdateParam(queryParams, "ids[]", id);
                }
                AddOrUpdateParam(queryParams, "includes[]", "cover_art"); // *** LUÔN THÊM COVER ART ***
                // Có thể thêm author, artist nếu cần cho các trang danh sách này
                // AddOrUpdateParam(queryParams, "includes[]", "author");
                // AddOrUpdateParam(queryParams, "includes[]", "artist");
                AddOrUpdateParam(queryParams, "limit", mangaIds.Count.ToString());

                var url = BuildUrlWithParams("manga", queryParams);
                Logger.LogInformation("Constructed manga fetch by IDs URL: {Url}", url);

                // ... (rest of the method)
                var mangaList = await GetApiAsync<MangaList>(url);
                // ... (error handling)
                return mangaList;
            }
        }
    }
    ```

## Bước 2: Tạo Helper để Trích xuất Cover Filename từ Relationships

Mục đích: Có một phương thức tập trung để lấy filename của ảnh bìa chính từ danh sách relationships của đối tượng `Manga`.

1.  **Sửa `Services\APIServices\Services\CoverApiService.cs`:**
    *   Thêm một phương thức `public static string? ExtractCoverFileNameFromRelationships(List<Relationship>? relationships)` vào lớp `CoverApiService`. Phương thức này sẽ tìm relationship `cover_art` và trả về `fileName` từ attributes của nó. Chúng ta dùng `static` vì nó không cần trạng thái của instance.

    ```csharp
    // Services\APIServices\Services\CoverApiService.cs
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using System.Diagnostics;
    using System.Text.Json; // Thêm using này

    namespace MangaReader.WebUI.Services.APIServices.Services
    {
        // ... (using statements)
        public class CoverApiService(
            // ... constructor
        )
            : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
              ICoverApiService
        {
            // ... (existing fields)

            /// <summary>
            /// Helper tĩnh để trích xuất filename của cover art chính từ danh sách relationships.
            /// Ưu tiên relationship 'cover_art' đầu tiên tìm thấy.
            /// </summary>
            /// <param name="relationships">Danh sách relationships từ đối tượng Manga.</param>
            /// <param name="logger">Logger để ghi log (truyền vào từ lớp gọi).</param>
            /// <returns>Filename của cover art hoặc null nếu không tìm thấy.</returns>
            public static string? ExtractCoverFileNameFromRelationships(List<Relationship>? relationships, ILogger? logger = null)
            {
                if (relationships == null || !relationships.Any())
                {
                    logger?.LogWarning("ExtractCoverFileName: Danh sách relationships rỗng hoặc null.");
                    return null;
                }

                var coverRelationship = relationships.FirstOrDefault(r => r.Type == "cover_art");

                if (coverRelationship == null)
                {
                    logger?.LogWarning("ExtractCoverFileName: Không tìm thấy relationship có type 'cover_art'.");
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
                        }
                        logger?.LogWarning("ExtractCoverFileName: Thuộc tính 'fileName' không tồn tại hoặc không phải string trong attributes của relationship {RelationshipId}.", coverRelationship.Id);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "ExtractCoverFileName: Lỗi khi đọc attributes của relationship {RelationshipId}.", coverRelationship.Id);
                    }
                }
                else
                {
                     logger?.LogWarning("ExtractCoverFileName: Relationship 'cover_art' {RelationshipId} không có attributes hoặc attributes không phải là object. Đảm bảo 'includes[]=cover_art' được sử dụng.", coverRelationship.Id);
                }

                // Fallback nếu không lấy được filename từ attributes (ít khả năng xảy ra nếu include đúng)
                logger?.LogWarning("ExtractCoverFileName: Không thể trích xuất filename từ attributes của cover art {RelationshipId}. Fallback trả về null.", coverRelationship.Id);
                return null;
            }

            // ... (existing methods: GetAllCoversForMangaAsync, FetchRepresentativeCoverUrlsAsync, FetchCoverUrlAsync, FetchCoversForMangaAsync)
            // CHÚNG TA SẼ XÓA FetchRepresentativeCoverUrlsAsync và FetchCoverUrlAsync Ở BƯỚC SAU

            /// <summary>
            /// Helper tạo URL proxy cho ảnh bìa với kích thước tùy chọn.
            /// (Giữ nguyên phương thức này)
            /// </summary>
            public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
            {
                var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.{size}.jpg";
                // Hoặc sử dụng URL thumbnail '.256.jpg' nếu muốn ảnh nhỏ hơn
                // var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.256.jpg";
                return $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
            }
        }
    }
    ```

## Bước 3: Cập nhật Logic Tạo MangaViewModel

Mục đích: Sử dụng thông tin cover art đã include để tạo URL ảnh bìa, thay vì gọi `CoverApiService` riêng.

1.  **Sửa `Services\MangaServices\MangaPageService\MangaSearchService.cs`:**
    *   Trong phương thức `ConvertToMangaViewModelsAsync`:
        *   Xóa bỏ việc gọi `_coverApiService.FetchRepresentativeCoverUrlsAsync`.
        *   Trong vòng lặp `foreach (var manga in mangaList)`:
            *   Gọi `CoverApiService.ExtractCoverFileNameFromRelationships(manga.Relationships, _logger)` để lấy `fileName`.
            *   Gọi `_coverApiService.GetProxiedCoverUrl(id, fileName)` để tạo `coverUrl`.

    ```csharp
    // Services\MangaServices\MangaPageService\MangaSearchService.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Services.MangaServices.MangaInformation;
    using MangaReader.WebUI.Services.UtilityServices;
    using System.Text.Json;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.APIServices.Services; // Thêm using cho CoverApiService static method

    namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
    {
        public class MangaSearchService
        {
            // ... (constructor and other methods)

            /// <summary>
            /// Chuyển đổi kết quả từ API sang danh sách MangaViewModel
            /// </summary>
            private async Task<List<MangaViewModel>> ConvertToMangaViewModelsAsync(List<MangaReader.WebUI.Models.Mangadex.Manga>? mangaList)
            {
                var mangaViewModels = new List<MangaViewModel>();
                if (mangaList == null || !mangaList.Any())
                {
                    return mangaViewModels;
                }

                // *** KHÔNG CẦN LẤY COVER RIÊNG NỮA ***
                // var mangaIds = mangaList.Select(m => m.Id.ToString()).ToList();
                // var coverUrls = await _coverApiService.FetchRepresentativeCoverUrlsAsync(mangaIds) ?? new Dictionary<string, string>();

                foreach (var manga in mangaList)
                {
                    try
                    {
                        if (manga.Attributes == null)
                        {
                            _logger.LogWarning($"Manga ID: {manga.Id} không có thuộc tính Attributes");
                            continue;
                        }

                        string id = manga.Id.ToString();
                        var attributes = manga.Attributes;

                        // Lấy title
                        string title = _mangaTitleService.GetMangaTitle(
                            attributes.Title,
                            attributes.AltTitles
                        );

                        // Lấy author/artist
                        var (author, artist) = _mangaRelationshipService.GetAuthorArtist(
                            manga.Relationships // Truyền trực tiếp relationships
                        );

                        // Lấy description
                        string description = _mangaDescriptionService.GetDescription(
                            attributes
                        );

                        // Lấy status
                        string status = _localizationService.GetStatus(attributes.Status); // Truyền status trực tiếp

                        // Lấy datetime
                        DateTime? lastUpdated = attributes.UpdatedAt.DateTime;

                        // *** LẤY COVER TỪ RELATIONSHIP ***
                        string coverUrl = "/images/cover-placeholder.jpg"; // Mặc định
                        var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(manga.Relationships, _logger);
                        if (!string.IsNullOrEmpty(coverFileName))
                        {
                            // Sử dụng GetProxiedCoverUrl từ CoverApiService instance
                            coverUrl = _coverApiService.GetProxiedCoverUrl(id, coverFileName);
                        }
                        else {
                            _logger.LogWarning($"Không tìm thấy cover filename cho manga ID {id} từ relationships.");
                        }
                        // ********************************

                        var viewModel = new MangaViewModel
                        {
                            Id = id,
                            Title = title,
                            Author = author,
                            Artist = artist,
                            Description = description,
                            CoverUrl = coverUrl, // Sử dụng coverUrl mới
                            Status = status,
                            LastUpdated = lastUpdated,
                            // Lấy tags từ attributes nếu có (API Service đã include tag chưa?)
                            Tags = _mangaTagService.GetMangaTags(attributes),
                            // Thêm các trường khác nếu cần
                            OriginalLanguage = attributes.OriginalLanguage,
                            PublicationDemographic = attributes.PublicationDemographic,
                            ContentRating = attributes.ContentRating
                        };

                        mangaViewModels.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi chuyển đổi manga ID: {manga?.Id}");
                        continue;
                    }
                }

                return mangaViewModels;
            }
        }
    }
    ```

2.  **Sửa `Services\MangaServices\MangaInfoService.cs`:**
    *   Trong phương thức `GetMangaInfoAsync`:
        *   Xóa bỏ việc gọi `_coverApiService.FetchCoverUrlAsync`.
        *   Sau khi gọi `_mangaApiService.FetchMangaDetailsAsync`, lấy `mangaResponse.Data.Relationships`.
        *   Gọi `CoverApiService.ExtractCoverFileNameFromRelationships` để lấy `fileName`.
        *   Gọi `_coverApiService.GetProxiedCoverUrl` để tạo `coverUrl`.

    ```csharp
    // Services\MangaServices\MangaInfoService.cs
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.MangaServices.MangaInformation;
    using MangaReader.WebUI.Services.MangaServices.Models;
    using MangaReader.WebUI.Services.APIServices.Services; // Thêm using cho CoverApiService static method

    namespace MangaReader.WebUI.Services.MangaServices
    {
        public class MangaInfoService : IMangaInfoService
        {
            private readonly MangaTitleService _mangaTitleService;
            private readonly IMangaApiService _mangaApiService;
            private readonly ICoverApiService _coverApiService;
            private readonly ILogger<MangaInfoService> _logger;

            public MangaInfoService(
                MangaTitleService mangaTitleService,
                IMangaApiService mangaApiService,
                ICoverApiService coverApiService,
                ILogger<MangaInfoService> logger)
            {
                _mangaTitleService = mangaTitleService;
                _mangaApiService = mangaApiService;
                _coverApiService = coverApiService;
                _logger = logger;
            }

            public async Task<MangaInfoViewModel> GetMangaInfoAsync(string mangaId)
            {
                // ... (kiểm tra mangaId)
                try
                {
                    _logger.LogInformation($"Bắt đầu lấy thông tin cơ bản cho manga ID: {mangaId}");

                    // 1. Lấy tiêu đề manga (đã bao gồm gọi API MangaDetails)
                    // Phương thức này đã gọi FetchMangaDetailsAsync và bao gồm relationships
                    string mangaTitle = await _mangaTitleService.GetMangaTitleFromIdAsync(mangaId);

                    // 2. Lấy lại thông tin Manga chi tiết để lấy relationships (hơi thừa, có thể tối ưu bằng cách trả về cả Manga object từ GetMangaTitleFromIdAsync)
                    // Tạm thời gọi lại API để lấy relationships
                    var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);
                    string coverUrl = "/images/cover-placeholder.jpg"; // Mặc định

                    if (mangaResponse?.Result == "ok" && mangaResponse.Data != null)
                    {
                        // *** LẤY COVER TỪ RELATIONSHIP CỦA MANGA RESPONSE ***
                        var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(mangaResponse.Data.Relationships, _logger);
                        if (!string.IsNullOrEmpty(coverFileName))
                        {
                            coverUrl = _coverApiService.GetProxiedCoverUrl(mangaId, coverFileName);
                        }
                        else
                        {
                             _logger.LogWarning($"Không tìm thấy cover filename cho manga ID {mangaId} từ relationships trong MangaInfoService.");
                        }
                    } else {
                         _logger.LogWarning($"Không thể lấy lại chi tiết manga {mangaId} để trích xuất cover trong MangaInfoService.");
                    }
                     // **********************************************************

                    // *** XÓA LỆNH GỌI COVER API RIÊNG ***
                    // string coverUrl = await _coverApiService.FetchCoverUrlAsync(mangaId);

                    if (string.IsNullOrEmpty(mangaTitle) || mangaTitle == "Không có tiêu đề")
                    {
                        _logger.LogWarning($"Không thể lấy tiêu đề cho manga ID: {mangaId}. Sử dụng ID làm tiêu đề.");
                        mangaTitle = $"Manga ID: {mangaId}";
                    }

                    if (string.IsNullOrEmpty(coverUrl)) {
                         coverUrl = "/images/cover-placeholder.jpg";
                    }


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
                    // ... (error handling)
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

3.  **Sửa `Controllers\HomeController.cs`:**
    *   Trong phương thức `Index` và `GetLatestMangaPartial`:
        *   Xóa bỏ việc gọi `_coverApiService.FetchCoverUrlAsync`.
        *   Trong vòng lặp `foreach (var manga in mangaListToProcess)`:
            *   Gọi `CoverApiService.ExtractCoverFileNameFromRelationships(manga.Relationships, _logger)` để lấy `fileName`.
            *   Gọi `_coverApiService.GetProxiedCoverUrl(id, fileName)` để tạo `coverUrl`.

    ```csharp
    // Controllers\HomeController.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Services.MangaServices;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Text.Json;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.APIServices.Services; // Thêm using này

    namespace MangaReader.WebUI.Controllers
    {
        public class HomeController : Controller
        {
            // ... (constructor)

            public async Task<IActionResult> Index()
            {
                // ... (try-catch, check connection)
                try
                {
                    var sortOptions = new SortManga {
                        SortBy = "Mới cập nhật", // Hoặc "latest"
                        Languages = new List<string> { "vi", "en" }
                        // Thêm các tùy chọn khác nếu cần
                    };

                    // Gọi API service đã được cập nhật (đã bao gồm cover_art)
                    var recentMangaResponse = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);

                    if (recentMangaResponse?.Data == null || !recentMangaResponse.Data.Any())
                    {
                        // ... (handle no data)
                        return View("Index", new List<MangaViewModel>());
                    }

                    var viewModels = new List<MangaViewModel>();
                    var mangaListToProcess = recentMangaResponse.Data; // Không cần ToList() nếu Data là List

                    foreach (var manga in mangaListToProcess)
                    {
                        try
                        {
                            string id = manga.Id.ToString();
                            var attributes = manga.Attributes;

                            if (attributes == null)
                            {
                                _logger.LogWarning($"Manga ID: {id} không có thuộc tính Attributes");
                                continue;
                            }

                            string title = GetLocalizedTitle(JsonSerializer.Serialize(attributes.Title ?? new Dictionary<string, string>()));

                            // *** LẤY COVER TỪ RELATIONSHIP ***
                            string coverUrl = "/images/cover-placeholder.jpg"; // Mặc định
                            var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(manga.Relationships, _logger);
                             if (!string.IsNullOrEmpty(coverFileName))
                            {
                                coverUrl = _coverApiService.GetProxiedCoverUrl(id, coverFileName);
                            }
                            else {
                                _logger.LogWarning($"Không tìm thấy cover filename cho manga ID {id} từ relationships trong HomeController.");
                            }
                            // ********************************

                            // *** XÓA LỆNH GỌI COVER API RIÊNG ***
                            // string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);

                             viewModels.Add(new MangaViewModel
                            {
                                Id = id,
                                Title = title,
                                CoverUrl = coverUrl
                                // Thêm các thuộc tính khác nếu cần cho trang chủ
                            });
                        }
                        catch (Exception ex)
                        {
                           _logger.LogError(ex, $"Lỗi khi xử lý manga ID: {manga?.Id} trên trang chủ.");
                        }
                    }
                    // ... (handle empty viewModels, HTMX request)
                     return View("Index", viewModels);
                }
                 // ... (catch blocks)
            }

             // Phương thức GetLatestMangaPartial cũng cần sửa tương tự
            public async Task<IActionResult> GetLatestMangaPartial()
            {
                 try
                 {
                      var sortOptions = new SortManga {
                          SortBy = "Mới cập nhật",
                          Languages = new List<string> { "vi", "en" }
                      };

                      var recentMangaResponse = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);

                      var viewModels = new List<MangaViewModel>();
                      var mangaListToProcess = recentMangaResponse?.Data ?? new List<MangaReader.WebUI.Models.Mangadex.Manga>();

                      foreach (var manga in mangaListToProcess)
                      {
                           try
                           {
                                string id = manga.Id.ToString();
                                var attributes = manga.Attributes;
                                if (attributes == null) continue;

                                string title = GetLocalizedTitle(JsonSerializer.Serialize(attributes.Title ?? new Dictionary<string, string>()));

                                // *** LẤY COVER TỪ RELATIONSHIP ***
                                string coverUrl = "/images/cover-placeholder.jpg";
                                var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(manga.Relationships, _logger);
                                if (!string.IsNullOrEmpty(coverFileName))
                                {
                                     coverUrl = _coverApiService.GetProxiedCoverUrl(id, coverFileName);
                                }
                                 else {
                                      _logger.LogWarning($"Không tìm thấy cover filename cho manga ID {id} từ relationships trong GetLatestMangaPartial.");
                                }
                                // ********************************

                                // *** XÓA LỆNH GỌI COVER API RIÊNG ***
                                // string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);

                                viewModels.Add(new MangaViewModel
                                {
                                     Id = id,
                                     Title = title,
                                     CoverUrl = coverUrl
                                });
                           }
                           catch (Exception ex)
                           {
                                _logger.LogError(ex, $"Lỗi khi xử lý manga ID: {manga?.Id} trong partial.");
                           }
                      }

                      return PartialView("_MangaGridPartial", viewModels);
                 }
                 catch (Exception ex)
                 {
                      _logger.LogError(ex, "Lỗi khi tải danh sách manga mới nhất cho partial.");
                      return PartialView("_ErrorPartial", "Không thể tải danh sách manga mới nhất.");
                 }
            }

            // ... (other methods like ApiTest, Privacy, Error, GetLocalizedTitle)
             private string GetLocalizedTitle(string titleJson)
             {
                 // ... (implementation remains the same)
             }
        }
    }
    ```

4.  **Sửa `Services\MangaServices\MangaPageService\MangaDetailsService.cs`:**
    *   Trong phương thức `CreateMangaViewModelAsync`: Đảm bảo việc lấy `coverUrl` sử dụng `ExtractCoverFileNameFromRelationships` và `GetProxiedCoverUrl`. *Phương thức này đã bao gồm logic này trong bước 3.1, nhưng cần kiểm tra lại.*
    *   **Quan trọng:** Đảm bảo phương thức này **KHÔNG** xóa bỏ logic nào liên quan đến việc gọi `_coverApiService.GetAllCoversForMangaAsync` nếu logic đó được dùng để hiển thị gallery ảnh bìa *sau này* trong trang Details (ví dụ: trong một tab riêng hoặc modal). Logic lấy ảnh bìa *chính* phải dùng relationship, logic lấy *tất cả* ảnh bìa dùng `GetAllCoversForMangaAsync`.

    ```csharp
    // Services\MangaServices\MangaPageService\MangaDetailsService.cs
    using MangaReader.WebUI.Models;
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReader.WebUI.Services.MangaServices.ChapterServices;
    using MangaReader.WebUI.Services.MangaServices.MangaInformation;
    using MangaReader.WebUI.Services.UtilityServices;
    using System.Text.Json;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using MangaReader.WebUI.Services.APIServices.Services; // Thêm using này

    namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
    {
        public class MangaDetailsService
        {
            // ... (constructor)

            public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
            {
                // ... (lấy mangaResponse)
                var mangaData = mangaResponse.Data;

                // Tạo MangaViewModel (đã sử dụng cover từ relationship)
                var mangaViewModel = await CreateMangaViewModelAsync(mangaData);

                // Lấy danh sách chapters
                var chapterViewModels = await GetChaptersAsync(id);

                // *** NẾU CÓ LOGIC HIỂN THỊ GALLERY COVER, GIỮ NGUYÊN HOẶC THÊM VÀO ĐÂY ***
                // Ví dụ: Có thể fetch tất cả covers và thêm vào ViewModel nếu cần
                // var allCoversResponse = await _coverApiService.GetAllCoversForMangaAsync(id);
                // if (allCoversResponse?.Data != null)
                // {
                //     mangaViewModel.AllCoverUrls = allCoversResponse.Data
                //         .Select(c => _coverApiService.GetProxiedCoverUrl(id, c.Attributes?.FileName))
                //         .Where(url => !string.IsNullOrEmpty(url))
                //         .ToList();
                // }
                // *** KẾT THÚC PHẦN GALLERY COVER (Ví dụ) ***

                return new MangaDetailViewModel
                {
                    Manga = mangaViewModel,
                    Chapters = chapterViewModels
                };
                // ... (catch blocks)
            }

            // ... (GetAlternativeTitlesByLanguageAsync)

            /// <summary>
            /// Tạo đối tượng MangaViewModel từ dữ liệu manga
            /// </summary>
            private async Task<MangaViewModel> CreateMangaViewModelAsync(Manga? mangaData)
            {
                 // ... (kiểm tra null)
                string id = mangaData.Id.ToString();
                var attributes = mangaData.Attributes;

                try
                {
                    // ... (lấy title, lưu session, lấy description, tags, author/artist)

                    // *** LẤY COVER CHÍNH TỪ RELATIONSHIP ***
                    string coverUrl = "/images/cover-placeholder.jpg"; // Mặc định
                    var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(mangaData.Relationships, _logger);
                    if (!string.IsNullOrEmpty(coverFileName))
                    {
                        coverUrl = _coverApiService.GetProxiedCoverUrl(id, coverFileName);
                    }
                     else {
                          _logger.LogWarning($"Không tìm thấy cover filename cho manga ID {id} từ relationships trong CreateMangaViewModelAsync.");
                     }
                     // *************************************

                    // *** XÓA LỆNH GỌI FetchCoverUrlAsync RIÊNG (NẾU CÓ) ***
                    // string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);

                    // ... (lấy status, các thuộc tính khác, isFollowing)

                    return new MangaViewModel
                    {
                        Id = id,
                        Title = mangaTitle,
                        Description = description,
                        CoverUrl = coverUrl, // Sử dụng coverUrl đã lấy từ relationship
                        Status = status,
                        Tags = tags,
                        Author = author,
                        Artist = artist,
                        // ... (các thuộc tính khác)
                        IsFollowing = isFollowing,
                        // ...
                    };
                }
                catch (Exception ex)
                {
                     // ... (error handling)
                     return new MangaViewModel { Id = id, Title = "Lỗi tạo ViewModel" };
                }
            }
            // ... (GetChaptersAsync)
        }
    }
    ```

## Bước 4: (Tùy chọn) Dọn dẹp Code Thừa

Mục đích: Xóa các phương thức không còn được sử dụng trong `CoverApiService`.

1.  **Kiểm tra lại Code:** Tìm kiếm tất cả các nơi sử dụng `FetchRepresentativeCoverUrlsAsync` và `FetchCoverUrlAsync` trong toàn bộ dự án. Đảm bảo chúng thực sự không còn cần thiết (kể cả trong các trường hợp fallback hoặc logic đặc biệt nào đó).
2.  **Xóa Phương thức:**
    *   Xóa bỏ phương thức `FetchRepresentativeCoverUrlsAsync` và `FetchCoverUrlAsync` khỏi interface `Services\APIServices\Interfaces\ICoverApiService.cs`.
    *   Xóa bỏ việc triển khai các phương thức này trong lớp `Services\APIServices\Services\CoverApiService.cs`.
    *   **Giữ lại** phương thức `GetAllCoversForMangaAsync` và `GetProxiedCoverUrl`.

    ```csharp
    // Services\APIServices\Interfaces\ICoverApiService.cs
    using MangaReader.WebUI.Models.Mangadex;

    namespace MangaReader.WebUI.Services.APIServices.Interfaces
    {
        public interface ICoverApiService
        {
            Task<CoverList?> GetAllCoversForMangaAsync(string mangaId); // Giữ lại

            // *** XÓA CÁC PHƯƠNG THỨC NÀY ***
            // Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds);
            // Task<string> FetchCoverUrlAsync(string mangaId);
            // Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10); // Có thể xóa nếu không dùng

             // Giữ lại helper nếu cần truy cập từ bên ngoài hoặc di chuyển logic vào service cần dùng
             string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512);
        }
    }
    ```

    ```csharp
    // Services\APIServices\Services\CoverApiService.cs
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReader.WebUI.Services.APIServices.Interfaces;
    using System.Diagnostics;
    using System.Text.Json;

    namespace MangaReader.WebUI.Services.APIServices.Services
    {
        public class CoverApiService(
            // ... constructor
        )
            : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
              ICoverApiService
        {
            // ... (fields, ExtractCoverFileNameFromRelationships)

            public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
            {
                // ... (Giữ nguyên implementation)
            }

             // *** XÓA BỎ IMPLEMENTATION CỦA CÁC PHƯƠNG THỨC NÀY ***
            // public async Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds) { ... }
            // public async Task<string> FetchCoverUrlAsync(string mangaId) { ... }
            // public async Task<CoverList?> FetchCoversForMangaAsync(string mangaId, int limit = 10) { ... } // Xóa nếu không dùng

            public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
            {
                // ... (Giữ nguyên implementation)
            }
        }
    }
    ```