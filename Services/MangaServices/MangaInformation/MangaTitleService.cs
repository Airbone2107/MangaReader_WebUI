using MangaReader.WebUI.Services.UtilityServices;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaTitleService
    {
        private readonly ILogger<MangaTitleService> _logger;
        private readonly HttpClient _httpClient;
        private readonly JsonConversionService _jsonConversionService;
        private readonly string _baseUrl = "https://manga-reader-app-backend.onrender.com/api/mangadex";

        public MangaTitleService(
            ILogger<MangaTitleService> logger,
            HttpClient httpClient,
            JsonConversionService jsonConversionService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _jsonConversionService = jsonConversionService;
        }

        /// <summary>
        /// Lấy tiêu đề manga mặc định từ đối tượng title
        /// </summary>
        /// <param name="titleObj">Đối tượng title từ attributes</param>
        /// <returns>Tiêu đề manga mặc định</returns>
        public string GetDefaultMangaTitle(object titleObj)
        {
            try
            {
                if (titleObj == null)
                    return "Không có tiêu đề";
                
                // Truy cập trực tiếp vào giá trị đầu tiên
                return ((Dictionary<string, object>)titleObj).Values.FirstOrDefault()?.ToString() ?? "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý tiêu đề mặc định: {ex.Message}");
                return "Không có tiêu đề";
            }
        }

        /// <summary>
        /// Lấy tiêu đề manga ưu tiên
        /// </summary>
        /// <param name="titleObj">Đối tượng title từ attributes</param>
        /// <param name="altTitlesObj">Đối tượng altTitles từ attributes</param>
        /// <returns>Tiêu đề manga ưu tiên</returns>
        public string GetMangaTitle(object titleObj, object altTitlesObj)
        {
            try
            {
                // Lấy danh sách tiêu đề thay thế
                var altTitlesDictionary = GetAlternativeTitles(altTitlesObj);
                
                // Kiểm tra xem có tên tiếng Việt (vi) không
                if (altTitlesDictionary.ContainsKey("vi"))
                {
                    return altTitlesDictionary["vi"].FirstOrDefault();
                }
                
                // Nếu không có tên tiếng Việt, sử dụng tên mặc định
                return GetDefaultMangaTitle(titleObj);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý tiêu đề ưu tiên: {ex.Message}");
                return "Không có tiêu đề";
            }
        }

        /// <summary>
        /// Xử lý và lấy danh sách tiêu đề thay thế
        /// </summary>
        /// <param name="altTitlesObj">Đối tượng altTitles từ attributes</param>
        /// <returns>Dictionary chứa danh sách tiêu đề thay thế theo ngôn ngữ</returns>
        public Dictionary<string, List<string>> GetAlternativeTitles(object altTitlesObj)
        {
            var altTitlesDictionary = new Dictionary<string, List<string>>();
            
            try
            {
                if (altTitlesObj == null)
                    return altTitlesDictionary;
                
                // Chuyển đổi trực tiếp thành List<object>
                var altTitlesList = (List<object>)altTitlesObj;
                
                foreach (var altTitle in altTitlesList)
                {
                    // Xử lý trực tiếp mỗi altTitle như một Dictionary
                    var altTitleDict = (Dictionary<string, object>)altTitle;
                    var langKey = altTitleDict.Keys.First();
                    var titleText = altTitleDict[langKey]?.ToString();
                    
                    if (altTitlesDictionary.ContainsKey(langKey))
                    {
                        altTitlesDictionary[langKey].Add(titleText);
                    }
                    else
                    {
                        altTitlesDictionary[langKey] = new List<string> { titleText };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý tiêu đề thay thế: {ex.Message}");
            }
            
            return altTitlesDictionary;
        }

        /// <summary>
        /// Lấy một tiêu đề thay thế ưu tiên (thường là tiếng Anh)
        /// </summary>
        /// <param name="altTitlesDictionary">Dictionary chứa danh sách tiêu đề thay thế theo ngôn ngữ</param>
        /// <returns>Một tiêu đề thay thế ưu tiên</returns>
        public string GetPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary)
        {
            // Kiểm tra theo thứ tự ưu tiên: tiếng Anh, tiếng Nhật, bất kỳ
            if (altTitlesDictionary.ContainsKey("en"))
            {
                return altTitlesDictionary["en"].FirstOrDefault() ?? "";
            }
            
            if (altTitlesDictionary.ContainsKey("jp"))
            {
                return altTitlesDictionary["jp"].FirstOrDefault() ?? "";
            }
            
            // Nếu không có tiếng Anh hoặc tiếng Nhật, lấy ngôn ngữ đầu tiên có sẵn
            if (altTitlesDictionary.Count > 0)
            {
                var firstLang = altTitlesDictionary.Keys.First();
                return altTitlesDictionary[firstLang].FirstOrDefault() ?? "";
            }
            return "";
        }

        /// <summary>
        /// Lấy tiêu đề manga từ ID manga bằng cách gọi API
        /// </summary>
        /// <param name="mangaId">ID của manga cần lấy tiêu đề</param>
        /// <returns>Tiêu đề manga theo thứ tự ưu tiên (Tiếng Việt, tiêu đề mặc định)</returns>
        public async Task<string> GetMangaTitleFromIdAsync(string mangaId)
        {
            try
            {
                if (string.IsNullOrEmpty(mangaId))
                {
                    _logger.LogWarning("MangaId không được cung cấp khi gọi GetMangaTitleFromIdAsync");
                    throw new ArgumentNullException(nameof(mangaId), "MangaId không được để trống");
                }

                _logger.LogInformation($"Đang lấy tiêu đề cho manga: {mangaId}");
                
                // Gọi API để lấy thông tin manga
                string url = $"{_baseUrl}/manga/{mangaId}";
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
                var mangaData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);

                // Kiểm tra xem API có trả về kết quả thành công không
                if (!mangaData.TryGetProperty("result", out var resultElement) || 
                    resultElement.GetString() != "ok")
                {
                    _logger.LogError($"API trả về kết quả không thành công: {content}");
                    throw new InvalidOperationException("API trả về kết quả không thành công");
                }

                if (!mangaData.TryGetProperty("data", out var dataElement) ||
                    !dataElement.TryGetProperty("attributes", out var attributesElement))
                {
                    _logger.LogError($"Dữ liệu trả về từ API không có trường data hoặc attributes: {content}");
                    throw new InvalidOperationException("Dữ liệu API không đúng định dạng mong đợi");
                }

                // Chuyển JSON thành Dictionary để xử lý
                var mangaDict = _jsonConversionService.ConvertJsonElementToDict(dataElement);
                var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                
                // Lấy tiêu đề từ attributes
                if (!attributesDict.ContainsKey("title") || attributesDict["title"] == null)
                {
                    _logger.LogWarning($"Manga {mangaId} không có thuộc tính title");
                    return "Không có tiêu đề";
                }
                
                // Sử dụng các phương thức có sẵn để lấy tiêu đề
                if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                {
                    string title = GetMangaTitle(attributesDict["title"], attributesDict["altTitles"]);
                    _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId}: {title}");
                    return title;
                }
                else
                {
                    _logger.LogInformation("Không tìm thấy tiêu đề trong dữ liệu được API trả về");
                    return "Không có tiêu đề";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy tiêu đề manga {mangaId}: {ex.Message}", ex);
                return "Không có tiêu đề";
            }
        }
    }
}
