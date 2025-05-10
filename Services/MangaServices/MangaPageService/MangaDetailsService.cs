using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly ChapterService _chapterService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMangaToDetailViewModelMapper _mangaDetailViewModelMapper;
        private readonly IMangaDataExtractor _mangaDataExtractor;

        public MangaDetailsService(
            IMangaApiService mangaApiService,
            ILogger<MangaDetailsService> logger,
            IMangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IMangaToDetailViewModelMapper mangaDetailViewModelMapper,
            IMangaDataExtractor mangaDataExtractor)
        {
            _mangaApiService = mangaApiService;
            _logger = logger;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaDetailViewModelMapper = mangaDetailViewModelMapper;
            _mangaDataExtractor = mangaDataExtractor;
        }

        /// <summary>
        /// Lấy thông tin chi tiết manga từ API
        /// </summary>
        public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
        {
            try
            {
                _logger.LogInformation($"Đang lấy chi tiết manga ID: {id}");
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(id);

                if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                {
                    _logger.LogError($"Không thể lấy chi tiết manga {id}. Response: {mangaResponse?.Result}");
                    // Trả về ViewModel rỗng với Dictionary rỗng
                    return new MangaDetailViewModel { 
                        Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, 
                        Chapters = new List<ChapterViewModel>(), 
                        AlternativeTitlesByLanguage = new Dictionary<string, List<string>>() 
                    };
                }

                var mangaData = mangaResponse.Data;
                var chapterViewModels = await GetChaptersAsync(id);

                // Sử dụng mapper mới
                var mangaDetailViewModel = await _mangaDetailViewModelMapper.MapToMangaDetailViewModelAsync(mangaData, chapterViewModels);

                // Xử lý IsFollowing nếu mapper không tự xử lý
                if (mangaDetailViewModel.Manga != null)
                {
                    mangaDetailViewModel.Manga.IsFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                }

                // Lưu title vào session (nếu cần)
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && mangaDetailViewModel.Manga != null && !string.IsNullOrEmpty(mangaDetailViewModel.Manga.Title))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaDetailViewModel.Manga.Title);
                    _logger.LogInformation($"Đã lưu tiêu đề manga {id} vào session: {mangaDetailViewModel.Manga.Title}");
                }

                return mangaDetailViewModel;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chi tiết manga {id}: {jsonEx.Message}");
                return new MangaDetailViewModel { 
                    Manga = new MangaViewModel { Id = id, Title = "Lỗi định dạng dữ liệu" }, 
                    Chapters = new List<ChapterViewModel>(), 
                    AlternativeTitlesByLanguage = new Dictionary<string, List<string>>() 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi nghiêm trọng khi lấy chi tiết manga {id}: {ex.Message}");
                return new MangaDetailViewModel { 
                    Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" }, 
                    Chapters = new List<ChapterViewModel>(), 
                    AlternativeTitlesByLanguage = new Dictionary<string, List<string>>() 
                };
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
