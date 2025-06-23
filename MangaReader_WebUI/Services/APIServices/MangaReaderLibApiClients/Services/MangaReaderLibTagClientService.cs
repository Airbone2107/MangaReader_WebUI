using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;
using MangaReaderLib.Services.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibTagClientService : IMangaReaderLibTagClient
    {
        private readonly ITagReader _innerReader;
        private readonly ILogger<MangaReaderLibTagClientService> _wrapperLogger;

        public MangaReaderLibTagClientService(
            ITagReader tagReader,
            ILogger<MangaReaderLibTagClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = tagReader;
        }

        public Task<ApiResponse<ResourceObject<TagAttributesDto>>?> GetTagByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Getting tag by ID {TagId}", tagId);
            return _innerReader.GetTagByIdAsync(tagId, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<TagAttributesDto>>?> GetTagsAsync(int? offset = null, int? limit = null, Guid? tagGroupId = null, string? nameFilter = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTagClientService (Wrapper): Getting tags with name filter {NameFilter}", nameFilter);
            return _innerReader.GetTagsAsync(offset, limit, tagGroupId, nameFilter, orderBy, ascending, cancellationToken);
        }
    }
} 