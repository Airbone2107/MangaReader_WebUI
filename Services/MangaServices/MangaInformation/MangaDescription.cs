using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using manga_reader_web.Services.UtilityServices;

namespace manga_reader_web.Services.MangaServices.MangaInformation
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
        /// Lấy mô tả manga từ nhiều ngôn ngữ (ưu tiên tiếng Việt, tiếng Anh)
        /// </summary>
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
                    if (descriptionDict.ContainsKey("vi"))
                        description = descriptionDict["vi"].ToString();
                    // Nếu không có tiếng Việt, lấy tiếng Anh
                    else if (descriptionDict.ContainsKey("en"))
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
                        _logger.LogError($"Lỗi khi xử lý description manga: {ex.Message}");
                        description = "";
                    }
                }
            }
            return description;
        }
    }
}
