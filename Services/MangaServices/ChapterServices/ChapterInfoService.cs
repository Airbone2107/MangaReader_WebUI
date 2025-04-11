using manga_reader_web.Services.MangaServices.Models;
using manga_reader_web.Services.UtilityServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace manga_reader_web.Services.MangaServices.ChapterServices
{
    public class ChapterInfoService : IChapterInfoService
    {
        private readonly MangaDexService _mangaDexService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly ChapterAttributeService _chapterAttributeService;
        private readonly ILogger<ChapterInfoService> _logger;

        public ChapterInfoService(
            MangaDexService mangaDexService,
            JsonConversionService jsonConversionService,
            ChapterAttributeService chapterAttributeService,
            ILogger<ChapterInfoService> logger)
        {
            _mangaDexService = mangaDexService;
            _jsonConversionService = jsonConversionService;
            _chapterAttributeService = chapterAttributeService;
            _logger = logger;
        }

        public async Task<ChapterInfo> GetChapterInfoAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi GetChapterInfoAsync.");
                return null;
            }

            try
            {
                _logger.LogInformation($"Đang lấy thông tin chi tiết cho chapter ID: {chapterId}");
                
                // Sử dụng ChapterAttributeService để lấy từng thuộc tính một cách an toàn
                string chapterNumber = await _chapterAttributeService.GetChapterNumberAsync(chapterId);
                string chapterTitle = await _chapterAttributeService.GetChapterTitleAsync(chapterId);
                DateTime publishedAt = await _chapterAttributeService.GetPublishedAtAsync(chapterId);
                
                // Tạo tiêu đề hiển thị sử dụng phương thức từ ChapterAttributeService
                string displayTitle = _chapterAttributeService.CreateDisplayTitle(chapterNumber, chapterTitle);

                return new ChapterInfo
                {
                    Id = chapterId,
                    Title = displayTitle,
                    PublishedAt = publishedAt
                };
            }
            catch (Exception ex)
            {
                // Log lỗi cụ thể hơn nếu có thể
                if (ex is JsonException jsonEx) {
                    _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chapter ID: {chapterId}");
                } else if (ex is HttpRequestException httpEx) {
                     _logger.LogError(httpEx, $"Lỗi HTTP khi lấy chapter ID: {chapterId}");
                }
                else {
                    _logger.LogError(ex, $"Lỗi ngoại lệ không xác định khi lấy thông tin chi tiết cho chapter ID: {chapterId}: {ex.Message}");
                }
                return null; // Trả về null nếu có lỗi
            }
        }
    }
} 