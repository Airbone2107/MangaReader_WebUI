using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using manga_reader_web.Models;
using manga_reader_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace manga_reader_web.Controllers
{
    public class ChapterController : Controller
    {
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<ChapterController> _logger;

        public ChapterController(MangaDexService mangaDexService, ILogger<ChapterController> logger)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
        }

        // GET: Chapter/Read/5
        public async Task<IActionResult> Read(string id, string mangaId)
        {
            try
            {
                _logger.LogInformation($"Đang tải chapter {id} của manga {mangaId}");
                
                // Tải các trang của chapter
                var pages = await _mangaDexService.FetchChapterPagesAsync(id);
                
                // Nếu chưa có mangaId, tìm trong danh sách chapters để biết thuộc manga nào
                if (string.IsNullOrEmpty(mangaId))
                {
                    _logger.LogWarning($"MangaId không được cung cấp cho chapter {id}");
                    ViewBag.ErrorMessage = "Không thể xác định manga cho chapter này.";
                    return View(new ChapterReadViewModel());
                }

                // Lấy thông tin của manga
                var manga = await _mangaDexService.FetchMangaDetailsAsync(mangaId);
                
                var mangaObj = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                var mangaDict = new Dictionary<string, object>();
                
                // Chuyển đổi JsonElement thành Dictionary
                foreach (var property in mangaObj.EnumerateObject())
                {
                    mangaDict[property.Name] = property.Value;
                }
                
                if (!mangaDict.ContainsKey("attributes") || mangaDict["attributes"] == null)
                {
                    _logger.LogWarning($"Manga {mangaId} không có thuộc tính attributes");
                    ViewBag.ErrorMessage = "Dữ liệu manga không hợp lệ.";
                    return View(new ChapterReadViewModel());
                }
                
                var attributesElement = (JsonElement)mangaDict["attributes"];
                var attributesDict = new Dictionary<string, object>();
                
                // Chuyển đổi JsonElement thành Dictionary
                foreach (var property in attributesElement.EnumerateObject())
                {
                    attributesDict[property.Name] = property.Value;
                }
                
                if (!attributesDict.ContainsKey("title") || attributesDict["title"] == null)
                {
                    _logger.LogWarning($"Manga {mangaId} không có thuộc tính title");
                    ViewBag.ErrorMessage = "Dữ liệu manga không hợp lệ, thiếu tiêu đề.";
                    return View(new ChapterReadViewModel());
                }
                
                var titleElement = (JsonElement)attributesDict["title"];
                var mangaTitle = GetLocalizedTitle(titleElement.ToString()) ?? "Không có tiêu đề";
                _logger.LogInformation($"Tiêu đề manga: {mangaTitle}");
                
                // Đầu tiên, cần xác định ngôn ngữ của chapter hiện tại
                string currentChapterLanguage = "unknown";
                
                // Tìm chapter hiện tại và ngôn ngữ của nó
                var allSessionChaptersJson = HttpContext.Session.GetString($"Manga_{mangaId}_AllChapters");
                
                if (!string.IsNullOrEmpty(allSessionChaptersJson))
                {
                    try
                    {
                        var allChaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(allSessionChaptersJson);
                        var currentChapter = allChaptersList.FirstOrDefault(c => c.Id == id);
                        if (currentChapter != null)
                        {
                            currentChapterLanguage = currentChapter.Language;
                            _logger.LogInformation($"Đã xác định ngôn ngữ của chapter hiện tại: {currentChapterLanguage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi đọc danh sách tất cả chapters từ session: {ex.Message}");
                    }
                }
                
                // Nếu không thể xác định từ session, cần tải chapter hiện tại để xác định ngôn ngữ
                if (currentChapterLanguage == "unknown")
                {
                    _logger.LogInformation("Không thể xác định ngôn ngữ từ session, tải chapter từ API");
                    var chapterInfo = await _mangaDexService.FetchChapterInfoAsync(id);
                    
                    if (chapterInfo != null)
                    {
                        var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapterInfo.ToString());
                        if (chapterElement.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("attributes", out JsonElement attributesElement2) &&
                            attributesElement2.TryGetProperty("translatedLanguage", out JsonElement langElement))
                        {
                            currentChapterLanguage = langElement.GetString() ?? "unknown";
                            _logger.LogInformation($"Đã xác định ngôn ngữ của chapter từ API: {currentChapterLanguage}");
                        }
                    }
                }
                
                // Lấy danh sách chapter có cùng ngôn ngữ từ session
                List<ChapterViewModel> chaptersList = new List<ChapterViewModel>();
                var sessionChaptersJson = HttpContext.Session.GetString($"Manga_{mangaId}_Chapters_{currentChapterLanguage}");
                
                if (!string.IsNullOrEmpty(sessionChaptersJson))
                {
                    _logger.LogInformation($"Đã tìm thấy danh sách chapters ngôn ngữ {currentChapterLanguage} trong session");
                    try
                    {
                        chaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(sessionChaptersJson);
                        _logger.LogInformation($"Đã lấy {chaptersList.Count} chapters từ session");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi đọc danh sách chapters từ session: {ex.Message}");
                        // Nếu lỗi, session sẽ bị bỏ qua và tiếp tục lấy chapters từ API
                    }
                }
                
                // Nếu không có chapter trong session, lấy từ API và lọc theo ngôn ngữ
                if (chaptersList == null || chaptersList.Count == 0)
                {
                    _logger.LogInformation($"Không tìm thấy chapters ngôn ngữ {currentChapterLanguage} trong session, lấy từ API");
                    var chapters = await _mangaDexService.FetchChaptersAsync(mangaId, currentChapterLanguage);
                    _logger.LogInformation($"Đã lấy {chapters.Count} chapters từ API");
                    
                    // Lưu tất cả chapters (để sử dụng trong tương lai)
                    var allChaptersList = new List<ChapterViewModel>();
                    
                    // Chỉ lấy chapters với ngôn ngữ hiện tại
                    chaptersList = new List<ChapterViewModel>();
                    
                    foreach (var chapter in chapters)
                    {
                        try
                        {
                            var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapter.ToString());
                            var chapterDict = new Dictionary<string, object>();
                            
                            // Chuyển đổi JsonElement thành Dictionary
                            foreach (var property in chapterElement.EnumerateObject())
                            {
                                chapterDict[property.Name] = property.Value;
                            }
                            
                            if (!chapterDict.ContainsKey("id"))
                            {
                                _logger.LogWarning("Chapter không có ID, bỏ qua");
                                continue;
                            }
                            
                            var chapterId = chapterDict["id"].ToString();
                            
                            if (!chapterDict.ContainsKey("attributes") || chapterDict["attributes"] == null)
                            {
                                _logger.LogWarning($"Chapter {chapterId} không có attributes, bỏ qua");
                                continue;
                            }
                            
                            var chapterAttributesElement = (JsonElement)chapterDict["attributes"];
                            var chapterAttributesDict = new Dictionary<string, object>();
                            
                            // Chuyển đổi JsonElement thành Dictionary
                            foreach (var property in chapterAttributesElement.EnumerateObject())
                            {
                                chapterAttributesDict[property.Name] = property.Value;
                            }
                            
                            var chapterNumber = chapterAttributesDict.ContainsKey("chapter") && chapterAttributesDict["chapter"] != null
                                ? chapterAttributesDict["chapter"].ToString() 
                                : "?";
                                
                            var chapterTitle = chapterAttributesDict.ContainsKey("title") && chapterAttributesDict["title"] != null
                                ? chapterAttributesDict["title"].ToString() 
                                : "";
                            
                            // Xử lý tên chương theo cách của Flutter
                            var displayTitle = string.IsNullOrEmpty(chapterTitle) || chapterTitle.Equals(chapterNumber, StringComparison.OrdinalIgnoreCase)
                                ? $"Chương {chapterNumber}"
                                : $"Chương {chapterNumber}: {chapterTitle}";
                            
                            var language = chapterAttributesDict.ContainsKey("translatedLanguage") && chapterAttributesDict["translatedLanguage"] != null
                                ? chapterAttributesDict["translatedLanguage"].ToString() 
                                : "unknown";
                                
                            var publishAt = chapterAttributesDict.ContainsKey("publishAt") && chapterAttributesDict["publishAt"] != null
                                ? DateTime.Parse(chapterAttributesDict["publishAt"].ToString())
                                : DateTime.Now;
                            
                            var chapterViewModel = new ChapterViewModel
                            {
                                Id = chapterId,
                                Number = chapterNumber,
                                Title = displayTitle,
                                Language = language,
                                PublishedAt = publishAt
                            };
                            
                            // Thêm vào danh sách tất cả chapters
                            allChaptersList.Add(chapterViewModel);
                            
                            // Chỉ thêm vào danh sách chapters cùng ngôn ngữ
                            if (language == currentChapterLanguage)
                            {
                                chaptersList.Add(chapterViewModel);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Lỗi xử lý chapter: {ex.Message}");
                        }
                    }
                    
                    // Sắp xếp chapters theo số thứ tự
                    chaptersList = chaptersList.OrderBy(c => 
                    {
                        if (float.TryParse(c.Number, out float number))
                            return number;
                        return 0;
                    }).ToList();
                    
                    // Lưu danh sách tất cả chapters vào session
                    HttpContext.Session.SetString($"Manga_{mangaId}_AllChapters", JsonSerializer.Serialize(allChaptersList));
                    
                    // Lưu danh sách chapters theo ngôn ngữ vào session
                    HttpContext.Session.SetString($"Manga_{mangaId}_Chapters_{currentChapterLanguage}", JsonSerializer.Serialize(chaptersList));
                    _logger.LogInformation($"Đã lưu {chaptersList.Count} chapters ngôn ngữ {currentChapterLanguage} vào session");
                }
                
                // Tìm chapter hiện tại trong danh sách các chapter cùng ngôn ngữ
                int currentIndex = chaptersList.FindIndex(c => c.Id == id);
                _logger.LogInformation($"Chapter hiện tại ở vị trí: {currentIndex}, tổng số: {chaptersList.Count}");
                
                string prevChapterId = null;
                string nextChapterId = null;
                
                if (currentIndex > 0)
                {
                    prevChapterId = chaptersList[currentIndex - 1].Id;
                }
                
                if (currentIndex < chaptersList.Count - 1 && currentIndex >= 0)
                {
                    nextChapterId = chaptersList[currentIndex + 1].Id;
                }
                
                _logger.LogInformation($"Chapter trước: {prevChapterId}, Chapter sau: {nextChapterId}");
                
                // Lấy thông tin về chapter hiện tại
                ChapterViewModel currentChapterViewModel = null;
                
                if (currentIndex >= 0 && currentIndex < chaptersList.Count)
                {
                    currentChapterViewModel = chaptersList[currentIndex];
                }
                else
                {
                    _logger.LogWarning($"Không tìm thấy chapter {id} trong danh sách");
                    
                    // Tạo một chapter mặc định với thông tin tối thiểu
                    currentChapterViewModel = new ChapterViewModel
                    {
                        Id = id,
                        Number = "?",
                        Title = "Chương không xác định",
                        Language = currentChapterLanguage
                    };
                }
                
                // Tạo view model
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = id,
                    ChapterTitle = currentChapterViewModel.Title,
                    ChapterNumber = currentChapterViewModel.Number,
                    ChapterLanguage = currentChapterLanguage,
                    Pages = pages,
                    PrevChapterId = prevChapterId,
                    NextChapterId = nextChapterId
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}\nStack Trace: {ex.StackTrace}");
                ViewBag.ErrorMessage = $"Không thể tải chapter. Lỗi: {ex.Message}";
                return View(new ChapterReadViewModel());
            }
        }
        
        // Hàm utility để xử lý dữ liệu
        private string GetLocalizedTitle(string titleJson)
        {
            try
            {
                var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                if (titles.ContainsKey("vi"))
                    return titles["vi"];
                if (titles.ContainsKey("en"))
                    return titles["en"];
                // Nếu không có, lấy giá trị đầu tiên
                return titles.FirstOrDefault().Value;
            }
            catch
            {
                return null;
            }
        }
    }
} 