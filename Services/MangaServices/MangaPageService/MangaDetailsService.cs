using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly LocalizationService _localizationService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly MangaUtilityService _mangaUtilityService;
        private readonly MangaTitleService _mangaTitleService;
        private readonly MangaTagService _mangaTagService;
        private readonly MangaRelationshipService _mangaRelationshipService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly ChapterService _chapterService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MangaDescription _mangaDescription;

        public MangaDetailsService(
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<MangaDetailsService> logger,
            LocalizationService localizationService,
            JsonConversionService jsonConversionService,
            MangaUtilityService mangaUtilityService,
            MangaTitleService mangaTitleService,
            MangaTagService mangaTagService,
            MangaRelationshipService mangaRelationshipService,
            IMangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            MangaDescription mangaDescription)
        {
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
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
                _logger.LogInformation($"Đang lấy chi tiết manga ID: {id}");
                // Gọi API service để lấy chi tiết manga
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);

                // Kiểm tra kết quả trả về từ API service
                if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                {
                    _logger.LogError($"Không thể lấy chi tiết manga {id}. Response: {mangaResponse?.Result}");
                    // Trả về ViewModel rỗng hoặc lỗi
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, Chapters = new List<ChapterViewModel>() };
                }

                var mangaData = mangaResponse.Data; // Lấy trực tiếp đối tượng Manga

                // Tạo MangaViewModel từ đối tượng Manga
                var mangaViewModel = await CreateMangaViewModelAsync(mangaData);

                // Lấy danh sách chapters
                var chapterViewModels = await GetChaptersAsync(id);

                return new MangaDetailViewModel
                {
                    Manga = mangaViewModel,
                    Chapters = chapterViewModels
                };
            }
            catch (JsonException jsonEx) // Bắt lỗi JSON cụ thể
            {
                _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chi tiết manga {id}: {jsonEx.Message}");
                // Trả về ViewModel lỗi
                return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi định dạng dữ liệu" }, Chapters = new List<ChapterViewModel>() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi nghiêm trọng khi lấy chi tiết manga {id}: {ex.Message}");
                // Trả về ViewModel lỗi
                return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, Chapters = new List<ChapterViewModel>() };
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
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);
                if (mangaResponse?.Result == "ok" && mangaResponse.Data?.Attributes?.AltTitles != null)
                {
                    // Gọi helper service với dữ liệu từ model mới
                    return _mangaTitleService.GetAlternativeTitles(mangaResponse.Data.Attributes.AltTitles);
                }
                return new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy tiêu đề thay thế cho manga {id}: {ex.Message}");
                return new Dictionary<string, List<string>>();
            }
        }

        /// <summary>
        /// Tạo đối tượng MangaViewModel từ dữ liệu manga
        /// </summary>
        private async Task<MangaViewModel> CreateMangaViewModelAsync(Manga? mangaData)
        {
            // Thêm kiểm tra null cho mangaData
            if (mangaData == null || mangaData.Attributes == null)
            {
                _logger.LogWarning($"Dữ liệu manga hoặc attributes bị null khi tạo ViewModel");
                // Trả về ViewModel lỗi hoặc mặc định
                return new MangaViewModel { Id = mangaData?.Id.ToString() ?? "unknown", Title = "Lỗi dữ liệu" };
            }

            string id = mangaData.Id.ToString();
            var attributes = mangaData.Attributes; // Sử dụng trực tiếp attributes

            try
            {
                // Lấy title
                string mangaTitle = _mangaTitleService.GetMangaTitle(attributes.Title, attributes.AltTitles);

                // Lưu title vào session (giữ nguyên)
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && !string.IsNullOrEmpty(mangaTitle))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaTitle);
                    _logger.LogInformation($"Đã lưu tiêu đề manga {id} vào session: {mangaTitle}");
                }

                // Lấy description
                string description = _mangaDescription.GetDescription(attributes);

                // Lấy tags
                var tags = _mangaTagService.GetMangaTags(attributes);

                // Lấy author/artist
                var (author, artist) = _mangaRelationshipService.GetAuthorArtist(mangaData.Relationships);

                // Lấy ảnh bìa
                string coverUrl = await _coverApiService.FetchCoverUrlAsync(id);
                if (string.IsNullOrEmpty(coverUrl))
                {
                    coverUrl = "/images/cover-placeholder.jpg"; // Ảnh mặc định
                }

                // Lấy trạng thái
                string status = _localizationService.GetStatus(attributes);

                // Lấy các thuộc tính khác trực tiếp
                string originalLanguage = attributes.OriginalLanguage ?? "";
                string publicationDemographic = attributes.PublicationDemographic ?? "";
                string contentRating = attributes.ContentRating ?? "";
                DateTime? lastUpdated = attributes.UpdatedAt.DateTime; // Truy cập trực tiếp DateTimeOffset
                string alternativeTitles = _mangaTitleService.GetPreferredAlternativeTitle(
                                            _mangaTitleService.GetAlternativeTitles(attributes.AltTitles));

                // Kiểm tra trạng thái follow
                bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);

                return new MangaViewModel
                {
                    Id = id,
                    Title = mangaTitle,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = status,
                    Tags = tags,
                    Author = author,
                    Artist = artist,
                    OriginalLanguage = originalLanguage,
                    PublicationDemographic = publicationDemographic,
                    ContentRating = contentRating,
                    AlternativeTitles = alternativeTitles,
                    LastUpdated = lastUpdated,
                    IsFollowing = isFollowing,
                    Rating = _mangaUtilityService.GetMangaRating(id), // Giữ nguyên logic giả
                    Views = 0 // Giữ nguyên
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tạo MangaViewModel cho ID: {id}: {ex.Message}");
                // Trả về ViewModel lỗi
                return new MangaViewModel { Id = id, Title = "Lỗi tạo ViewModel" };
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
                if (httpContext != null && chapterViewModels.Any()) // Chỉ lưu nếu có chapter
                {
                    // Phân loại chapters theo ngôn ngữ và lưu riêng từng ngôn ngữ
                    var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                    
                    foreach (var kvp in chaptersByLanguage)
                    {
                        httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{kvp.Key}", JsonSerializer.Serialize(kvp.Value));
                        _logger.LogInformation($"Đã lưu {kvp.Value.Count} chapters ngôn ngữ {kvp.Key} của manga {mangaId} vào session");
                    }
                }
                
                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}: {ex.Message}");
                return new List<ChapterViewModel>();
            }
        }
    }
}
