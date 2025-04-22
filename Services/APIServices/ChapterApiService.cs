using MangaReader.WebUI.Models.Mangadex;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MangaReader.WebUI.Services.APIServices
{
    public class ChapterApiService : BaseApiService, IChapterApiService
    {
        private readonly string _imageProxyBaseUrl;

        public ChapterApiService(HttpClient httpClient, ILogger<ChapterApiService> logger, IConfiguration configuration)
            : base(httpClient, logger, configuration)
        {
            // Lấy BaseUrl gốc từ configuration mà không thêm /mangadex
            _imageProxyBaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/') 
                              ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }

        public async Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            Logger.LogInformation($"Fetching chapters for manga ID: {mangaId} with models...");

            var allChapters = new List<Chapter>();
            int offset = 0;
            int limit = 100;
            int totalFetched = 0;
            int totalAvailable = 0;

            var validLanguages = languages.Split(',')
                                          .Select(l => l.Trim())
                                          .Where(l => !string.IsNullOrEmpty(l))
                                          .ToList();

            if (!validLanguages.Any())
            {
                Logger.LogWarning("No valid languages specified for chapters");
                return new ChapterList { Data = new List<Chapter>() };
            }

            do
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[chapter]", order);
                AddOrUpdateParam(queryParams, "order[volume]", order);
                AddOrUpdateParam(queryParams, "includes[]", "scanlation_group"); // Giữ lại include này quan trọng
                AddOrUpdateParam(queryParams, "includes[]", "user"); // Thêm user nếu cần tên uploader

                foreach (var lang in validLanguages)
                {
                    AddOrUpdateParam(queryParams, "translatedLanguage[]", lang);
                }

                var url = BuildUrlWithParams($"manga/{mangaId}/feed", queryParams);
                Logger.LogInformation($"Sending request to: {url}");

                try
                {
                    var response = await HttpClient.GetAsync(url);
                    var contentStream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Deserialize thành ChapterList
                        var chapterListResponse = await JsonSerializer.DeserializeAsync<ChapterList>(contentStream, JsonOptions);

                        if (chapterListResponse?.Data == null || !chapterListResponse.Data.Any())
                        {
                            Logger.LogInformation("No more chapters found or data is invalid.");
                            break; // Không còn chapter nào hoặc dữ liệu không hợp lệ
                        }

                        allChapters.AddRange(chapterListResponse.Data);
                        totalFetched += chapterListResponse.Data.Count;
                        totalAvailable = chapterListResponse.Total; // Lấy tổng số từ response đầu tiên
                        offset += limit;

                        // Dừng nếu đã fetch đủ số lượng yêu cầu (maxChapters) hoặc đã fetch hết
                        if ((maxChapters.HasValue && totalFetched >= maxChapters.Value) || totalFetched >= totalAvailable)
                        {
                            break;
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        LogApiError(nameof(FetchChaptersAsync), response, errorContent);
                        return null; // Trả về null nếu có lỗi API
                    }
                }
                catch (JsonException jsonEx)
                {
                    Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchChaptersAsync)} for manga ID: {mangaId}");
                    return null;
                }
                catch (HttpRequestException httpEx)
                {
                    Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchChaptersAsync)} for manga ID: {mangaId}");
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Unexpected exception in {nameof(FetchChaptersAsync)} for manga ID: {mangaId}");
                    return null;
                }
            } while (offset < totalAvailable); // Tiếp tục nếu còn chapter

            // Giới hạn số lượng chapter nếu cần
            if (maxChapters.HasValue && allChapters.Count > maxChapters.Value)
            {
                allChapters = allChapters.Take(maxChapters.Value).ToList();
            }

            Logger.LogInformation($"Successfully fetched {allChapters.Count} chapters for manga ID: {mangaId}.");
            // Trả về đối tượng ChapterList hoàn chỉnh
            return new ChapterList
            {
                Result = "ok",
                Response = "collection",
                Data = allChapters,
                Limit = limit, // Có thể là limit cuối cùng hoặc limit yêu cầu
                Offset = 0, // Offset không còn ý nghĩa khi đã gộp kết quả
                Total = totalAvailable // Tổng số chapter thực tế
            };
        }

        public async Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            Logger.LogInformation($"Fetching info for chapter ID: {chapterId} with models...");
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "scanlation_group", "manga", "user" } } // Thêm user nếu cần
            };
            var url = BuildUrlWithParams($"chapter/{chapterId}", queryParams);
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    var chapterResponse = await JsonSerializer.DeserializeAsync<ChapterResponse>(contentStream, JsonOptions);
                    if (chapterResponse?.Data == null)
                    {
                        Logger.LogWarning($"API response for chapter info {chapterId} is successful but data is null.");
                        return null;
                    }
                    Logger.LogInformation($"Successfully fetched info for chapter ID: {chapterId}");
                    return chapterResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchChapterInfoAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchChapterInfoAsync)} for chapter ID: {chapterId}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchChapterInfoAsync)} for chapter ID: {chapterId}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchChapterInfoAsync)} for chapter ID: {chapterId}");
                return null;
            }
        }

        // Thay đổi kiểu trả về thành AtHomeServerResponse?
        public async Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            Logger.LogInformation($"Fetching pages for chapter ID: {chapterId} with models...");
            var url = BuildUrlWithParams($"at-home/server/{chapterId}");
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                var response = await HttpClient.GetAsync(url);
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize thành AtHomeServerResponse
                    var atHomeResponse = await JsonSerializer.DeserializeAsync<AtHomeServerResponse>(contentStream, JsonOptions);

                    if (atHomeResponse == null || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null)
                    {
                        Logger.LogWarning($"API response for chapter pages {chapterId} has invalid format or missing data.");
                        return null;
                    }

                    Logger.LogInformation($"Successfully fetched page info for chapter ID: {chapterId}");
                    return atHomeResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogApiError(nameof(FetchChapterPagesAsync), response, errorContent);
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, $"JSON Deserialization error in {nameof(FetchChapterPagesAsync)} for chapter ID: {chapterId}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, $"HTTP Request error in {nameof(FetchChapterPagesAsync)} for chapter ID: {chapterId}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unexpected exception in {nameof(FetchChapterPagesAsync)} for chapter ID: {chapterId}");
                return null;
            }
        }
    }
} 