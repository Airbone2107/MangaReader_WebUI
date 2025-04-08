using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using manga_reader_web.Models;
using manga_reader_web.Services;
using manga_reader_web.Services.MangaServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace manga_reader_web.Controllers
{
    public class MangaController : Controller
    {
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<MangaController> _logger;
        private readonly LocalizationService _localizationService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly MangaUtilityService _mangaUtilityService;
        private readonly MangaTitleService _mangaTitleService;

        public MangaController(
            MangaDexService mangaDexService, 
            ILogger<MangaController> logger,
            LocalizationService localizationService,
            JsonConversionService jsonConversionService,
            MangaUtilityService mangaUtilityService,
            MangaTitleService mangaTitleService)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
            _localizationService = localizationService;
            _jsonConversionService = jsonConversionService;
            _mangaUtilityService = mangaUtilityService;
            _mangaTitleService = mangaTitleService;
        }

        /// <summary>
        /// API endpoint để lấy danh sách thẻ (tags) từ MangaDex
        /// </summary>
        [HttpGet]
        [Route("api/manga/tags")]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                _logger.LogInformation("Đang lấy danh sách tags từ MangaDex");
                var tags = await _mangaDexService.FetchTagsAsync();
                return Json(new { success = true, data = tags });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách tags: {ex.Message}", ex);
                return Json(new { success = false, error = "Không thể tải danh sách tags từ MangaDex" });
            }
        }
        
        // GET: Manga/Details/5
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp cho trang chi tiết manga
                ViewData["PageType"] = "manga-details";
                
                var manga = await _mangaDexService.FetchMangaDetailsAsync(id);
                var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                var mangaDict = _jsonConversionService.ConvertJsonElementToDict(mangaElement);
                
                // Kiểm tra attributes tồn tại
                if (!mangaDict.ContainsKey("attributes") || mangaDict["attributes"] == null)
                {
                    ViewBag.ErrorMessage = "Dữ liệu manga không hợp lệ, thiếu thông tin attributes.";
                    return View(new MangaDetailViewModel());
                }
                
                var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                
                // Sử dụng MangaTitleService để xử lý tiêu đề
                // Chuẩn bị dữ liệu altTitles nếu có
                object altTitlesObj = null;
                if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                {
                    altTitlesObj = attributesDict["altTitles"];
                }
                
                // Gọi phương thức mới với cả title và altTitles
                string mangaTitle = _mangaTitleService.GetMangaTitle(attributesDict["title"], altTitlesObj);
                
                // Xử lý mô tả
                var description = "";
                if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                {
                    // Thử lấy description từ attributesDict
                    var descriptionObj = attributesDict["description"];
                    
                    if (descriptionObj is Dictionary<string, object> descriptionDict)
                    {
                        // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                        if (descriptionDict.ContainsKey("vi"))
                            description = descriptionDict["vi"].ToString();
                        // Nếu không có tiếng Việt, lấy tiếng Anh
                        else if (descriptionDict.ContainsKey("en"))
                            description = descriptionDict["en"].ToString();
                        // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                        else if (descriptionDict.Count > 0)
                            description = descriptionDict.FirstOrDefault().Value?.ToString() ?? "";
                    }
                    else
                    {
                        // Thử phương pháp khác nếu description không phải là Dictionary
                        try
                        {
                            // Sử dụng hàm GetLocalizedDescription với JSON serialized của description
                            string descriptionJson = JsonSerializer.Serialize(descriptionObj);
                            description = _localizationService.GetLocalizedDescription(descriptionJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Lỗi khi xử lý description manga: {ex.Message}");
                            description = "";
                        }
                    }
                }
                
                // Xử lý các thuộc tính khác
                string originalLanguage = attributesDict.ContainsKey("originalLanguage") ? attributesDict["originalLanguage"]?.ToString() : "";
                string publicationDemographic = attributesDict.ContainsKey("publicationDemographic") ? attributesDict["publicationDemographic"]?.ToString() : "";
                string contentRating = attributesDict.ContainsKey("contentRating") ? attributesDict["contentRating"]?.ToString() : "";
                
                // Sử dụng MangaTitleService để xử lý tiêu đề thay thế
                string alternativeTitles = "";
                var altTitlesDictionary = new Dictionary<string, List<string>>();

                if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                {
                    try
                    {
                        // Sử dụng MangaTitleService để xử lý tiêu đề thay thế
                        altTitlesDictionary = _mangaTitleService.GetAlternativeTitles(attributesDict["altTitles"]);
                        
                        // Lấy tiêu đề thay thế ưu tiên
                        alternativeTitles = _mangaTitleService.GetPreferredAlternativeTitle(altTitlesDictionary);
                        
                        // Lưu từ điển tiêu đề phụ vào ViewData để sử dụng trong View
                        ViewData["AlternativeTitlesByLanguage"] = altTitlesDictionary;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý tiêu đề thay thế: {ex.Message}");
                    }
                }
                
                // Xử lý thời gian cập nhật
                DateTime? lastUpdated = null;
                if (attributesDict.ContainsKey("updatedAt") && attributesDict["updatedAt"] != null)
                {
                    if (DateTime.TryParse(attributesDict["updatedAt"].ToString(), out DateTime updatedAt))
                    {
                        lastUpdated = updatedAt;
                    }
                }
                
                // Xử lý tags
                var tags = new List<string>();
                if (attributesDict.ContainsKey("tags") && attributesDict["tags"] != null)
                {
                    try
                    {
                        // Đổi cách phân tích JSON tags để phù hợp với cấu trúc API MangaDex
                        var tagsArray = attributesDict["tags"];
                        if (tagsArray is JsonElement tagsElement && tagsElement.ValueKind == JsonValueKind.Array)
                        {
                            var tagsList = new List<Dictionary<string, object>>();
                            
                            foreach (var tagElem in tagsElement.EnumerateArray())
                            {
                                tagsList.Add(_jsonConversionService.ConvertJsonElementToDict(tagElem));
                            }
                        
                            foreach (var tagDict in tagsList)
                            {
                                if (tagDict.ContainsKey("attributes") && tagDict["attributes"] != null)
                                {
                                    var tagAttrs = (Dictionary<string, object>)tagDict["attributes"];
                                    if (tagAttrs.ContainsKey("name") && tagAttrs["name"] != null)
                                    {
                                        // Xử lý tên tag theo các ngôn ngữ
                                        Dictionary<string, string> tagNameDict;
                                        
                                        if (tagAttrs["name"] is Dictionary<string, object> existingDict)
                                        {
                                            tagNameDict = existingDict.ToDictionary(
                                                kv => kv.Key,
                                                kv => kv.Value?.ToString() ?? ""
                                            );
                                        }
                                        else if (tagAttrs["name"] is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                                        {
                                            // Xử lý trường hợp nameObj là JsonElement
                                            tagNameDict = _jsonConversionService.ConvertJsonElementToDict(jsonElement)
                                                .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                                        }
                                        else
                                        {
                                            // Trường hợp không thể xác định, sử dụng dictionary trống
                                            tagNameDict = new Dictionary<string, string>();
                                            _logger.LogWarning($"Không thể xử lý nameObj có kiểu {tagAttrs["name"]?.GetType().Name ?? "null"}");
                                        }
                                        
                                        // Ưu tiên tên tag tiếng Việt, sau đó đến tiếng Anh
                                        if (tagNameDict.ContainsKey("vi") && !string.IsNullOrEmpty(tagNameDict["vi"]))
                                            tags.Add(tagNameDict["vi"]);
                                        else if (tagNameDict.ContainsKey("en") && !string.IsNullOrEmpty(tagNameDict["en"]))
                                            tags.Add(tagNameDict["en"]);
                                        else if (tagNameDict.Count > 0)
                                            tags.Add(tagNameDict.FirstOrDefault(t => !string.IsNullOrEmpty(t.Value)).Value ?? "Không rõ");
                                    }
                                    
                                    // Thêm nhóm tag nếu có
                                    if (tagAttrs.ContainsKey("group") && tagAttrs["group"] != null)
                                    {
                                        var group = tagAttrs["group"].ToString();
                                        if (!string.IsNullOrEmpty(group))
                                        {
                                            // Có thể thêm tiền tố hoặc hậu tố vào tên tag để hiển thị nhóm tag
                                            // Ví dụ: tags[tags.Count - 1] += $" ({TranslateTagGroup(group)})";
                                        }
                                    }
                                }
                            }
                        }
                        else if (tagsArray is List<object> tagsList)
                        {
                            // Nếu đã là List<object>, xử lý tương tự
                            foreach (var tag in tagsList)
                            {
                                if (tag is Dictionary<string, object> tagDict && 
                                    tagDict.ContainsKey("attributes") && 
                                    tagDict["attributes"] != null)
                                {
                                    var tagAttrs = (Dictionary<string, object>)tagDict["attributes"];
                                    if (tagAttrs.ContainsKey("name") && tagAttrs["name"] != null)
                                    {
                                        var nameObj = tagAttrs["name"];
                                        Dictionary<string, string> tagNameDict;
                                        
                                        if (nameObj is Dictionary<string, object> nameDict)
                                        {
                                            tagNameDict = nameDict.ToDictionary(
                                                kv => kv.Key,
                                                kv => kv.Value?.ToString() ?? ""
                                            );
                                        }
                                        else if (nameObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                                        {
                                            // Xử lý trường hợp nameObj là JsonElement
                                            tagNameDict = _jsonConversionService.ConvertJsonElementToDict(jsonElement)
                                                .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                                        }
                                        else
                                        {
                                            // Trường hợp không thể xác định, sử dụng dictionary trống
                                            tagNameDict = new Dictionary<string, string>();
                                            _logger.LogWarning($"Không thể xử lý nameObj có kiểu {nameObj?.GetType().Name ?? "null"}");
                                        }
                                        
                                        // Ưu tiên tên tag tiếng Việt, sau đó đến tiếng Anh
                                        if (tagNameDict.ContainsKey("vi") && !string.IsNullOrEmpty(tagNameDict["vi"]))
                                            tags.Add(tagNameDict["vi"]);
                                        else if (tagNameDict.ContainsKey("en") && !string.IsNullOrEmpty(tagNameDict["en"]))
                                            tags.Add(tagNameDict["en"]);
                                        else if (tagNameDict.Count > 0)
                                            tags.Add(tagNameDict.FirstOrDefault(t => !string.IsNullOrEmpty(t.Value)).Value ?? "Không rõ");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý tags: {ex.Message}\nStack: {ex.StackTrace}");
                    }
                }
                
                // Kiểm tra thêm nếu tags là rỗng, thử lấy từ relationships
                if (tags.Count == 0 && mangaDict.ContainsKey("relationships") && mangaDict["relationships"] != null)
                {
                    try
                    {
                        var relationships = mangaDict["relationships"];
                        List<object> relationshipsList;
                        
                        if (relationships is JsonElement relElement && relElement.ValueKind == JsonValueKind.Array)
                        {
                            relationshipsList = _jsonConversionService.ConvertJsonElementToList(relElement);
                        }
                        else
                        {
                            relationshipsList = (List<object>)relationships;
                        }
                        
                        foreach (var rel in relationshipsList)
                        {
                            var relDict = rel as Dictionary<string, object>;
                            if (relDict == null) continue;
                            
                            if (relDict.ContainsKey("type") && relDict["type"]?.ToString() == "tag" && 
                                relDict.ContainsKey("attributes") && relDict["attributes"] != null)
                            {
                                var tagAttrs = (Dictionary<string, object>)relDict["attributes"];
                                if (tagAttrs.ContainsKey("name") && tagAttrs["name"] != null)
                                {
                                    var nameObj = tagAttrs["name"];
                                    Dictionary<string, string> tagNameDict;
                                    
                                    if (nameObj is Dictionary<string, object> nameDict)
                                    {
                                        tagNameDict = nameDict.ToDictionary(
                                            kv => kv.Key,
                                            kv => kv.Value?.ToString() ?? ""
                                        );
                                    }
                                    else if (nameObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                                    {
                                        // Xử lý trường hợp nameObj là JsonElement
                                        tagNameDict = _jsonConversionService.ConvertJsonElementToDict(jsonElement)
                                            .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                                    }
                                    else
                                    {
                                        // Trường hợp không thể xác định, sử dụng dictionary trống
                                        tagNameDict = new Dictionary<string, string>();
                                        _logger.LogWarning($"Không thể xử lý nameObj có kiểu {nameObj?.GetType().Name ?? "null"}");
                                    }
                                    
                                    // Ưu tiên tên tag tiếng Việt, sau đó đến tiếng Anh
                                    if (tagNameDict.ContainsKey("vi") && !string.IsNullOrEmpty(tagNameDict["vi"]))
                                        tags.Add(tagNameDict["vi"]);
                                    else if (tagNameDict.ContainsKey("en") && !string.IsNullOrEmpty(tagNameDict["en"]))
                                        tags.Add(tagNameDict["en"]);
                                    else if (tagNameDict.Count > 0)
                                        tags.Add(tagNameDict.FirstOrDefault(t => !string.IsNullOrEmpty(t.Value)).Value ?? "Không rõ");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý tags từ relationships: {ex.Message}");
                    }
                }
                
                // Sắp xếp tags theo thứ tự alphabet cho dễ đọc
                tags = tags.Distinct().OrderBy(t => t).ToList();
                
                // Tải ảnh bìa
                string coverUrl = await _mangaDexService.FetchCoverUrlAsync(id);
                
                // Xử lý thông tin tác giả, họa sĩ, nhà xuất bản từ relationships
                string author = "Không rõ";
                string artist = "";
                string publisher = "";
                
                if (mangaDict.ContainsKey("relationships") && mangaDict["relationships"] != null)
                {
                    try
                    {
                        var relationships = (List<object>)mangaDict["relationships"];
                        
                        foreach (var rel in relationships)
                        {
                            var relDict = (Dictionary<string, object>)rel;
                            if (!relDict.ContainsKey("type") || !relDict.ContainsKey("id"))
                                continue;
                                
                            string relType = relDict["type"].ToString();
                            string relId = relDict["id"].ToString();
                            
                            // Xử lý tác giả và họa sĩ từ relationships
                            if (relType == "author" || relType == "artist")
                            {
                                // Nếu có attributes chứa tên tác giả/họa sĩ
                                if (relDict.ContainsKey("attributes") && relDict["attributes"] != null)
                                {
                                    var attrs = (Dictionary<string, object>)relDict["attributes"];
                                    if (attrs.ContainsKey("name") && attrs["name"] != null)
                                    {
                                        if (relType == "author")
                                            author = attrs["name"].ToString();
                                        else if (relType == "artist")
                                            artist = attrs["name"].ToString();
                                    }
                                }
                            }
                            else if (relType == "publisher")
                            {
                                // Xử lý nhà xuất bản
                                if (relDict.ContainsKey("attributes") && relDict["attributes"] != null)
                                {
                                    var attrs = (Dictionary<string, object>)relDict["attributes"];
                                    if (attrs.ContainsKey("name") && attrs["name"] != null)
                                    {
                                        publisher = attrs["name"].ToString();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý relationships: {ex.Message}");
                    }
                }
                
                // Xử lý trạng thái follow
                bool isFollowing = false;
                try
                {
                    // Kiểm tra xem có cookie hoặc session lưu danh sách manga đang theo dõi không
                    string followedMangaJson = HttpContext.Request.Cookies["followed_manga"];
                    if (!string.IsNullOrEmpty(followedMangaJson))
                    {
                        var followedList = JsonSerializer.Deserialize<List<string>>(followedMangaJson);
                        isFollowing = followedList != null && followedList.Contains(id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi khi kiểm tra trạng thái follow: {ex.Message}");
                }
                
                // Tạo manga view model với thông tin mở rộng
                var mangaViewModel = new MangaViewModel
                {
                    Id = id,
                    Title = mangaTitle,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = attributesDict.ContainsKey("status") ? attributesDict["status"].ToString() : "unknown",
                    Tags = tags,
                    Author = author,
                    Artist = artist,
                    Publisher = publisher,
                    OriginalLanguage = originalLanguage,
                    PublicationDemographic = publicationDemographic,
                    ContentRating = contentRating,
                    AlternativeTitles = alternativeTitles,
                    LastUpdated = lastUpdated,
                    IsFollowing = isFollowing,
                    // Các thông tin hiển thị phụ trợ
                    Rating = _mangaUtilityService.GetMangaRating(id),
                    Views = 0 // MangaDex không cung cấp thông tin số lượt xem
                };
                
                // Tải danh sách chapters
                var chapters = await _mangaDexService.FetchChaptersAsync(id, "vi,en");
                var chapterViewModels = new List<ChapterViewModel>();
                
                foreach (var chapter in chapters)
                {
                    try {
                        var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapter.ToString());
                        var chapterDict = _jsonConversionService.ConvertJsonElementToDict(chapterElement);
                        
                        if (!chapterDict.ContainsKey("id"))
                        {
                            continue; // Bỏ qua chapter này nếu không có ID
                        }
                        
                        if (!chapterDict.ContainsKey("attributes") || chapterDict["attributes"] == null)
                        {
                            continue; // Bỏ qua chapter này nếu không có attributes
                        }
                        
                        var chapterAttributesDict = (Dictionary<string, object>)chapterDict["attributes"];
                        
                        var chapterNumber = chapterAttributesDict.ContainsKey("chapter") && chapterAttributesDict["chapter"] != null
                            ? chapterAttributesDict["chapter"].ToString() 
                            : "?";
                            
                        var chapterTitle = chapterAttributesDict.ContainsKey("title") && chapterAttributesDict["title"] != null
                            ? chapterAttributesDict["title"].ToString() 
                            : "";
                            
                        // Xử lý tên chương theo cách của Flutter: nếu tên chương trùng số chương hoặc rỗng, chỉ hiển thị "Chương X"
                        var displayTitle = string.IsNullOrEmpty(chapterTitle) || chapterTitle == chapterNumber 
                            ? $"Chương {chapterNumber}" 
                            : $"Chương {chapterNumber}: {chapterTitle}";
                            
                        var publishedAt = chapterAttributesDict.ContainsKey("publishAt") && chapterAttributesDict["publishAt"] != null
                            ? DateTime.Parse(chapterAttributesDict["publishAt"].ToString()) 
                            : DateTime.Now;
                            
                        var language = chapterAttributesDict.ContainsKey("translatedLanguage") && chapterAttributesDict["translatedLanguage"] != null
                            ? chapterAttributesDict["translatedLanguage"].ToString() 
                            : "unknown";
                        
                        chapterViewModels.Add(new ChapterViewModel
                        {
                            Id = chapterDict["id"].ToString(),
                            Title = displayTitle,
                            Number = chapterNumber,
                            Language = language,
                            PublishedAt = publishedAt
                        });
                    }
                    catch (Exception ex) {
                        _logger.LogError($"Lỗi khi xử lý chapter: {ex.Message}");
                        continue; // Bỏ qua chapter này và tiếp tục
                    }
                }
                
                // Sắp xếp chapters theo thứ tự giảm dần
                chapterViewModels = chapterViewModels.OrderByDescending(c => 
                {
                    if (float.TryParse(c.Number, out float number))
                        return number;
                    return 0;
                }).ToList();
                
                // Lưu danh sách tất cả chapters vào session storage
                HttpContext.Session.SetString($"Manga_{id}_AllChapters", JsonSerializer.Serialize(chapterViewModels));
                _logger.LogInformation($"Đã lưu {chapterViewModels.Count} chapters của manga {id} vào session");
                
                // Phân loại chapters theo ngôn ngữ và lưu riêng từng ngôn ngữ
                var chaptersByLanguage = chapterViewModels.GroupBy(c => c.Language)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                foreach (var language in chaptersByLanguage.Keys)
                {
                    var chaptersInLanguage = chaptersByLanguage[language];
                    // Sắp xếp chapters theo thứ tự tăng dần của số chương
                    chaptersInLanguage = chaptersInLanguage.OrderBy(c => 
                    {
                        if (float.TryParse(c.Number, out float number))
                            return number;
                        return 0;
                    }).ToList();
                    
                    HttpContext.Session.SetString($"Manga_{id}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                    _logger.LogInformation($"Đã lưu {chaptersInLanguage.Count} chapters ngôn ngữ {language} của manga {id} vào session");
                }
                
                var viewModel = new MangaDetailViewModel
                {
                    Manga = mangaViewModel,
                    Chapters = chapterViewModels
                };
                
                // Nếu là HTMX request, chỉ trả về nội dung một phần
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView(viewModel);
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chi tiết manga: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải chi tiết manga. Vui lòng thử lại sau.";
                return View(new MangaDetailViewModel());
            }
        }
        
        // GET: Manga/Search
        public async Task<IActionResult> Search(
            string title = "", 
            List<string> status = null, 
            string sortBy = "latest",
            string authors = "",
            string artists = "",
            int? year = null,
            List<string> availableTranslatedLanguage = null,
            List<string> publicationDemographic = null,
            List<string> contentRating = null,
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            List<string> genres = null,
            string includedTagsStr = "",
            string excludedTagsStr = "",
            int page = 1, 
            int pageSize = 24)
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp (trang tìm kiếm sử dụng CSS như trang chủ)
                ViewData["PageType"] = "home";

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
                
                // Xử lý danh sách tác giả
                if (!string.IsNullOrEmpty(authors))
                {
                    sortManga.Authors = authors.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                    _logger.LogInformation($"Tìm kiếm với tác giả: {string.Join(", ", sortManga.Authors)}");
                }
                
                // Xử lý danh sách họa sĩ
                if (!string.IsNullOrEmpty(artists))
                {
                    sortManga.Artists = artists.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                    _logger.LogInformation($"Tìm kiếm với họa sĩ: {string.Join(", ", sortManga.Artists)}");
                }
                
                // Xử lý danh sách ngôn ngữ
                if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
                {
                    sortManga.Languages = availableTranslatedLanguage;
                    _logger.LogInformation($"Tìm kiếm với ngôn ngữ: {string.Join(", ", sortManga.Languages)}");
                }
                
                // Xử lý danh sách đánh giá nội dung
                if (contentRating != null && contentRating.Any())
                {
                    sortManga.ContentRating = contentRating;
                    _logger.LogInformation($"Tìm kiếm với mức độ nội dung: {string.Join(", ", sortManga.ContentRating)}");
                }
                else
                {
                    // Mặc định: nội dung an toàn
                    sortManga.ContentRating = new List<string> { "safe", "suggestive", "erotica" };
                }
                
                // Xử lý danh sách includedTags từ chuỗi
                if (!string.IsNullOrEmpty(includedTagsStr))
                {
                    sortManga.IncludedTags = includedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                    _logger.LogInformation($"Tìm kiếm với includedTags: {string.Join(", ", sortManga.IncludedTags)}");
                }
                
                // Xử lý danh sách excludedTags từ chuỗi
                if (!string.IsNullOrEmpty(excludedTagsStr))
                {
                    sortManga.ExcludedTags = excludedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                    _logger.LogInformation($"Tìm kiếm với excludedTags: {string.Join(", ", sortManga.ExcludedTags)}");
                }

                // Lấy kết quả từ API và tổng số manga từ API
                // Xử lý giới hạn 10000 kết quả từ API
                const int MAX_API_RESULTS = 10000;
                int offset = (page - 1) * pageSize;
                int limit = pageSize;

                // Kiểm tra nếu đang truy cập trang cuối cùng gần với giới hạn 10000
                if (offset + limit > MAX_API_RESULTS)
                {
                    // Tính lại limit để không vượt quá 10000 kết quả
                    if (offset < MAX_API_RESULTS)
                    {
                        limit = MAX_API_RESULTS - offset;
                        _logger.LogInformation($"Đã điều chỉnh limit: {limit} cho trang cuối (offset: {offset})");
                    }
                    else
                    {
                        // Trường hợp offset đã vượt quá 10000, không thể lấy kết quả
                        _logger.LogWarning($"Offset {offset} vượt quá giới hạn API 10000 kết quả, không thể lấy dữ liệu");
                        limit = 0;
                    }
                }

                var result = await _mangaDexService.FetchMangaAsync(limit: limit, offset: offset, sortManga: sortManga);
                
                // Xử lý đặc biệt cho trường hợp limit = 0 (offset vượt quá 10000)
                if (limit == 0)
                {
                    ViewBag.ErrorMessage = "Không thể hiển thị kết quả vì đã vượt quá giới hạn 10000 kết quả từ API.";
                    return View(new MangaListViewModel
                    {
                        Mangas = new List<MangaViewModel>(),
                        CurrentPage = page,
                        PageSize = 24, // Giá trị cố định cho pageSize
                        TotalCount = 10000,
                        MaxPages = 0,
                        SortOptions = sortManga
                    });
                }
                
                // Lấy tổng số manga từ kết quả API (nếu có)
                int totalCount = 0;
                
                // Kiểm tra nếu kết quả trả về là JsonElement (có thể chứa thông tin totalCount)
                if (result != null && result.Count > 0)
                {
                    try 
                    {
                        // Thử lấy totalCount từ response metadata
                        var firstItem = result[0];
                        if (firstItem is JsonElement element)
                        {
                            // Kiểm tra xem có property total không
                            if (element.TryGetProperty("total", out JsonElement totalElement))
                            {
                                totalCount = totalElement.GetInt32();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi lấy tổng số manga: {ex.Message}");
                    }
                }
                
                // Nếu không lấy được tổng số, sử dụng số lượng kết quả nhân với tỷ lệ page hiện tại
                if (totalCount <= 0)
                {
                    // Ước tính tổng số manga dựa trên số lượng kết quả và offset
                    totalCount = Math.Max(result.Count * 10, (page - 1) * pageSize + result.Count + pageSize);
                }
                
                // Tính toán số trang tối đa dựa trên giới hạn 10000 kết quả của API
                int maxPages = (int)Math.Ceiling(Math.Min(totalCount, MAX_API_RESULTS) / (double)pageSize);

                var mangaViewModels = new List<MangaViewModel>();
                foreach (var manga in result)
                {
                    try
                    {
                        // Sử dụng JsonSerializer để chuyển đổi chính xác
                        var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                        // Chuyển đổi JsonElement thành Dictionary
                        var mangaDict = _jsonConversionService.ConvertJsonElementToDict(mangaElement);
                        
                        if (!mangaDict.ContainsKey("id") || mangaDict["id"] == null)
                        {
                            _logger.LogWarning("Manga không có ID");
                            continue;
                        }
                        
                        var id = mangaDict["id"].ToString();
                        
                        // Kiểm tra attributes tồn tại
                        if (!mangaDict.ContainsKey("attributes") || mangaDict["attributes"] == null)
                        {
                            _logger.LogWarning($"Manga ID {id} thiếu thông tin attributes");
                            continue;
                        }
                        
                        // Chuyển đổi attributes thành Dictionary
                        var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                        
                        // Kiểm tra title tồn tại
                        if (!attributesDict.ContainsKey("title") || attributesDict["title"] == null)
                        {
                            _logger.LogWarning($"Manga ID {id} thiếu thông tin title");
                            continue;
                        }
                        
                        // Sử dụng MangaTitleService để xử lý tiêu đề
                        // Chuẩn bị dữ liệu altTitles nếu có
                        object altTitlesObj = null;
                        if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                        {
                            altTitlesObj = attributesDict["altTitles"];
                        }
                        
                        // Gọi phương thức mới với cả title và altTitles
                        string mangaTitle = _mangaTitleService.GetMangaTitle(attributesDict["title"], altTitlesObj);
                        
                        var description = "";
                        if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                        {
                            description = _localizationService.GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
                        }
                        
                        // Tải ảnh bìa
                        string coverUrl = "";
                        try
                        {
                            coverUrl = await _mangaDexService.FetchCoverUrlAsync(id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Không tải được ảnh bìa cho manga {id}: {ex.Message}");
                        }

                        mangaViewModels.Add(new MangaViewModel
                        {
                            Id = id,
                            Title = mangaTitle,
                            Description = description,
                            CoverUrl = coverUrl,
                            Status = attributesDict.ContainsKey("status") ? attributesDict["status"].ToString() : "unknown"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý manga: {ex.Message}");
                        // Ghi log nhưng vẫn tiếp tục với manga tiếp theo
                    }
                }

                var viewModel = new MangaListViewModel
                {
                    Mangas = mangaViewModels,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    MaxPages = maxPages,
                    SortOptions = sortManga
                };

                // Nếu là HTMX request, chỉ trả về nội dung một phần
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView(viewModel);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}\nStack trace: {ex.StackTrace}");
                
                // Hiển thị thông báo lỗi chi tiết hơn cho người dùng
                string errorMessage = "Không thể tải danh sách manga. ";
                if (ex.InnerException != null)
                {
                    errorMessage += $"Chi tiết: {ex.Message} - {ex.InnerException.Message}";
                }
                else
                {
                    errorMessage += $"Chi tiết: {ex.Message}";
                }
                
                ViewBag.ErrorMessage = errorMessage;
                
                // Tạo một viewModel trống để tránh null reference exception
                return View(new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = new SortManga { 
                        Title = title, 
                        Status = status ?? new List<string>(),  
                        SortBy = sortBy ?? "latest"
                    }
                });
            }
        }
    }
} 