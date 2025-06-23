using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibAuthorClientService : IMangaReaderLibAuthorClient
    {
        private readonly IAuthorReader _innerReader;
        private readonly ILogger<MangaReaderLibAuthorClientService> _wrapperLogger;

        public MangaReaderLibAuthorClientService(
            IAuthorReader authorReader,
            ILogger<MangaReaderLibAuthorClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = authorReader;
        }

        public Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorByIdAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Getting author by ID {AuthorId}", authorId);
            return _innerReader.GetAuthorByIdAsync(authorId, cancellationToken);
        }

        public Task<ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorsAsync(int? offset = null, int? limit = null, string? nameFilter = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Getting authors with filter: {NameFilter}", nameFilter);
            return _innerReader.GetAuthorsAsync(offset, limit, nameFilter, orderBy, ascending, cancellationToken);
        }
    }
} 