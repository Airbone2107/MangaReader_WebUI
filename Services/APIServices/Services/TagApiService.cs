using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="ITagApiService"/>.
    /// Tương tác với endpoint API lấy danh sách Tag (thể loại) của MangaDex thông qua Backend API proxy.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency và gọi constructor lớp cơ sở.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình.</param>
    /// <param name="logger">Logger cho TagApiService.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API.</param>
    public class TagApiService(
        HttpClient httpClient,
        ILogger<TagApiService> logger,
        IConfiguration configuration,
        IApiRequestHandler apiRequestHandler)
        : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
          ITagApiService
    {
        /// <inheritdoc/>
        public async Task<TagListResponse?> FetchTagsAsync()
        {
            var url = BuildUrlWithParams("manga/tag");
            Logger.LogInformation("Fetching all tags from URL: {Url}", url);

            var tagListResponse = await GetApiAsync<TagListResponse>(url);
            // Trả về list rỗng nếu lỗi, thay vì null
            if (tagListResponse == null)
            {
                 Logger.LogWarning("Fetching tags failed. Returning empty list.");
                 return new TagListResponse { Result = "error", Response = "collection", Data = new List<Tag>(), Limit = 100, Offset = 0, Total = 0 };
            }

            #if DEBUG
            Debug.Assert(tagListResponse.Result == "ok", $"[TagApiService] FetchTagsAsync - API returned error: {tagListResponse.Result}. URL: {url}");
            Debug.Assert(tagListResponse.Data != null, $"[TagApiService] FetchTagsAsync - API returned null Data despite ok result. URL: {url}");
            #endif

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