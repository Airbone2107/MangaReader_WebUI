using MangaReader.WebUI.Models;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.APIServices;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;

namespace MangaReader.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IApiStatusService _apiStatusService;
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IApiStatusService apiStatusService, 
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<HomeController> logger)
        {
            _apiStatusService = apiStatusService;
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Thiết lập page type để chọn CSS phù hợp
                ViewData["PageType"] = "home";
                
                // Kiểm tra kết nối API trước
                bool isConnected = await _apiStatusService.TestConnectionAsync();
                if (!isConnected)
                {
                    _logger.LogWarning("Không thể kết nối đến API MangaDex");
                    ViewBag.ErrorMessage = "Không thể kết nối đến API. Vui lòng thử lại sau.";
                    ViewBag.IsConnected = false;
                    return View("Index", new List<MangaViewModel>());
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
                    var recentManga = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);

                    // Nếu không có dữ liệu
                    if (recentManga == null || recentManga.Count == 0)
                    {
                        _logger.LogWarning("API đã kết nối nhưng không trả về dữ liệu manga");
                        ViewBag.ErrorMessage = "Không có dữ liệu manga. Vui lòng thử lại sau.";
                        return View("Index", new List<MangaViewModel>());
                    }

                    // Chuyển đổi thành MangaViewModel
                    var viewModels = new List<MangaViewModel>();
                    
                    // Bỏ qua phần tử đầu tiên vì nó chỉ chứa thông tin tổng số (metadata)
                    // không phải đối tượng manga thông thường nên không có thuộc tính id
                    var mangaListToProcess = recentManga.Count > 1 ? recentManga.Skip(1).ToList() : new List<dynamic>();
                    
                    foreach (var manga in mangaListToProcess)
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
                            string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);

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
                        return PartialView("Index", viewModels);
                    }

                    return View("Index", viewModels);
                }
                catch (Exception apiEx)
                {
                    _logger.LogError($"Lỗi khi gọi API MangaDex: {apiEx.Message}");
                    ViewBag.ErrorMessage = $"Lỗi khi tải dữ liệu từ MangaDex: {apiEx.Message}";
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
                ViewBag.StackTrace = ex.StackTrace; // Thêm stack trace để debug
                return View("Index", new List<MangaViewModel>());
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
                testResults.Add("API Connection", await _apiStatusService.TestConnectionAsync() ? "Success" : "Failed");
                
                // Kiểm tra lấy manga với limit = 1
                try
                {
                    var manga = await _mangaApiService.FetchMangaAsync(1, 0);
                    testResults.Add("Fetch Manga", $"Success - Found {manga?.Count ?? 0} items");
                }
                catch (Exception ex)
                {
                    testResults.Add("Fetch Manga", $"Failed - {ex.Message}");
                }
                
                // Nếu là HTMX request, chỉ trả về nội dung một phần
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
            // Thiết lập page type để chọn CSS phù hợp
            ViewData["PageType"] = "home";
            
            // Nếu là HTMX request, chỉ trả về nội dung một phần
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("Privacy");
            }
            
            return View("Privacy");
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
                return PartialView("Error", model);
            }
            
            return View("Error", model);
        }
        
        // Hàm utility để xử lý dữ liệu
        private string GetLocalizedTitle(string titleJson)
        {
            try
            {
                var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                
                // Ưu tiên trả về tiêu đề tiếng Việt nếu có
                if (titles.TryGetValue("vi", out var viTitle) && !string.IsNullOrEmpty(viTitle))
                {
                    return viTitle;
                }
                
                // Sau đó là tiếng Anh
                if (titles.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
                {
                    return enTitle;
                }
                
                // Cuối cùng là ngôn ngữ gốc (key đầu tiên trong từ điển)
                return titles.Values.FirstOrDefault(t => !string.IsNullOrEmpty(t));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi parse tiêu đề manga: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Phương thức lấy danh sách manga mới nhất và trả về dạng partial view
        /// </summary>
        public async Task<IActionResult> GetLatestMangaPartial()
        {
            try
            {
                // Lấy danh sách manga mới nhất
                var sortOptions = new SortManga { 
                    SortBy = "Mới cập nhật",
                    Languages = new List<string> { "vi", "en" }
                };
                
                var recentManga = await _mangaApiService.FetchMangaAsync(10, 0, sortOptions);
                
                // Chuyển đổi thành MangaViewModel
                var viewModels = new List<MangaViewModel>();
                
                // Bỏ qua phần tử đầu tiên vì nó chỉ chứa thông tin tổng số
                var mangaListToProcess = recentManga.Count > 1 ? recentManga.Skip(1).ToList() : new List<dynamic>();
                
                foreach (var manga in mangaListToProcess)
                {
                    try
                    {
                        var mangaObj = JsonSerializer.Deserialize<ExpandoObject>(manga.ToString());
                        var mangaDict = (IDictionary<string, object>)mangaObj;
                        
                        var id = mangaDict["id"].ToString();
                        var attributes = JsonSerializer.Deserialize<ExpandoObject>(mangaDict["attributes"].ToString());
                        var attributesDict = (IDictionary<string, object>)attributes;
                        
                        var title = GetLocalizedTitle(attributesDict["title"].ToString()) ?? "Không có tiêu đề";
                        
                        // Tải ảnh bìa
                        string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);

                        viewModels.Add(new MangaViewModel
                        {
                            Id = id,
                            Title = title,
                            CoverUrl = coverUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý manga trong partial: {ex.Message}");
                    }
                }
                
                return PartialView("_MangaListPartial", viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải danh sách manga mới nhất: {ex.Message}");
                return PartialView("_ErrorPartial", "Không thể tải danh sách manga mới nhất.");
            }
        }
    }
}
