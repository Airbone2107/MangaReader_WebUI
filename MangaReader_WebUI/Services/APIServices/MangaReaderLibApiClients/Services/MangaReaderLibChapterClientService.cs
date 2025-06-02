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
        private readonly ChapterClient _innerClient;
        private readonly ILogger<MangaReaderLibChapterClientService> _wrapperLogger;

        public MangaReaderLibChapterClientService(
            IMangaReaderLibApiClient apiClient, 
            ILogger<ChapterClient> innerClientLogger,
            ILogger<MangaReaderLibChapterClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new ChapterClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> CreateChapterAsync(CreateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Creating chapter for translated manga {TranslatedMangaId}", request.TranslatedMangaId);
            return await _innerClient.CreateChapterAsync(request, cancellationToken);
        }

        public async Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Deleting chapter {ChapterId}", chapterId);
            await _innerClient.DeleteChapterAsync(chapterId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> GetChapterByIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Getting chapter by ID {ChapterId}", chapterId);
            return await _innerClient.GetChapterByIdAsync(chapterId, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(Guid translatedMangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Getting chapters for translated manga {TranslatedMangaId}", translatedMangaId);
            return await _innerClient.GetChaptersByTranslatedMangaAsync(translatedMangaId, offset, limit, orderBy, ascending, cancellationToken);
        }

        public async Task UpdateChapterAsync(Guid chapterId, UpdateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterClientService (Wrapper): Updating chapter {ChapterId}", chapterId);
            await _innerClient.UpdateChapterAsync(chapterId, request, cancellationToken);
        }
    }
} 