using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using System.Globalization;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly ILogger<ChapterService> _logger;
        private readonly IMangaReaderLibToChapterViewModelMapper _chapterViewModelMapper;
        private readonly IMangaReaderLibToSimpleChapterInfoMapper _simpleChapterInfoMapper;

        public ChapterService(
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibChapterClient chapterClient,
            ILogger<ChapterService> logger,
            IMangaReaderLibToChapterViewModelMapper chapterViewModelMapper,
            IMangaReaderLibToSimpleChapterInfoMapper simpleChapterInfoMapper)
        {
            _mangaClient = mangaClient;
            _chapterClient = chapterClient;
            _logger = logger;
            _chapterViewModelMapper = chapterViewModelMapper;
            _simpleChapterInfoMapper = simpleChapterInfoMapper;
        }
        
        public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, List<string>? languages = null)
        {
            try
            {
                if (!Guid.TryParse(mangaId, out var mangaGuid))
                {
                    _logger.LogError("MangaId không hợp lệ: {MangaId}", mangaId);
                    return new List<ChapterViewModel>();
                }

                var translationsResponse = await _mangaClient.GetMangaTranslationsAsync(mangaGuid);
                if (translationsResponse?.Data == null || !translationsResponse.Data.Any())
                {
                    _logger.LogWarning("Không tìm thấy bản dịch nào cho manga {MangaId}", mangaId);
                    return new List<ChapterViewModel>();
                }
                
                var allChapterViewModels = new List<ChapterViewModel>();
                var targetTranslations = translationsResponse.Data;

                // Nếu có danh sách ngôn ngữ được chỉ định, lọc các bản dịch
                if (languages != null && languages.Any())
                {
                    targetTranslations = targetTranslations
                        .Where(t => languages.Contains(t.Attributes.LanguageKey, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }

                foreach(var translation in targetTranslations)
                {
                    if (Guid.TryParse(translation.Id, out var tmGuid))
                    {
                        var chapterListResponse = await _chapterClient.GetChaptersByTranslatedMangaAsync(tmGuid, limit: 5000);
                        if(chapterListResponse?.Data != null)
                        {
                            foreach (var chapterDto in chapterListResponse.Data)
                            {
                                allChapterViewModels.Add(_chapterViewModelMapper.MapToChapterViewModel(chapterDto, translation.Attributes.LanguageKey));
                            }
                        }
                    }
                }
                
                return SortChaptersByNumberDescending(allChapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chapters cho manga {MangaId}", mangaId);
                return new List<ChapterViewModel>();
            }
        }

        private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
        {
            return chapters
                .OrderByDescending(c => ParseChapterNumber(c.Number) ?? double.MinValue)
                .ThenByDescending(c => c.PublishedAt)
                .ToList();
        }

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

        private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
        {
            return chapters
                .OrderBy(c => ParseChapterNumber(c.Number) ?? double.MaxValue)
                .ThenBy(c => c.PublishedAt)
                .ToList();
        }

        public async Task<List<SimpleChapterInfoViewModel>> GetLatestChaptersAsync(string mangaId, int limit, List<string>? languages = null)
        {
            var allChapters = await GetChaptersAsync(mangaId, languages);
            return allChapters
                .OrderByDescending(c => c.PublishedAt)
                .Take(limit)
                .Select(vm => new SimpleChapterInfoViewModel { ChapterId = vm.Id, DisplayTitle = vm.Title, PublishedAt = vm.PublishedAt})
                .ToList();
        }

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
