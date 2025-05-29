using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<LibApiCollectionResponse<LibResourceObject<LibTagGroupAttributesDto>>?> GetTagGroupsAsync(
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
            return await _apiClient.GetAsync<LibApiCollectionResponse<LibResourceObject<LibTagGroupAttributesDto>>>(requestUri, cancellationToken);
        }
        
        public async Task<LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>?> GetTagGroupByIdAsync(Guid tagGroupId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting tag group by ID: {TagGroupId}", tagGroupId);
            return await _apiClient.GetAsync<LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>>($"TagGroups/{tagGroupId}", cancellationToken);
        }

        public async Task<LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>?> CreateTagGroupAsync(LibCreateTagGroupRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new tag group: {Name}", request.Name);
            return await _apiClient.PostAsync<LibCreateTagGroupRequestDto, LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>>("TagGroups", request, cancellationToken);
        }
    }
} 