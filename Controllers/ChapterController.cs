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
                // Tải các trang của chapter
                var pages = await _mangaDexService.FetchChapterPagesAsync(id);
                
                // Nếu chưa có mangaId, tìm trong danh sách chapters để biết thuộc manga nào
                if (string.IsNullOrEmpty(mangaId))
                {
                    // Cần thực hiện một tìm kiếm chapter để lấy mangaId
                    // Trong thực tế, việc này nên được thực hiện bởi một API endpoint riêng
                    // Nhưng trong ví dụ này, chúng ta giả định rằng mangaId đã được cung cấp
                }

                // Lấy thông tin của manga
                var manga = await _mangaDexService.FetchMangaDetailsAsync(mangaId);
                var mangaObj = JsonSerializer.Deserialize<ExpandoObject>(manga.ToString());
                var mangaDict = (IDictionary<string, object>)mangaObj;
                
                var attributes = JsonSerializer.Deserialize<ExpandoObject>(mangaDict["attributes"].ToString());
                var attributesDict = (IDictionary<string, object>)attributes;
                
                var mangaTitle = GetLocalizedTitle(attributesDict["title"].ToString()) ?? "Không có tiêu đề";
                
                // Lấy thông tin các chapter của manga để xác định chapter trước và sau
                var chapters = await _mangaDexService.FetchChaptersAsync(mangaId, "vi,en");
                var chaptersList = new List<ChapterViewModel>();
                
                foreach (var chapter in chapters)
                {
                    var chapterObj = JsonSerializer.Deserialize<ExpandoObject>(chapter.ToString());
                    var chapterDict = (IDictionary<string, object>)chapterObj;
                    
                    var chapterId = chapterDict["id"].ToString();
                    var chapterAttributes = JsonSerializer.Deserialize<ExpandoObject>(chapterDict["attributes"].ToString());
                    var chapterAttributesDict = (IDictionary<string, object>)chapterAttributes;
                    
                    var chapterNumber = chapterAttributesDict.ContainsKey("chapter") ? chapterAttributesDict["chapter"].ToString() : "?";
                    
                    chaptersList.Add(new ChapterViewModel
                    {
                        Id = chapterId,
                        Number = chapterNumber
                    });
                }
                
                // Sắp xếp chapters theo số thứ tự
                chaptersList = chaptersList.OrderBy(c => 
                {
                    if (float.TryParse(c.Number, out float number))
                        return number;
                    return 0;
                }).ToList();
                
                // Tìm chapter hiện tại trong danh sách
                int currentIndex = chaptersList.FindIndex(c => c.Id == id);
                string prevChapterId = null;
                string nextChapterId = null;
                
                if (currentIndex > 0)
                {
                    prevChapterId = chaptersList[currentIndex - 1].Id;
                }
                
                if (currentIndex < chaptersList.Count - 1)
                {
                    nextChapterId = chaptersList[currentIndex + 1].Id;
                }
                
                // Lấy thông tin chi tiết của chapter hiện tại
                var currentChapter = chapters.FirstOrDefault(c => 
                {
                    var chapterObj = JsonSerializer.Deserialize<ExpandoObject>(c.ToString());
                    var chapterDict = (IDictionary<string, object>)chapterObj;
                    return chapterDict["id"].ToString() == id;
                });
                
                var currentChapterObj = JsonSerializer.Deserialize<ExpandoObject>(currentChapter.ToString());
                var currentChapterDict = (IDictionary<string, object>)currentChapterObj;
                
                var currentChapterAttributes = JsonSerializer.Deserialize<ExpandoObject>(currentChapterDict["attributes"].ToString());
                var currentChapterAttributesDict = (IDictionary<string, object>)currentChapterAttributes;
                
                var chapterTitle = currentChapterAttributesDict.ContainsKey("title") ? 
                    currentChapterAttributesDict["title"].ToString() : "";
                var currentChapterNumber = currentChapterAttributesDict.ContainsKey("chapter") ? 
                    currentChapterAttributesDict["chapter"].ToString() : "?";
                
                // Tạo view model
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = id,
                    ChapterTitle = chapterTitle,
                    ChapterNumber = currentChapterNumber,
                    Pages = pages,
                    PrevChapterId = prevChapterId,
                    NextChapterId = nextChapterId
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}");
                ViewBag.ErrorMessage = "Không thể tải chapter. Vui lòng thử lại sau.";
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