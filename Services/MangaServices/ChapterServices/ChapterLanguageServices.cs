using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterLanguageServices
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChapterLanguageServices> _logger;
        private readonly string _baseUrl = "https://manga-reader-app-backend.onrender.com/api/mangadex";
        private readonly JsonSerializerOptions _jsonOptions;

        public ChapterLanguageServices(HttpClient httpClient, IConfiguration configuration, ILogger<ChapterLanguageServices> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
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
                !dataElement.TryGetProperty("attributes", out JsonElement attributesElement) ||
                !attributesElement.TryGetProperty("translatedLanguage", out JsonElement languageElement))
            {
                _logger.LogError($"Dữ liệu trả về từ API không có trường ngôn ngữ: {content}");
                throw new InvalidOperationException("Dữ liệu API không đúng định dạng mong đợi");
            }

            string language = languageElement.GetString();
            _logger.LogInformation($"Đã lấy được ngôn ngữ: {language} cho Chapter: {chapterId}");
            return language;
        }
    }
}
