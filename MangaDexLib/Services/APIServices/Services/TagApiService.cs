using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaDexLib.Services.APIServices.Services
{
    public class TagApiService : BaseApiService, ITagApiService
    {
        public TagApiService(
            HttpClient httpClient,
            ILogger<TagApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
        }

        public async Task<TagListResponse?> FetchTagsAsync()
        {
            var url = BuildUrlWithParams("manga/tag");
            Logger.LogInformation("Fetching all tags from URL: {Url}", url);

            var tagListResponse = await GetApiAsync<TagListResponse>(url);
            if (tagListResponse == null)
            {
                Logger.LogWarning("Fetching tags failed. Returning empty list.");
                return new TagListResponse { Result = "error", Response = "collection", Data = new List<Tag>(), Limit = 100, Offset = 0, Total = 0 };
            }

            if (tagListResponse.Result != "ok" || tagListResponse.Data == null)
            {
                Logger.LogWarning("API response for tags has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    tagListResponse.Result, tagListResponse.Data != null, url);
                return new TagListResponse { Result = tagListResponse.Result ?? "error", Response = "collection", Data = new List<Tag>(), Limit = tagListResponse.Limit, Offset = tagListResponse.Offset, Total = tagListResponse.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} tags.", tagListResponse.Data.Count);
            return tagListResponse;
        }
    }
} 