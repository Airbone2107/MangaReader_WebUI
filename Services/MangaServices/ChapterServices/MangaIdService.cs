using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class MangaIdService
    {
        private readonly IChapterApiService _chapterApiService;
        private readonly ILogger<MangaIdService> _logger;

        public MangaIdService(
            IChapterApiService chapterApiService,
            ILogger<MangaIdService> logger)
        {
            _chapterApiService = chapterApiService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy MangaID từ ChapterID bằng cách gọi API
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>MangaID nếu tìm thấy, null nếu không tìm thấy</returns>
        public async Task<string> GetMangaIdFromChapterAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                _logger.LogWarning("ChapterId không được cung cấp khi gọi GetMangaIdFromChapterAsync");
                throw new ArgumentNullException(nameof(chapterId), "ChapterId không được để trống");
            }

            _logger.LogInformation($"Đang lấy MangaID cho Chapter: {chapterId}");

            try
            {
                // Gọi API service mới
                var chapterResponse = await _chapterApiService.FetchChapterInfoAsync(chapterId);

                if (chapterResponse?.Result == "ok" && chapterResponse.Data?.Relationships != null)
                {
                    // Tìm relationship là manga
                    var mangaRelationship = chapterResponse.Data.Relationships
                                                .FirstOrDefault(r => r.Type == "manga");

                    if (mangaRelationship != null)
                    {
                        string mangaId = mangaRelationship.Id.ToString();
                        _logger.LogInformation($"Đã tìm thấy MangaID: {mangaId} cho Chapter: {chapterId}");
                        return mangaId;
                    }
                    else
                    {
                        _logger.LogWarning($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                        throw new KeyNotFoundException($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                    }
                }
                else
                {
                    _logger.LogError($"Không lấy được thông tin hoặc relationships cho chapter {chapterId}. Response: {chapterResponse?.Result}");
                    throw new InvalidOperationException($"Không thể lấy thông tin relationships cho chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy MangaID cho chapter {chapterId}");
                throw; // Ném lại lỗi
            }
        }
    }
}
