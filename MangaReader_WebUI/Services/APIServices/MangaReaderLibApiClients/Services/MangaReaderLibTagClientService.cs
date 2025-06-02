using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;
using System.Text;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibTagClientService : IMangaReaderLibTagClient
    {
        private readonly TagClient _innerClient;
        private readonly ILogger<MangaReaderLibTagClientService> _wrapperLogger;

        public MangaReaderLibTagClientService(
            IMangaReaderLibApiClient apiClient,
            ILogger<TagClient> innerClientLogger,
            ILogger<MangaReaderLibTagClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new TagClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<ResourceObject<TagAttributesDto>>?> CreateTagAsync(CreateTagRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Creating tag {Name} in group {TagGroupId}", request.Name, request.TagGroupId);
            return await _innerClient.CreateTagAsync(request, cancellationToken);
        }

        public async Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Deleting tag {TagId}", tagId);
            await _innerClient.DeleteTagAsync(tagId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<TagAttributesDto>>?> GetTagByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Getting tag by ID {TagId}", tagId);
            return await _innerClient.GetTagByIdAsync(tagId, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<TagAttributesDto>>?> GetTagsAsync(int? offset = null, int? limit = null, Guid? tagGroupId = null, string? nameFilter = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Getting tags with name filter {NameFilter}", nameFilter);
            return await _innerClient.GetTagsAsync(offset, limit, tagGroupId, nameFilter, orderBy, ascending, cancellationToken);
        }

        public async Task UpdateTagAsync(Guid tagId, UpdateTagRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Updating tag {TagId}", tagId);
            await _innerClient.UpdateTagAsync(tagId, request, cancellationToken);
        }
    }
} 