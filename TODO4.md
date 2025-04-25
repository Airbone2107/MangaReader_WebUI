# TODO.md: Sửa lỗi JsonException trong MangaDetailsService

## Mục tiêu

Khắc phục lỗi `JsonException: 'M' is an invalid start of a value` xảy ra trong `MangaDetailsService` khi xử lý kết quả từ `IMangaApiService` bằng cách sử dụng trực tiếp đối tượng C# đã được deserialize thay vì cố gắng deserialize lại.

---

## Các bước thực hiện

### 1. Cập nhật phương thức `GetMangaDetailsAsync` trong `MangaDetailsService.cs`

*   **Mục tiêu:** Loại bỏ việc deserialize không cần thiết và sử dụng trực tiếp đối tượng `MangaResponse`.
*   **Vị trí:** `Services/MangaServices/MangaPageService/MangaDetailsService.cs`

*   **Code cũ (Gây lỗi):**
    ```csharp
    public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
    {
        try
        {
            var manga = await _mangaApiService.FetchMangaDetailsAsync(id); // manga là MangaResponse?
            // LỖI Ở ĐÂY: Cố gắng deserialize .ToString() của object
            var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString()); 
            var mangaDict = _jsonConversionService.ConvertJsonElementToDict(mangaElement); 
            
            var attributesDict = (Dictionary<string, object>)mangaDict["attributes"]; 
            
            // Tạo MangaViewModel từ mangaDict và attributesDict (sai)
            var mangaViewModel = await CreateMangaViewModelAsync(id, mangaDict, attributesDict); 
            
            var chapterViewModels = await GetChaptersAsync(id);
            
            return new MangaDetailViewModel
            {
                Manga = mangaViewModel,
                Chapters = chapterViewModels
            };
        }
        // ... catch blocks ...
    }
    ```

