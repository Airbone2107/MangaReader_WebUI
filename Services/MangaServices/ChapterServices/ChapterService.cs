using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.Models;
using MangaReader.WebUI.Services.UtilityServices;
using System.Text.Json;
using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly ILogger<ChapterService> _logger;
        private readonly string _backendBaseUrl; // Lấy base URL của backend

        public ChapterService(
            IChapterApiService chapterApiService,
            JsonConversionService jsonConversionService,
            IConfiguration configuration,
            ILogger<ChapterService> logger)
        {
            _chapterApiService = chapterApiService;
            _jsonConversionService = jsonConversionService;
            _logger = logger;
            _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
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
                // Gọi API service mới, yêu cầu lấy hết chapters (maxChapters = null)
                var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: null);
                var chapterViewModels = new List<ChapterViewModel>();

                if (chapterListResponse?.Data != null)
                {
                    foreach (var chapter in chapterListResponse.Data) // Lặp qua List<Chapter>
                    {
                        try
                        {
                            // Xử lý chapter thành ChapterViewModel
                            var chapterViewModel = ProcessChapter(chapter); // Truyền thẳng model Chapter
                            if (chapterViewModel != null)
                            {
                                chapterViewModels.Add(chapterViewModel);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapter?.Id}");
                            continue; // Bỏ qua chapter này và tiếp tục
                        }
                    }
                }
                else
                {
                     _logger.LogWarning($"Không có dữ liệu chapter trả về cho manga {mangaId} với ngôn ngữ {languages}.");
                }

                // Sắp xếp chapters theo thứ tự giảm dần
                return SortChaptersByNumberDescending(chapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}");
                return new List<ChapterViewModel>();
            }
        }

        /// <summary>
        /// Xử lý một chapter từ model Chapter thành ChapterViewModel
        /// </summary>
        /// <param name="chapter">Model Chapter</param>
        /// <returns>ChapterViewModel đã được xử lý, hoặc null nếu có lỗi</returns>
        private ChapterViewModel ProcessChapter(MangaReader.WebUI.Models.Mangadex.Chapter chapter)
        {
            try
            {
                if (chapter?.Attributes == null)
                {
                    _logger.LogWarning($"Chapter {chapter?.Id} không có attributes, bỏ qua");
                    return null;
                }

                var attributes = chapter.Attributes;

                // Lấy thông tin hiển thị (số chương, tiêu đề)
                var (displayTitle, chapterNumber) = GetChapterDisplayInfo(attributes);

                // Lấy các thông tin khác
                var language = attributes.TranslatedLanguage ?? "unknown";
                var publishedAt = attributes.PublishAt.DateTime; // Lấy DateTime từ DateTimeOffset

                // Xử lý relationships
                var relationships = ProcessChapterRelationships(chapter.Relationships);

                return new ChapterViewModel
                {
                    Id = chapter.Id.ToString(),
                    Title = displayTitle,
                    Number = chapterNumber,
                    Language = language,
                    PublishedAt = publishedAt,
                    Relationships = relationships
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapter?.Id}");
                return null;
            }
        }
        
        /// <summary>
        /// Lấy thông tin hiển thị của chapter (tiêu đề và số chương)
        /// </summary>
        /// <param name="attributes">ChapterAttributes chứa thuộc tính của chapter</param>
        /// <returns>Tuple gồm (displayTitle, chapterNumber)</returns>
        private (string displayTitle, string chapterNumber) GetChapterDisplayInfo(ChapterAttributes attributes)
        {
            string chapterNumber = attributes.ChapterNumber; // Có thể null
            string chapterTitle = attributes.Title; // Có thể null

            if (string.IsNullOrEmpty(chapterNumber))
            {
                 return (!string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot", chapterNumber);
            }

            var displayTitle = string.IsNullOrEmpty(chapterTitle) || chapterTitle == chapterNumber
                ? $"Chương {chapterNumber}"
                : $"Chương {chapterNumber}: {chapterTitle}";

            return (displayTitle, chapterNumber);
        }
        
        /// <summary>
        /// Xử lý relationships của chapter
        /// </summary>
        /// <param name="relationships">Danh sách relationships của chapter</param>
        /// <returns>Danh sách ChapterRelationship</returns>
        private List<ChapterRelationship> ProcessChapterRelationships(List<Relationship>? relationships)
        {
            var result = new List<ChapterRelationship>();
            if (relationships == null) return result;

            foreach (var relationship in relationships)
            {
                if (relationship != null && !string.IsNullOrEmpty(relationship.Type))
                {
                    result.Add(new ChapterRelationship
                    {
                        Id = relationship.Id.ToString(),
                        Type = relationship.Type
                    });
                }
            }
            return result;
        }
        
        /// <summary>
        /// Sắp xếp chapters theo số chương giảm dần
        /// </summary>
        /// <param name="chapters">Danh sách chapters cần sắp xếp</param>
        /// <returns>Danh sách chapters đã sắp xếp</returns>
        private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
        {
            return chapters
                .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                .OrderByDescending(c => c.Number ?? double.MinValue) // Đẩy null/không phải số về đầu khi giảm dần
                .Select(c => c.Chapter)
                .ToList();
        }
        
        /// <summary>
        /// Sắp xếp chapters theo số chương tăng dần
        /// </summary>
        /// <param name="chapters">Danh sách chapters cần sắp xếp</param>
        /// <returns>Danh sách chapters đã sắp xếp</returns>
        private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
        {
            return chapters
                .Select(c => new { Chapter = c, Number = ParseChapterNumber(c.Number) })
                .OrderBy(c => c.Number ?? double.MaxValue) // Đẩy null/không phải số về cuối khi tăng dần
                .Select(c => c.Chapter)
                .ToList();
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

            foreach (var language in chaptersByLanguage.Keys)
            {
                chaptersByLanguage[language] = SortChaptersByNumberAscending(chaptersByLanguage[language]);
            }

            return chaptersByLanguage;
        }

        /// <summary>
        /// Lấy thông tin của một chapter theo ID
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy</param>
        /// <returns>ChapterViewModel hoặc null nếu không tìm thấy</returns>
        public async Task<ChapterViewModel> GetChapterById(string chapterId)
        {
            try
            {
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);
                if (chapterResponse?.Result != "ok" || chapterResponse.Data == null)
                {
                    _logger.LogWarning($"Không tìm thấy chapter với ID: {chapterId} hoặc API lỗi.");
                    return null;
                }

                var chapterViewModel = ProcessChapter(chapterResponse.Data); // Xử lý model Chapter
                return chapterViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin chapter {chapterId}");
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách URL trang ảnh của chapter
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy trang</param>
        /// <returns>Danh sách URL trang ảnh</returns>
        public async Task<List<string>> GetChapterPages(string chapterId)
        {
            try
            {
                var atHomeResponse = await _chapterApiService.FetchChapterPagesAsync(chapterId);
                if (atHomeResponse == null || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null)
                {
                    _logger.LogWarning($"Không thể lấy thông tin trang ảnh cho chapter {chapterId}");
                    return new List<string>();
                }

                 // Mapping: Tạo danh sách URL ảnh đầy đủ qua proxy
                var pages = atHomeResponse.Chapter.Data
                    .Select(pageFile => $"{_backendBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString($"{atHomeResponse.BaseUrl}/data/{atHomeResponse.Chapter.Hash}/{pageFile}")}")
                    .ToList();

                return pages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy trang chapter {chapterId}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Lấy danh sách chapters mới nhất của một manga
        /// </summary>
        /// <param name="mangaId">ID của manga</param>
        /// <param name="limit">Số lượng chapters cần lấy</param>
        /// <param name="languages">Danh sách ngôn ngữ cần lấy (mặc định: "vi,en")</param>
        /// <returns>Danh sách SimpleChapterInfo</returns>
        public async Task<List<SimpleChapterInfo>> GetLatestChaptersAsync(string mangaId, int limit, string languages = "vi,en")
        {
            try
            {
                // Gọi API service mới
                var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: limit);
                var simpleChapters = new List<SimpleChapterInfo>();

                if (chapterListResponse?.Data != null)
                {
                    foreach (var chapter in chapterListResponse.Data)
                    {
                        try
                        {
                             if (chapter?.Attributes == null) continue;

                            var attributes = chapter.Attributes;
                            var (displayTitle, _) = GetChapterDisplayInfo(attributes); // Chỉ cần displayTitle
                            var publishedAt = attributes.PublishAt.DateTime;

                            simpleChapters.Add(new SimpleChapterInfo
                            {
                                ChapterId = chapter.Id.ToString(),
                                DisplayTitle = displayTitle,
                                PublishedAt = publishedAt
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapter?.Id} trong GetLatestChaptersAsync");
                            continue;
                        }
                    }
                }

                // API service đã giới hạn số lượng, không cần Take(limit) ở đây nữa
                // Sắp xếp theo ngày xuất bản giảm dần (API service nên làm điều này, nhưng kiểm tra lại)
                return simpleChapters
                    .OrderByDescending(c => c.PublishedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chapters mới nhất cho manga {mangaId}");
                return new List<SimpleChapterInfo>();
            }
        }

        // Helper để parse số chapter, trả về null nếu không parse được
        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
