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
                
                var title = "";
                if (attributesDict.ContainsKey("title") && attributesDict["title"] != null)
                {
                    title = GetLocalizedTitle(attributesDict["title"].ToString()) ?? "Không có tiêu đề";
                }
                else
                {
                    title = "Không có tiêu đề";
                }
                
                var description = "";
                if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
                {
                    description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
                }
                
                // Tải ảnh bìa
                string coverUrl = await _mangaDexService.FetchCoverUrlAsync(id);
                
                // Tạo manga view model
                var mangaViewModel = new MangaViewModel
                {
                    Id = id,
                    Title = title,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = attributesDict.ContainsKey("status") ? attributesDict["status"].ToString() : "unknown",
                    Author = "Đang tải..." // Cần cải thiện để lấy tác giả từ relationship
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
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chi tiết manga: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải chi tiết manga. Vui lòng thử lại sau.";
                return View(new MangaDetailViewModel());
            }
        }
        
        // Các hàm utility để xử lý dữ liệu
        private string GetLocalizedTitle(string titleJson)
        {
            try
            {
                if (string.IsNullOrEmpty(titleJson))
                    return null;
                    
                var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                
                if (titles == null || titles.Count == 0)
                    return null;
                    
                // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                if (titles.ContainsKey("vi"))
                    return titles["vi"];
                if (titles.ContainsKey("en"))
                    return titles["en"];
                    
                // Nếu không có, lấy giá trị đầu tiên
                var firstItem = titles.FirstOrDefault();
                return firstItem.Equals(default(KeyValuePair<string, string>)) ? null : firstItem.Value;
            }
            catch
            {
                return null;
            }
        }
        
        private string GetLocalizedDescription(string descriptionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(descriptionJson))
                    return null;
                    
                var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);
                
                if (descriptions == null || descriptions.Count == 0)
                    return null;
                    
                // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                if (descriptions.ContainsKey("vi"))
                    return descriptions["vi"];
                if (descriptions.ContainsKey("en"))
                    return descriptions["en"];
                    
                // Nếu không có, lấy giá trị đầu tiên
                var firstItem = descriptions.FirstOrDefault();
                return firstItem.Equals(default(KeyValuePair<string, string>)) ? null : firstItem.Value;
            }
            catch
            {
                return null;
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
    }
} 