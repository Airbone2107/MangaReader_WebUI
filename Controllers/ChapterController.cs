using manga_reader_web.Models;
using manga_reader_web.Services.MangaServices.ChapterServices;
using Microsoft.AspNetCore.Mvc;

namespace manga_reader_web.Controllers
{
    public class ChapterController : Controller
    {
        private readonly ILogger<ChapterController> _logger;
        private readonly ChapterReadingServices _chapterReadingServices;

        public ChapterController(
            ChapterReadingServices chapterReadingServices,
            ILogger<ChapterController> logger)
        {
            _chapterReadingServices = chapterReadingServices;
            _logger = logger;
        }

        // GET: Chapter/Read/5
        public async Task<IActionResult> Read(string id)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xử lý yêu cầu đọc chapter {id}");
                
                var viewModel = await _chapterReadingServices.GetChapterReadViewModel(id);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}");
                ViewBag.ErrorMessage = $"Không thể tải chapter. Lỗi: {ex.Message}";
                return View(new ChapterReadViewModel());
            }
        }
    }
} 