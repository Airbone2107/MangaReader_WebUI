using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibChapterPageClientService : IMangaReaderLibChapterPageClient
    {
        private readonly ChapterPageClient _innerClient;
        private readonly ILogger<MangaReaderLibChapterPageClientService> _wrapperLogger;

        public MangaReaderLibChapterPageClientService(
            IMangaReaderLibApiClient apiClient,
            ILogger<ChapterPageClient> innerClientLogger,
            ILogger<MangaReaderLibChapterPageClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new ChapterPageClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<CreateChapterPageEntryResponseDto>?> CreateChapterPageEntryAsync(Guid chapterId, CreateChapterPageEntryRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterPageClientService (Wrapper): Creating chapter page entry for chapter {ChapterId}, page {PageNumber}", chapterId, request.PageNumber);
            return await _innerClient.CreateChapterPageEntryAsync(chapterId, request, cancellationToken);
        }

        public async Task DeleteChapterPageAsync(Guid pageId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterPageClientService (Wrapper): Deleting chapter page {PageId}", pageId);
            await _innerClient.DeleteChapterPageAsync(pageId, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>>?> GetChapterPagesAsync(Guid chapterId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterPageClientService (Wrapper): Getting chapter pages for {ChapterId}", chapterId);
            return await _innerClient.GetChapterPagesAsync(chapterId, offset, limit, cancellationToken);
        }

        public async Task UpdateChapterPageDetailsAsync(Guid pageId, UpdateChapterPageDetailsRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterPageClientService (Wrapper): Updating chapter page details for {PageId}", pageId);
            await _innerClient.UpdateChapterPageDetailsAsync(pageId, request, cancellationToken);
        }

        public async Task<ApiResponse<UploadChapterPageImageResponseDto>?> UploadChapterPageImageAsync(Guid pageId, Stream imageStream, string fileName, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibChapterPageClientService (Wrapper): Uploading image for page {PageId}", pageId);
            return await _innerClient.UploadChapterPageImageAsync(pageId, imageStream, fileName, cancellationToken);
        }
    }
} 