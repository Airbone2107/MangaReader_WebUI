using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.MangaServices.Models;

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

                // Sử dụng Task.WhenAll để thực hiện các cuộc gọi API song song (nếu có thể và an toàn về rate limit)
                // Tuy nhiên, để đảm bảo tuân thủ rate limit, gọi tuần tự có thể an toàn hơn.
                // Hoặc thêm delay vào đây nếu service này được gọi nhiều lần liên tiếp.

                // 1. Lấy tiêu đề manga
                string mangaTitle = await _mangaTitleService.GetMangaTitleFromIdAsync(mangaId);
                if (string.IsNullOrEmpty(mangaTitle) || mangaTitle == "Không có tiêu đề")
                {
                    _logger.LogWarning($"Không thể lấy tiêu đề cho manga ID: {mangaId}. Sử dụng ID làm tiêu đề.");
                    mangaTitle = $"Manga ID: {mangaId}";
                }

                // await Task.Delay(_rateLimitDelay); // Bỏ comment nếu cần delay giữa 2 API call

                // 2. Lấy ảnh bìa
                string coverUrl = await _coverApiService.FetchCoverUrlAsync(mangaId);

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
                // Trả về null hoặc một đối tượng mặc định tùy theo yêu cầu xử lý lỗi
                return new MangaInfoViewModel // Trả về object với thông tin mặc định/lỗi
                {
                     MangaId = mangaId,
                     MangaTitle = $"Lỗi lấy tiêu đề ({mangaId})",
                     CoverUrl = "/images/cover-placeholder.jpg" // Ảnh mặc định
                };
            }
        }
    }
} 