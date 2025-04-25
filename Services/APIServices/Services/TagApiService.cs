using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Text.Json;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    public class TagApiService : BaseApiService, ITagApiService
    {
        public TagApiService(HttpClient httpClient, ILogger<TagApiService> logger, IConfiguration configuration)
            : base(httpClient, logger, configuration)
        {
        }

        public async Task<TagListResponse?> FetchTagsAsync()
        {
            Logger.LogInformation("Fetching manga tags with models...");
            var url = BuildUrlWithParams("manga/tag");
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tagListResponse = await JsonSerializer.DeserializeAsync<TagListResponse>(contentStream, JsonOptions);
                    if (tagListResponse?.Data == null)
                    {
                        Logger.LogWarning("API response for tags is successful but data is null.");
                        return new TagListResponse { Result = "ok", Response = "collection", Data = new List<Tag>(), Limit = 100, Offset = 0, Total = 0 };
                    }
                    Logger.LogInformation($"Successfully fetched {tagListResponse.Data.Count} tags.");
                    return tagListResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchTagsAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchTagsAsync)}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchTagsAsync)}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchTagsAsync)}");
                return null;
            }
        }
    }
} 