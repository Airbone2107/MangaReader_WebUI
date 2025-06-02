using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces; // Cần dùng IApiClient từ MangaReaderLib cho constructor của AuthorClient

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibAuthorClientService : IMangaReaderLibAuthorClient
    {
        private readonly AuthorClient _innerClient;
        private readonly ILogger<MangaReaderLibAuthorClientService> _wrapperLogger;

        public MangaReaderLibAuthorClientService(
            IMangaReaderLibApiClient apiClient, 
            ILogger<AuthorClient> innerClientLogger, 
            ILogger<MangaReaderLibAuthorClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new AuthorClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> CreateAuthorAsync(CreateAuthorRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Creating author {Name}", request.Name);
            return await _innerClient.CreateAuthorAsync(request, cancellationToken);
        }

        public async Task DeleteAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Deleting author {AuthorId}", authorId);
            await _innerClient.DeleteAuthorAsync(authorId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorByIdAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Getting author by ID {AuthorId}", authorId);
            return await _innerClient.GetAuthorByIdAsync(authorId, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorsAsync(int? offset = null, int? limit = null, string? nameFilter = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Getting authors with filter: {NameFilter}", nameFilter);
            return await _innerClient.GetAuthorsAsync(offset, limit, nameFilter, orderBy, ascending, cancellationToken);
        }

        public async Task UpdateAuthorAsync(Guid authorId, UpdateAuthorRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibAuthorClientService (Wrapper): Updating author {AuthorId}", authorId);
            await _innerClient.UpdateAuthorAsync(authorId, request, cancellationToken);
        }
    }
} 