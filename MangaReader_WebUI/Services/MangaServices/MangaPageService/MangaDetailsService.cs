using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly ChapterService _chapterService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMangaReaderLibToMangaDetailViewModelMapper _mangaDetailViewModelMapper;

        public MangaDetailsService(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<MangaDetailsService> logger,
            IMangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IMangaReaderLibToMangaDetailViewModelMapper mangaDetailViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaDetailViewModelMapper = mangaDetailViewModelMapper;
        }

        /// <summary>
        /// Lấy thông tin chi tiết manga từ API
        /// </summary>
        public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var mangaGuid))
                {
                    _logger.LogError("MangaId không hợp lệ: {MangaId}", id);
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "ID Manga không hợp lệ" } };
                }

                _logger.LogInformation("Đang lấy chi tiết manga ID: {id}", id);
                var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid, new List<string> { "author", "cover_art", "tag" });

                if (mangaResponse?.Data == null)
                {
                    _logger.LogError("Không thể lấy chi tiết manga {id}. API không trả về dữ liệu.", id);
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" } };
                }

                var mangaData = mangaResponse.Data;
                var chapterViewModels = await GetChaptersAsync(id);
                var mangaDetailViewModel = await _mangaDetailViewModelMapper.MapToMangaDetailViewModelAsync(mangaData, chapterViewModels);

                if (mangaDetailViewModel.Manga != null)
                {
                    mangaDetailViewModel.Manga.IsFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                }

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && mangaDetailViewModel.Manga != null && !string.IsNullOrEmpty(mangaDetailViewModel.Manga.Title))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaDetailViewModel.Manga.Title);
                }

                return mangaDetailViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy chi tiết manga {id}", id);
                return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" } };
            }
        }

        /// <summary>
        /// Lấy danh sách chapters của manga
        /// </summary>
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId)
        {
            try
            {
                var chapterViewModels = await _chapterService.GetChaptersAsync(mangaId, "vi,en");
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && chapterViewModels.Any())
                {
                    var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                    foreach (var kvp in chaptersByLanguage)
                    {
                        httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{kvp.Key}", JsonSerializer.Serialize(kvp.Value));
                    }
                }
                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chapters cho manga {mangaId}", mangaId);
                return new List<ChapterViewModel>();
            }
        }
    }
}
