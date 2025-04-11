using manga_reader_web.Models;
using manga_reader_web.Services.MangaServices.Models;
using manga_reader_web.Services.UtilityServices;
using System.Text.Json;

namespace manga_reader_web.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly MangaDexService _mangaDexService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly ILogger<ChapterService> _logger;

        public ChapterService(
            MangaDexService mangaDexService,
            JsonConversionService jsonConversionService,
            ILogger<ChapterService> logger)
        {
            _mangaDexService = mangaDexService;
            _jsonConversionService = jsonConversionService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách chapters của một manga
        /// </summary>
        /// <param name="mangaId">ID của manga</param>
        /// <param name="languages">Danh sách ngôn ngữ cần lấy (mặc định: "vi,en")</param>
        /// <returns>Danh sách chapters đã được xử lý</returns>
        public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string languages = "vi,en")
        {
            try
            {
                var chapters = await _mangaDexService.FetchChaptersAsync(mangaId, languages);
                var chapterViewModels = new List<ChapterViewModel>();
                
                foreach (var chapter in chapters)
                {
                    try {
                        var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapter.ToString());
                        var chapterDict = _jsonConversionService.ConvertJsonElementToDict(chapterElement);
                        
                        // Xử lý chapter thành ChapterViewModel
                        var chapterViewModel = ProcessChapter(chapterDict);
                        if (chapterViewModel != null)
                        {
                            chapterViewModels.Add(chapterViewModel);
                        }
                    }
                    catch (Exception ex) {
                        _logger.LogError($"Lỗi khi xử lý chapter: {ex.Message}");
                        continue; // Bỏ qua chapter này và tiếp tục
                    }
                }
                
                // Sắp xếp chapters theo thứ tự giảm dần
                return SortChaptersByNumberDescending(chapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách chapters: {ex.Message}");
                return new List<ChapterViewModel>();
            }
        }

        /// <summary>
        /// Xử lý một chapter từ dữ liệu JSON thành ChapterViewModel
        /// </summary>
        /// <param name="chapterDict">Dictionary chứa dữ liệu chapter</param>
        /// <returns>ChapterViewModel đã được xử lý, hoặc null nếu có lỗi</returns>
        private ChapterViewModel ProcessChapter(Dictionary<string, object> chapterDict)
        {
            try
            {
                if (!chapterDict.ContainsKey("id"))
                {
                    _logger.LogWarning("Chapter không có ID, bỏ qua");
                    return null;
                }
                
                if (!chapterDict.ContainsKey("attributes") || chapterDict["attributes"] == null)
                {
                    _logger.LogWarning($"Chapter {chapterDict["id"]} không có attributes, bỏ qua");
                    return null;
                }
                
                var chapterAttributesDict = (Dictionary<string, object>)chapterDict["attributes"];
                
                // Lấy thông tin hiển thị (số chương, tiêu đề)
                var (displayTitle, chapterNumber) = GetChapterDisplayInfo(chapterAttributesDict);
                
                // Lấy các thông tin khác
                var language = GetChapterLanguage(chapterAttributesDict);
                var publishedAt = GetChapterPublishedDate(chapterAttributesDict);
                
                // Xử lý relationships
                var relationships = ProcessChapterRelationships(chapterDict);
                
                return new ChapterViewModel
                {
                    Id = chapterDict["id"].ToString(),
                    Title = displayTitle,
                    Number = chapterNumber,
                    Language = language,
                    PublishedAt = publishedAt,
                    Relationships = relationships
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý chapter: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Lấy thông tin hiển thị của chapter (tiêu đề và số chương)
        /// </summary>
        /// <param name="attributesDict">Dictionary chứa thuộc tính của chapter</param>
        /// <returns>Tuple gồm (displayTitle, chapterNumber)</returns>
        private (string displayTitle, string chapterNumber) GetChapterDisplayInfo(Dictionary<string, object> attributesDict)
        {
            // Lấy chapterNumber, cho phép null
            string chapterNumber = null;
            if (attributesDict.ContainsKey("chapter") && attributesDict["chapter"] != null)
            {
                chapterNumber = attributesDict["chapter"].ToString();
            }
                
            // Lấy chapterTitle, cho phép null
            string chapterTitle = null;
            if (attributesDict.ContainsKey("title") && attributesDict["title"] != null)
            {
                chapterTitle = attributesDict["title"].ToString();
            }
                
            // Nếu tên chương trùng số chương hoặc rỗng, chỉ hiển thị "Chương X"
            var displayTitle = string.IsNullOrEmpty(chapterTitle) || chapterTitle == chapterNumber 
                ? $"Chương {chapterNumber ?? "?"}" 
                : $"Chương {chapterNumber ?? "?"}: {chapterTitle}";
                
            return (displayTitle, chapterNumber);
        }
        
        /// <summary>
        /// Lấy ngôn ngữ của chapter
        /// </summary>
        /// <param name="attributesDict">Dictionary chứa thuộc tính của chapter</param>
        /// <returns>Mã ngôn ngữ của chapter</returns>
        private string GetChapterLanguage(Dictionary<string, object> attributesDict)
        {
            return attributesDict.ContainsKey("translatedLanguage") && attributesDict["translatedLanguage"] != null
                ? attributesDict["translatedLanguage"].ToString() 
                : "unknown";
        }
        
        /// <summary>
        /// Lấy ngày xuất bản của chapter
        /// </summary>
        /// <param name="attributesDict">Dictionary chứa thuộc tính của chapter</param>
        /// <returns>Ngày xuất bản của chapter</returns>
        private DateTime GetChapterPublishedDate(Dictionary<string, object> attributesDict)
        {
            return attributesDict.ContainsKey("publishAt") && attributesDict["publishAt"] != null
                ? DateTime.Parse(attributesDict["publishAt"].ToString()) 
                : DateTime.Now;
        }
        
        /// <summary>
        /// Xử lý relationships của chapter
        /// </summary>
        /// <param name="chapterDict">Dictionary chứa dữ liệu chapter</param>
        /// <returns>Danh sách relationships của chapter</returns>
        private List<ChapterRelationship> ProcessChapterRelationships(Dictionary<string, object> chapterDict)
        {
            var relationships = new List<ChapterRelationship>();
            
            if (!chapterDict.ContainsKey("relationships") || chapterDict["relationships"] == null)
            {
                return relationships;
            }
            
            var relationshipsArray = chapterDict["relationships"] as List<object>;
            if (relationshipsArray == null)
            {
                return relationships;
            }
            
            foreach (var relationship in relationshipsArray)
            {
                var relationshipDict = relationship as Dictionary<string, object>;
                if (relationshipDict != null && 
                    relationshipDict.ContainsKey("id") && 
                    relationshipDict.ContainsKey("type"))
                {
                    relationships.Add(new ChapterRelationship
                    {
                        Id = relationshipDict["id"].ToString(),
                        Type = relationshipDict["type"].ToString()
                    });
                }
            }
            
            return relationships;
        }
        
        /// <summary>
        /// Sắp xếp chapters theo số chương giảm dần
        /// </summary>
        /// <param name="chapters">Danh sách chapters cần sắp xếp</param>
        /// <returns>Danh sách chapters đã sắp xếp</returns>
        private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
        {
            return chapters.OrderByDescending(c => 
            {
                if (c.Number != null && float.TryParse(c.Number, out float number))
                    return number;
                return 0;
            }).ToList();
        }
        
        /// <summary>
        /// Sắp xếp chapters theo số chương tăng dần
        /// </summary>
        /// <param name="chapters">Danh sách chapters cần sắp xếp</param>
        /// <returns>Danh sách chapters đã sắp xếp</returns>
        private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
        {
            return chapters.OrderBy(c => 
            {
                if (c.Number != null && float.TryParse(c.Number, out float number))
                    return number;
                return 0;
            }).ToList();
        }

        /// <summary>
        /// Lấy danh sách chapters theo ngôn ngữ
        /// </summary>
        /// <param name="chapters">Danh sách tất cả chapters</param>
        /// <returns>Dictionary với key là ngôn ngữ, value là danh sách chapters theo ngôn ngữ đó</returns>
        public Dictionary<string, List<ChapterViewModel>> GetChaptersByLanguage(List<ChapterViewModel> chapters)
        {
            var chaptersByLanguage = chapters.GroupBy(c => c.Language)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            // Sắp xếp chapters theo thứ tự tăng dần của số chương cho mỗi ngôn ngữ
            foreach (var language in chaptersByLanguage.Keys)
            {
                var chaptersInLanguage = chaptersByLanguage[language];
                chaptersInLanguage = SortChaptersByNumberAscending(chaptersInLanguage);
                chaptersByLanguage[language] = chaptersInLanguage;
            }
            
            return chaptersByLanguage;
        }

        /// <summary>
        /// Lấy danh sách các chapter mới nhất của một manga.
        /// </summary>
        /// <param name="mangaId">ID của manga.</param>
        /// <param name="limit">Số lượng chapter tối đa cần lấy.</param>
        /// <param name="languages">Danh sách ngôn ngữ ưu tiên, phân tách bằng dấu phẩy.</param>
        /// <returns>Danh sách các SimpleChapterInfo.</returns>
        public async Task<List<SimpleChapterInfo>> GetLatestChaptersAsync(string mangaId, int limit, string languages = "vi,en")
        {
            if (string.IsNullOrEmpty(mangaId) || limit <= 0)
            {
                return new List<SimpleChapterInfo>();
            }

            try
            {
                _logger.LogInformation($"Đang lấy {limit} chapter mới nhất cho manga {mangaId} với ngôn ngữ [{languages}]");

                // Gọi MangaDexService để lấy chapters, yêu cầu sắp xếp theo publishAt giảm dần
                // Lưu ý: MangaDex API có thể sắp xếp theo 'publishAt' hoặc 'readableAt'
                // Sử dụng 'publishAt' để lấy theo ngày đăng tải chính thức
                var chaptersData = await _mangaDexService.FetchChaptersAsync(mangaId, languages, order: "desc", maxChapters: limit);

                var latestChapters = new List<SimpleChapterInfo>();

                foreach (var chapter in chaptersData)
                {
                    try
                    {
                        var chapterElement = JsonSerializer.Deserialize<JsonElement>(chapter.ToString());
                        var chapterDict = _jsonConversionService.ConvertJsonElementToDict(chapterElement);

                        if (!chapterDict.ContainsKey("id") || !chapterDict.ContainsKey("attributes")) continue;

                        var attributesDict = (Dictionary<string, object>)chapterDict["attributes"];
                        var (displayTitle, _) = GetChapterDisplayInfo(attributesDict); // Chỉ cần displayTitle
                        var publishedAt = GetChapterPublishedDate(attributesDict);

                        latestChapters.Add(new SimpleChapterInfo
                        {
                            ChapterId = chapterDict["id"].ToString(),
                            DisplayTitle = displayTitle,
                            PublishedAt = publishedAt
                        });

                        // Dừng lại khi đã đủ số lượng chapter yêu cầu
                        if (latestChapters.Count >= limit)
                        {
                            break;
                        }
                    }
                    catch (Exception exInner)
                    {
                        _logger.LogError(exInner, $"Lỗi khi xử lý dữ liệu chapter cho manga {mangaId}");
                        continue; // Bỏ qua chapter lỗi
                    }
                }

                _logger.LogInformation($"Đã lấy được {latestChapters.Count} chapter mới nhất cho manga {mangaId}");
                // Không cần sắp xếp lại vì đã yêu cầu API sắp xếp
                return latestChapters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chapter mới nhất cho manga {mangaId}");
                return new List<SimpleChapterInfo>(); // Trả về danh sách rỗng nếu có lỗi
            }
        }
    }
}
