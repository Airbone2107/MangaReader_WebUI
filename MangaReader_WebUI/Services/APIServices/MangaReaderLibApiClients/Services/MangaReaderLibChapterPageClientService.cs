using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MangaReaderLib.Services.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibChapterPageClientService : IMangaReaderLibChapterPageClient
    {
        private readonly IChapterPageReader _innerReader;
        private readonly ILogger<MangaReaderLibChapterPageClientService> _wrapperLogger;

        public MangaReaderLibChapterPageClientService(
            IChapterPageReader chapterPageReader,
            ILogger<MangaReaderLibChapterPageClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = chapterPageReader;
        }

        public Task<ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>>?> GetChapterPagesAsync(Guid chapterId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterPageClientService (Wrapper): Getting chapter pages for {ChapterId}", chapterId);
            return _innerReader.GetChapterPagesAsync(chapterId, offset, limit, cancellationToken);
        }
    }
} 