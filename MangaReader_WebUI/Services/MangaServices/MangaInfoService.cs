using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class MangaInfoService : IMangaInfoService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaInfoService> _logger;
        private readonly IMangaReaderLibToMangaInfoViewModelMapper _mangaToInfoViewModelMapper;

        public MangaInfoService(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<MangaInfoService> logger,
            IMangaReaderLibToMangaInfoViewModelMapper mangaToInfoViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaToInfoViewModelMapper = mangaToInfoViewModelMapper;
        }

        public async Task<MangaInfoViewModel?> GetMangaInfoAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId) || !Guid.TryParse(mangaId, out var mangaGuid))
            {
                _logger.LogWarning("MangaId không hợp lệ khi gọi GetMangaInfoAsync: {MangaId}", mangaId);
                return null;
            }

            try
            {
                _logger.LogInformation("Bắt đầu lấy thông tin cơ bản cho manga ID: {MangaId}", mangaId);

                // Yêu cầu include cover_art để có publicId
                var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid, new List<string> { "cover_art" });

                if (mangaResponse?.Data == null)
                {
                    _logger.LogWarning("Không thể lấy chi tiết manga {MangaId} trong MangaInfoService. API trả về null hoặc không có dữ liệu.", mangaId);
                    return new MangaInfoViewModel
                    {
                        MangaId = mangaId,
                        MangaTitle = $"Lỗi tải tiêu đề ({mangaId})",
                        CoverUrl = "/images/cover-placeholder.jpg"
                    };
                }

                var mangaInfoViewModel = _mangaToInfoViewModelMapper.MapToMangaInfoViewModel(mangaResponse.Data);
                
                _logger.LogInformation("Lấy thông tin cơ bản thành công cho manga ID: {MangaId}", mangaId);
                return mangaInfoViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin cơ bản cho manga ID: {MangaId}", mangaId);
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