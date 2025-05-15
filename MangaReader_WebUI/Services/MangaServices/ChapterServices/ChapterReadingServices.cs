using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterReadingServices
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly MangaIdService _mangaIdService;
        private readonly ChapterLanguageServices _chapterLanguageServices;
        private readonly ChapterService _chapterService;
        private readonly ILogger<ChapterReadingServices> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _backendBaseUrl; // Lấy base URL của backend
        private readonly IMangaDataExtractor _mangaDataExtractor;
        private readonly IMangaApiService _mangaApiService;

        public ChapterReadingServices(
            IChapterApiService chapterApiService,
            MangaIdService mangaIdService,
            ChapterLanguageServices chapterLanguageServices,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<ChapterReadingServices> logger,
            IMangaDataExtractor mangaDataExtractor,
            IMangaApiService mangaApiService)
        {
            _chapterApiService = chapterApiService;
            _mangaIdService = mangaIdService;
            _chapterLanguageServices = chapterLanguageServices;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
            _mangaDataExtractor = mangaDataExtractor;
            _mangaApiService = mangaApiService;
        }

        public async Task<ChapterReadViewModel> GetChapterReadViewModel(string chapterId)
        {
            try
            {
                _logger.LogInformation($"Đang tải chapter {chapterId}");

                // 1. Tải thông tin server ảnh và tên file ảnh
                var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
                if (atHomeResponse == null || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null)
                {
                    _logger.LogError($"Không thể lấy thông tin trang ảnh cho chapter {chapterId}");
                    throw new Exception("Không thể tải trang ảnh cho chapter này.");
                }

                // 2. Mapping: Tạo danh sách URL ảnh đầy đủ qua proxy
                var pages = atHomeResponse.Chapter.Data
                    .Select(pageFile => $"{_backendBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"{atHomeResponse.BaseUrl}/data/{atHomeResponse.Chapter.Hash}/{pageFile}")}")
                    .ToList();
                _logger.LogInformation($"Đã tạo {pages.Count} URL ảnh cho chapter {chapterId}");

                // 3. Lấy mangaId
                string mangaId = await _mangaIdService.GetMangaIdFromChapterAsync(chapterId);
                _logger.LogInformation($"Đã xác định được mangaId: {mangaId} cho chapter {chapterId}");

                // 4. Lấy ngôn ngữ chapter hiện tại
                string currentChapterLanguage = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);
                _logger.LogInformation($"Đã lấy được ngôn ngữ {currentChapterLanguage} từ API");

                // 5. Lấy tiêu đề manga
                string mangaTitle = await GetMangaTitleAsync(mangaId);

                // 6. Lấy danh sách chapters
                var chaptersList = await GetChaptersAsync(mangaId, currentChapterLanguage);

                // 7. Tìm chapter hiện tại và các chapter liền kề
                var (currentChapterViewModel, prevChapterId, nextChapterId) =
                    FindCurrentAndAdjacentChapters(chaptersList, chapterId, currentChapterLanguage);

                // Lấy thông tin chapter hiện tại để trích xuất title và number
                ChapterAttributes currentChapterAttributes = null;
                var currentChapterDataResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);
                if (currentChapterDataResponse?.Data?.Attributes != null)
                {
                    currentChapterAttributes = currentChapterDataResponse.Data.Attributes;
                }
                
                string displayChapterTitle = "Không xác định";
                string displayChapterNumber = "?";

                if (currentChapterAttributes != null)
                {
                    displayChapterTitle = _mangaDataExtractor.ExtractChapterDisplayTitle(currentChapterAttributes);
                    displayChapterNumber = _mangaDataExtractor.ExtractChapterNumber(currentChapterAttributes);
                } 
                else if (currentChapterViewModel != null) // Fallback nếu API lỗi, dùng từ Sibling
                {
                    displayChapterTitle = currentChapterViewModel.Title;
                    displayChapterNumber = currentChapterViewModel.Number;
                }

                // 8. Tạo view model
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = chapterId,
                    ChapterTitle = displayChapterTitle,
                    ChapterNumber = displayChapterNumber,
                    ChapterLanguage = currentChapterLanguage,
                    Pages = pages, // Danh sách URL đã xử lý
                    PrevChapterId = prevChapterId,
                    NextChapterId = nextChapterId,
                    SiblingChapters = chaptersList
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải chapter {chapterId}");
                throw;
            }
        }
        
        public async Task<string> GetMangaTitleAsync(string mangaId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string sessionTitle = httpContext?.Session.GetString($"Manga_{mangaId}_Title");
            if (!string.IsNullOrEmpty(sessionTitle))
            {
                _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId} từ session: {sessionTitle}");
                return sessionTitle;
            }
            
            // Lấy từ API và dùng extractor
            var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);
            if (mangaResponse?.Data?.Attributes != null)
            {
                string title = _mangaDataExtractor.ExtractMangaTitle(mangaResponse.Data.Attributes.Title, mangaResponse.Data.Attributes.AltTitles);
                 if (httpContext != null && !string.IsNullOrEmpty(title) && title != "Không có tiêu đề")
                {
                    httpContext.Session.SetString($"Manga_{mangaId}_Title", title);
                    _logger.LogInformation($"Đã lưu tiêu đề manga {mangaId} vào session: {title}");
                }
                return title;
            }
            return "Không có tiêu đề";
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string language)
        {
             var httpContext = _httpContextAccessor.HttpContext;
             var sessionChaptersJson = httpContext?.Session.GetString($"Manga_{mangaId}_Chapters_{language}");

             if (!string.IsNullOrEmpty(sessionChaptersJson))
             {
                 try
                 {
                     var chaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(sessionChaptersJson);
                     if (chaptersList != null && chaptersList.Any())
                     {
                         _logger.LogInformation($"Đã lấy {chaptersList.Count} chapters ngôn ngữ {language} từ session");
                         return chaptersList;
                     }
                 }
                 catch (JsonException ex)
                 {
                      _logger.LogWarning(ex, $"Lỗi deserialize chapters từ session cho manga {mangaId}, ngôn ngữ {language}. Sẽ lấy lại từ API.");
                 }
             }
             return await GetChaptersFromApiAsync(mangaId, language);
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersFromApiAsync(string mangaId, string language)
        {
            _logger.LogInformation($"Tiến hành lấy danh sách chapters cho manga {mangaId} với ngôn ngữ {language}");
            var allChapters = await _chapterService.GetChaptersAsync(mangaId, language);
            _logger.LogInformation($"Đã lấy {allChapters.Count} chapters từ API");

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                 // Lưu danh sách tất cả chapters vào session (nếu cần)
                 // httpContext.Session.SetString($"Manga_{mangaId}_AllChapters", JsonSerializer.Serialize(allChapters));

                 // Phân loại và lưu theo ngôn ngữ
                 var chaptersByLanguage = _chapterService.GetChaptersByLanguage(allChapters);
                 if (chaptersByLanguage.TryGetValue(language, out var chaptersInLanguage))
                 {
                     httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                     _logger.LogInformation($"Đã lưu {chaptersInLanguage.Count} chapters ngôn ngữ {language} vào session");
                     return chaptersInLanguage;
                 }
            }
            return new List<ChapterViewModel>();
        }
        
        private (ChapterViewModel currentChapter, string prevId, string nextId) FindCurrentAndAdjacentChapters(
            List<ChapterViewModel> chapters, string chapterId, string language)
        {
            _logger.LogInformation($"Xác định chapter hiện tại và các chapter liền kề trong danh sách {chapters.Count} chapters");

            var currentChapter = chapters.FirstOrDefault(c => c.Id == chapterId);

            if (currentChapter == null)
            {
                _logger.LogWarning($"Không tìm thấy chapter {chapterId} trong danh sách chapters ngôn ngữ {language}");
                // Trả về ViewModel mặc định nếu không tìm thấy chapter hiện tại
                return (new ChapterViewModel { Id = chapterId, Title = "Chương không xác định", Number = "?", Language = language }, null, null);
            }

            // Sắp xếp danh sách chapters theo số chương tăng dần để xác định chương trước/sau
            // Cần xử lý trường hợp chapterNumber là null hoặc không phải số
            var sortedChapters = chapters
                .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                .OrderBy(c => c.Number ?? double.MaxValue) // Đẩy null/không phải số về cuối
                .Select(c => c.Chapter)
                .ToList();


            int index = sortedChapters.FindIndex(c => c.Id == chapterId);

            string prevId = (index > 0) ? sortedChapters[index - 1].Id : null;
            string nextId = (index >= 0 && index < sortedChapters.Count - 1) ? sortedChapters[index + 1].Id : null;

            _logger.LogInformation($"Chapter hiện tại: {currentChapter.Title}, Chapter trước: {(prevId != null ? "có" : "không có")}, Chapter sau: {(nextId != null ? "có" : "không có")}");

            return (currentChapter, prevId, nextId);
        }

        // Helper để parse số chapter, trả về null nếu không parse được
        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
