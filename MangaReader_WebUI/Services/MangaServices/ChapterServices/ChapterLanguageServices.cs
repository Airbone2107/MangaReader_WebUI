using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterLanguageServices
    {
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly IMangaReaderLibTranslatedMangaClient _translatedMangaClient;
        private readonly ILogger<ChapterLanguageServices> _logger;

        public ChapterLanguageServices(
            IMangaReaderLibChapterClient chapterClient,
            IMangaReaderLibTranslatedMangaClient translatedMangaClient,
            ILogger<ChapterLanguageServices> logger)
        {
            _chapterClient = chapterClient;
            _translatedMangaClient = translatedMangaClient;
            _logger = logger;
        }

        /// <summary>
        /// Lấy ngôn ngữ của chapter từ chapterID bằng cách gọi API
        /// </summary>
        /// <param name="chapterId">ID của chapter cần lấy thông tin</param>
        /// <returns>Mã ngôn ngữ (ví dụ: 'vi', 'en', 'jp',...) nếu tìm thấy, null nếu không tìm thấy</returns>
        public async Task<string> GetChapterLanguageAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId) || !Guid.TryParse(chapterId, out var chapterGuid))
            {
                _logger.LogWarning("ChapterId không hợp lệ khi gọi GetChapterLanguageAsync: {ChapterId}", chapterId);
                throw new ArgumentException("ChapterId không hợp lệ", nameof(chapterId));
            }

            _logger.LogInformation("Đang lấy thông tin ngôn ngữ cho Chapter: {ChapterId}", chapterId);

            try
            {
                var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);
                if (chapterResponse?.Data?.Relationships == null)
                {
                     _logger.LogError("Không lấy được thông tin hoặc relationships cho chapter {ChapterId}.", chapterId);
                    throw new InvalidOperationException($"Không thể lấy thông tin cho chapter {chapterId}");
                }

                var tmRelationship = chapterResponse.Data.Relationships.FirstOrDefault(r => r.Type.Equals("translated_manga", StringComparison.OrdinalIgnoreCase));
                if (tmRelationship != null && Guid.TryParse(tmRelationship.Id, out var tmGuid))
                {
                    var tmDetails = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                    if (!string.IsNullOrEmpty(tmDetails?.Data?.Attributes?.LanguageKey))
                    {
                        string lang = tmDetails.Data.Attributes.LanguageKey;
                        _logger.LogInformation("Đã lấy được ngôn ngữ: {Language} cho Chapter: {ChapterId}", lang, chapterId);
                        return lang;
                    }
                }

                _logger.LogWarning("Không thể xác định ngôn ngữ cho chapter {ChapterId}, trả về 'en' mặc định.", chapterId);
                return "en"; // Fallback
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy ngôn ngữ cho chapter {ChapterId}", chapterId);
                throw;
            }
        }
    }
}
