using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
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

        [JsonPropertyName("chapterId")]
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
        private readonly TimeSpan _rateLimitDelay;
        private readonly ILastReadMangaViewModelMapper _lastReadMapper;
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly IMangaReaderLibToChapterInfoMapper _chapterInfoMapper;
        private readonly IMangaReaderLibTranslatedMangaClient _translatedMangaClient;

        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IConfiguration configuration,
            ILogger<ReadingHistoryService> logger,
            ILastReadMangaViewModelMapper lastReadMapper,
            IMangaReaderLibChapterClient chapterClient,
            IMangaReaderLibToChapterInfoMapper chapterInfoMapper,
            IMangaReaderLibTranslatedMangaClient translatedMangaClient)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _configuration = configuration;
            _logger = logger;
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 250));
            _lastReadMapper = lastReadMapper;
            _chapterClient = chapterClient;
            _chapterInfoMapper = chapterInfoMapper;
            _translatedMangaClient = translatedMangaClient;
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
                    _logger.LogError("Lỗi khi gọi API backend lấy lịch sử đọc. Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                    }
                    return historyViewModels;
                }

                var content = await response.Content.ReadAsStringAsync();
                var backendHistory = JsonSerializer.Deserialize<List<BackendHistoryItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (backendHistory == null || !backendHistory.Any())
                {
                    _logger.LogInformation("Không có lịch sử đọc nào từ backend.");
                    return historyViewModels;
                }

                _logger.LogInformation("Nhận được {Count} mục lịch sử từ backend. Bắt đầu lấy chi tiết...", backendHistory.Count);

                foreach (var item in backendHistory)
                {
                    await Task.Delay(_rateLimitDelay);

                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning("Không thể lấy thông tin cho MangaId: {MangaId} trong lịch sử đọc. Bỏ qua mục này.", item.MangaId);
                        continue;
                    }

                    ChapterInfoViewModel? chapterInfoViewModel = null;
                    try
                    {
                        if (!Guid.TryParse(item.ChapterId, out var chapterGuid))
                        {
                            _logger.LogWarning("ChapterId không hợp lệ: {ChapterId}. Bỏ qua.", item.ChapterId);
                            continue;
                        }
                        
                        var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);
                        if (chapterResponse?.Data == null)
                        {
                            _logger.LogWarning("Không tìm thấy chapter với ID: {ChapterId} trong lịch sử đọc hoặc API lỗi. Bỏ qua mục này.", item.ChapterId);
                            continue;
                        }

                        var tmRel = chapterResponse.Data.Relationships?.FirstOrDefault(r => r.Type == "translated_manga");
                        string langKey = "en"; // Mặc định
                        if (tmRel != null && Guid.TryParse(tmRel.Id, out var tmGuid))
                        {
                            var tmResponse = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                            if (!string.IsNullOrEmpty(tmResponse?.Data?.Attributes?.LanguageKey))
                            {
                                langKey = tmResponse.Data.Attributes.LanguageKey;
                            }
                        }

                        chapterInfoViewModel = _chapterInfoMapper.MapToChapterInfo(chapterResponse.Data, langKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lấy thông tin chapter {ChapterId} trong lịch sử đọc. Bỏ qua mục này.", item.ChapterId);
                        continue;
                    }

                    if (chapterInfoViewModel == null)
                    {
                        _logger.LogWarning("Thông tin Chapter cho ChapterId: {ChapterId} vẫn null sau khi thử lấy. Bỏ qua mục lịch sử này.", item.ChapterId);
                        continue;
                    }

                    var historyViewModel = _lastReadMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfoViewModel, item.LastReadAt);
                    historyViewModels.Add(historyViewModel);

                    _logger.LogDebug("Đã xử lý xong mục lịch sử cho manga: {MangaTitle}, chapter: {ChapterTitle}", mangaInfo.MangaTitle, chapterInfoViewModel.Title);
                }

                _logger.LogInformation("Hoàn tất xử lý {Count} mục lịch sử đọc.", historyViewModels.Count);
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