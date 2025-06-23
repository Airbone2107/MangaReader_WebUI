using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.MangaPageService;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Controllers
{
    public class MangaController : Controller
    {
        private readonly IMangaReaderLibTagClient _tagClient;
        private readonly IMangaReaderLibAuthorClient _authorClient;
        private readonly IMangaReaderLibToTagListResponseMapper _tagListResponseMapper;
        private readonly ILogger<MangaController> _logger;
        private readonly MangaDetailsService _mangaDetailsService;
        private readonly MangaSearchService _mangaSearchService;
        private readonly ViewRenderService _viewRenderService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFollowedMangaService _followedMangaService;
        private readonly IReadingHistoryService _readingHistoryService;

        public MangaController(
            IMangaReaderLibTagClient tagClient,
            IMangaReaderLibAuthorClient authorClient,
            IMangaReaderLibToTagListResponseMapper tagListResponseMapper,
            ILogger<MangaController> logger,
            MangaDetailsService mangaDetailsService,
            MangaSearchService mangaSearchService,
            ViewRenderService viewRenderService,
            IMangaFollowService mangaFollowService,
            IUserService userService,
            IHttpClientFactory httpClientFactory,
            IFollowedMangaService followedMangaService,
            IReadingHistoryService readingHistoryService)
        {
            _tagClient = tagClient;
            _authorClient = authorClient;
            _tagListResponseMapper = tagListResponseMapper;
            _logger = logger;
            _mangaDetailsService = mangaDetailsService;
            _mangaSearchService = mangaSearchService;
            _viewRenderService = viewRenderService;
            _mangaFollowService = mangaFollowService;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _followedMangaService = followedMangaService;
            _readingHistoryService = readingHistoryService;
        }

        private static class SessionKeys
        {
            public const string CurrentSearchResultData = "CurrentSearchResultData";
        }

        [HttpGet]
        [Route("api/manga/tags")]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                _logger.LogInformation("Đang lấy danh sách tags từ MangaReaderLib API");
                var tagsDataFromLib = await _tagClient.GetTagsAsync(limit: 500); // Lấy tối đa 500 tags
                if (tagsDataFromLib == null)
                {
                    throw new Exception("API không trả về dữ liệu tags.");
                }

                // Map kết quả từ MangaReaderLib DTO sang MangaDex DTO mà frontend đang sử dụng
                var tagsForFrontend = _tagListResponseMapper.MapToTagListResponse(tagsDataFromLib);

                return Json(new { success = true, data = tagsForFrontend });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tags từ MangaReaderLib API.");
                return Json(new { success = false, error = "Không thể tải danh sách tags." });
            }
        }

        // GET: Manga/Details/5
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                ViewData["PageType"] = "manga-details";
                var viewModel = await _mangaDetailsService.GetMangaDetailsAsync(id);

                if (_userService.IsAuthenticated())
                {
                    bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                    if (viewModel.Manga != null)
                    {
                        viewModel.Manga.IsFollowing = isFollowing;
                    }
                }
                else
                {
                    if (viewModel.Manga != null)
                    {
                        viewModel.Manga.IsFollowing = false;
                    }
                }

                if (viewModel.AlternativeTitlesByLanguage != null && viewModel.AlternativeTitlesByLanguage.Any())
                {
                    ViewData["AlternativeTitlesByLanguage"] = viewModel.AlternativeTitlesByLanguage;
                }

                return _viewRenderService.RenderViewBasedOnRequest(this, "Details", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết manga: {Message}", ex.Message);
                ViewBag.ErrorMessage = "Không thể tải chi tiết manga. Vui lòng thử lại sau.";
                return View("Details", new MangaDetailViewModel { AlternativeTitlesByLanguage = new Dictionary<string, List<string>>() });
            }
        }

        // GET: Manga/Search
        public async Task<IActionResult> Search(
            string title = "",
            List<string>? status = null,
            string sortBy = "updatedAt",
            string? authors = null,
            string? artists = null,
            int? year = null,
            List<string>? publicationDemographic = null,
            List<string>? contentRating = null,
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            string includedTagsStr = "",
            string excludedTagsStr = "",
            int page = 1,
            int pageSize = 24)
        {
            try
            {
                _logger.LogInformation("[SEARCH_VIEW] Bắt đầu action Search với page={Page}, pageSize={PageSize}", page, pageSize);
                ViewData["PageType"] = "home";

                // Gộp 2 chuỗi ID từ authors và artists
                var authorIds = new List<string>();
                if (!string.IsNullOrEmpty(authors)) authorIds.AddRange(authors.Split(',', StringSplitOptions.RemoveEmptyEntries));
                if (!string.IsNullOrEmpty(artists)) authorIds.AddRange(artists.Split(',', StringSplitOptions.RemoveEmptyEntries));
                
                // Lấy danh sách ID duy nhất
                var uniqueAuthorIds = authorIds.Distinct().ToList();

                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, status, sortBy, uniqueAuthorIds, year,
                    publicationDemographic, contentRating,
                    includedTagsMode, excludedTagsMode, includedTagsStr, excludedTagsStr);

                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas != null && viewModel.Mangas.Any())
                {
                    HttpContext.Session.SetString(SessionKeys.CurrentSearchResultData,
                        JsonSerializer.Serialize(viewModel.Mangas));
                }

                string initialViewMode = Request.Cookies.TryGetValue("MangaViewMode", out string? cookieViewMode) && (cookieViewMode == "grid" || cookieViewMode == "list")
                    ? cookieViewMode : "grid";

                ViewData["InitialViewMode"] = initialViewMode;

                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    string hxTarget = Request.Headers["HX-Target"].FirstOrDefault() ?? "";
                    string referer = Request.Headers["Referer"].FirstOrDefault() ?? "";

                    if (!string.IsNullOrEmpty(referer) && !referer.Contains("/Manga/Search"))
                    {
                        return PartialView("Search", viewModel);
                    }

                    if (hxTarget == "search-results-and-pagination" || hxTarget == "main-content")
                    {
                        return PartialView("_SearchResultsWrapperPartial", viewModel);
                    }
                    else
                    {
                        return PartialView("Search", viewModel);
                    }
                }

                return View("Search", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách manga.");
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga. Chi tiết: {ex.Message}";
                return View("Search", new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = new SortManga { Title = title, Status = status, SortBy = sortBy }
                });
            }
        }

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
                }
                else if (checkResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _userService.RemoveToken();
                    return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Không thể kiểm tra trạng thái theo dõi hiện tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking following status for manga {MangaId}", request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi khi kiểm tra trạng thái theo dõi." });
            }

            backendEndpoint = isCurrentlyFollowing ? "/api/users/unfollow" : "/api/users/follow";
            bool targetFollowingState = !isCurrentlyFollowing;
            string successMessage = targetFollowingState ? "Đã theo dõi truyện" : "Đã hủy theo dõi truyện";

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { mangaId = request.MangaId };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(backendEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true, isFollowing = targetFollowingState, message = successMessage });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                        return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                    }
                    return StatusCode((int)response.StatusCode, new { success = false, message = $"Lỗi từ backend: {response.ReasonPhrase}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong proxy action {Endpoint} cho manga {MangaId}", backendEndpoint, request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi xử lý yêu cầu" });
            }
        }

        public async Task<IActionResult> GetSearchResultsPartial(
            string title = "", 
            List<string>? status = null, 
            string sortBy = "updatedAt",
            string? authors = null, 
            string? artists = null,
            int? year = null,
            List<string>? publicationDemographic = null, 
            List<string>? contentRating = null, 
            string includedTagsMode = "AND", 
            string excludedTagsMode = "OR",
            string includedTagsStr = "", 
            string excludedTagsStr = "",
            int page = 1, 
            int pageSize = 24)
        {
            try
            {
                // Gộp 2 chuỗi ID từ authors và artists
                var authorIds = new List<string>();
                if (!string.IsNullOrEmpty(authors)) authorIds.AddRange(authors.Split(',', StringSplitOptions.RemoveEmptyEntries));
                if (!string.IsNullOrEmpty(artists)) authorIds.AddRange(artists.Split(',', StringSplitOptions.RemoveEmptyEntries));
                
                // Lấy danh sách ID duy nhất
                var uniqueAuthorIds = authorIds.Distinct().ToList();

                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, status, sortBy, uniqueAuthorIds, year,
                    publicationDemographic, contentRating,
                    includedTagsMode, excludedTagsMode, includedTagsStr, excludedTagsStr);

                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas.Count == 0)
                {
                    return PartialView("_NoResultsPartial");
                }
                return PartialView("_SearchResultsWrapperPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải kết quả tìm kiếm.");
                return PartialView("_NoResultsPartial");
            }
        }

        [HttpGet]
        public IActionResult GetMangaViewPartial(string viewMode = "grid")
        {
            try
            {
                var mangasJson = HttpContext.Session.GetString(SessionKeys.CurrentSearchResultData);
                if (string.IsNullOrEmpty(mangasJson))
                {
                    return PartialView("_NoResultsPartial");
                }

                var mangas = JsonSerializer.Deserialize<List<MangaViewModel>>(mangasJson);
                ViewData["InitialViewMode"] = viewMode;

                var viewModel = new MangaListViewModel
                {
                    Mangas = mangas ?? new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = mangas?.Count ?? 0,
                    TotalCount = mangas?.Count ?? 0,
                    MaxPages = 1
                };

                return PartialView("_SearchResultsPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu manga từ Session.");
                return PartialView("_NoResultsPartial");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Followed()
        {
            if (!_userService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Followed", "Manga") });
            }

            try
            {
                var followedMangas = await _followedMangaService.GetFollowedMangaListAsync();
                return _viewRenderService.RenderViewBasedOnRequest(this, "Followed", followedMangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang truyện đang theo dõi.");
                ViewBag.ErrorMessage = "Không thể tải danh sách truyện đang theo dõi. Vui lòng thử lại sau.";
                return View("Followed", new List<FollowedMangaViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            if (!_userService.IsAuthenticated())
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_UnauthorizedPartial");
                }
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("History", "Manga") });
            }

            try
            {
                var history = await _readingHistoryService.GetReadingHistoryAsync();
                return _viewRenderService.RenderViewBasedOnRequest(this, "History", history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang lịch sử đọc truyện.");
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                    return PartialView("_ErrorPartial");
                }
                ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                return View("History", new List<LastReadMangaViewModel>());
            }
        }

        [HttpGet("api/manga/search-authors")]
        public async Task<IActionResult> SearchAuthors([FromQuery] string nameFilter)
        {
            if (string.IsNullOrWhiteSpace(nameFilter) || nameFilter.Length < 2)
            {
                return Ok(new List<AuthorSearchResultViewModel>());
            }

            try
            {
                var authorResponse = await _authorClient.GetAuthorsAsync(nameFilter: nameFilter, limit: 10);
                if (authorResponse?.Data == null)
                {
                    return Ok(new List<AuthorSearchResultViewModel>());
                }

                var results = authorResponse.Data.Select(a => new AuthorSearchResultViewModel
                {
                    Id = Guid.Parse(a.Id),
                    Name = a.Attributes.Name
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm tác giả với filter: {NameFilter}", nameFilter);
                return StatusCode(500, "Lỗi máy chủ khi tìm kiếm tác giả.");
            }
        }
    }

    public class MangaActionRequest
    {
        public string? MangaId { get; set; }
    }

    public class FollowingStatusResponse
    {
        public bool IsFollowing { get; set; }
    }
}