using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Services.MangaServices
{
    // Model để deserialize response từ backend /reading-history
    public class BackendHistoryItem
    {
        [JsonPropertyName("mangaId")]
        public string MangaId { get; set; }

        [JsonPropertyName("chapterId")] // Đảm bảo khớp với key JSON từ backend
        public string ChapterId { get; set; }

        [JsonPropertyName("lastReadAt")]
        public DateTime LastReadAt { get; set; }
    }

    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReadingHistoryService> _logger;
        private readonly TimeSpan _rateLimitDelay; // Delay giữa các nhóm API call
        private readonly ILastReadMangaViewModelMapper _lastReadMapper;
        private readonly IChapterApiService _chapterApiService;
        private readonly IChapterToChapterViewModelMapper _chapterViewModelMapper; // Để map sang ChapterViewModel rồi lấy Title

        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IConfiguration configuration,
            ILogger<ReadingHistoryService> logger,
            ILastReadMangaViewModelMapper lastReadMapper,
            IChapterApiService chapterApiService,
            IChapterToChapterViewModelMapper chapterViewModelMapper)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _configuration = configuration;
            _logger = logger;
            // Lấy giá trị delay từ config hoặc đặt mặc định (vd: 550ms)
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 250));
            _lastReadMapper = lastReadMapper;
            _chapterApiService = chapterApiService;
            _chapterViewModelMapper = chapterViewModelMapper;
        }

        public async Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync()
        {
            var historyViewModels = new List<LastReadMangaViewModel>();

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy lịch sử đọc.");
                return historyViewModels;
            }

            var token = _userService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Không thể lấy token người dùng đã đăng nhập.");
                return historyViewModels;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Đang gọi API backend /api/users/reading-history");
                var response = await client.GetAsync("/api/users/reading-history");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi khi gọi API backend lấy lịch sử đọc. Status: {response.StatusCode}, Content: {errorContent}");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken(); // Xóa token nếu backend báo Unauthorized
                    }
                    return historyViewModels; // Trả về rỗng nếu lỗi
                }

                var content = await response.Content.ReadAsStringAsync();
                var backendHistory = JsonSerializer.Deserialize<List<BackendHistoryItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (backendHistory == null || !backendHistory.Any())
                {
                    _logger.LogInformation("Không có lịch sử đọc nào từ backend.");
                    return historyViewModels;
                }

                _logger.LogInformation($"Nhận được {backendHistory.Count} mục lịch sử từ backend. Bắt đầu lấy chi tiết...");

                foreach (var item in backendHistory)
                {
                    await Task.Delay(_rateLimitDelay);

                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning($"Không thể lấy thông tin cho MangaId: {item.MangaId} trong lịch sử đọc. Bỏ qua mục này.");
                        continue; 
                    }

                    ChapterInfoViewModel chapterInfoViewModel = null!;
                    try 
                    {
                        var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(item.ChapterId);
                        if (chapterResponse?.Result != "ok" || chapterResponse.Data == null)
                        {
                            _logger.LogWarning($"Không tìm thấy chapter với ID: {item.ChapterId} trong lịch sử đọc hoặc API lỗi. Bỏ qua mục này.");
                            continue; 
                        }
                        
                        var mappedChapter = _chapterViewModelMapper.MapToChapterViewModel(chapterResponse.Data);
                        chapterInfoViewModel = new ChapterInfoViewModel
                        {
                            Id = item.ChapterId,
                            Title = mappedChapter.Title, 
                            PublishedAt = mappedChapter.PublishedAt
                        };
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, $"Lỗi khi lấy thông tin chapter {item.ChapterId} trong lịch sử đọc. Bỏ qua mục này.");
                        continue; 
                    }
                    
                    if (chapterInfoViewModel == null) 
                    {
                        _logger.LogWarning($"Thông tin Chapter cho ChapterId: {item.ChapterId} vẫn null sau khi thử lấy. Bỏ qua mục lịch sử này.");
                        continue; 
                    }

                    var historyViewModel = _lastReadMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfoViewModel, item.LastReadAt);
                    historyViewModels.Add(historyViewModel);
                    
                    _logger.LogDebug($"Đã xử lý xong mục lịch sử cho manga: {mangaInfo.MangaTitle}, chapter: {chapterInfoViewModel.Title}");
                }

                _logger.LogInformation($"Hoàn tất xử lý {historyViewModels.Count} mục lịch sử đọc.");
                return historyViewModels;

            }
            catch (JsonException jsonEx)
            {
                 _logger.LogError(jsonEx, "Lỗi khi deserialize lịch sử đọc từ backend.");
                 return historyViewModels; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ngoại lệ khi lấy và xử lý lịch sử đọc.");
                return historyViewModels; 
            }
        }
    }
} 