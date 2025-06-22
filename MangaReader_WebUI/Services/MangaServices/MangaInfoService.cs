using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class MangaInfoService : IMangaInfoService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ILogger<MangaInfoService> _logger;
        private readonly IMangaToInfoViewModelMapper _mangaToInfoViewModelMapper;

        public MangaInfoService(
            IMangaApiService mangaApiService,
            ILogger<MangaInfoService> logger,
            IMangaToInfoViewModelMapper mangaToInfoViewModelMapper)
        {
            _mangaApiService = mangaApiService;
            _logger = logger;
            _mangaToInfoViewModelMapper = mangaToInfoViewModelMapper;
        }

        public async Task<MangaInfoViewModel?> GetMangaInfoAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("MangaId không được cung cấp khi gọi GetMangaInfoAsync.");
                return null;
            }

            try
            {
                _logger.LogInformation($"Bắt đầu lấy thông tin cơ bản cho manga ID: {mangaId}");

                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);

                if (mangaResponse?.Result != "ok" || mangaResponse.Data == null)
                {
                    _logger.LogWarning($"Không thể lấy chi tiết manga {mangaId} trong MangaInfoService. Response: {mangaResponse?.Result}");
                    return new MangaInfoViewModel
                    {
                        MangaId = mangaId,
                        MangaTitle = $"Lỗi tải tiêu đề ({mangaId})",
                        CoverUrl = "/images/cover-placeholder.jpg"
                    };
                }

                var mangaInfoViewModel = _mangaToInfoViewModelMapper.MapToMangaInfoViewModel(mangaResponse.Data);
                
                _logger.LogInformation($"Lấy thông tin cơ bản thành công cho manga ID: {mangaId}");
                return mangaInfoViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin cơ bản cho manga ID: {mangaId}");
                return new MangaInfoViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = $"Lỗi lấy tiêu đề ({mangaId})",
                    CoverUrl = "/images/cover-placeholder.jpg"
                };
            }
        }
    }
} 