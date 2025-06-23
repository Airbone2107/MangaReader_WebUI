using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterReadingServices
    {
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibChapterPageClient _chapterPageClient;
        private readonly IMangaReaderLibToPageServerResponseMapper _atHomeResponseMapper;
        private readonly ChapterLanguageServices _chapterLanguageServices;
        private readonly ChapterService _chapterService;
        private readonly ILogger<ChapterReadingServices> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChapterReadingServices(
            IMangaReaderLibChapterClient chapterClient,
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibChapterPageClient chapterPageClient,
            IMangaReaderLibToPageServerResponseMapper atHomeResponseMapper,
            ChapterLanguageServices chapterLanguageServices,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ChapterReadingServices> logger)
        {
            _chapterClient = chapterClient;
            _mangaClient = mangaClient;
            _chapterPageClient = chapterPageClient;
            _atHomeResponseMapper = atHomeResponseMapper;
            _chapterLanguageServices = chapterLanguageServices;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ChapterReadViewModel> GetChapterReadViewModel(string chapterId)
        {
            try
            {
                if (!Guid.TryParse(chapterId, out var chapterGuid))
                {
                    throw new ArgumentException("ChapterId không hợp lệ.", nameof(chapterId));
                }

                _logger.LogInformation("Đang tải chapter {ChapterId}", chapterId);
                
                var chapterDetailsResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);
                if (chapterDetailsResponse?.Data?.Relationships == null)
                {
                    _logger.LogError("Không thể lấy chi tiết chapter {ChapterId}.", chapterId);
                    throw new Exception("Không thể tải chi tiết chương này.");
                }

                var mangaRelationship = chapterDetailsResponse.Data.Relationships
                                            .FirstOrDefault(r => r.Type.Equals("manga", StringComparison.OrdinalIgnoreCase));
                if (mangaRelationship == null)
                {
                     throw new KeyNotFoundException($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                }
                string mangaId = mangaRelationship.Id;

                var pagesResponse = await _chapterPageClient.GetChapterPagesAsync(chapterGuid, limit: 500);
                if (pagesResponse?.Data == null)
                {
                    _logger.LogError("Không thể lấy thông tin trang ảnh cho chapter {ChapterId}.", chapterId);
                    throw new Exception("Không thể tải trang ảnh cho chương này.");
                }

                var pageServerResponse = _atHomeResponseMapper.MapToAtHomeServerResponse(pagesResponse, chapterId, "");
                List<string> pages = pageServerResponse.Chapter.Data;
                
                _logger.LogInformation("Đã tạo {Count} URL ảnh cho chapter {ChapterId}", pages.Count, chapterId);

                string currentChapterLanguage = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);
                _logger.LogInformation("Đã lấy được ngôn ngữ {Language} cho chapter", currentChapterLanguage);

                string mangaTitle = await GetMangaTitleAsync(mangaId);
                var chaptersList = await GetChaptersAsync(mangaId, currentChapterLanguage);

                var (currentChapterViewModel, prevChapterId, nextChapterId) = FindCurrentAndAdjacentChapters(chaptersList, chapterId);

                string displayChapterTitle = currentChapterViewModel.Title;
                string displayChapterNumber = currentChapterViewModel.Number;
                
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = chapterId,
                    ChapterTitle = displayChapterTitle,
                    ChapterNumber = displayChapterNumber,
                    ChapterLanguage = currentChapterLanguage,
                    Pages = pages,
                    PrevChapterId = prevChapterId,
                    NextChapterId = nextChapterId,
                    SiblingChapters = chaptersList
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chapter {ChapterId}", chapterId);
                throw;
            }
        }
        
        public async Task<string> GetMangaTitleAsync(string mangaId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string sessionTitle = httpContext?.Session.GetString($"Manga_{mangaId}_Title");
            if (!string.IsNullOrEmpty(sessionTitle))
            {
                return sessionTitle;
            }
            
            if (!Guid.TryParse(mangaId, out var mangaGuid)) return "Không có tiêu đề";

            var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid);
            string title = mangaResponse?.Data?.Attributes?.Title ?? "Không có tiêu đề";

            if (httpContext != null && !string.IsNullOrEmpty(title) && title != "Không có tiêu đề")
            {
                httpContext.Session.SetString($"Manga_{mangaId}_Title", title);
            }
            return title;
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
                     if (chaptersList != null && chaptersList.Any()) return chaptersList;
                 }
                 catch (JsonException ex)
                 {
                      _logger.LogWarning(ex, "Lỗi deserialize chapters từ session.");
                 }
             }
             return await GetChaptersFromApiAsync(mangaId, language);
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersFromApiAsync(string mangaId, string language)
        {
            var allChapters = await _chapterService.GetChaptersAsync(mangaId, language);
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                 var chaptersByLanguage = _chapterService.GetChaptersByLanguage(allChapters);
                 if (chaptersByLanguage.TryGetValue(language, out var chaptersInLanguage))
                 {
                     httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                     return chaptersInLanguage;
                 }
            }
            return new List<ChapterViewModel>();
        }
        
        private (ChapterViewModel currentChapter, string? prevId, string? nextId) FindCurrentAndAdjacentChapters(
            List<ChapterViewModel> chapters, string chapterId)
        {
            var currentChapter = chapters.FirstOrDefault(c => c.Id == chapterId);
            if (currentChapter == null)
            {
                return (new ChapterViewModel { Id = chapterId, Title = "Chương không xác định" }, null, null);
            }
            
            var sortedChapters = chapters
                .OrderBy(c => ParseChapterNumber(c.Number) ?? double.MaxValue)
                .ThenBy(c => c.PublishedAt)
                .ToList();

            int index = sortedChapters.FindIndex(c => c.Id == chapterId);
            string? prevId = (index > 0) ? sortedChapters[index - 1].Id : null;
            string? nextId = (index >= 0 && index < sortedChapters.Count - 1) ? sortedChapters[index + 1].Id : null;

            return (currentChapter, prevId, nextId);
        }

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
