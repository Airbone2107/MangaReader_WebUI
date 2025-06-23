using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Shared;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MangaReader.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<HomeController> _logger;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;

        public HomeController(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<HomeController> logger,
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaViewModelMapper = mangaViewModelMapper;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["PageType"] = "home";

                var recentMangaResponse = await _mangaClient.GetMangasAsync(
                    limit: 10,
                    orderBy: "updatedAt",
                    ascending: false,
                    includes: new List<string> { "cover_art", "author" }
                );

                if (recentMangaResponse?.Data == null || !recentMangaResponse.Data.Any())
                {
                    _logger.LogWarning("API không trả về dữ liệu manga mới nhất.");
                    ViewBag.ErrorMessage = "Không có dữ liệu manga. Vui lòng thử lại sau.";
                    return View("Index", new List<MangaViewModel>());
                }

                var viewModels = new List<MangaViewModel>();
                foreach (var mangaDto in recentMangaResponse.Data)
                {
                    try
                    {
                        var viewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaDto);
                        viewModels.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi map manga ID: {mangaDto?.Id} trên trang chủ.");
                    }
                }
                
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_MangaGridPartial", viewModels);
                }

                return View("Index", viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang chủ.");
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View("Index", new List<MangaViewModel>());
            }
        }

        public async Task<IActionResult> GetLatestMangaPartial()
        {
            try
            {
                 var recentMangaResponse = await _mangaClient.GetMangasAsync(
                    limit: 10,
                    orderBy: "updatedAt",
                    ascending: false,
                    includes: new List<string> { "cover_art", "author" }
                );

                var viewModels = new List<MangaViewModel>();
                if (recentMangaResponse?.Data != null)
                {
                    foreach (var mangaDto in recentMangaResponse.Data)
                    {
                        try
                        {
                            viewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaDto));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi map manga ID: {mangaDto?.Id} trong partial.");
                        }
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
            return View(model);
        }
    }
}
