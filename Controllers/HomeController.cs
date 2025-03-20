using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using manga_reader_web.Models;
using manga_reader_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace manga_reader_web.Controllers
{
    public class HomeController : Controller
    {
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(MangaDexService mangaDexService, ILogger<HomeController> logger)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp
                ViewData["PageType"] = "home";
                
                // Kiểm tra kết nối API trước
                bool isConnected = await _mangaDexService.TestConnectionAsync();
                if (!isConnected)
                {
                    _logger.LogWarning("Không thể kết nối đến API MangaDex");
                    ViewBag.ErrorMessage = "Không thể kết nối đến API. Vui lòng thử lại sau.";
                    ViewBag.IsConnected = false;
                    return View(new List<MangaViewModel>());
                }
                
                ViewBag.IsConnected = true;
                
                // Lấy danh sách manga mới nhất
                try
                {
                    var sortOptions = new SortManga { 
                        SortBy = "Mới cập nhật",
                        Languages = new List<string> { "vi", "en" }
                    };
                    
                    Console.WriteLine("Đang lấy danh sách manga mới nhất...");
                    var recentManga = await _mangaDexService.FetchMangaAsync(10, 0, sortOptions);

                    // Nếu không có dữ liệu
                    if (recentManga == null || recentManga.Count == 0)
                    {
                        _logger.LogWarning("API đã kết nối nhưng không trả về dữ liệu manga");
                        ViewBag.ErrorMessage = "Không có dữ liệu manga. Vui lòng thử lại sau.";
                        return View(new List<MangaViewModel>());
                    }

                    // Chuyển đổi thành MangaViewModel
                    var viewModels = new List<MangaViewModel>();
                    foreach (var manga in recentManga)
                    {
                        try
                        {
                            // Parse dynamic object
                            var mangaObj = JsonSerializer.Deserialize<ExpandoObject>(manga.ToString());
                            var mangaDict = (IDictionary<string, object>)mangaObj;
                            
                            var id = mangaDict["id"].ToString();
                            var attributes = JsonSerializer.Deserialize<ExpandoObject>(mangaDict["attributes"].ToString());
                            var attributesDict = (IDictionary<string, object>)attributes;
                            
                            var title = GetLocalizedTitle(attributesDict["title"].ToString()) ?? "Không có tiêu đề";
                            
                            // Tải ảnh bìa
                            string coverUrl = await _mangaDexService.FetchCoverUrlAsync(id);

                            viewModels.Add(new MangaViewModel
                            {
                                Id = id,
                                Title = title,
                                CoverUrl = coverUrl
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Lỗi khi xử lý manga: {ex.Message}");
                            // Ghi log nhưng vẫn tiếp tục với manga tiếp theo
                        }
                    }

                    // Nếu không thể xử lý dữ liệu từ bất kỳ manga nào
                    if (viewModels.Count == 0)
                    {
                        ViewBag.ErrorMessage = "Không thể hiển thị dữ liệu manga. Định dạng dữ liệu không hợp lệ.";
                    }

                    // Nếu là HTMX request, chỉ trả về nội dung một phần
                    if (Request.Headers.ContainsKey("HX-Request"))
                    {
                        return PartialView(viewModels);
                    }

                    return View(viewModels);
                }
                catch (Exception apiEx)
                {
                    _logger.LogError($"Lỗi khi gọi API MangaDex: {apiEx.Message}");
                    ViewBag.ErrorMessage = $"Lỗi khi tải dữ liệu từ MangaDex: {apiEx.Message}";
                    ViewBag.StackTrace = apiEx.StackTrace;
                    return View(new List<MangaViewModel>());
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
                ViewBag.StackTrace = ex.StackTrace; // Thêm stack trace để debug
                return View(new List<MangaViewModel>());
            }
        }

        // Trang test cho việc debug
        public async Task<IActionResult> ApiTest()
        {
            // Thiết lập page type để chọn CSS phù hợp
            ViewData["PageType"] = "home";
            
            var testResults = new Dictionary<string, string>();
            
            try
            {
                // Kiểm tra kết nối chung
                testResults.Add("API Connection", await _mangaDexService.TestConnectionAsync() ? "Success" : "Failed");
                
                // Kiểm tra lấy manga với limit = 1
                try
                {
                    var manga = await _mangaDexService.FetchMangaAsync(1, 0);
                    testResults.Add("Fetch Manga", $"Success - Found {manga?.Count ?? 0} items");
                }
                catch (Exception ex)
                {
                    testResults.Add("Fetch Manga", $"Failed - {ex.Message}");
                }
                
                // Nếu là HTMX request, chỉ trả về nội dung một phần
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView(testResults);
                }
                
                return View(testResults);
            }
            catch (Exception ex)
            {
                testResults.Add("General Error", ex.Message);
                return View(testResults);
            }
        }

        public IActionResult Privacy()
        {
            // Thiết lập page type để chọn CSS phù hợp
            ViewData["PageType"] = "home";
            
            // Nếu là HTMX request, chỉ trả về nội dung một phần
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView();
            }
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Thiết lập page type để chọn CSS phù hợp
            ViewData["PageType"] = "home";
            
            var model = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            
            // Nếu là HTMX request, chỉ trả về nội dung một phần
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView(model);
            }
            
            return View(model);
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
