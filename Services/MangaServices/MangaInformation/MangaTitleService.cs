using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaTitleService
    {
        private readonly ILogger<MangaTitleService> _logger;
        private readonly IMangaApiService _mangaApiService;
        private readonly JsonConversionService _jsonConversionService;

        public MangaTitleService(
            ILogger<MangaTitleService> logger,
            IMangaApiService mangaApiService,
            JsonConversionService jsonConversionService)
        {
            _logger = logger;
            _mangaApiService = mangaApiService;
            _jsonConversionService = jsonConversionService;
        }

        /// <summary>
        /// Lấy tiêu đề manga mặc định từ Dictionary tiêu đề
        /// </summary>
        /// <param name="titleDict">Dictionary tiêu đề từ attributes</param>
        /// <returns>Tiêu đề manga mặc định</returns>
        public string GetDefaultMangaTitle(Dictionary<string, string>? titleDict)
        {
            if (titleDict == null || !titleDict.Any())
                return "Không có tiêu đề";

            try
            {
                // Ưu tiên 'en' nếu có
                if (titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle)) return enTitle;
                // Lấy giá trị đầu tiên
                return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề mặc định từ Dictionary.");
                return "Không có tiêu đề";
            }
        }

        /// <summary>
        /// Lấy tiêu đề manga ưu tiên
        /// </summary>
        /// <param name="titleDict">Dictionary tiêu đề từ attributes</param>
        /// <param name="altTitlesList">Danh sách tiêu đề thay thế từ attributes</param>
        /// <returns>Tiêu đề manga ưu tiên</returns>
        public string GetMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
        {
            try
            {
                // Lấy danh sách tiêu đề thay thế
                var altTitlesDictionary = GetAlternativeTitles(altTitlesList);

                // Kiểm tra xem có tên tiếng Việt (vi) không
                if (altTitlesDictionary.TryGetValue("vi", out var viTitles) && viTitles.Any())
                {
                    return viTitles.First(); // Lấy tiêu đề tiếng Việt đầu tiên
                }

                // Nếu không có tên tiếng Việt, sử dụng tên mặc định
                return GetDefaultMangaTitle(titleDict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề ưu tiên.");
                return GetDefaultMangaTitle(titleDict); // Fallback về tiêu đề mặc định
            }
        }

        /// <summary>
        /// Xử lý và lấy danh sách tiêu đề thay thế
        /// </summary>
        /// <param name="altTitlesList">Danh sách tiêu đề thay thế từ attributes</param>
        /// <returns>Dictionary chứa danh sách tiêu đề thay thế theo ngôn ngữ</returns>
        public Dictionary<string, List<string>> GetAlternativeTitles(List<Dictionary<string, string>>? altTitlesList)
        {
            var altTitlesDictionary = new Dictionary<string, List<string>>();
            if (altTitlesList == null) return altTitlesDictionary;

            try
            {
                foreach (var altTitleDict in altTitlesList)
                {
                    if (altTitleDict != null && altTitleDict.Any())
                    {
                        var langKey = altTitleDict.Keys.First();
                        var titleText = altTitleDict[langKey];

                        if (!string.IsNullOrEmpty(titleText))
                        {
                            if (!altTitlesDictionary.ContainsKey(langKey))
                            {
                                altTitlesDictionary[langKey] = new List<string>();
                            }
                            altTitlesDictionary[langKey].Add(titleText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề thay thế từ List.");
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
            if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
            if (altTitlesDictionary.TryGetValue("jp", out var jpTitles) && jpTitles.Any()) return jpTitles.First(); // Thường là romaji
            if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First(); // Romaji
            return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
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

                // Gọi API service mới
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);

                if (mangaResponse?.Result != "ok" || mangaResponse.Data?.Attributes == null)
                {
                     _logger.LogError($"Không lấy được thông tin hoặc attributes cho manga {mangaId}. Response: {mangaResponse?.Result}");
                     return "Không có tiêu đề"; // Trả về giá trị mặc định nếu lỗi
                }

                var attributes = mangaResponse.Data.Attributes;

                // Sử dụng các phương thức đã cập nhật để lấy tiêu đề
                string title = GetMangaTitle(attributes.Title, attributes.AltTitles);
                _logger.LogInformation($"Đã lấy tiêu đề manga {mangaId}: {title}");
                return title;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy tiêu đề manga {mangaId}");
                return "Không có tiêu đề";
            }
        }

        // --- Các phương thức tương thích ngược để duy trì API hiện tại ---

        public string GetDefaultMangaTitle(object titleObj)
        {
             if (titleObj is Dictionary<string, string> titleDict)
             {
                 return GetDefaultMangaTitle(titleDict);
             }
             // Fallback hoặc xử lý lỗi nếu kiểu không đúng
             _logger.LogWarning($"GetDefaultMangaTitle(object): Kiểu dữ liệu không mong đợi: {titleObj?.GetType()}");
             return "Không có tiêu đề";
        }

        public string GetMangaTitle(object titleObj, object altTitlesObj)
        {
             Dictionary<string, string>? titleDict = titleObj as Dictionary<string, string>;
             List<Dictionary<string, string>>? altTitlesList = null;

             // Cần xử lý altTitlesObj cẩn thận hơn vì nó là List<Dictionary<string, object>> từ json
             if (altTitlesObj is List<object> altObjList)
             {
                 altTitlesList = altObjList.OfType<Dictionary<string, object>>()
                                          .Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""))
                                          .ToList();
             }
             else if (altTitlesObj is List<Dictionary<string, string>> altDictList) // Trường hợp đã đúng kiểu
             {
                 altTitlesList = altDictList;
             }

             return GetMangaTitle(titleDict, altTitlesList);
        }

        public Dictionary<string, List<string>> GetAlternativeTitles(object altTitlesObj)
        {
             List<Dictionary<string, string>>? altTitlesList = null;
             if (altTitlesObj is List<object> altObjList)
             {
                 altTitlesList = altObjList.OfType<Dictionary<string, object>>()
                                          .Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""))
                                          .ToList();
             }
             else if (altTitlesObj is List<Dictionary<string, string>> altDictList)
             {
                 altTitlesList = altDictList;
             }
             return GetAlternativeTitles(altTitlesList);
        }
    }
}
