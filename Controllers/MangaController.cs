using manga_reader_web.Models;
using manga_reader_web.Services.AuthServices;
using manga_reader_web.Services.MangaServices;
using manga_reader_web.Services.MangaServices.MangaInformation;
using manga_reader_web.Services.MangaServices.MangaPageService;
using manga_reader_web.Services.MangaServices.Models;
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
        private readonly IFollowedMangaService _followedMangaService;

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
            IHttpClientFactory httpClientFactory,
            IFollowedMangaService followedMangaService)
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
            _followedMangaService = followedMangaService;
        }

        // Thêm class SessionKeys
        private static class SessionKeys
        {
            public const string CurrentSearchResultData = "CurrentSearchResultData";
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
                _logger.LogInformation("[SEARCH_VIEW] Bắt đầu action Search với page={Page}, pageSize={PageSize}", page, pageSize);
                
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

                _logger.LogInformation("[SEARCH_VIEW] Gọi API search manga với các tham số đã chuyển đổi");
                
                // Thực hiện tìm kiếm và xử lý kết quả bằng MangaSearchService
                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                // Kiểm tra nếu có lỗi cụ thể cần hiển thị (ví dụ: vượt quá giới hạn API)
                if (viewModel.Mangas.Count == 0 && viewModel.TotalCount > 0)
                {
                    ViewBag.ErrorMessage = "Không thể hiển thị kết quả vì đã vượt quá giới hạn 10000 kết quả từ API.";
                }

                // Lưu dữ liệu manga vào Session
                if (viewModel.Mangas != null && viewModel.Mangas.Any())
                {
                    _logger.LogInformation("[SEARCH_VIEW] Lưu {Count} manga vào Session", viewModel.Mangas.Count);
                    HttpContext.Session.SetString(SessionKeys.CurrentSearchResultData, 
                        JsonSerializer.Serialize(viewModel.Mangas));
                }
                else
                {
                    _logger.LogWarning("[SEARCH_VIEW] Không có dữ liệu manga để lưu vào Session");
                }

                // Xác định view mode từ cookie nếu có
                string initialViewMode = "grid"; // Mặc định là grid
                
                // Đọc cookie để xác định chế độ xem mong muốn của người dùng
                if (Request.Cookies.TryGetValue("MangaViewMode", out string cookieViewMode))
                {
                    // Kiểm tra giá trị hợp lệ
                    if (cookieViewMode == "grid" || cookieViewMode == "list")
                    {
                        initialViewMode = cookieViewMode;
                        _logger.LogInformation("[SEARCH_VIEW] Đọc chế độ xem từ cookie: {Mode}", initialViewMode);
                    }
                }
                else
                {
                    _logger.LogInformation("[SEARCH_VIEW] Không tìm thấy cookie chế độ xem, sử dụng mặc định: {Mode}", initialViewMode);
                }

                // Đặt giá trị view mode để sử dụng trong view
                ViewData["InitialViewMode"] = initialViewMode;
                _logger.LogInformation("[SEARCH_VIEW] Render view với mode: {Mode}", initialViewMode);

                // Kiểm tra nếu là HTMX request
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    _logger.LogInformation("[SEARCH_VIEW] Phát hiện HTMX request");
                    
                    // Kiểm tra HX-Target để xác định phần nào của trang cần update
                    string hxTarget = Request.Headers["HX-Target"].FirstOrDefault() ?? "";
                    
                    // Lấy Current-URL và Referer để kiểm tra chuyển trang
                    string hxCurrentUrl = Request.Headers["HX-Current-URL"].FirstOrDefault() ?? "";
                    string referer = Request.Headers["Referer"].FirstOrDefault() ?? "";
                    
                    // Kiểm tra xem đây có phải là request chuyển trang không
                    bool isPageNavigation = false;
                    
                    // Nếu trang nguồn không chứa "/Manga/Search" nhưng đang gọi đến Search
                    if (!string.IsNullOrEmpty(referer) && !referer.Contains("/Manga/Search"))
                    {
                        _logger.LogInformation("[SEARCH_VIEW] Phát hiện chuyển trang từ {0} đến trang tìm kiếm", referer);
                        isPageNavigation = true;
                    }
                    
                    // Nếu đang chuyển trang, trả về nội dung đầy đủ của trang Search dưới dạng partial
                    if (isPageNavigation)
                    {
                        _logger.LogInformation("[SEARCH_VIEW] Trả về toàn bộ nội dung trang Search qua HTMX");
                        return PartialView("Search", viewModel);
                    }
                    
                    // Nếu target là search-results-and-pagination, chỉ trả về phần kết quả và phân trang
                    if (hxTarget == "search-results-and-pagination" || hxTarget == "main-content")
                    {
                        _logger.LogInformation("[SEARCH_VIEW] HTMX request cho search-results-and-pagination");
                        return PartialView("_SearchResultsWrapperPartial", viewModel);
                    }
                    // Nếu có HX-Request nhưng HX-Target không phải là search-results-and-pagination
                    // có thể là yêu cầu tải toàn bộ trang thông qua HTMX (hiếm gặp)
                    else
                    {
                        _logger.LogInformation("[SEARCH_VIEW] HTMX request cho toàn bộ trang, HX-Target: {0}", hxTarget);
                        return PartialView("Search", viewModel);
                    }
                }

                // Trả về view đầy đủ nếu không phải HTMX request
                return View(viewModel);
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

        // Action để lấy partial view cho chế độ xem
        [HttpGet]
        public IActionResult GetMangaViewPartial(string viewMode = "grid")
        {
            try
            {
                _logger.LogInformation("[VIEW_MODE] Bắt đầu action GetMangaViewPartial với viewMode={ViewMode}", viewMode);
                
                // Lấy dữ liệu manga từ Session
                var mangasJson = HttpContext.Session.GetString(SessionKeys.CurrentSearchResultData);
                
                // Nếu không có dữ liệu trong Session, trả về một partial view thông báo
                if (string.IsNullOrEmpty(mangasJson))
                {
                    _logger.LogWarning("[VIEW_MODE] Không tìm thấy dữ liệu manga trong Session");
                    return PartialView("_NoResultsPartial");
                }
                
                // Deserialize dữ liệu từ Session
                var mangas = JsonSerializer.Deserialize<List<MangaViewModel>>(mangasJson);
                
                _logger.LogInformation("[VIEW_MODE] Lấy được {Count} manga từ Session", mangas?.Count ?? 0);
                
                // Thiết lập ViewData cho chế độ xem
                ViewData["InitialViewMode"] = viewMode;
                
                // Tạo model đầy đủ để truyền vào SearchResultsPartial
                var viewModel = new MangaListViewModel
                {
                    Mangas = mangas ?? new List<MangaViewModel>(),
                    CurrentPage = 1, // Giá trị mặc định, không ảnh hưởng đến việc hiển thị kết quả
                    PageSize = mangas?.Count ?? 0,
                    TotalCount = mangas?.Count ?? 0,
                    MaxPages = 1
                };
                
                _logger.LogInformation("[VIEW_MODE] Trả về _SearchResultsPartial với viewMode={ViewMode}", viewMode);
                
                // Trả về partial view với dữ liệu từ Session
                return PartialView("_SearchResultsPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("[VIEW_MODE] Lỗi khi lấy dữ liệu manga từ Session: {Message}", ex.Message);
                return PartialView("_NoResultsPartial");
            }
        }

        // GET: /Manga/Followed
        public async Task<IActionResult> Followed()
        {
            // 1. Kiểm tra xác thực người dùng
            if (!_userService.IsAuthenticated())
            {
                // Chuyển hướng đến trang đăng nhập nếu chưa đăng nhập
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Followed", "Manga") });
            }

            try
            {
                _logger.LogInformation("Bắt đầu lấy danh sách truyện đang theo dõi.");
                // 2. Gọi Service mới để lấy dữ liệu
                var followedMangas = await _followedMangaService.GetFollowedMangaListAsync();

                _logger.LogInformation($"Tìm thấy {followedMangas.Count} truyện đang theo dõi.");

                // 3. Tạo ViewModel (nếu cần thiết - có thể dùng List<FollowedMangaViewModel> trực tiếp)
                var viewModel = followedMangas; // Đơn giản hóa, dùng trực tiếp list

                // 4. Sử dụng ViewRenderService để trả về View hoặc PartialView
                return _viewRenderService.RenderViewBasedOnRequest(this, viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang truyện đang theo dõi.");
                // Có thể hiển thị trang lỗi hoặc thông báo lỗi
                ViewBag.ErrorMessage = "Không thể tải danh sách truyện đang theo dõi. Vui lòng thử lại sau.";
                // Trả về view với model rỗng hoặc view lỗi
                return View(new List<FollowedMangaViewModel>());
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