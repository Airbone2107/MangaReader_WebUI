using manga_reader_web.Models;
using manga_reader_web.Services.AuthServices;
using manga_reader_web.Services.MangaServices;
using manga_reader_web.Services.MangaServices.MangaInformation;
using manga_reader_web.Services.MangaServices.MangaPageService;
using manga_reader_web.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
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
        private readonly IMangaFollowService _mangaFollowService;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;

        public MangaController(
            MangaDexService mangaDexService, 
            ILogger<MangaController> logger,
            JsonConversionService jsonConversionService,
            MangaTitleService mangaTitleService,
            MangaDetailsService mangaDetailsService,
            MangaSearchService mangaSearchService,
            ViewRenderService viewRenderService,
            IMangaFollowService mangaFollowService,
            IUserService userService,
            IHttpClientFactory httpClientFactory)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
            _jsonConversionService = jsonConversionService;
            _mangaTitleService = mangaTitleService;
            _mangaDetailsService = mangaDetailsService;
            _mangaSearchService = mangaSearchService;
            _viewRenderService = viewRenderService;
            _mangaFollowService = mangaFollowService;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
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
                
                // Kiểm tra trạng thái theo dõi nếu người dùng đã đăng nhập
                if (_userService.IsAuthenticated())
                {
                    // Gọi API để kiểm tra trạng thái theo dõi
                    bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                    // Cập nhật trạng thái theo dõi trong model
                    viewModel.Manga.IsFollowing = isFollowing;
                }
                else
                {
                    // Nếu chưa đăng nhập, mặc định là false
                    viewModel.Manga.IsFollowing = false;
                }

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
        public async Task<IActionResult> ToggleFollow(string mangaId)
        {
            try
            {
                _logger.LogInformation($"Yêu cầu toggle follow cho manga {mangaId}");
                
                if (string.IsNullOrEmpty(mangaId))
                {
                    return Json(new { success = false, error = "Manga ID không hợp lệ" });
                }
                
                if (!_userService.IsAuthenticated())
                {
                    return Json(new { success = false, error = "Vui lòng đăng nhập để theo dõi truyện", requireLogin = true });
                }
                
                // Sử dụng service để thực hiện toggle follow
                bool isFollowing = await _mangaFollowService.ToggleFollowStatusAsync(mangaId);
                
                return Json(new { 
                    success = true, 
                    isFollowing = isFollowing,
                    message = isFollowing ? "Đã theo dõi truyện" : "Đã hủy theo dõi truyện" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi toggle follow manga: {ex.Message}", ex);
                return Json(new { success = false, error = "Không thể cập nhật trạng thái theo dõi manga" });
            }
        }

        /// <summary>
        /// Proxy action để xử lý toggle follow sử dụng API backend trực tiếp
        /// </summary>
        [HttpPost]
        [Route("api/proxy/toggle-follow")]
        public async Task<IActionResult> ToggleFollowProxy([FromBody] MangaActionRequest request)
        {
            if (string.IsNullOrEmpty(request?.MangaId))
            {
                return BadRequest(new { success = false, message = "Manga ID không hợp lệ" });
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Toggle follow attempt failed: User not authenticated.");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
            }

            string backendEndpoint;
            bool isCurrentlyFollowing;

            // --- Kiểm tra trạng thái hiện tại trước khi gọi backend ---
            try
            {
                var checkClient = _httpClientFactory.CreateClient("BackendApiClient");
                var checkToken = _userService.GetToken();
                checkClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", checkToken);
                var checkResponse = await checkClient.GetAsync($"/api/users/user/following/{request.MangaId}");

                if (checkResponse.IsSuccessStatusCode)
                {
                    var checkContent = await checkResponse.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<FollowingStatusResponse>(checkContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    isCurrentlyFollowing = statusResponse?.IsFollowing ?? false;
                    _logger.LogInformation("Current following status for manga {MangaId}: {IsFollowing}", request.MangaId, isCurrentlyFollowing);
                }
                else if (checkResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                     _logger.LogWarning("Unauthorized check for following status manga {MangaId}.", request.MangaId);
                     _userService.RemoveToken(); // Remove invalid token
                     return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                }
                else
                {
                    _logger.LogError("Failed to check following status for manga {MangaId}. Status: {StatusCode}", request.MangaId, checkResponse.StatusCode);
                    return StatusCode(500, new { success = false, message = "Không thể kiểm tra trạng thái theo dõi hiện tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking following status for manga {MangaId}", request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi khi kiểm tra trạng thái theo dõi." });
            }

            // Quyết định endpoint backend nào cần gọi
            backendEndpoint = isCurrentlyFollowing ? "/api/users/unfollow" : "/api/users/follow";
            bool targetFollowingState = !isCurrentlyFollowing; // Trạng thái mong đợi sau khi gọi
            string successMessage = targetFollowingState ? "Đã theo dõi truyện" : "Đã hủy theo dõi truyện";

            // --- Gọi endpoint follow/unfollow của backend ---
            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken(); // Lấy token lại trong trường hợp bị xóa
                 if (string.IsNullOrEmpty(token)) {
                     // Không nên xảy ra nếu IsAuthenticated đã pass, nhưng kiểm tra lại
                     return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
                 }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { mangaId = request.MangaId };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Proxying {Action} request to backend: {Endpoint} for manga {MangaId}",
                                       targetFollowingState ? "FOLLOW" : "UNFOLLOW", backendEndpoint, request.MangaId);

                var response = await client.PostAsync(backendEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    // Backend thành công. Trả về định dạng mong muốn cho JS.
                    _logger.LogInformation("Backend request successful for {Endpoint}, manga {MangaId}", backendEndpoint, request.MangaId);
                    return Ok(new { success = true, isFollowing = targetFollowingState, message = successMessage });
                }
                else
                {
                    // Ghi log lỗi từ backend
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Backend request failed for {Endpoint}, manga {MangaId}. Status: {StatusCode}, Content: {ErrorContent}",
                                     backendEndpoint, request.MangaId, response.StatusCode, errorContent);

                    // Nếu backend trả về 401, làm mất hiệu lực phiên làm việc frontend
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                        return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                    }

                    // Chuyển tiếp lỗi chung
                    return StatusCode((int)response.StatusCode, new { success = false, message = $"Lỗi từ backend: {response.ReasonPhrase}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong proxy action {Endpoint} cho manga {MangaId}", backendEndpoint, request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi xử lý yêu cầu" });
            }
        }

        /// <summary>
        /// Partial view cho kết quả tìm kiếm (được sử dụng bởi HTMX)
        /// </summary>
        public async Task<IActionResult> GetSearchResultsPartial(
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

                // Thực hiện tìm kiếm và xử lý kết quả
                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas.Count == 0)
                {
                    return PartialView("_NoResultsPartial");
                }

                return PartialView("_SearchResultsWrapperPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải kết quả tìm kiếm: {ex.Message}");
                return PartialView("_NoResultsPartial");
            }
        }
    }

    // Lớp hỗ trợ cho request body
    public class MangaActionRequest
    {
        public string MangaId { get; set; }
    }

    // Lớp hỗ trợ để phân tích phản hồi kiểm tra trạng thái
    public class FollowingStatusResponse
    {
        public bool IsFollowing { get; set; }
    }
} 