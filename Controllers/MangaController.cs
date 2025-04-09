using manga_reader_web.Models;
using manga_reader_web.Services;
using manga_reader_web.Services.MangaServices;
using manga_reader_web.Services.MangaServices.MangaInformation;
using manga_reader_web.Services.MangaServices.MangaPageService;
using manga_reader_web.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace manga_reader_web.Controllers
{
    public class MangaController : Controller
    {
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<MangaController> _logger;
        private readonly JsonConversionService _jsonConversionService;
        private readonly MangaTitleService _mangaTitleService;
        private readonly MangaDetailsService _mangaDetailsService;
        private readonly MangaSearchService _mangaSearchService;
        private readonly ViewRenderService _viewRenderService;

        public MangaController(
            MangaDexService mangaDexService, 
            ILogger<MangaController> logger,
            JsonConversionService jsonConversionService,
            MangaTitleService mangaTitleService,
            MangaDetailsService mangaDetailsService,
            MangaSearchService mangaSearchService,
            ViewRenderService viewRenderService)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
            _jsonConversionService = jsonConversionService;
            _mangaTitleService = mangaTitleService;
            _mangaDetailsService = mangaDetailsService;
            _mangaSearchService = mangaSearchService;
            _viewRenderService = viewRenderService;
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
                
                // Sử dụng MangaDetailsService để lấy thông tin chi tiết manga
                var viewModel = await _mangaDetailsService.GetMangaDetailsAsync(id);
                
                // Nếu có dictionary tiêu đề thay thế, truyền vào ViewData
                if (viewModel.Manga != null && !string.IsNullOrEmpty(viewModel.Manga.AlternativeTitles))
                {
                    // Sử dụng phương thức mới để lấy tiêu đề thay thế
                    var altTitlesDictionary = await _mangaDetailsService.GetAlternativeTitlesByLanguageAsync(id);
                    ViewData["AlternativeTitlesByLanguage"] = altTitlesDictionary;
                }
                
                // Sử dụng ViewRenderService để trả về view phù hợp
                return _viewRenderService.RenderViewBasedOnRequest(this, viewModel);
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

                // Sử dụng MangaSearchService để chuyển đổi tham số tìm kiếm thành SortManga
                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, 
                    status, 
                    sortBy,
                    authors, 
                    artists, 
                    year, 
                    availableTranslatedLanguage, 
                    publicationDemographic, 
                    contentRating,
                    includedTagsMode, 
                    excludedTagsMode, 
                    genres, 
                    includedTagsStr, 
                    excludedTagsStr);

                // Thực hiện tìm kiếm và xử lý kết quả bằng MangaSearchService
                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                // Kiểm tra nếu có lỗi cụ thể cần hiển thị (ví dụ: vượt quá giới hạn API)
                if (viewModel.Mangas.Count == 0 && viewModel.TotalCount > 0)
                {
                    ViewBag.ErrorMessage = "Không thể hiển thị kết quả vì đã vượt quá giới hạn 10000 kết quả từ API.";
                }

                // Sử dụng ViewRenderService để trả về view phù hợp
                return _viewRenderService.RenderViewBasedOnRequest(this, viewModel);
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

        /// <summary>
        /// API endpoint để theo dõi/hủy theo dõi manga
        /// </summary>
        [HttpPost]
        [Route("api/manga/toggle-follow")]
        public IActionResult ToggleFollow(string mangaId)
        {
            try
            {
                _logger.LogInformation($"Placeholder: Yêu cầu toggle follow cho manga {mangaId}");
                
                if (string.IsNullOrEmpty(mangaId))
                {
                    return Json(new { success = false, error = "Manga ID không hợp lệ" });
                }
                
                // TODO: Triển khai logic toggle follow
                bool isFollowing = false; // Mặc định trả về false
                
                return Json(new { 
                    success = true, 
                    isFollowing = isFollowing,
                    message = isFollowing ? "Đã theo dõi manga" : "Đã hủy theo dõi manga" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi toggle follow manga: {ex.Message}", ex);
                return Json(new { success = false, error = "Không thể cập nhật trạng thái theo dõi manga" });
            }
        }
    }
} 