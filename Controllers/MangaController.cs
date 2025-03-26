using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using manga_reader_web.Models;
using manga_reader_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace manga_reader_web.Controllers
{
    public class MangaController : Controller
    {
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<MangaController> _logger;

        public MangaController(MangaDexService mangaDexService, ILogger<MangaController> logger)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
        }

        // GET: Manga
        public async Task<IActionResult> Index(string title = "", int page = 1, int pageSize = 24)
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp (trang liệt kê sử dụng CSS như trang chủ)
                ViewData["PageType"] = "home";
                
                var sortManga = new SortManga
                {
                    Title = title,
                    SortBy = "latest"
                };

                var mangas = await _mangaDexService.FetchMangaAsync(limit: pageSize, offset: (page - 1) * pageSize, sortManga: sortManga);
                var totalCount = 100; // Giả định tổng số manga

                var mangaViewModels = new List<MangaViewModel>();
                foreach (var manga in mangas)
                {
                    try
                    {
                        // Sử dụng JsonSerializer để chuyển đổi chính xác
                        var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                        // Chuyển đổi JsonElement thành Dictionary
                        var mangaDict = ConvertJsonElementToDict(mangaElement);
                        
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
                        
                        // Lấy title từ attributesDict
                        var titleObj = attributesDict["title"];
                        string mangaTitle = "Không có tiêu đề";
                        
                        // Phương pháp xử lý đúng cách title
                        if (titleObj is Dictionary<string, object> titleDict)
                        {
                            // Ưu tiên tiếng Việt
                            if (titleDict.ContainsKey("vi"))
                                mangaTitle = titleDict["vi"].ToString();
                            // Nếu không có tiếng Việt, lấy tiếng Anh
                            else if (titleDict.ContainsKey("en"))
                                mangaTitle = titleDict["en"].ToString();
                            // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                            else if (titleDict.Count > 0)
                                mangaTitle = titleDict.FirstOrDefault().Value?.ToString() ?? "Không có tiêu đề";
                        }
                        else
                        {
                            // Thử phương pháp khác nếu title không phải là Dictionary
                            mangaTitle = GetLocalizedTitle(JsonSerializer.Serialize(titleObj)) ?? "Không có tiêu đề";
                        }
                        
                        var description = "";
                        if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                        {
                            description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
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
                    SortOptions = sortManga
                };

                // Nếu là HTMX request, chỉ trả về nội dung một phần
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_MangaList", viewModel);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải danh sách manga. Vui lòng thử lại sau.";
                return View(new MangaListViewModel());
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
                var mangaDict = ConvertJsonElementToDict(mangaElement);
                
                // Kiểm tra attributes tồn tại
                if (!mangaDict.ContainsKey("attributes") || mangaDict["attributes"] == null)
                {
                    ViewBag.ErrorMessage = "Dữ liệu manga không hợp lệ, thiếu thông tin attributes.";
                    return View(new MangaDetailViewModel());
                }
                
                var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                
                // Xử lý title
                string title = "Không có tiêu đề";
                if (attributesDict.ContainsKey("title") && attributesDict["title"] != null)
                {
                    // Thử lấy title từ attributesDict
                    var titleObj = attributesDict["title"];
                    
                    if (titleObj is Dictionary<string, object> titleDict)
                    {
                        // Ưu tiên tiếng Việt
                        if (titleDict.ContainsKey("vi"))
                            title = titleDict["vi"].ToString();
                        // Nếu không có tiếng Việt, lấy tiếng Anh
                        else if (titleDict.ContainsKey("en"))
                            title = titleDict["en"].ToString();
                        // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                        else if (titleDict.Count > 0)
                            title = titleDict.FirstOrDefault().Value?.ToString() ?? "Không có tiêu đề";
                    }
                    else
                    {
                        // Thử phương pháp khác nếu title không phải là Dictionary
                        try
                        {
                            // Sử dụng hàm GetLocalizedTitle với JSON serialized của title
                            string titleJson = JsonSerializer.Serialize(titleObj);
                            title = GetLocalizedTitle(titleJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Lỗi khi xử lý title manga: {ex.Message}");
                            title = "Không có tiêu đề";
                        }
                    }
                }
                
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
                            description = GetLocalizedDescription(descriptionJson);
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
                
                // Xử lý tiêu đề thay thế
                string alternativeTitles = "";
                if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                {
                    try
                    {
                        // Chuyển đổi danh sách tiêu đề thay thế thành một chuỗi (ưu tiên tiếng Việt và tiếng Anh)
                        var altTitlesJson = attributesDict["altTitles"].ToString();
                        var altTitlesList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(altTitlesJson);
                        
                        var altTitlesStrings = new List<string>();
                        foreach (var altTitleDict in altTitlesList)
                        {
                            // Ưu tiên lấy tiêu đề tiếng Việt hoặc tiếng Anh
                            if (altTitleDict.ContainsKey("vi"))
                                altTitlesStrings.Add(altTitleDict["vi"]);
                            else if (altTitleDict.ContainsKey("en"))
                                altTitlesStrings.Add(altTitleDict["en"]);
                            else if (altTitleDict.Count > 0)
                                altTitlesStrings.Add(altTitleDict.FirstOrDefault().Value);
                        }
                        
                        // Loại bỏ các tiêu đề trùng lặp
                        altTitlesStrings = altTitlesStrings.Distinct().ToList();
                        
                        if (altTitlesStrings.Count > 0)
                            alternativeTitles = string.Join(", ", altTitlesStrings);
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
                                tagsList.Add(ConvertJsonElementToDict(tagElem));
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
                                        var nameObj = tagAttrs["name"];
                                        
                                        if (nameObj is Dictionary<string, object> existingDict)
                                        {
                                            tagNameDict = existingDict.ToDictionary(
                                                kv => kv.Key,
                                                kv => kv.Value?.ToString() ?? ""
                                            );
                                        }
                                        else
                                        {
                                            // Cố gắng chuyển đổi từ JsonElement nếu cần
                                            tagNameDict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                                                nameObj.ToString(),
                                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                            );
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
                                        else
                                        {
                                            tagNameDict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                                                nameObj.ToString(),
                                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                            );
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
                            relationshipsList = ConvertJsonElementToList(relElement);
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
                                    else
                                    {
                                        tagNameDict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                                            nameObj.ToString(),
                                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                        );
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
                    Title = title,
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
                    Rating = GetMangaRating(id),
                    Views = 0 // MangaDex không cung cấp thông tin số lượt xem
                };
                
                // Tải danh sách chapters
                var chapters = await _mangaDexService.FetchChaptersAsync(id, "vi,en");
                var chapterViewModels = new List<ChapterViewModel>();
                
                foreach (var chapter in chapters)
                {
                    try {
                        var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapter.ToString());
                        var chapterDict = ConvertJsonElementToDict(chapterElement);
                        
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
        
        // Method để lấy title theo ngôn ngữ ưu tiên
        private string GetLocalizedTitle(string titleJson)
        {
            try
            {
                if (string.IsNullOrEmpty(titleJson))
                    return "Không có tiêu đề";
                
                // Thử deserialize trực tiếp
                try
                {
                    var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                    
                    if (titles == null || titles.Count == 0)
                        return "Không có tiêu đề";
                        
                    // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                    if (titles.ContainsKey("vi"))
                        return titles["vi"];
                    if (titles.ContainsKey("en"))
                        return titles["en"];
                        
                    // Nếu không có, lấy giá trị đầu tiên
                    var firstItem = titles.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "Không có tiêu đề" : firstItem.Value;
                }
                catch (JsonException)
                {
                    // Nếu deserialization lỗi, có thể JSON không đúng định dạng mong đợi
                    // Trường hợp đặc biệt khi JSON là object thay vì string
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(titleJson);
                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                            if (jsonElement.TryGetProperty("vi", out var viTitle))
                                return viTitle.GetString() ?? "Không có tiêu đề";
                            if (jsonElement.TryGetProperty("en", out var enTitle))
                                return enTitle.GetString() ?? "Không có tiêu đề";
                            
                            // Nếu không có, lấy property đầu tiên
                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "Không có tiêu đề";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi của phương pháp thứ hai
                    }
                }
                
                // Nếu không thể parse, trả về giá trị mặc định
                return "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý tiêu đề truyện: {ex.Message}");
                return "Không có tiêu đề";
            }
        }
        
        private string GetLocalizedDescription(string descriptionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(descriptionJson))
                    return "";
                
                // Thử deserialize trực tiếp
                try
                {
                    var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);
                    
                    if (descriptions == null || descriptions.Count == 0)
                        return "";
                        
                    // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                    if (descriptions.ContainsKey("vi"))
                        return descriptions["vi"];
                    if (descriptions.ContainsKey("en"))
                        return descriptions["en"];
                        
                    // Nếu không có, lấy giá trị đầu tiên
                    var firstItem = descriptions.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "" : firstItem.Value;
                }
                catch (JsonException)
                {
                    // Nếu deserialization lỗi, có thể JSON không đúng định dạng mong đợi
                    // Trường hợp đặc biệt khi JSON là object thay vì string
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(descriptionJson);
                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                            if (jsonElement.TryGetProperty("vi", out var viDescription))
                                return viDescription.GetString() ?? "";
                            if (jsonElement.TryGetProperty("en", out var enDescription))
                                return enDescription.GetString() ?? "";
                            
                            // Nếu không có, lấy property đầu tiên
                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi của phương pháp thứ hai
                    }
                }
                
                // Nếu không thể parse, trả về chuỗi rỗng
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý mô tả truyện: {ex.Message}");
                return "";
            }
        }

        // Thêm phương thức mới này vào class MangaController để chuyển đổi JsonElement thành Dictionary
        private Dictionary<string, object> ConvertJsonElementToDict(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            if (element.ValueKind != JsonValueKind.Object)
            {
                return dict;
            }

            foreach (var property in element.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        dict[property.Name] = ConvertJsonElementToDict(property.Value);
                        break;
                    case JsonValueKind.Array:
                        dict[property.Name] = ConvertJsonElementToList(property.Value);
                        break;
                    case JsonValueKind.String:
                        dict[property.Name] = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (property.Value.TryGetInt32(out int intValue))
                        {
                            dict[property.Name] = intValue;
                        }
                        else if (property.Value.TryGetInt64(out long longValue))
                        {
                            dict[property.Name] = longValue;
                        }
                        else
                        {
                            dict[property.Name] = property.Value.GetDouble();
                        }
                        break;
                    case JsonValueKind.True:
                        dict[property.Name] = true;
                        break;
                    case JsonValueKind.False:
                        dict[property.Name] = false;
                        break;
                    case JsonValueKind.Null:
                        dict[property.Name] = null;
                        break;
                    default:
                        dict[property.Name] = property.Value.ToString();
                        break;
                }
            }
            return dict;
        }

        private List<object> ConvertJsonElementToList(JsonElement element)
        {
            var list = new List<object>();
            if (element.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var item in element.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                        list.Add(ConvertJsonElementToDict(item));
                        break;
                    case JsonValueKind.Array:
                        list.Add(ConvertJsonElementToList(item));
                        break;
                    case JsonValueKind.String:
                        list.Add(item.GetString());
                        break;
                    case JsonValueKind.Number:
                        if (item.TryGetInt32(out int intValue))
                        {
                            list.Add(intValue);
                        }
                        else if (item.TryGetInt64(out long longValue))
                        {
                            list.Add(longValue);
                        }
                        else
                        {
                            list.Add(item.GetDouble());
                        }
                        break;
                    case JsonValueKind.True:
                        list.Add(true);
                        break;
                    case JsonValueKind.False:
                        list.Add(false);
                        break;
                    case JsonValueKind.Null:
                        list.Add(null);
                        break;
                    default:
                        list.Add(item.ToString());
                        break;
                }
            }
            return list;
        }

        // Các action khác liên quan đến danh sách, search, filter
        public async Task<IActionResult> Search(string title = "", string tags = "", string artists = "", string authors = "",
            int year = 0, string status = "", string publicationDemographic = "", string contentRating = "",
            int page = 1, int pageSize = 24)
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp (trang tìm kiếm sử dụng CSS như trang chủ)
                ViewData["PageType"] = "home";

                var sortManga = new SortManga
                {
                    Title = title,
                    SortBy = "latest"
                };

                var mangas = await _mangaDexService.FetchMangaAsync(limit: pageSize, offset: (page - 1) * pageSize, sortManga: sortManga);
                var totalCount = 100; // Giả định tổng số manga

                var mangaViewModels = new List<MangaViewModel>();
                foreach (var manga in mangas)
                {
                    try
                    {
                        // Sử dụng JsonSerializer để chuyển đổi chính xác
                        var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                        // Chuyển đổi JsonElement thành Dictionary
                        var mangaDict = ConvertJsonElementToDict(mangaElement);
                        
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
                        
                        // Lấy title từ attributesDict
                        var titleObj = attributesDict["title"];
                        string mangaTitle = "Không có tiêu đề";
                        
                        // Phương pháp xử lý đúng cách title
                        if (titleObj is Dictionary<string, object> titleDict)
                        {
                            // Ưu tiên tiếng Việt
                            if (titleDict.ContainsKey("vi"))
                                mangaTitle = titleDict["vi"].ToString();
                            // Nếu không có tiếng Việt, lấy tiếng Anh
                            else if (titleDict.ContainsKey("en"))
                                mangaTitle = titleDict["en"].ToString();
                            // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                            else if (titleDict.Count > 0)
                                mangaTitle = titleDict.FirstOrDefault().Value?.ToString() ?? "Không có tiêu đề";
                        }
                        else
                        {
                            // Thử phương pháp khác nếu title không phải là Dictionary
                            mangaTitle = GetLocalizedTitle(JsonSerializer.Serialize(titleObj)) ?? "Không có tiêu đề";
                        }
                        
                        var description = "";
                        if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                        {
                            description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
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
                _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải danh sách manga. Vui lòng thử lại sau.";
                return View(new MangaListViewModel());
            }
        }

        public async Task<IActionResult> Popular(int page = 1)
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp (trang phổ biến sử dụng CSS như trang chủ)
                ViewData["PageType"] = "home";
                
                var sortManga = new SortManga
                {
                    Title = "",
                    SortBy = "popular"
                };

                var mangas = await _mangaDexService.FetchMangaAsync(limit: 24, offset: (page - 1) * 24, sortManga: sortManga);
                var totalCount = 100; // Giả định tổng số manga

                var mangaViewModels = new List<MangaViewModel>();
                foreach (var manga in mangas)
                {
                    try
                    {
                        // Sử dụng JsonSerializer để chuyển đổi chính xác
                        var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                        // Chuyển đổi JsonElement thành Dictionary
                        var mangaDict = ConvertJsonElementToDict(mangaElement);
                        
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
                        
                        // Lấy title từ attributesDict
                        var titleObj = attributesDict["title"];
                        string mangaTitle = "Không có tiêu đề";
                        
                        // Phương pháp xử lý đúng cách title
                        if (titleObj is Dictionary<string, object> titleDict)
                        {
                            // Ưu tiên tiếng Việt
                            if (titleDict.ContainsKey("vi"))
                                mangaTitle = titleDict["vi"].ToString();
                            // Nếu không có tiếng Việt, lấy tiếng Anh
                            else if (titleDict.ContainsKey("en"))
                                mangaTitle = titleDict["en"].ToString();
                            // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                            else if (titleDict.Count > 0)
                                mangaTitle = titleDict.FirstOrDefault().Value?.ToString() ?? "Không có tiêu đề";
                        }
                        else
                        {
                            // Thử phương pháp khác nếu title không phải là Dictionary
                            mangaTitle = GetLocalizedTitle(JsonSerializer.Serialize(titleObj)) ?? "Không có tiêu đề";
                        }
                        
                        var description = "";
                        if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                        {
                            description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
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
                    PageSize = 24,
                    TotalCount = totalCount,
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
                _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải danh sách manga. Vui lòng thử lại sau.";
                return View(new MangaListViewModel());
            }
        }

        public async Task<IActionResult> Latest(int page = 1)
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp (trang mới cập nhật sử dụng CSS như trang chủ)
                ViewData["PageType"] = "home";
                
                var sortManga = new SortManga
                {
                    Title = "",
                    SortBy = "latest"
                };

                var mangas = await _mangaDexService.FetchMangaAsync(limit: 24, offset: (page - 1) * 24, sortManga: sortManga);
                var totalCount = 100; // Giả định tổng số manga

                var mangaViewModels = new List<MangaViewModel>();
                foreach (var manga in mangas)
                {
                    try
                    {
                        // Sử dụng JsonSerializer để chuyển đổi chính xác
                        var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                        // Chuyển đổi JsonElement thành Dictionary
                        var mangaDict = ConvertJsonElementToDict(mangaElement);
                        
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
                        
                        // Lấy title từ attributesDict
                        var titleObj = attributesDict["title"];
                        string mangaTitle = "Không có tiêu đề";
                        
                        // Phương pháp xử lý đúng cách title
                        if (titleObj is Dictionary<string, object> titleDict)
                        {
                            // Ưu tiên tiếng Việt
                            if (titleDict.ContainsKey("vi"))
                                mangaTitle = titleDict["vi"].ToString();
                            // Nếu không có tiếng Việt, lấy tiếng Anh
                            else if (titleDict.ContainsKey("en"))
                                mangaTitle = titleDict["en"].ToString();
                            // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                            else if (titleDict.Count > 0)
                                mangaTitle = titleDict.FirstOrDefault().Value?.ToString() ?? "Không có tiêu đề";
                        }
                        else
                        {
                            // Thử phương pháp khác nếu title không phải là Dictionary
                            mangaTitle = GetLocalizedTitle(JsonSerializer.Serialize(titleObj)) ?? "Không có tiêu đề";
                        }
                        
                        var description = "";
                        if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                        {
                            description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
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
                    PageSize = 24,
                    TotalCount = totalCount,
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
                _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải danh sách manga. Vui lòng thử lại sau.";
                return View(new MangaListViewModel());
            }
        }

        // Phương thức mô phỏng tính toán rating
        private double GetMangaRating(string mangaId)
        {
            // MangaDex API không cung cấp thông tin rating trực tiếp
            // Trong thực tế, bạn có thể lưu và tính toán điểm đánh giá từ người dùng
            // Hoặc lấy từ nguồn API khác
            
            // Mô phỏng: Tạo rating giả dựa trên ID manga
            try
            {
                // Tạo một số giả từ 0-10 dựa trên mangaId
                var idSum = mangaId.Sum(c => c);
                return Math.Round((idSum % 40 + 60) / 10.0, 2); // Trả về số từ 6.0-10.0
            }
            catch
            {
                return 7.5; // Giá trị mặc định nếu có lỗi
            }
        }
    }
} 