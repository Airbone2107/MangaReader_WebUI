using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaDescription
    {
        private readonly ILogger<MangaDescription> _logger;
        private readonly LocalizationService _localizationService;

        public MangaDescription(
            ILogger<MangaDescription> logger,
            LocalizationService localizationService)
        {
            _logger = logger;
            _localizationService = localizationService;
        }

        /// <summary>
        /// Lấy mô tả manga từ MangaAttributes (ưu tiên tiếng Việt, tiếng Anh)
        /// </summary>
        /// <param name="attributes">MangaAttributes chứa thông tin description</param>
        /// <returns>Mô tả đã được chọn theo ngôn ngữ ưu tiên</returns>
        public string GetDescription(MangaAttributes? attributes)
        {
            if (attributes?.Description == null || !attributes.Description.Any())
            {
                return "";
            }

            try
            {
                // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                if (attributes.Description.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
                {
                    return viDesc;
                }
                if (attributes.Description.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
                {
                    return enDesc;
                }

                // Lấy giá trị đầu tiên nếu không có vi/en
                return attributes.Description.FirstOrDefault().Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý description manga từ MangaAttributes.");
                return "";
            }
        }

        /// <summary>
        /// Lấy mô tả manga từ Dictionary attributes (phương thức cũ để tương thích ngược)
        /// </summary>
        /// <param name="attributesDict">Dictionary chứa thông tin attributes</param>
        /// <returns>Mô tả đã được chọn theo ngôn ngữ ưu tiên</returns>
        public string GetDescription(Dictionary<string, object> attributesDict)
        {
            var description = "";
            if (attributesDict.ContainsKey("description") && attributesDict["description"] != null)
            {
                // Thử lấy description từ attributesDict
                var descriptionObj = attributesDict["description"];
                
                if (descriptionObj is Dictionary<string, object> descriptionDict)
                {
                    // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
                    if (descriptionDict.ContainsKey("vi") && descriptionDict["vi"] != null)
                        description = descriptionDict["vi"].ToString();
                    // Nếu không có tiếng Việt, lấy tiếng Anh
                    else if (descriptionDict.ContainsKey("en") && descriptionDict["en"] != null)
                        description = descriptionDict["en"].ToString();
                    // Hoặc lấy giá trị đầu tiên nếu không có các ngôn ngữ ưu tiên
                    else if (descriptionDict.Count > 0)
                        description = descriptionDict.FirstOrDefault().Value?.ToString() ?? "";
                }
                else
                {
                    // Thử phương pháp khác nếu description không phải là Dictionary
                    try
                    {
                        // Sử dụng hàm GetLocalizedDescription với JSON serialized của description
                        string descriptionJson = JsonSerializer.Serialize(descriptionObj);
                        description = _localizationService.GetLocalizedDescription(descriptionJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi xử lý description manga từ JSON.");
                        description = "";
                    }
                }
            }
            return description;
        }
    }
}
