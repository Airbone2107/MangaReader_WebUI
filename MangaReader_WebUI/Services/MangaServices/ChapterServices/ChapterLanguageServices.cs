using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterLanguageServices
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly ILogger<ChapterLanguageServices> _logger;

        public ChapterLanguageServices(
            IChapterApiService chapterApiService,
            ILogger<ChapterLanguageServices> logger)
        {
            _chapterApiService = chapterApiService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy ngôn ngữ của chapter từ chapterID bằng cách gọi API
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>Mã ngôn ngữ (ví dụ: 'vi', 'en', 'jp',...) nếu tìm thấy, null nếu không tìm thấy</returns>
        public async Task<string> GetChapterLanguageAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi GetChapterLanguageAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy thông tin ngôn ngữ cho Chapter: {chapterId}");

            try
            {
                // Gọi API service mới
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

                if (chapterResponse?.Result == "ok" && chapterResponse.Data?.Attributes?.TranslatedLanguage != null)
                {
                    string language = chapterResponse.Data.Attributes.TranslatedLanguage;
                    _logger.LogInformation($"Đã lấy được ngôn ngữ: {language} cho Chapter: {chapterId}");
                    return language;
                }
                else
                {
                    _logger.LogError($"Không lấy được thông tin ngôn ngữ cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                    throw new InvalidOperationException($"Không thể lấy ngôn ngữ cho chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy ngôn ngữ cho chapter {chapterId}");
                throw; // Ném lại lỗi để lớp gọi xử lý
            }
        }
    }
}
