using System.Text.Json;

namespace MangaReader.WebUI.Services.UtilityServices
{
    public class LocalizationService
    {
        private readonly ILogger<LocalizationService> _logger;

        public LocalizationService(ILogger<LocalizationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lấy tiêu đề theo ngôn ngữ ưu tiên từ JSON
        /// </summary>
        /// <param name="titleJson">Chuỗi JSON chứa thông tin tiêu đề</param>
        /// <returns>Tiêu đề được địa phương hóa</returns>
        public string GetLocalizedTitle(string titleJson)
        {
            try
            {
                if (string.IsNullOrEmpty(titleJson))
                    return "Không có tiêu đề";
                
                // Thử deserialize trực tiếp
                try
                {
                    var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                    
                    if (titles == null || titles.Count == 0)
                        return "Không có tiêu đề";
                        
                    // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                    if (titles.ContainsKey("vi"))
                        return titles["vi"];
                    if (titles.ContainsKey("en"))
                        return titles["en"];
                        
                    // Nếu không có, lấy giá trị đầu tiên
                    var firstItem = titles.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "Không có tiêu đề" : firstItem.Value;
                }
                catch (JsonException)
                {
                    // Nếu deserialization lỗi, có thể JSON không đúng định dạng mong đợi
                    // Trường hợp đặc biệt khi JSON là object thay vì string
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(titleJson);
                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                            if (jsonElement.TryGetProperty("vi", out var viTitle))
                                return viTitle.GetString() ?? "Không có tiêu đề";
                            if (jsonElement.TryGetProperty("en", out var enTitle))
                                return enTitle.GetString() ?? "Không có tiêu đề";
                            
                            // Nếu không có, lấy property đầu tiên
                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "Không có tiêu đề";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi của phương pháp thứ hai
                    }
                }
                
                // Nếu không thể parse, trả về giá trị mặc định
                return "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý tiêu đề truyện: {ex.Message}");
                return "Không có tiêu đề";
            }
        }
        
        /// <summary>
        /// Lấy mô tả theo ngôn ngữ ưu tiên từ JSON
        /// </summary>
        /// <param name="descriptionJson">Chuỗi JSON chứa thông tin mô tả</param>
        /// <returns>Mô tả được địa phương hóa</returns>
        public string GetLocalizedDescription(string descriptionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(descriptionJson))
                    return "";
                
                // Thử deserialize trực tiếp
                try
                {
                    var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);
                    
                    if (descriptions == null || descriptions.Count == 0)
                        return "";
                        
                    // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                    if (descriptions.ContainsKey("vi"))
                        return descriptions["vi"];
                    if (descriptions.ContainsKey("en"))
                        return descriptions["en"];
                        
                    // Nếu không có, lấy giá trị đầu tiên
                    var firstItem = descriptions.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "" : firstItem.Value;
                }
                catch (JsonException)
                {
                    // Nếu deserialization lỗi, có thể JSON không đúng định dạng mong đợi
                    // Trường hợp đặc biệt khi JSON là object thay vì string
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(descriptionJson);
                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                            if (jsonElement.TryGetProperty("vi", out var viDescription))
                                return viDescription.GetString() ?? "";
                            if (jsonElement.TryGetProperty("en", out var enDescription))
                                return enDescription.GetString() ?? "";
                            
                            // Nếu không có, lấy property đầu tiên
                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi của phương pháp thứ hai
                    }
                }
                
                // Nếu không thể parse, trả về chuỗi rỗng
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý mô tả truyện: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Lấy trạng thái đã dịch từ attributes
        /// </summary>
        public string GetStatus(Dictionary<string, object> attributesDict)
        {
            string status = attributesDict.ContainsKey("status") ? attributesDict["status"]?.ToString() ?? "unknown" : "unknown";
            return status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }
    }
}
