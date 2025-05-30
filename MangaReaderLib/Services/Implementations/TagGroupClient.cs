using MangaReaderLib.DTOs.TagGroups;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MangaReaderLib.Services.Implementations
{
    public class TagGroupClient : ITagGroupClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<TagGroupClient> _logger;

        public TagGroupClient(IApiClient apiClient, ILogger<TagGroupClient> logger)
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

        public async Task<ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupsAsync(
            int? offset = null, int? limit = null, string? nameFilter = null, 
            string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting tag groups with filter: NameFilter={NameFilter}", nameFilter);
            
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "nameFilter", nameFilter);
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            
            string requestUri = BuildQueryString("TagGroups", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>>(requestUri, cancellationToken);
        }
        
        public async Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> GetTagGroupByIdAsync(Guid tagGroupId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting tag group by ID: {TagGroupId}", tagGroupId);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<TagGroupAttributesDto>>>($"TagGroups/{tagGroupId}", cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<TagGroupAttributesDto>>?> CreateTagGroupAsync(CreateTagGroupRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new tag group: {Name}", request.Name);
            return await _apiClient.PostAsync<CreateTagGroupRequestDto, ApiResponse<ResourceObject<TagGroupAttributesDto>>>("TagGroups", request, cancellationToken);
        }

        public async Task UpdateTagGroupAsync(Guid tagGroupId, UpdateTagGroupRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating tag group with ID: {TagGroupId}", tagGroupId);
            await _apiClient.PutAsync($"TagGroups/{tagGroupId}", request, cancellationToken);
        }

        public async Task DeleteTagGroupAsync(Guid tagGroupId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting tag group with ID: {TagGroupId}", tagGroupId);
            await _apiClient.DeleteAsync($"TagGroups/{tagGroupId}", cancellationToken);
        }
    }
} 