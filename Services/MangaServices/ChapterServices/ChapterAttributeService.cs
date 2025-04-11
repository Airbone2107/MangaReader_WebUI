using manga_reader_web.Services.MangaServices.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace manga_reader_web.Services.MangaServices.ChapterServices
{
    public class ChapterAttributeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChapterAttributeService> _logger;
        private readonly string _baseUrl = "https://manga-reader-app-backend.onrender.com/api/mangadex";
        private readonly JsonSerializerOptions _jsonOptions;

        public ChapterAttributeService(HttpClient httpClient, IConfiguration configuration, ILogger<ChapterAttributeService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Lấy thông tin chapter từ API MangaDex
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>JsonElement chứa dữ liệu attributes của chapter</returns>
        private async Task<JsonElement?> FetchChapterDataAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi FetchChapterDataAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy thông tin cho Chapter: {chapterId}");
            
            // Gọi API để lấy thông tin chapter
            string url = $"{_baseUrl}/chapter/{chapterId}";
            _logger.LogInformation($"Đang gọi API: {url}");
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Lỗi khi gọi API: {response.StatusCode} - {response.ReasonPhrase}");
                _logger.LogDebug($"Nội dung phản hồi: {content}");
                throw new HttpRequestException($"API trả về lỗi {response.StatusCode}: {response.ReasonPhrase}");
            }

            // Đọc và phân tích dữ liệu JSON
            var chapterData = JsonSerializer.Deserialize<JsonElement>(content);

            // Kiểm tra xem API có trả về kết quả thành công không
            if (!chapterData.TryGetProperty("result", out JsonElement resultElement) || 
                resultElement.GetString() != "ok")
            {
                _logger.LogError($"API trả về kết quả không thành công: {content}");
                throw new InvalidOperationException("API trả về kết quả không thành công");
            }

            if (!chapterData.TryGetProperty("data", out JsonElement dataElement) ||
                !dataElement.TryGetProperty("attributes", out JsonElement attributesElement))
            {
                _logger.LogError($"Dữ liệu trả về từ API không có trường attributes: {content}");
                return null;
            }

            return attributesElement;
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
                var attributesElement = await FetchChapterDataAsync(chapterId);
                if (attributesElement == null)
                {
                    return "?";
                }

                string chapterNumber = "?";
                if (attributesElement.Value.TryGetProperty("chapter", out JsonElement chapterNumElement))
                {
                    if (chapterNumElement.ValueKind == JsonValueKind.String)
                    {
                        chapterNumber = chapterNumElement.GetString() ?? "?";
                    }
                    else if (chapterNumElement.ValueKind == JsonValueKind.Number)
                    {
                        chapterNumber = chapterNumElement.ToString();
                    }
                }

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
                var attributesElement = await FetchChapterDataAsync(chapterId);
                if (attributesElement == null)
                {
                    return "";
                }

                string chapterTitle = "";
                if (attributesElement.Value.TryGetProperty("title", out JsonElement titleElement) && 
                    titleElement.ValueKind == JsonValueKind.String)
                {
                    chapterTitle = titleElement.GetString() ?? "";
                }

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
                var attributesElement = await FetchChapterDataAsync(chapterId);
                if (attributesElement == null)
                {
                    return DateTime.MinValue;
                }

                DateTime publishedAt = DateTime.MinValue;
                if (attributesElement.Value.TryGetProperty("publishAt", out JsonElement publishAtElement) && 
                    publishAtElement.ValueKind == JsonValueKind.String)
                {
                    if (publishAtElement.TryGetDateTime(out var date))
                    {
                        publishedAt = date;
                    }
                    else
                    {
                        _logger.LogWarning($"Không thể parse ngày publishAt: {publishAtElement.GetString()} cho chapter {chapterId}");
                    }
                }

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