using MangaReader.WebUI.Models.ViewModels.Chapter; // ViewModel mới
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly ILogger<ChapterService> _logger;
        private readonly string _backendBaseUrl; // Lấy base URL của backend
        private readonly IChapterToChapterViewModelMapper _chapterViewModelMapper;
        private readonly IChapterToSimpleInfoMapper _simpleChapterInfoMapper;

        public ChapterService(
            IChapterApiService chapterApiService,
            IConfiguration configuration,
            ILogger<ChapterService> logger,
            IChapterToChapterViewModelMapper chapterViewModelMapper,
            IChapterToSimpleInfoMapper simpleChapterInfoMapper
            )
        {
            _chapterApiService = chapterApiService;
            _logger = logger;
            _backendBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
            _chapterViewModelMapper = chapterViewModelMapper;
            _simpleChapterInfoMapper = simpleChapterInfoMapper;
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
                var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: null);
                var chapterViewModels = new List<ChapterViewModel>();

                if (chapterListResponse?.Data != null)
                {
                    foreach (var chapterData in chapterListResponse.Data)
                    {
                        try
                        {
                            var chapterViewModel = _chapterViewModelMapper.MapToChapterViewModel(chapterData);
                            if (chapterViewModel != null)
                            {
                                chapterViewModels.Add(chapterViewModel);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapterData?.Id}");
                            continue;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Không có dữ liệu chapter trả về cho manga {mangaId} với ngôn ngữ {languages}.");
                }
                return SortChaptersByNumberDescending(chapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách chapters cho manga {mangaId}");
                return new List<ChapterViewModel>();
            }
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
        public async Task<ChapterViewModel?> GetChapterById(string chapterId)
        {
            try
            {
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);
                if (chapterResponse?.Result != "ok" || chapterResponse.Data == null)
                {
                    _logger.LogWarning($"Không tìm thấy chapter với ID: {chapterId} hoặc API lỗi.");
                    return null;
                }
                return _chapterViewModelMapper.MapToChapterViewModel(chapterResponse.Data);
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
        public async Task<List<SimpleChapterInfoViewModel>> GetLatestChaptersAsync(string mangaId, int limit, string languages = "vi,en")
        {
            try
            {
                var chapterListResponse = await _chapterApiService.FetchChaptersAsync(mangaId, languages, maxChapters: limit);
                var simpleChapters = new List<SimpleChapterInfoViewModel>();

                if (chapterListResponse?.Data != null)
                {
                    foreach (var chapterData in chapterListResponse.Data)
                    {
                        try
                        {
                            var simpleInfo = _simpleChapterInfoMapper.MapToSimpleChapterInfo(chapterData);
                            simpleChapters.Add(simpleInfo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi xử lý chapter ID: {chapterData?.Id} trong GetLatestChaptersAsync");
                            continue;
                        }
                    }
                }
                return simpleChapters
                    .OrderByDescending(c => c.PublishedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chapters mới nhất cho manga {mangaId}");
                return new List<SimpleChapterInfoViewModel>();
            }
        }

        // Helper để parse số chapter, trả về null nếu không parse được
        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
