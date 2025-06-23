using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TagGroups;
using MangaReaderLib.Services.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibTagGroupClientService : IMangaReaderLibTagGroupClient
    {
        private readonly ITagGroupReader _innerReader;
        private readonly ILogger<MangaReaderLibTagGroupClientService> _wrapperLogger;

        public MangaReaderLibTagGroupClientService(
            ITagGroupReader tagGroupReader,
            ILogger<MangaReaderLibTagGroupClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = tagGroupReader;
        }

        public Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupByIdAsync(Guid tagGroupId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Getting tag group by ID {TagGroupId}", tagGroupId);
            return _innerReader.GetTagGroupByIdAsync(tagGroupId, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupsAsync(int? offset = null, int? limit = null, string? nameFilter = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagGroupClientService (Wrapper): Getting tag groups with name filter {NameFilter}", nameFilter);
            return _innerReader.GetTagGroupsAsync(offset, limit, nameFilter, orderBy, ascending, cancellationToken);
        }
    }
} 