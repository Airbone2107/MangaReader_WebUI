using MangaReader.WebUI.Models.Auth;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly ChapterService _chapterService;
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550);
        private readonly IFollowedMangaViewModelMapper _followedMangaMapper;

        public FollowedMangaService(
            IUserService userService,
            IMangaInfoService mangaInfoService,
            ChapterService chapterService,
            ILogger<FollowedMangaService> logger,
            IFollowedMangaViewModelMapper followedMangaMapper)
        {
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _chapterService = chapterService;
            _logger = logger;
            _followedMangaMapper = followedMangaMapper;
        }

        public async Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync()
        {
            var followedMangaList = new List<FollowedMangaViewModel>();

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy danh sách theo dõi.");
                return followedMangaList;
            }

            try
            {
                UserModel user = await _userService.GetUserInfoAsync();
                if (user == null || user.FollowingManga == null || !user.FollowingManga.Any())
                {
                    _logger.LogInformation("Người dùng không theo dõi manga nào.");
                    return followedMangaList;
                }

                _logger.LogInformation("Người dùng đang theo dõi {Count} manga. Bắt đầu lấy thông tin...", user.FollowingManga.Count);

                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        await Task.Delay(_rateLimitDelay);
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);

                        if (mangaInfo == null)
                        {
                            _logger.LogWarning("Không thể lấy thông tin cơ bản cho manga ID: {MangaId}. Bỏ qua.", mangaId);
                            continue;
                        }

                        await Task.Delay(_rateLimitDelay);
                        // SỬA LỖI: Chuyển đổi chuỗi "vi,en" thành List<string>
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, new List<string> { "vi", "en" });

                        var followedMangaViewModel = _followedMangaMapper.MapToFollowedMangaViewModel(mangaInfo, latestChapters ?? new List<SimpleChapterInfoViewModel>());
                        followedMangaList.Add(followedMangaViewModel);
                        _logger.LogDebug("Đã xử lý xong manga: {MangaTitle}", mangaInfo.MangaTitle);

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, "Lỗi khi xử lý manga ID: {MangaId} trong danh sách theo dõi.", mangaId);
                    }
                }

                _logger.LogInformation("Hoàn tất lấy thông tin cho {Count} truyện đang theo dõi.", followedMangaList.Count);
                return followedMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi.");
                return new List<FollowedMangaViewModel>();
            }
        }
    }
}