using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.MangaServices.Models;
using MangaReader.WebUI.Services.APIServices.Services;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class MangaInfoService : IMangaInfoService
    {
        private readonly MangaTitleService _mangaTitleService;
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        private readonly ILogger<MangaInfoService> _logger;
        // Xem xét thêm rate limit nếu cần gọi nhiều lần liên tục từ các service khác nhau
        // private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550);

        public MangaInfoService(
            MangaTitleService mangaTitleService,
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<MangaInfoService> logger)
        {
            _mangaTitleService = mangaTitleService;
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
            _logger = logger;
        }

        public async Task<MangaInfoViewModel> GetMangaInfoAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("MangaId không được cung cấp khi gọi GetMangaInfoAsync.");
                return null;
            }

            try
            {
                _logger.LogInformation($"Bắt đầu lấy thông tin cơ bản cho manga ID: {mangaId}");

                // 1. Gọi API để lấy chi tiết Manga (bao gồm relationships và attributes)
                var mangaResponse = await _mangaApiService.FetchMangaDetailsAsync(mangaId);
                string mangaTitle = $"Manga ID: {mangaId}"; // Mặc định nếu lỗi
                string coverUrl = "/images/cover-placeholder.jpg"; // Mặc định

                if (mangaResponse?.Result == "ok" && mangaResponse.Data != null && mangaResponse.Data.Attributes != null)
                {
                    var attributes = mangaResponse.Data.Attributes;
                    var relationships = mangaResponse.Data.Relationships;

                    // 2. Lấy tiêu đề từ attributes
                    mangaTitle = _mangaTitleService.GetMangaTitle(attributes.Title, attributes.AltTitles);
                    if (string.IsNullOrEmpty(mangaTitle) || mangaTitle == "Không có tiêu đề")
                    {
                         _logger.LogWarning($"Không thể lấy tiêu đề cho manga ID: {mangaId}. Sử dụng ID làm tiêu đề.");
                         mangaTitle = $"Manga ID: {mangaId}";
                    }

                    // 3. Lấy cover từ relationships
                    // Truyền _logger vào hàm helper
                    var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(relationships, _logger);
                    if (!string.IsNullOrEmpty(coverFileName))
                    {
                        // Sử dụng instance _coverApiService để gọi GetProxiedCoverUrl
                        coverUrl = _coverApiService.GetProxiedCoverUrl(mangaId, coverFileName);
                    }
                    else
                    {
                         _logger.LogDebug($"Không tìm thấy cover filename cho manga ID {mangaId} từ relationships trong MangaInfoService.");
                    }
                }
                else
                {
                     _logger.LogWarning($"Không thể lấy chi tiết manga {mangaId} trong MangaInfoService. Response: {mangaResponse?.Result}");
                }

                _logger.LogInformation($"Lấy thông tin cơ bản thành công cho manga ID: {mangaId}");

                return new MangaInfoViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    CoverUrl = coverUrl
                };
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