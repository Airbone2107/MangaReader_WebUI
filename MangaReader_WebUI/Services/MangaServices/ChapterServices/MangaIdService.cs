using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class MangaIdService
    {
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly ILogger<MangaIdService> _logger;

        public MangaIdService(
            IMangaReaderLibChapterClient chapterClient,
            ILogger<MangaIdService> logger)
        {
            _chapterClient = chapterClient;
            _logger = logger;
        }

        /// <summary>
        /// Lấy MangaID từ ChapterID bằng cách gọi API
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>MangaID nếu tìm thấy, null nếu không tìm thấy</returns>
        public async Task<string> GetMangaIdFromChapterAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId) || !Guid.TryParse(chapterId, out var chapterGuid))
            {
                _logger.LogWarning("ChapterId không hợp lệ khi gọi GetMangaIdFromChapterAsync: {ChapterId}", chapterId);
                throw new ArgumentException("ChapterId không hợp lệ.", nameof(chapterId));
            }

            _logger.LogInformation("Đang lấy MangaID cho Chapter: {ChapterId}", chapterId);

            try
            {
                var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);

                if (chapterResponse?.Data?.Relationships != null)
                {
                    var mangaRelationship = chapterResponse.Data.Relationships
                                                .FirstOrDefault(r => r.Type.Equals("manga", StringComparison.OrdinalIgnoreCase));

                    if (mangaRelationship != null)
                    {
                        string mangaId = mangaRelationship.Id;
                        _logger.LogInformation("Đã tìm thấy MangaID: {MangaId} cho Chapter: {ChapterId}", mangaId, chapterId);
                        return mangaId;
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy relationship 'manga' cho Chapter: {ChapterId}", chapterId);
                        throw new KeyNotFoundException($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                    }
                }
                else
                {
                    _logger.LogError("Không lấy được thông tin hoặc relationships cho chapter {ChapterId}. Response: {Result}", chapterId, chapterResponse?.Result);
                    throw new InvalidOperationException($"Không thể lấy thông tin relationships cho chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy MangaID cho chapter {ChapterId}", chapterId);
                throw;
            }
        }
    }
}
