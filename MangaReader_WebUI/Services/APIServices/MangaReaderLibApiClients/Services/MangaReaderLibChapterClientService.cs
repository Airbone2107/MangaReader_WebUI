using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibChapterClientService : IMangaReaderLibChapterClient
    {
        private readonly IChapterReader _innerReader;
        private readonly ILogger<MangaReaderLibChapterClientService> _wrapperLogger;

        public MangaReaderLibChapterClientService(
            IChapterReader chapterReader,
            ILogger<MangaReaderLibChapterClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = chapterReader;
        }

        public Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> GetChapterByIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Getting chapter by ID {ChapterId}", chapterId);
            return _innerReader.GetChapterByIdAsync(chapterId, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(Guid translatedMangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Getting chapters for translated manga {TranslatedMangaId}", translatedMangaId);
            return _innerReader.GetChaptersByTranslatedMangaAsync(translatedMangaId, offset, limit, orderBy, ascending, cancellationToken);
        }
    }
} 