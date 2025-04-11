using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using manga_reader_web.Models.Auth; // Cần UserModel
using manga_reader_web.Services.AuthServices;
using manga_reader_web.Services.MangaServices.ChapterServices;
using manga_reader_web.Services.MangaServices.Models;
using Microsoft.Extensions.Logging;

namespace manga_reader_web.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService; // THÊM: Thay thế MangaDetailsService
        private readonly ChapterService _chapterService; // Dùng để lấy chapter mới nhất
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550); // Khoảng delay (hơn 500ms để an toàn > 2 req/s)

        public FollowedMangaService(
            IUserService userService,
            IMangaInfoService mangaInfoService, // THÊM: Thay thế MangaDetailsService
            ChapterService chapterService,
            ILogger<FollowedMangaService> logger)
        {
            _userService = userService;
            _mangaInfoService = mangaInfoService; // THÊM: Thay thế MangaDetailsService
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

                _logger.LogInformation($"Người dùng đang theo dõi {user.FollowingManga.Count} manga. Bắt đầu lấy thông tin (sử dụng MangaInfoService)...");

                // Lặp qua danh sách ID manga đang theo dõi
                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        // 1. Áp dụng delay TRƯỚC khi gọi GetMangaInfoAsync (vì nó chứa 2 API call)
                        await Task.Delay(_rateLimitDelay);
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);

                        if (mangaInfo == null) // Kiểm tra nếu GetMangaInfoAsync trả về null (do lỗi)
                        {
                             _logger.LogWarning($"Không thể lấy thông tin cơ bản cho manga ID: {mangaId}. Bỏ qua.");
                             continue; // Bỏ qua manga này
                        }

                        // 2. Áp dụng delay TRƯỚC khi gọi GetLatestChaptersAsync
                        await Task.Delay(_rateLimitDelay);
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                        // Tạo ViewModel cho manga này
                        var followedManga = new FollowedMangaViewModel
                        {
                            MangaId = mangaId,
                            MangaTitle = mangaInfo.MangaTitle, // Lấy từ mangaInfo
                            CoverUrl = mangaInfo.CoverUrl,     // Lấy từ mangaInfo
                            LatestChapters = latestChapters ?? new List<SimpleChapterInfo>() // Đảm bảo không null
                        };

                        followedMangaList.Add(followedManga);
                        _logger.LogDebug($"Đã xử lý xong manga (qua InfoService): {mangaInfo.MangaTitle}");

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi (sử dụng InfoService).");
                        // Bỏ qua manga bị lỗi và tiếp tục với manga tiếp theo
                    }
                    // Không cần thêm delay nhỏ ở đây nữa vì đã có delay trước mỗi nhóm API call
                }

                _logger.LogInformation($"Hoàn tất lấy thông tin (qua InfoService) cho {followedMangaList.Count} truyện đang theo dõi.");
                return followedMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi (sử dụng InfoService).");
                // Có thể throw lại lỗi hoặc trả về danh sách rỗng tùy theo yêu cầu
                return new List<FollowedMangaViewModel>();
            }
        }
    }
} 