*   **Code sửa:**
    ```csharp
    using MangaReader.WebUI.Models.Mangadex; // Đảm bảo có using này
    using System.Text.Json; // Cho JsonException

    // ... các using khác ...

    namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
    {
        public class MangaDetailsService
        {
            // ... các dependencies khác ...
            private readonly IMangaApiService _mangaApiService;
            private readonly ICoverApiService _coverApiService;
            private readonly MangaTitleService _mangaTitleService;
            private readonly MangaDescription _mangaDescription;
            private readonly MangaTagService _mangaTagService;
            private readonly MangaRelationshipService _mangaRelationshipService;
            private readonly LocalizationService _localizationService;
            private readonly IMangaFollowService _mangaFollowService;
            private readonly MangaUtilityService _mangaUtilityService;
            private readonly ChapterService _chapterService;
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly ILogger<MangaDetailsService> _logger;


            public MangaDetailsService(
                IMangaApiService mangaApiService,
                ICoverApiService coverApiService,
                ILogger<MangaDetailsService> logger,
                LocalizationService localizationService,
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
                    var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id); // Trả về MangaResponse?

                    // Kiểm tra kết quả trả về từ API service
                    if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                    {
                        _logger.LogError($"Không thể lấy chi tiết manga {id}. Response: {mangaResponse?.Result}");
                        // Trả về ViewModel rỗng hoặc lỗi
                        return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, Chapters = new List<ChapterViewModel>() };
                    }

                    var mangaData = mangaResponse.Data; // Lấy trực tiếp đối tượng Manga

                    // Tạo MangaViewModel từ đối tượng Manga (cần cập nhật CreateMangaViewModelAsync)
                    var mangaViewModel = await CreateMangaViewModelAsync(mangaData); // Truyền đối tượng Manga

                    // Lấy danh sách chapters (ChapterService đã được cập nhật)
                    var chapterViewModels = await GetChaptersAsync(id);

                    return new MangaDetailViewModel
                    {
                        Manga = mangaViewModel,
                        Chapters = chapterViewModels
                    };
                }
                catch (JsonException jsonEx) // Bắt lỗi JSON cụ thể nếu CreateMangaViewModelAsync có vấn đề
                {
                     _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chi tiết manga {id}: {jsonEx.Message}");
                     // Trả về ViewModel lỗi
                     return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi định dạng dữ liệu" }, Chapters = new List<ChapterViewModel>() };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi nghiêm trọng khi lấy chi tiết manga {id}");
                    // Trả về ViewModel lỗi
                     return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, Chapters = new List<ChapterViewModel>() };
                }
            }

            // ... các phương thức khác (GetAlternativeTitlesByLanguageAsync, CreateMangaViewModelAsync, GetChaptersAsync) ...

            // Cần cập nhật CreateMangaViewModelAsync để nhận Manga? thay vì Dictionary
            private async Task<MangaViewModel> CreateMangaViewModelAsync(Manga? mangaData)
            {
                 // Thêm kiểm tra null cho mangaData
                if (mangaData == null || mangaData.Attributes == null)
                {
                    _logger.LogWarning($"Dữ liệu manga hoặc attributes bị null khi tạo ViewModel cho ID: {mangaData?.Id}");
                    // Trả về ViewModel lỗi hoặc mặc định
                    return new MangaViewModel { Id = mangaData?.Id.ToString() ?? "unknown", Title = "Lỗi dữ liệu" };
                }

                string id = mangaData.Id.ToString();
                var attributes = mangaData.Attributes; // Sử dụng trực tiếp attributes

                try
                {
                    // Lấy title (MangaTitleService đã cập nhật để nhận Dictionary/List<Dictionary>)
                    string mangaTitle = _mangaTitleService.GetMangaTitle(attributes.Title, attributes.AltTitles);

                    // Lưu title vào session (giữ nguyên)
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null && !string.IsNullOrEmpty(mangaTitle))
                    {
                        httpContext.Session.SetString($"Manga_{id}_Title", mangaTitle);
                        _logger.LogInformation($"Đã lưu tiêu đề manga {id} vào session: {mangaTitle}");
                    }

                    // Lấy description (MangaDescription đã cập nhật để nhận MangaAttributes)
                    string description = _mangaDescription.GetDescription(attributes);

                    // Lấy tags (MangaTagService đã cập nhật để nhận MangaAttributes)
                    var tags = _mangaTagService.GetMangaTags(attributes);

                    // Lấy author/artist (MangaRelationshipService đã cập nhật để nhận List<Relationship>)
                    var (author, artist) = _mangaRelationshipService.GetAuthorArtist(mangaData.Relationships);

                    // Lấy ảnh bìa (ICoverApiService)
                    string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);
                    if (string.IsNullOrEmpty(coverUrl))
                    {
                        coverUrl = "/images/cover-placeholder.jpg"; // Ảnh mặc định
                    }

                    // Lấy trạng thái (LocalizationService đã cập nhật để nhận MangaAttributes)
                    string status = _localizationService.GetStatus(attributes);

                    // Lấy các thuộc tính khác trực tiếp
                    string originalLanguage = attributes.OriginalLanguage ?? "";
                    string publicationDemographic = attributes.PublicationDemographic ?? "";
                    string contentRating = attributes.ContentRating ?? "";
                    DateTime? lastUpdated = attributes.UpdatedAt.DateTime; // Truy cập trực tiếp DateTimeOffset
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

            // GetAlternativeTitlesByLanguageAsync cũng cần cập nhật để dùng mangaResponse.Data.Attributes
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
        }
    }

    // Cũng cần đảm bảo LocalizationService.GetStatus nhận MangaAttributes
    namespace MangaReader.WebUI.Services.UtilityServices
    {
        public class LocalizationService
        {
             private readonly ILogger<LocalizationService> _logger;

            public LocalizationService(ILogger<LocalizationService> logger)
            {
                _logger = logger;
            }
            // Phương thức mới nhận MangaAttributes
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

            // Các phương thức khác giữ nguyên...
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
                catch (JsonException jEx) { _logger.LogError(jEx, $"Lỗi JSON khi parse title: {titleJson}"); return "Không có tiêu đề"; }
                catch (Exception ex) { _logger.LogError(ex, $"Lỗi khi xử lý tiêu đề truyện: {titleJson}"); return "Không có tiêu đề"; }
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
                catch (JsonException jEx) { _logger.LogError(jEx, $"Lỗi JSON khi parse description: {descriptionJson}"); return ""; }
                catch (Exception ex) { _logger.LogError(ex, $"Lỗi khi xử lý mô tả truyện: {descriptionJson}"); return ""; }
            }
        }
    }
    ```

### 2. Kiểm tra và cập nhật các Helper Services (nếu cần)

*   Đảm bảo các service trong `Services/MangaServices/MangaInformation` (như `MangaTitleService`, `MangaTagService`, `MangaRelationshipService`, `MangaDescription`) và `LocalizationService` đã được cập nhật để nhận tham số là các model strongly-typed (`MangaAttributes`, `List<Relationship>`, `List<Tag>`) thay vì `Dictionary<string, object>`. Xem lại file `TODO2.md` trước đó để chắc chắn các thay đổi này đã được áp dụng đúng.

---

Sau khi thực hiện các thay đổi này, lỗi `JsonException` trong `MangaDetailsService` sẽ được khắc phục. Hãy build lại và kiểm tra trang chi tiết manga.