using MangaReader.WebUI.Models;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly LocalizationService _localizationService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly MangaUtilityService _mangaUtilityService;
        private readonly MangaTitleService _mangaTitleService;
        private readonly MangaTagService _mangaTagService;
        private readonly MangaRelationshipService _mangaRelationshipService;
        private readonly MangaFollowService _mangaFollowService;
        private readonly ChapterService _chapterService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MangaDescription _mangaDescription;

        public MangaDetailsService(
            MangaDexService mangaDexService,
            ILogger<MangaDetailsService> logger,
            LocalizationService localizationService,
            JsonConversionService jsonConversionService,
            MangaUtilityService mangaUtilityService,
            MangaTitleService mangaTitleService,
            MangaTagService mangaTagService,
            MangaRelationshipService mangaRelationshipService,
            MangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            MangaDescription mangaDescription)
        {
            _mangaDexService = mangaDexService;
            _logger = logger;
            _localizationService = localizationService;
            _jsonConversionService = jsonConversionService;
            _mangaUtilityService = mangaUtilityService;
            _mangaTitleService = mangaTitleService;
            _mangaTagService = mangaTagService;
            _mangaRelationshipService = mangaRelationshipService;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaDescription = mangaDescription;
        }

        /// <summary>
        /// Lấy thông tin chi tiết manga từ API
        /// </summary>
        public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
        {
            try
            {
                var manga = await _mangaDexService.FetchMangaDetailsAsync(id);
                var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                var mangaDict = _jsonConversionService.ConvertJsonElementToDict(mangaElement);
                
                var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                
                // Tạo MangaViewModel với thông tin chi tiết
                var mangaViewModel = await CreateMangaViewModelAsync(id, mangaDict, attributesDict);
                
                // Lấy danh sách chapters
                var chapterViewModels = await GetChaptersAsync(id);
                
                return new MangaDetailViewModel
                {
                    Manga = mangaViewModel,
                    Chapters = chapterViewModels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy chi tiết manga: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy danh sách tiêu đề thay thế theo ngôn ngữ cho manga
        /// </summary>
        /// <param name="id">ID của manga</param>
        /// <returns>Dictionary chứa các tiêu đề thay thế được nhóm theo mã ngôn ngữ</returns>
        public async Task<Dictionary<string, List<string>>> GetAlternativeTitlesByLanguageAsync(string id)
        {
            try
            {
                var manga = await _mangaDexService.FetchMangaDetailsAsync(id);
                var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                var mangaDict = _jsonConversionService.ConvertJsonElementToDict(mangaElement);
                
                if (mangaDict.ContainsKey("attributes") && mangaDict["attributes"] != null)
                {
                    var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                    if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                    {
                        return _mangaTitleService.GetAlternativeTitles(attributesDict["altTitles"]);
                    }
                }
                
                // Trả về dictionary rỗng nếu không có tiêu đề thay thế
                return new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy tiêu đề thay thế cho manga {id}: {ex.Message}");
                return new Dictionary<string, List<string>>();
            }
        }

        /// <summary>
        /// Tạo đối tượng MangaViewModel từ dữ liệu manga
        /// </summary>
        private async Task<MangaViewModel> CreateMangaViewModelAsync(string id, Dictionary<string, object> mangaDict, Dictionary<string, object> attributesDict)
        {
            try
            {    
                // Lấy title của manga
                string mangaTitle = _mangaTitleService.GetMangaTitle(attributesDict["title"], attributesDict["altTitles"]);
                
                // Lưu tiêu đề manga vào session để tái sử dụng trong ChapterController
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && !string.IsNullOrEmpty(mangaTitle))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaTitle);
                    _logger.LogInformation($"Đã lưu tiêu đề manga {id} vào session: {mangaTitle}");
                }
                
                // Lấy mô tả của manga từ dịch vụ MangaDescription
                string description = _mangaDescription.GetDescription(attributesDict);
                
                // Xử lý các thuộc tính khác
                string originalLanguage = attributesDict.ContainsKey("originalLanguage") ? attributesDict["originalLanguage"]?.ToString() : "";
                string publicationDemographic = attributesDict.ContainsKey("publicationDemographic") ? attributesDict["publicationDemographic"]?.ToString() : "";
                string contentRating = attributesDict.ContainsKey("contentRating") ? attributesDict["contentRating"]?.ToString() : "";
                
                // Xử lý tiêu đề thay thế
                string alternativeTitles = "";
                var altTitlesDictionary = new Dictionary<string, List<string>>();

                if (attributesDict.ContainsKey("altTitles") && attributesDict["altTitles"] != null)
                {
                    try
                    {
                        // Sử dụng MangaTitleService để xử lý tiêu đề thay thế
                        altTitlesDictionary = _mangaTitleService.GetAlternativeTitles(attributesDict["altTitles"]);
                        
                        // Lấy tiêu đề thay thế ưu tiên
                        alternativeTitles = _mangaTitleService.GetPreferredAlternativeTitle(altTitlesDictionary);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xử lý tiêu đề thay thế: {ex.Message}");
                    }
                }
                
                // Xử lý tags
                var tags = _mangaTagService.GetMangaTags(mangaDict);
                
                // Tải ảnh bìa
                string coverUrl = await _mangaDexService.FetchCoverUrlAsync(id);
                
                // Xử lý thời gian cập nhật
                DateTime? lastUpdated = null;
                if (attributesDict.ContainsKey("updatedAt") && attributesDict["updatedAt"] != null)
                {
                    if (DateTime.TryParse(attributesDict["updatedAt"].ToString(), out DateTime updatedAt))
                    {
                        lastUpdated = updatedAt;
                    }
                }

                // Xử lý tác giả và họa sĩ
                var result = (ValueTuple<string, string>)_mangaRelationshipService.GetAuthorArtist(mangaDict);
                var (author, artist) = result;

                // Kiểm tra trạng thái follow
                bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                
                return new MangaViewModel
                {
                    Id = id,
                    Title = mangaTitle,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = attributesDict.ContainsKey("status") ? attributesDict["status"].ToString() : "unknown",
                    Tags = tags,
                    Author = author,
                    Artist = artist,
                    OriginalLanguage = originalLanguage,
                    PublicationDemographic = publicationDemographic,
                    ContentRating = contentRating,
                    AlternativeTitles = alternativeTitles,
                    LastUpdated = lastUpdated,
                    IsFollowing = isFollowing,
                    // Các thông tin hiển thị phụ trợ
                    Rating = _mangaUtilityService.GetMangaRating(id),
                    Views = 0 // MangaDex không cung cấp thông tin số lượt xem
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tạo MangaViewModel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy danh sách chapters của manga
        /// </summary>
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId)
        {
            try
            {
                // Lấy danh sách chapters từ ChapterService
                var chapterViewModels = await _chapterService.GetChaptersAsync(mangaId, "vi,en");
                
                // Lưu danh sách tất cả chapters vào session storage
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    httpContext.Session.SetString($"Manga_{mangaId}_AllChapters", JsonSerializer.Serialize(chapterViewModels));
                    _logger.LogInformation($"Đã lưu {chapterViewModels.Count} chapters của manga {mangaId} vào session");
                    
                    // Phân loại chapters theo ngôn ngữ và lưu riêng từng ngôn ngữ
                    var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                    
                    foreach (var language in chaptersByLanguage.Keys)
                    {
                        var chaptersInLanguage = chaptersByLanguage[language];
                        httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                        _logger.LogInformation($"Đã lưu {chaptersInLanguage.Count} chapters ngôn ngữ {language} của manga {mangaId} vào session");
                    }
                }
                
                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách chapters: {ex.Message}");
                return new List<ChapterViewModel>();
            }
        }
    }
}
