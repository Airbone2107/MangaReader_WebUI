using MangaReader.WebUI.Models;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Controllers
{
    public class ChapterController : Controller
    {
        private readonly ILogger<ChapterController> _logger;
        private readonly ChapterReadingServices _chapterReadingServices;
        private readonly ViewRenderService _viewRenderService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;

        public ChapterController(
            ChapterReadingServices chapterReadingServices,
            ViewRenderService viewRenderService,
            ILogger<ChapterController> logger,
            IHttpClientFactory httpClientFactory,
            IUserService userService)
        {
            _chapterReadingServices = chapterReadingServices;
            _viewRenderService = viewRenderService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
        }

        // GET: Chapter/Read/5
        public async Task<IActionResult> Read(string id)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xử lý yêu cầu đọc chapter {id}");
                
                var viewModel = await _chapterReadingServices.GetChapterReadViewModel(id);
                
                // Sử dụng ViewRenderService để trả về view phù hợp với loại request
                return _viewRenderService.RenderViewBasedOnRequest(this, "Read", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}");
                ViewBag.ErrorMessage = $"Không thể tải chapter. Lỗi: {ex.Message}";
                return View("Read", new ChapterReadViewModel());
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

        [HttpPost]
        public async Task<IActionResult> SaveReadingProgress(string mangaId, string chapterId)
        {
            _logger.LogInformation($"Nhận yêu cầu lưu tiến độ đọc: MangaId={mangaId}, ChapterId={chapterId}");

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lưu tiến độ.");
                return Unauthorized();
            }

            var token = _userService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Không thể lấy token người dùng đã đăng nhập.");
                return Unauthorized();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { mangaId = mangaId, lastChapter = chapterId };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/users/reading-progress", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Lưu tiến độ đọc thành công cho MangaId={mangaId}, ChapterId={chapterId}");
                    return Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi khi gọi API backend để lưu tiến độ đọc. Status: {response.StatusCode}, Content: {errorContent}");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                        return Unauthorized();
                    }
                    return StatusCode((int)response.StatusCode, $"Lỗi từ backend: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi ngoại lệ khi lưu tiến độ đọc cho MangaId={mangaId}, ChapterId={chapterId}");
                return StatusCode(500, "Lỗi máy chủ nội bộ khi lưu tiến độ đọc.");
            }
        }
    }
} 