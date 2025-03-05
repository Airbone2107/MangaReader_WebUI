using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using manga_reader_web.Models;
using manga_reader_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
                    var mangaDict = (IDictionary<string, object>)manga;
                    
                    var id = mangaDict["id"].ToString();
                    var attributes = JsonSerializer.Deserialize<ExpandoObject>(mangaDict["attributes"].ToString());
                    var attributesDict = (IDictionary<string, object>)attributes;
                    
                    var mangaTitle = GetLocalizedTitle(attributesDict["title"].ToString()) ?? "Không có tiêu đề";
                    var description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
                    
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
                var mangaObj = JsonSerializer.Deserialize<ExpandoObject>(manga.ToString());
                var mangaDict = (IDictionary<string, object>)mangaObj;
                
                var attributes = JsonSerializer.Deserialize<ExpandoObject>(mangaDict["attributes"].ToString());
                var attributesDict = (IDictionary<string, object>)attributes;
                
                var title = GetLocalizedTitle(attributesDict["title"].ToString()) ?? "Không có tiêu đề";
                var description = GetLocalizedDescription(attributesDict["description"].ToString()) ?? "";
                
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
                    var chapterObj = JsonSerializer.Deserialize<ExpandoObject>(chapter.ToString());
                    var chapterDict = (IDictionary<string, object>)chapterObj;
                    
                    var chapterAttributes = JsonSerializer.Deserialize<ExpandoObject>(chapterDict["attributes"].ToString());
                    var chapterAttributesDict = (IDictionary<string, object>)chapterAttributes;
                    
                    var chapterNumber = chapterAttributesDict.ContainsKey("chapter") ? chapterAttributesDict["chapter"].ToString() : "?";
                    var chapterTitle = chapterAttributesDict.ContainsKey("title") ? chapterAttributesDict["title"].ToString() : $"Chapter {chapterNumber}";
                    var publishedAt = chapterAttributesDict.ContainsKey("publishAt") ? 
                        DateTime.Parse(chapterAttributesDict["publishAt"].ToString()) : DateTime.Now;
                    var language = chapterAttributesDict.ContainsKey("translatedLanguage") ? 
                        chapterAttributesDict["translatedLanguage"].ToString() : "unknown";
                    
                    chapterViewModels.Add(new ChapterViewModel
                    {
                        Id = chapterDict["id"].ToString(),
                        Title = chapterTitle,
                        Number = chapterNumber,
                        Language = language,
                        PublishedAt = publishedAt
                    });
                }
                
                // Sắp xếp chapters theo thứ tự giảm dần
                chapterViewModels = chapterViewModels.OrderByDescending(c => 
                {
                    if (float.TryParse(c.Number, out float number))
                        return number;
                    return 0;
                }).ToList();
                
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
        
        private string GetLocalizedDescription(string descriptionJson)
        {
            try
            {
                var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);
                // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                if (descriptions.ContainsKey("vi"))
                    return descriptions["vi"];
                if (descriptions.ContainsKey("en"))
                    return descriptions["en"];
                // Nếu không có, lấy giá trị đầu tiên
                return descriptions.FirstOrDefault().Value;
            }
            catch
            {
                return null;
            }
        }
    }
} 