using MangaReader.WebUI.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class MangaDexService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://manga-reader-app-backend.onrender.com/api/mangadex";
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<MangaDexService> _logger;

        public MangaDexService(HttpClient httpClient, ILogger<MangaDexService> logger = null)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Cấu hình timeout dài hơn cho các request
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Ghi log lỗi với thông tin chi tiết
        private void LogError(string functionName, HttpResponseMessage response, string content)
        {
            string errorMessage = $"Lỗi trong hàm {functionName}:\n" +
                                $"URL: {response.RequestMessage?.RequestUri}\n" +
                                $"Mã trạng thái: {(int)response.StatusCode}\n" +
                                $"Nội dung phản hồi: {content}";
            
            Console.WriteLine(errorMessage);
            _logger?.LogError(errorMessage);
            
            // In stack trace để debug
            Console.WriteLine("Stack trace:");
            Console.WriteLine(new StackTrace().ToString());
        }

        // Phương thức test kết nối đến API
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine($"Kiểm tra kết nối đến: {_baseUrl}/status");
                
                // Đặt thời gian chờ ngắn hơn cho request kiểm tra kết nối
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/status");
                
                var response = await _httpClient.SendAsync(requestMessage, cts.Token);
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Kết quả kiểm tra kết nối: {(int)response.StatusCode} - {content}");
                
                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException ex)
            {
                // Xử lý timeout riêng
                Console.WriteLine($"Timeout khi kiểm tra kết nối: {ex.Message}");
                _logger?.LogWarning($"Timeout khi kiểm tra kết nối đến API: {ex.Message}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                // Xử lý lỗi HTTP riêng
                Console.WriteLine($"Lỗi HTTP khi kiểm tra kết nối: {ex.Message}");
                _logger?.LogWarning($"Lỗi HTTP khi kết nối đến API: {ex.Message}");
                return false;
            }
            catch (OperationCanceledException ex)
            {
                // Bắt lỗi hủy thao tác 
                Console.WriteLine($"Kiểm tra kết nối bị hủy: {ex.Message}");
                _logger?.LogWarning($"Yêu cầu kiểm tra kết nối bị hủy: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Bắt tất cả lỗi khác
                Console.WriteLine($"Lỗi khi kiểm tra kết nối: {ex.Message}");
                _logger?.LogError($"Lỗi không xác định khi kiểm tra kết nối: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    _logger?.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Lấy danh sách manga
        public async Task<List<dynamic>> FetchMangaAsync(int? limit = null, int? offset = null, SortManga sortManga = null)
        {
            try
            {
                var queryParams = new Dictionary<string, List<string>>();

                if (limit.HasValue)
                    AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());

                if (offset.HasValue)
                    AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

                if (sortManga != null)
                {
                    // Ghi log thông tin tìm kiếm
                    var logMessage = $"Tìm kiếm manga với: Title={sortManga.Title}, Status={sortManga.Status}, SortBy={sortManga.SortBy}";
                    if (sortManga.IncludedTags != null && sortManga.IncludedTags.Any())
                        logMessage += $", IncludedTags={string.Join(",", sortManga.IncludedTags)}";
                    
                    Console.WriteLine(logMessage);
                    _logger?.LogInformation(logMessage);
                    
                    // Lấy các tham số từ SortManga
                    var parameters = sortManga.ToParams();
                    foreach (var param in parameters)
                    {
                        // Kiểm tra và xử lý đúng kiểu dữ liệu tham số
                        if (param.Key.EndsWith("[]") && param.Value is IEnumerable<string> values)
                        {
                            // Xử lý tham số dạng mảng
                            foreach (var value in values)
                            {
                                if (!string.IsNullOrEmpty(value))
                                    AddOrUpdateParam(queryParams, param.Key, value);
                            }
                        }
                        else if (param.Value is string[] strArray)
                        {
                            // Xử lý tham số dạng mảng string[]
                            foreach (var value in strArray)
                            {
                                if (!string.IsNullOrEmpty(value))
                                    AddOrUpdateParam(queryParams, param.Key, value);
                            }
                        }
                        else if (param.Value is string strValue && !string.IsNullOrEmpty(strValue))
                        {
                            // Xử lý tham số dạng chuỗi đơn
                            AddOrUpdateParam(queryParams, param.Key, strValue);
                        }
                        else if (param.Value != null)
                        {
                            // Xử lý các trường hợp khác
                            AddOrUpdateParam(queryParams, param.Key, param.Value.ToString());
                        }
                    }
                }
                else
                {
                    // Mặc định thêm tham số contentRating để lọc nội dung an toàn
                    AddOrUpdateParam(queryParams, "contentRating[]", "safe");
                    AddOrUpdateParam(queryParams, "contentRating[]", "suggestive");
                    
                    // Mặc định sắp xếp theo chương mới nhất
                    AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
                }

                var url = BuildUrl($"{_baseUrl}/manga", queryParams);
                Console.WriteLine($"Gửi request đến: {url}");
                
                // Thêm thời gian timeout dài hơn cho request này
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                
                var response = await _httpClient.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Nhận response với status: {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                        if (data.ContainsKey("data"))
                        {
                            var result = new List<dynamic>();
                            
                            // Thêm thông tin tổng số manga vào kết quả trả về nếu có
                            if (data.ContainsKey("total"))
                            {
                                int total = data["total"].GetInt32();
                                // Tạo một dynamic object để lưu totalCount
                                var totalInfo = new Dictionary<string, object>
                                {
                                    { "total", total }
                                };
                                
                                // Thêm vào đầu danh sách kết quả
                                result.Add(JsonSerializer.Deserialize<dynamic>(JsonSerializer.Serialize(totalInfo), _jsonOptions));
                            }
                            
                            // Thêm dữ liệu manga vào kết quả
                            var mangaList = JsonSerializer.Deserialize<List<dynamic>>(data["data"].ToString(), _jsonOptions);
                            result.AddRange(mangaList);
                            
                            return result;
                        }
                        else
                        {
                            Console.WriteLine($"Phản hồi không chứa trường 'data': {content}");
                            throw new Exception("Phản hồi API không hợp lệ: Không tìm thấy trường 'data'");
                        }
                    }
                    catch (JsonException jex)
                    {
                        Console.WriteLine($"Lỗi khi deserialize JSON: {jex.Message}");
                        Console.WriteLine($"JSON nhận được: {content}");
                        throw new Exception($"Lỗi khi xử lý phản hồi JSON: {jex.Message}");
                    }
                }
                else if ((int)response.StatusCode == 503)
                {
                    throw new Exception("Máy chủ MangaDex hiện đang bảo trì, xin vui lòng thử lại sau!");
                }
                else
                {
                    LogError("FetchMangaAsync", response, content);
                    throw new Exception($"Lỗi khi tải manga: {(int)response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Lỗi HTTP Request: {ex.Message}");
                _logger?.LogError($"Lỗi HTTP Request: {ex.Message}");
                throw new Exception($"Không thể kết nối đến API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Request timeout: {ex.Message}");
                _logger?.LogError($"Request timeout: {ex.Message}");
                throw new Exception("Yêu cầu đã hết thời gian chờ. Vui lòng thử lại sau.", ex);
            }
            catch (Exception ex) when (!(ex is HttpRequestException || ex is TaskCanceledException))
            {
                Console.WriteLine($"Lỗi không xác định: {ex.Message}");
                _logger?.LogError($"Lỗi không xác định: {ex.Message}");
                throw new Exception($"Đã xảy ra lỗi: {ex.Message}", ex);
            }
        }

        // Hàm trợ giúp thêm hoặc cập nhật tham số
        private void AddOrUpdateParam(Dictionary<string, List<string>> parameters, string key, string value)
        {
            if (!parameters.ContainsKey(key))
            {
                parameters[key] = new List<string>();
            }
            parameters[key].Add(value);
        }

        // Xây dựng URL với tham số
        private string BuildUrl(string baseUrl, Dictionary<string, List<string>> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return baseUrl;

            var sb = new StringBuilder(baseUrl);
            sb.Append('?');

            bool isFirst = true;
            foreach (var param in parameters)
            {
                foreach (var value in param.Value)
                {
                    if (!isFirst)
                        sb.Append('&');
                    else
                        isFirst = false;

                    sb.Append(Uri.EscapeDataString(param.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(value));
                }
            }

            return sb.ToString();
        }

        // Lấy chi tiết một manga
        public async Task<dynamic> FetchMangaDetailsAsync(string mangaId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/manga/{mangaId}?includes[]=author&includes[]=artist&includes[]=cover_art");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                return JsonSerializer.Deserialize<dynamic>(data["data"].ToString(), _jsonOptions);
            }
            else
            {
                LogError("FetchMangaDetailsAsync", response, content);
                throw new Exception("Lỗi khi tải chi tiết manga");
            }
        }

        // Lấy danh sách các chương của manga
        public async Task<List<dynamic>> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            var languageList = languages.Split(',');
            var validLanguages = new List<string>();

            foreach (var lang in languageList)
            {
                var trimmedLang = lang.Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(trimmedLang, @"^[a-z]{2}(-[a-z]{2})?$"))
                {
                    validLanguages.Add(trimmedLang);
                }
            }

            if (validLanguages.Count == 0)
            {
                validLanguages.Add("en"); // Mặc định tiếng Anh
            }

            int offset = 0;
            int limit = 100; // Số chương tối đa mỗi lần tải
            var allChapters = new List<dynamic>();

            // Tải chương theo trang cho đến khi đạt số lượng chương tối đa hoặc hết chương
            while (maxChapters == null || allChapters.Count < maxChapters.Value)
            {
                var queryParams = new Dictionary<string, List<string>>();
                
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[chapter]", order);

                foreach (var lang in validLanguages)
                {
                    AddOrUpdateParam(queryParams, "translatedLanguage[]", lang);
                }

                var url = BuildUrl($"{_baseUrl}/manga/{mangaId}/feed", queryParams);
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                    var chapters = JsonSerializer.Deserialize<List<dynamic>>(data["data"].ToString(), _jsonOptions);

                    if (chapters.Count == 0)
                    {
                        break;
                    }

                    allChapters.AddRange(chapters);

                    // Nếu đã tải đủ hoặc không còn chương nào thì dừng lại
                    if (maxChapters.HasValue && allChapters.Count >= maxChapters.Value || chapters.Count < limit)
                    {
                        break;
                    }

                    offset += limit;
                }
                else if ((int)response.StatusCode == 503)
                {
                    throw new Exception("Máy chủ MangaDex hiện đang bảo trì, xin vui lòng thử lại sau!");
                }
                else
                {
                    LogError("FetchChaptersAsync", response, content);
                    throw new Exception($"Lỗi trong hàm FetchChaptersAsync: Mã trạng thái: {(int)response.StatusCode}");
                }
            }

            return allChapters;
        }

        // Lấy ảnh bìa của manga
        public async Task<string> FetchCoverUrlAsync(string mangaId)
        {
            try
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "manga[]", mangaId);
                
                var url = BuildUrl($"{_baseUrl}/cover", queryParams);
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                    var coverData = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(data["data"].ToString(), _jsonOptions);

                    if (coverData != null && coverData.Count > 0)
                    {
                        var coverAttributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(coverData[0]["attributes"].ToString(), _jsonOptions);
                        string coverId = coverAttributes["fileName"].ToString();
                        
                        // URL này sẽ qua proxy để tránh CORS
                        return $"{_baseUrl}/proxy-image?url={Uri.EscapeDataString($"https://uploads.mangadex.org/covers/{mangaId}/{coverId}.512.jpg")}";
                    }
                    else
                    {
                        // Trả về URL ảnh mặc định nếu không tìm thấy
                        Console.WriteLine($"Không tìm thấy ảnh bìa cho manga {mangaId}");
                        return "/images/cover-placeholder.jpg";
                    }
                }
                else if ((int)response.StatusCode == 503)
                {
                    // Ảnh bìa mặc định nếu server bảo trì
                    Console.WriteLine("Server MangaDex đang bảo trì");
                    return "/images/cover-placeholder.jpg";
                }
                else
                {
                    // Ảnh bìa mặc định nếu có lỗi khác
                    LogError("FetchCoverUrlAsync", response, content);
                    Console.WriteLine($"Lỗi khi tải ảnh bìa: {(int)response.StatusCode}");
                    return "/images/cover-placeholder.jpg";
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nhưng vẫn trả về ảnh mặc định thay vì throw exception
                Console.WriteLine($"Lỗi không xác định khi tải ảnh bìa: {ex.Message}");
                _logger?.LogError($"Lỗi khi tải ảnh bìa: {ex.Message}");
                return "/images/cover-placeholder.jpg";
            }
        }

        // Lấy các trang của chương
        public async Task<List<string>> FetchChapterPagesAsync(string chapterId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/at-home/server/{chapterId}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                var chapterData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data["chapter"].ToString(), _jsonOptions);
                var pages = JsonSerializer.Deserialize<List<string>>(chapterData["data"].ToString(), _jsonOptions);
                var baseUrl = data["baseUrl"].ToString();
                var hash = chapterData["hash"].ToString();

                // Xây dựng URL đầy đủ cho các trang ảnh (qua proxy)
                return pages.ConvertAll(page => 
                    $"{_baseUrl}/proxy-image?url={Uri.EscapeDataString($"{baseUrl}/data/{hash}/{page}")}");
            }
            else
            {
                LogError("FetchChapterPagesAsync", response, content);
                throw new Exception("Lỗi khi tải các trang chương");
            }
        }

        // Lấy danh sách tags
        public async Task<List<dynamic>> FetchTagsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/manga/tag");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                return JsonSerializer.Deserialize<List<dynamic>>(data["data"].ToString(), _jsonOptions);
            }
            else
            {
                LogError("FetchTagsAsync", response, content);
                throw new Exception("Lỗi khi tải danh sách tags");
            }
        }

        // Lấy manga theo danh sách ID
        public async Task<List<dynamic>> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || mangaIds.Count == 0)
                return new List<dynamic>();

            try
            {
                var queryParams = new Dictionary<string, List<string>>();
                foreach (var id in mangaIds)
                {
                    AddOrUpdateParam(queryParams, "ids[]", id);
                }

                var url = BuildUrl($"{_baseUrl}/manga", queryParams);
                Console.WriteLine($"Gửi request lấy manga theo IDs: {url}");
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                    return JsonSerializer.Deserialize<List<dynamic>>(data["data"].ToString(), _jsonOptions);
                }
                else
                {
                    LogError("FetchMangaByIdsAsync", response, content);
                    throw new Exception($"Lỗi khi tải manga theo IDs: {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tải manga theo IDs: {ex.Message}");
                _logger?.LogError($"Lỗi khi tải manga theo IDs: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một chapter dựa vào ID
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>Đối tượng chứa thông tin chapter</returns>
        public async Task<dynamic> FetchChapterInfoAsync(string chapterId)
        {
            try
            {
                string url = $"{_baseUrl}/chapter/{chapterId}";
                _logger?.LogInformation($"Đang gọi API: {url}");
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);
                    return JsonSerializer.Deserialize<dynamic>(data["data"].ToString(), _jsonOptions);
                }
                else
                {
                    LogError("FetchChapterInfoAsync", response, content);
                    throw new Exception($"Lỗi khi lấy thông tin chapter: {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy thông tin chapter {chapterId}: {ex.Message}");
                _logger?.LogError($"Không thể lấy thông tin chapter {chapterId}: {ex.Message}");
                throw new Exception($"Không thể lấy thông tin chapter: {ex.Message}", ex);
            }
        }
    }
} 