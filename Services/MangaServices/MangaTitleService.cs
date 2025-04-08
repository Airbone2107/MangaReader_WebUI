using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using manga_reader_web.Services;

namespace manga_reader_web.Services.MangaServices
{
    public class MangaTitleService
    {
        private readonly LocalizationService _localizationService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly ILogger<MangaTitleService> _logger;

        public MangaTitleService(
            LocalizationService localizationService,
            JsonConversionService jsonConversionService,
            ILogger<MangaTitleService> logger)
        {
            _localizationService = localizationService;
            _jsonConversionService = jsonConversionService;
            _logger = logger;
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
                _logger.LogError($"Lỗi khi xử lý title manga: {ex.Message}");
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
                _logger.LogError($"Lỗi khi xử lý tiêu đề manga ưu tiên: {ex.Message}");
                return GetDefaultMangaTitle(titleObj);
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
    }
}
