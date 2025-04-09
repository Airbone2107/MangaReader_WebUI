using manga_reader_web.Models;
using manga_reader_web.Services;
using manga_reader_web.Services.MangaServices;
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
                        var chapterAttributesDict = (Dictionary<string, object>)chapterDict["attributes"];
                        
                        // Lấy chapterNumber, cho phép null
                        string chapterNumber = null;
                        if (chapterAttributesDict.ContainsKey("chapter") && chapterAttributesDict["chapter"] != null)
                        {
                            chapterNumber = chapterAttributesDict["chapter"].ToString();
                        }
                            
                        // Lấy chapterTitle, cho phép null
                        string chapterTitle = null;
                        if (chapterAttributesDict.ContainsKey("title") && chapterAttributesDict["title"] != null)
                        {
                            chapterTitle = chapterAttributesDict["title"].ToString();
                        }
                            
                        // Nếu tên chương trùng số chương hoặc rỗng, chỉ hiển thị "Chương X"
                        var displayTitle = string.IsNullOrEmpty(chapterTitle) || chapterTitle == chapterNumber 
                            ? $"Chương {chapterNumber ?? "?"}" 
                            : $"Chương {chapterNumber ?? "?"}: {chapterTitle}";
                            
                        var publishedAt = chapterAttributesDict.ContainsKey("publishAt") && chapterAttributesDict["publishAt"] != null
                            ? DateTime.Parse(chapterAttributesDict["publishAt"].ToString()) 
                            : DateTime.Now;
                            
                        var language = chapterAttributesDict.ContainsKey("translatedLanguage") && chapterAttributesDict["translatedLanguage"] != null
                            ? chapterAttributesDict["translatedLanguage"].ToString() 
                            : "unknown";
                        
                        // Xử lý relationships
                        var relationships = new List<ChapterRelationship>();
                        if (chapterDict.ContainsKey("relationships") && chapterDict["relationships"] != null)
                        {
                            var relationshipsArray = chapterDict["relationships"] as List<object>;
                            if (relationshipsArray != null)
                            {
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
                            }
                        }
                        
                        chapterViewModels.Add(new ChapterViewModel
                        {
                            Id = chapterDict["id"].ToString(),
                            Title = displayTitle,
                            Number = chapterNumber,
                            Language = language,
                            PublishedAt = publishedAt,
                            Relationships = relationships
                        });
                    }
                    catch (Exception ex) {
                        _logger.LogError($"Lỗi khi xử lý chapter: {ex.Message}");
                        continue; // Bỏ qua chapter này và tiếp tục
                    }
                }
                
                // Sắp xếp chapters theo thứ tự giảm dần
                chapterViewModels = chapterViewModels.OrderByDescending(c => 
                {
                    if (c.Number != null && float.TryParse(c.Number, out float number))
                        return number;
                    return 0;
                }).ToList();
                
                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách chapters: {ex.Message}");
                return new List<ChapterViewModel>();
            }
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
                chaptersInLanguage = chaptersInLanguage.OrderBy(c => 
                {
                    if (c.Number != null && float.TryParse(c.Number, out float number))
                        return number;
                    return 0;
                }).ToList();
                
                chaptersByLanguage[language] = chaptersInLanguage;
            }
            
            return chaptersByLanguage;
        }
    }
}
