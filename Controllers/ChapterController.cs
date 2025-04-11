using manga_reader_web.Models;
using manga_reader_web.Services.MangaServices.ChapterServices;
using manga_reader_web.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;

namespace manga_reader_web.Controllers
{
    public class ChapterController : Controller
    {
        private readonly ILogger<ChapterController> _logger;
        private readonly ChapterReadingServices _chapterReadingServices;
        private readonly ViewRenderService _viewRenderService;

        public ChapterController(
            ChapterReadingServices chapterReadingServices,
            ViewRenderService viewRenderService,
            ILogger<ChapterController> logger)
        {
            _chapterReadingServices = chapterReadingServices;
            _viewRenderService = viewRenderService;
            _logger = logger;
        }

        // GET: Chapter/Read/5
        public async Task<IActionResult> Read(string id)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xử lý yêu cầu đọc chapter {id}");
                
                var viewModel = await _chapterReadingServices.GetChapterReadViewModel(id);
                
                // Sử dụng ViewRenderService để trả về view phù hợp với loại request
                return _viewRenderService.RenderViewBasedOnRequest(this, viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}");
                ViewBag.ErrorMessage = $"Không thể tải chapter. Lỗi: {ex.Message}";
                return View(new ChapterReadViewModel());
            }
        }
        
        // GET: Chapter/GetChapterImagesPartial/5
        public async Task<IActionResult> GetChapterImagesPartial(string id)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xử lý yêu cầu lấy ảnh cho chapter {id}");
                
                var viewModel = await _chapterReadingServices.GetChapterReadViewModel(id);
                
                return PartialView("_ChapterImagesPartial", viewModel.Pages);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải ảnh chapter: {ex.Message}");
                return PartialView("_ChapterImagesPartial", new List<string>());
            }
        }
    }
} 