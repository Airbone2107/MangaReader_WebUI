using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Shared;
using MangaReader.WebUI.Services.MangaServices;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.APIServices.Services;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Enums;
using System;

namespace MangaReader.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IApiStatusService _apiStatusService;
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        private readonly ILogger<HomeController> _logger;
        private readonly IMangaToMangaViewModelMapper _mangaViewModelMapper;

        public HomeController(
            IApiStatusService apiStatusService, 
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<HomeController> logger,
            IMangaToMangaViewModelMapper mangaViewModelMapper)
        {
            _apiStatusService = apiStatusService;
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
            _logger = logger;
            _mangaViewModelMapper = mangaViewModelMapper;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["PageType"] = "home";
                
                bool isConnected = await _apiStatusService.TestConnectionAsync();
                if (!isConnected)
                {
                    _logger.LogWarning("Không thể kết nối đến API");
                    ViewBag.ErrorMessage = "Không thể kết nối đến API. Vui lòng thử lại sau.";
                    ViewBag.IsConnected = false;
                    return View("Index", new List<MangaViewModel>());
                }
                
                ViewBag.IsConnected = true;
                
                try
                {
                    var sortOptions = new SortManga { 
                        SortBy = "Mới cập nhật",
                        Languages = new List<string> { "vi", "en" }
                    };
                    
                    var currentSource = HttpContext.Request.Cookies.TryGetValue("MangaSource", out var sourceString) &&
                                        Enum.TryParse(sourceString, true, out MangaSource sourceEnum)
                                        ? sourceEnum
                                        : MangaSource.MangaDex; 

                    _logger.LogInformation("Trang chủ: Nguồn truyện hiện tại là {MangaSource}", currentSource);

                    if (currentSource == MangaSource.MangaDex)
                    {
                        sortOptions.ContentRating = new List<string> { "safe" };
                        _logger.LogInformation("Trang chủ (MangaDex): Áp dụng ContentRating = 'safe'");
                    }
                    else 
                    {
                        sortOptions.ContentRating = new List<string>(); 
                        _logger.LogInformation("Trang chủ (MangaReaderLib): Không áp dụng ContentRating filter");
                    }
                    
                    var recentMangaResponse = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);

                    if (recentMangaResponse?.Data == null || !recentMangaResponse.Data.Any())
                    {
                        _logger.LogWarning("API đã kết nối nhưng không trả về dữ liệu manga");
                        ViewBag.ErrorMessage = "Không có dữ liệu manga. Vui lòng thử lại sau.";
                        return View("Index", new List<MangaViewModel>());
                    }

                    var viewModels = new List<MangaViewModel>();
                    var mangaListToProcess = recentMangaResponse.Data;
                    
                    foreach (var manga in mangaListToProcess)
                    {
                        try
                        {
                            var viewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(manga);
                            viewModels.Add(viewModel);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi map manga ID: {manga?.Id} trên trang chủ.");
                        }
                    }

                    if (viewModels.Count == 0 && mangaListToProcess.Any())
                    {
                        ViewBag.ErrorMessage = "Không thể hiển thị dữ liệu manga. Định dạng dữ liệu không hợp lệ.";
                    }

                    if (Request.Headers.ContainsKey("HX-Request"))
                    {
                        return PartialView("Index", viewModels);
                    }

                    return View("Index", viewModels);
                }
                catch (Exception apiEx)
                {
                    _logger.LogError($"Lỗi khi gọi API: {apiEx.Message}");
                    ViewBag.ErrorMessage = $"Lỗi khi tải dữ liệu từ API: {apiEx.Message}";
                    ViewBag.StackTrace = apiEx.StackTrace;
                    return View("Index", new List<MangaViewModel>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải trang chủ: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace; 
                return View("Index", new List<MangaViewModel>());
            }
        }

        public async Task<IActionResult> ApiTest()
        {
            ViewData["PageType"] = "home";
            var testResults = new Dictionary<string, string>();
            
            try
            {
                testResults.Add("API Connection", await _apiStatusService.TestConnectionAsync() ? "Success" : "Failed");
                
                try
                {
                    var manga = await _mangaApiService.FetchMangaAsync(1, 0);
                    testResults.Add("Fetch Manga", $"Success - Found {manga?.Total ?? 0} items; {manga?.Data?.Count ?? 0} items returned");
                }
                catch (Exception ex)
                {
                    testResults.Add("Fetch Manga", $"Failed - {ex.Message}");
                }
                
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("ApiTest", testResults);
                }
                
                return View("ApiTest", testResults);
            }
            catch (Exception ex)
            {
                testResults.Add("General Error", ex.Message);
                return View("ApiTest", testResults);
            }
        }

        public IActionResult Privacy()
        {
            ViewData["PageType"] = "home";
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("Privacy");
            }
            return View("Privacy");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ViewData["PageType"] = "home";
            var model = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("Error", model);
            }
            return View("Error", model);
        }
        
        public async Task<IActionResult> GetLatestMangaPartial()
        {
            try
            {
                var sortOptions = new SortManga { 
                    SortBy = "Mới cập nhật",
                    Languages = new List<string> { "vi", "en" }
                };
                
                var recentMangaResponse = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);
                var viewModels = new List<MangaViewModel>();
                var mangaListToProcess = recentMangaResponse?.Data ?? new List<MangaReader.WebUI.Models.Mangadex.Manga>();
                
                foreach (var manga in mangaListToProcess)
                {
                    try
                    {
                        viewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(manga));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi map manga ID: {manga?.Id} trong partial.");
                    }
                }
                
                return PartialView("_MangaGridPartial", viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách manga mới nhất cho partial.");
                return PartialView("_ErrorPartial", "Không thể tải danh sách manga mới nhất.");
            }
        }
    }
}
