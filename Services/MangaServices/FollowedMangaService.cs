using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using manga_reader_web.Models.Auth; // Cần UserModel
using manga_reader_web.Services.AuthServices;
using manga_reader_web.Services.MangaServices.ChapterServices;
using manga_reader_web.Services.MangaServices.MangaPageService; // Cần MangaDetailsService
using manga_reader_web.Services.MangaServices.Models;
using Microsoft.Extensions.Logging;

namespace manga_reader_web.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly MangaDetailsService _mangaDetailsService; // Dùng để lấy thông tin manga
        private readonly ChapterService _chapterService; // Dùng để lấy chapter mới nhất
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550); // Khoảng delay (hơn 500ms để an toàn > 2 req/s)

        public FollowedMangaService(
            IUserService userService,
            MangaDetailsService mangaDetailsService,
            ChapterService chapterService,
            ILogger<FollowedMangaService> logger)
        {
            _userService = userService;
            _mangaDetailsService = mangaDetailsService;
            _chapterService = chapterService;
            _logger = logger;
        }

        public async Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync()
        {
            var followedMangaList = new List<FollowedMangaViewModel>();

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy danh sách theo dõi.");
                return followedMangaList; // Trả về danh sách rỗng
            }

            try
            {
                // Lấy thông tin người dùng, bao gồm danh sách manga đang theo dõi
                UserModel user = await _userService.GetUserInfoAsync();
                if (user == null || user.FollowingManga == null || !user.FollowingManga.Any())
                {
                    _logger.LogInformation("Người dùng không theo dõi manga nào.");
                    return followedMangaList;
                }

                _logger.LogInformation($"Người dùng đang theo dõi {user.FollowingManga.Count} manga. Bắt đầu lấy thông tin...");

                // Lặp qua danh sách ID manga đang theo dõi
                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        // Lấy thông tin chi tiết manga (chỉ cần title, cover)
                        // Cân nhắc tạo phương thức tối ưu hơn trong MangaDetailsService nếu cần
                        var mangaDetails = await _mangaDetailsService.GetMangaDetailsAsync(mangaId);

                        if (mangaDetails?.Manga == null)
                        {
                            _logger.LogWarning($"Không thể lấy thông tin chi tiết cho manga ID: {mangaId}");
                            continue; // Bỏ qua manga này nếu không lấy được thông tin
                        }

                        // Lấy 3 chapter mới nhất
                        // **QUAN TRỌNG: Áp dụng Rate Limit**
                        await Task.Delay(_rateLimitDelay); // Chờ trước khi gọi API chapter
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en"); // Ưu tiên Việt, Anh

                        // Tạo ViewModel cho manga này
                        var followedManga = new FollowedMangaViewModel
                        {
                            MangaId = mangaId,
                            MangaTitle = mangaDetails.Manga.Title,
                            CoverUrl = mangaDetails.Manga.CoverUrl,
                            LatestChapters = latestChapters ?? new List<SimpleChapterInfo>() // Đảm bảo không null
                        };

                        followedMangaList.Add(followedManga);
                        _logger.LogDebug($"Đã xử lý xong manga: {mangaDetails.Manga.Title}");

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi.");
                        // Bỏ qua manga bị lỗi và tiếp tục với manga tiếp theo
                    }
                }

                _logger.LogInformation($"Hoàn tất lấy thông tin cho {followedMangaList.Count} truyện đang theo dõi.");
                return followedMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi.");
                // Có thể throw lại lỗi hoặc trả về danh sách rỗng tùy theo yêu cầu
                return new List<FollowedMangaViewModel>();
            }
        }
    }
} 