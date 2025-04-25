using System.Text.Json;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.UtilityServices;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterAttributeService
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly ILogger<ChapterAttributeService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChapterAttributeService(
            IChapterApiService chapterApiService,
            JsonConversionService jsonConversionService,
            ILogger<ChapterAttributeService> logger)
        {
            _chapterApiService = chapterApiService;
            _jsonConversionService = jsonConversionService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Lấy thông tin chapter attributes từ API
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>ChapterAttributes hoặc null nếu không lấy được</returns>
        private async Task<ChapterAttributes?> FetchChapterAttributesAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi FetchChapterAttributesAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy thông tin Attributes cho Chapter: {chapterId}");

            // Gọi API để lấy thông tin chapter thông qua IChapterApiService
            var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

            if (chapterResponse?.Result != "ok" || chapterResponse.Data?.Attributes == null)
            {
                _logger.LogError($"Không lấy được thông tin hoặc attributes cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                return null;
            }

            return chapterResponse.Data.Attributes;
        }

        /// <summary>
        /// Lấy số chapter từ attributes của chapter
        /// </summary>
        /// <param name="chapterId">ID của chapter</param>
        /// <returns>Số chapter dưới dạng chuỗi, "?" nếu không có</returns>
        public async Task<string> GetChapterNumberAsync(string chapterId)
        {
            try
            {
                var attributes = await FetchChapterAttributesAsync(chapterId);
                if (attributes == null) return "?";

                string chapterNumber = attributes.ChapterNumber ?? "?";

                _logger.LogInformation($"Đã lấy được số chapter: {chapterNumber} cho Chapter: {chapterId}");
                return chapterNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy số chapter cho {chapterId}");
                return "?";
            }
        }

        /// <summary>
        /// Lấy tiêu đề chapter từ attributes của chapter
        /// </summary>
        /// <param name="chapterId">ID của chapter</param>
        /// <returns>Tiêu đề chapter, chuỗi rỗng nếu không có</returns>
        public async Task<string> GetChapterTitleAsync(string chapterId)
        {
            try
            {
                var attributes = await FetchChapterAttributesAsync(chapterId);
                if (attributes == null) return "";

                string chapterTitle = attributes.Title ?? "";

                _logger.LogInformation($"Đã lấy được tiêu đề chapter: '{chapterTitle}' cho Chapter: {chapterId}");
                return chapterTitle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy tiêu đề chapter cho {chapterId}");
                return "";
            }
        }

        /// <summary>
        /// Lấy ngày xuất bản của chapter từ attributes
        /// </summary>
        /// <param name="chapterId">ID của chapter</param>
        /// <returns>Ngày xuất bản, DateTime.MinValue nếu không có hoặc không thể parse</returns>
        public async Task<DateTime> GetPublishedAtAsync(string chapterId)
        {
            try
            {
                var attributes = await FetchChapterAttributesAsync(chapterId);
                if (attributes == null) return DateTime.MinValue;

                DateTime publishedAt = attributes.PublishAt.DateTime;

                _logger.LogInformation($"Đã lấy được ngày xuất bản: {publishedAt} cho Chapter: {chapterId}");
                return publishedAt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy ngày xuất bản cho {chapterId}");
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Tạo tiêu đề hiển thị dựa trên số chapter và tiêu đề
        /// </summary>
        /// <param name="chapterNumber">Số chapter</param>
        /// <param name="chapterTitle">Tiêu đề chapter</param>
        /// <returns>Tiêu đề hiển thị đã được định dạng</returns>
        public string CreateDisplayTitle(string chapterNumber, string chapterTitle)
        {
            if (string.IsNullOrEmpty(chapterNumber) || chapterNumber == "?")
            {
                // Trường hợp đặc biệt cho Oneshot khi chapter number là null/rỗng
                return !string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot";
            }
            
            string displayTitle = $"Chương {chapterNumber}";
            if (!string.IsNullOrEmpty(chapterTitle) && chapterTitle != chapterNumber)
            {
                displayTitle += $": {chapterTitle}";
            }
            
            return displayTitle;
        }
    }
} 