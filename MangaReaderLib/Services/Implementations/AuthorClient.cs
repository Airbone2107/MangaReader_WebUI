using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MangaReaderLib.Services.Implementations
{
    public class AuthorClient : IAuthorClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<AuthorClient> _logger;

        public AuthorClient(IApiClient apiClient, ILogger<AuthorClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        private string BuildQueryString(string baseUri, Dictionary<string, List<string>> queryParams)
        {
            var queryString = new StringBuilder();
            if (queryParams != null && queryParams.Any())
            {
                bool firstParam = true;
                foreach (var param in queryParams)
                {
                    if (param.Value != null && param.Value.Any())
                    {
                        foreach (var value in param.Value)
                        {
                             if (string.IsNullOrEmpty(value)) continue;

                            if (firstParam)
                            {
                                queryString.Append("?");
                                firstParam = false;
                            }
                            else
                            {
                                queryString.Append("&");
                            }
                            queryString.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
                        }
                    }
                }
            }
            return $"{baseUri}{queryString}";
        }

        private void AddQueryParam(Dictionary<string, List<string>> queryParams, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].Add(value);
            }
        }

        public async Task<ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorsAsync(
            int? offset = null, int? limit = null, string? nameFilter = null, 
            string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting authors with filter: NameFilter={NameFilter}", nameFilter);
            
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "nameFilter", nameFilter);
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            
            string requestUri = BuildQueryString("Authors", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorByIdAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting author by ID: {AuthorId}", authorId);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<AuthorAttributesDto>>>($"Authors/{authorId}", cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> CreateAuthorAsync(CreateAuthorRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new author: {Name}", request.Name);
            return await _apiClient.PostAsync<CreateAuthorRequestDto, ApiResponse<ResourceObject<AuthorAttributesDto>>>("Authors", request, cancellationToken);
        }

        public async Task UpdateAuthorAsync(Guid authorId, UpdateAuthorRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating author with ID: {AuthorId}", authorId);
            await _apiClient.PutAsync($"Authors/{authorId}", request, cancellationToken);
        }

        public async Task DeleteAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting author with ID: {AuthorId}", authorId);
            await _apiClient.DeleteAsync($"Authors/{authorId}", cancellationToken);
        }
    }
} 