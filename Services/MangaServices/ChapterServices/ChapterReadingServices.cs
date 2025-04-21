using MangaReader.WebUI.Models;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterReadingServices
    {
        private readonly MangaDexService _mangaDexService;
        private readonly MangaIdService _mangaIdService;
        private readonly ChapterLanguageServices _chapterLanguageServices;
        private readonly MangaTitleService _mangaTitleService;
        private readonly ChapterService _chapterService;
        private readonly ILogger<ChapterReadingServices> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChapterReadingServices(
            MangaDexService mangaDexService,
            MangaIdService mangaIdService,
            ChapterLanguageServices chapterLanguageServices,
            MangaTitleService mangaTitleService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ChapterReadingServices> logger)
        {
            _mangaDexService = mangaDexService;
            _mangaIdService = mangaIdService;
            _chapterLanguageServices = chapterLanguageServices;
            _mangaTitleService = mangaTitleService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ChapterReadViewModel> GetChapterReadViewModel(string id)
        {
            try
            {
                _logger.LogInformation($"Đang tải chapter {id}");
                
                // Tải các trang của chapter
                var pages = await _mangaDexService.FetchChapterPagesAsync(id);
                
                // Lấy mangaId từ API
                _logger.LogInformation($"Đang xác định mangaId cho chapter {id}");
                string mangaId;
                
                try 
                {
                    mangaId = await _mangaIdService.GetMangaIdFromChapterAsync(id);
                    _logger.LogInformation($"Đã xác định được mangaId: {mangaId} cho chapter {id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Không thể xác định mangaId cho chapter {id}: {ex.Message}");
                    throw new Exception("Không thể xác định manga cho chapter này.", ex);
                }

                // Tiến hành xử lý thông tin chapter
                string currentChapterLanguage = "unknown";
                List<ChapterViewModel> chaptersList = new List<ChapterViewModel>();
                ChapterViewModel currentChapterViewModel = null;
                string prevChapterId = null;
                string nextChapterId = null;
                string mangaTitle = null;
                
                // Kiểm tra xem có dữ liệu trong session không
                var httpContext = _httpContextAccessor.HttpContext;
                var allSessionChaptersJson = httpContext.Session.GetString($"Manga_{mangaId}_AllChapters");
                var sessionTitle = httpContext.Session.GetString($"Manga_{mangaId}_Title");
                bool hasDataInSession = !string.IsNullOrEmpty(allSessionChaptersJson) && !string.IsNullOrEmpty(sessionTitle);
                
                if (hasDataInSession)
                {
                    _logger.LogInformation("Tìm thấy dữ liệu chapters và tiêu đề trong session, tiến hành xử lý...");
                    
                    // Gán trực tiếp tiêu đề từ session
                    mangaTitle = sessionTitle;
                    _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId} từ session: {mangaTitle}");
                    
                    // TRƯỜNG HỢP 1: CÓ DỮ LIỆU TRONG SESSION
                    try
                    {
                        // Xác định ngôn ngữ của chapter hiện tại từ session
                        var allChaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(allSessionChaptersJson);
                        var currentChapter = allChaptersList.FirstOrDefault(c => c.Id == id);
                        
                        if (currentChapter != null)
                        {
                            currentChapterLanguage = currentChapter.Language;
                            _logger.LogInformation($"Đã xác định ngôn ngữ của chapter hiện tại từ session: {currentChapterLanguage}");
                        }
                        else
                        {
                            _logger.LogWarning($"Không tìm thấy chapter {id} trong session, sẽ cần lấy thông tin từ API");
                            // Nếu không tìm thấy chapter trong session, chuyển sang xử lý như không có session
                            hasDataInSession = false;
                        }
                        
                        if (hasDataInSession)
                        {
                            // Lấy danh sách chapters theo ngôn ngữ từ session
                            var sessionChaptersJson = httpContext.Session.GetString($"Manga_{mangaId}_Chapters_{currentChapterLanguage}");
                            
                            if (!string.IsNullOrEmpty(sessionChaptersJson))
                            {
                                chaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(sessionChaptersJson);
                                _logger.LogInformation($"Đã lấy {chaptersList.Count} chapters từ session");
                                
                                // Tìm chapter hiện tại và xác định chapter trước/sau
                                (currentChapterViewModel, prevChapterId, nextChapterId) = 
                                    FindCurrentAndAdjacentChapters(chaptersList, id, currentChapterLanguage);
                            }
                            else
                            {
                                _logger.LogWarning($"Không tìm thấy danh sách chapters ngôn ngữ {currentChapterLanguage} trong session, chuyển sang lấy từ API");
                                hasDataInSession = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý dữ liệu từ session: {ex.Message}");
                        hasDataInSession = false;
                    }
                }
                
                // TRƯỜNG HỢP 2: KHÔNG CÓ DỮ LIỆU TRONG SESSION HOẶC XỬ LÝ SESSION BỊ LỖI
                if (!hasDataInSession)
                {
                    _logger.LogInformation("Không có đủ dữ liệu hợp lệ trong session, tiến hành lấy dữ liệu từ API...");
                    
                    // Xác định ngôn ngữ của chapter từ API
                    currentChapterLanguage = await GetChapterLanguageFromApiAsync(id);
                    _logger.LogInformation($"Đã lấy được ngôn ngữ {currentChapterLanguage} từ API");
                    
                    // Lấy danh sách chapters từ API và xử lý
                    chaptersList = await GetChaptersFromApiAsync(mangaId, currentChapterLanguage);
                    
                    // Lấy tiêu đề manga từ API
                    mangaTitle = await GetMangaTitleFromApiAsync(mangaId);
                    
                    // Lưu danh sách chapters vào session để sử dụng sau này
                    if (chaptersList.Count > 0)
                    {
                        httpContext.Session.SetString(
                            $"Manga_{mangaId}_Chapters_{currentChapterLanguage}", 
                            JsonSerializer.Serialize(chaptersList));
                        _logger.LogInformation($"Đã lưu {chaptersList.Count} chapters ngôn ngữ {currentChapterLanguage} vào session");
                    }
                    
                    // Tìm chapter hiện tại và xác định chapter trước/sau
                    (currentChapterViewModel, prevChapterId, nextChapterId) = 
                        FindCurrentAndAdjacentChapters(chaptersList, id, currentChapterLanguage);
                }
                
                // Tạo view model
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = id,
                    ChapterTitle = currentChapterViewModel.Title,
                    ChapterNumber = currentChapterViewModel.Number,
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
                _logger.LogError($"Lỗi khi tải chapter: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
        }
        
        public async Task<string> GetMangaTitleAsync(string mangaId)
        {
            // Kiểm tra xem tiêu đề có trong session không
            var httpContext = _httpContextAccessor.HttpContext;
            string sessionTitle = httpContext.Session.GetString($"Manga_{mangaId}_Title");
            if (!string.IsNullOrEmpty(sessionTitle))
            {
                _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId} từ session: {sessionTitle}");
                return sessionTitle;
            }
            
            // Nếu không có trong session, gọi API để lấy tiêu đề
            return await GetMangaTitleFromApiAsync(mangaId);
        }
        
        private async Task<string> GetMangaTitleFromApiAsync(string mangaId)
        {
            _logger.LogInformation($"Tiến hành lấy tiêu đề manga {mangaId} từ API...");
            
            // Sử dụng phương thức từ MangaTitleService
            string mangaTitle = await _mangaTitleService.GetMangaTitleFromIdAsync(mangaId);
            
            // Lưu tiêu đề vào session để sử dụng sau này
            var httpContext = _httpContextAccessor.HttpContext;
            if (!string.IsNullOrEmpty(mangaTitle) && mangaTitle != "Không có tiêu đề")
            {
                httpContext.Session.SetString($"Manga_{mangaId}_Title", mangaTitle);
                _logger.LogInformation($"Đã lưu tiêu đề manga {mangaId} vào session: {mangaTitle}");
            }
            
            return mangaTitle;
        }
        
        private async Task<string> GetChapterLanguageFromApiAsync(string chapterId)
        {
            try
            {
                string language = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);
                _logger.LogInformation($"Đã xác định ngôn ngữ của chapter từ API: {language}");
                return language;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy ngôn ngữ của chapter {chapterId}: {ex.Message}");
                return "unknown";
            }
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersFromApiAsync(string mangaId, string language)
        {
            _logger.LogInformation($"Tiến hành lấy danh sách chapters cho manga {mangaId} với ngôn ngữ {language}");
            
            // Sử dụng ChapterService để lấy và xử lý danh sách chapters
            var allChapters = await _chapterService.GetChaptersAsync(mangaId, language);
            
            _logger.LogInformation($"Đã lấy {allChapters.Count} chapters từ API");
            
            // Lưu danh sách tất cả chapters vào session
            var httpContext = _httpContextAccessor.HttpContext;
            httpContext.Session.SetString($"Manga_{mangaId}_AllChapters", JsonSerializer.Serialize(allChapters));
            
            // Lọc và sắp xếp danh sách chapters theo ngôn ngữ hiện tại
            var chaptersByLanguage = _chapterService.GetChaptersByLanguage(allChapters);
            
            // Lấy danh sách chapters theo ngôn ngữ chỉ định
            if (chaptersByLanguage.ContainsKey(language))
            {
                return chaptersByLanguage[language];
            }
            
            // Nếu không tìm thấy chapters với ngôn ngữ chỉ định, trả về danh sách rỗng
            _logger.LogWarning($"Không tìm thấy chapters ngôn ngữ {language} cho manga {mangaId}");
            return new List<ChapterViewModel>();
        }
        
        private (ChapterViewModel currentChapter, string prevId, string nextId) FindCurrentAndAdjacentChapters(
            List<ChapterViewModel> chapters, string chapterId, string language)
        {
            ChapterViewModel currentChapterViewModel;
            string prevChapterId = null;
            string nextChapterId = null;
            
            // Tìm chapter hiện tại trong danh sách các chapter cùng ngôn ngữ
            int currentIndex = chapters.FindIndex(c => c.Id == chapterId);
            _logger.LogInformation($"Chapter hiện tại ở vị trí: {currentIndex}, tổng số: {chapters.Count}");
            
            if (currentIndex > 0)
            {
                prevChapterId = chapters[currentIndex - 1].Id;
            }
            
            if (currentIndex < chapters.Count - 1 && currentIndex >= 0)
            {
                nextChapterId = chapters[currentIndex + 1].Id;
            }
            
            _logger.LogInformation($"Chapter trước: {prevChapterId}, Chapter sau: {nextChapterId}");
            
            // Lấy thông tin về chapter hiện tại
            if (currentIndex >= 0 && currentIndex < chapters.Count)
            {
                currentChapterViewModel = chapters[currentIndex];
            }
            else
            {
                _logger.LogWarning($"Không tìm thấy chapter {chapterId} trong danh sách");
                
                // Tạo một chapter mặc định với thông tin tối thiểu
                currentChapterViewModel = new ChapterViewModel
                {
                    Id = chapterId,
                    Number = "?",
                    Title = "Chương không xác định",
                    Language = language
                };
            }
            
            return (currentChapterViewModel, prevChapterId, nextChapterId);
        }
    }
}
