using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TagGroups;
using System.Text;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibTagGroupClientService : IMangaReaderLibTagGroupClient
    {
        private readonly TagGroupClient _innerClient;
        private readonly ILogger<MangaReaderLibTagGroupClientService> _wrapperLogger;

        public MangaReaderLibTagGroupClientService(
            IMangaReaderLibApiClient apiClient,
            ILogger<TagGroupClient> innerClientLogger,
            ILogger<MangaReaderLibTagGroupClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new TagGroupClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> CreateTagGroupAsync(CreateTagGroupRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Creating tag group {Name}", request.Name);
            return await _innerClient.CreateTagGroupAsync(request, cancellationToken);
        }

        public async Task DeleteTagGroupAsync(Guid tagGroupId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Deleting tag group {TagGroupId}", tagGroupId);
            await _innerClient.DeleteTagGroupAsync(tagGroupId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupByIdAsync(Guid tagGroupId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Getting tag group by ID {TagGroupId}", tagGroupId);
            return await _innerClient.GetTagGroupByIdAsync(tagGroupId, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupsAsync(int? offset = null, int? limit = null, string? nameFilter = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Getting tag groups with name filter {NameFilter}", nameFilter);
            return await _innerClient.GetTagGroupsAsync(offset, limit, nameFilter, orderBy, ascending, cancellationToken);
        }

        public async Task UpdateTagGroupAsync(Guid tagGroupId, UpdateTagGroupRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Updating tag group {TagGroupId}", tagGroupId);
            await _innerClient.UpdateTagGroupAsync(tagGroupId, request, cancellationToken);
        }
    }
} 