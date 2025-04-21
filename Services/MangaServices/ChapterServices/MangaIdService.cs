using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class MangaIdService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MangaIdService> _logger;
        private readonly string _baseUrl = "https://manga-reader-app-backend.onrender.com/api/mangadex";
        private readonly JsonSerializerOptions _jsonOptions;

        public MangaIdService(HttpClient httpClient, IConfiguration configuration, ILogger<MangaIdService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Lấy MangaID từ ChapterID bằng cách gọi API
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>MangaID nếu tìm thấy, null nếu không tìm thấy</returns>
        public async Task<string> GetMangaIdFromChapterAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi GetMangaIdFromChapterAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy MangaID cho Chapter: {chapterId}");
            
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
                !dataElement.TryGetProperty("relationships", out JsonElement relationshipsArray))
            {
                _logger.LogError($"Dữ liệu trả về từ API không có trường data hoặc relationships: {content}");
                throw new InvalidOperationException("Dữ liệu API không đúng định dạng mong đợi");
            }

            // Tìm kiếm trong mảng relationships đối tượng có type là "manga"
            foreach (JsonElement relationship in relationshipsArray.EnumerateArray())
            {
                if (relationship.TryGetProperty("type", out JsonElement typeElement) &&
                    typeElement.GetString() == "manga" &&
                    relationship.TryGetProperty("id", out JsonElement idElement))
                {
                    string mangaId = idElement.GetString();
                    _logger.LogInformation($"Đã tìm thấy MangaID: {mangaId} cho Chapter: {chapterId}");
                    return mangaId;
                }
            }

            _logger.LogWarning($"Không tìm thấy MangaID cho Chapter: {chapterId}");
            throw new KeyNotFoundException($"Không tìm thấy MangaID cho Chapter: {chapterId}");
        }
    }
}
