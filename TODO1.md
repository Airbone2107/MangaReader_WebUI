# TODO: Tái Cấu Trúc - Bước 1: Tạo Project Library `MangaDexLib`

## Mục tiêu

Tách toàn bộ mã nguồn liên quan đến việc xử lý dữ liệu từ MangaDex (bao gồm Models, API Services, Mappers) từ project `MangaReader_WebUI` ra một Class Library riêng có tên là `MangaDexLib`. Project này sẽ độc lập, có thể tái sử dụng và chứa tất cả logic cần thiết để tương tác với MangaDex API thông qua backend proxy.

### Bước 1.2: Di chuyển Models

```csharp
// MangaDexLib/Models/Author.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Author
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!; // "author" hoặc "artist"

        [JsonPropertyName("attributes")]
        public AuthorAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class AuthorAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("biography")]
        public Dictionary<string, string>? Biography { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("pixiv")]
        public string? Pixiv { get; set; }

        [JsonPropertyName("melonBook")]
        public string? MelonBook { get; set; }

        [JsonPropertyName("fanBox")]
        public string? FanBox { get; set; }

        [JsonPropertyName("booth")]
        public string? Booth { get; set; }

        [JsonPropertyName("nicoVideo")]
        public string? NicoVideo { get; set; }

        [JsonPropertyName("skeb")]
        public string? Skeb { get; set; }

        [JsonPropertyName("fantia")]
        public string? Fantia { get; set; }

        [JsonPropertyName("tumblr")]
        public string? Tumblr { get; set; }

        [JsonPropertyName("youtube")]
        public string? Youtube { get; set; }

        [JsonPropertyName("weibo")]
        public string? Weibo { get; set; }

        [JsonPropertyName("naver")]
        public string? Naver { get; set; }

        [JsonPropertyName("namicomi")]
        public string? Namicomi { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    // Các lớp Response
    public class AuthorResponse : BaseEntityResponse<Author> { }
    public class AuthorList : BaseListResponse<Author> { }
}
```

```csharp
// MangaDexLib/Models/Chapter.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Chapter
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public ChapterAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class ChapterAttributes
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("chapter")]
        public string? ChapterNumber { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("translatedLanguage")]
        public string TranslatedLanguage { get; set; } = default!;

        [JsonPropertyName("uploader")]
        public Guid? Uploader { get; set; } // ID của User

        [JsonPropertyName("externalUrl")]
        public string? ExternalUrl { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("publishAt")]
        public DateTimeOffset PublishAt { get; set; }

        [JsonPropertyName("readableAt")]
        public DateTimeOffset ReadableAt { get; set; }
    }

    // Các lớp Response
    public class ChapterResponse : BaseEntityResponse<Chapter> { }
    public class ChapterList : BaseListResponse<Chapter> { }

    // Model cho AtHome Server Response
    public class AtHomeServerResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!;

        [JsonPropertyName("baseUrl")]
        public string? BaseUrl { get; set; }

        [JsonPropertyName("chapter")]
        public AtHomeChapterData? Chapter { get; set; }
    }

    public class AtHomeChapterData
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("data")]
        public List<string>? Data { get; set; } // Danh sách tên file ảnh chất lượng cao

        [JsonPropertyName("dataSaver")]
        public List<string>? DataSaver { get; set; } // Danh sách tên file ảnh tiết kiệm dữ liệu
    }
}
```

```csharp
// MangaDexLib/Models/Cover.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Cover
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public CoverAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class CoverAttributes
    {
        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = default!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    // Các lớp Response
    public class CoverResponse : BaseEntityResponse<Cover> { }
    public class CoverList : BaseListResponse<Cover> { }
}
```

```csharp
// MangaDexLib/Models/ErrorResponse.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class ErrorResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }
        [JsonPropertyName("errors")]
        public List<Error>? Errors { get; set; }

        public class Error
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("status")]
            public int Status { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("detail")]
            public string? Detail { get; set; }

            [JsonPropertyName("context")]
            public JsonElement? Context { get; set; }
        }
    }
}
```

```csharp
// MangaDexLib/Models/Manga.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Manga
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public MangaAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class MangaAttributes
    {
        [JsonPropertyName("title")]
        public Dictionary<string, string>? Title { get; set; }

        [JsonPropertyName("altTitles")]
        public List<Dictionary<string, string>>? AltTitles { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }

        [JsonPropertyName("links")]
        public Dictionary<string, string>? Links { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string OriginalLanguage { get; set; } = default!;

        [JsonPropertyName("lastVolume")]
        public string? LastVolume { get; set; }

        [JsonPropertyName("lastChapter")]
        public string? LastChapter { get; set; }

        [JsonPropertyName("publicationDemographic")]
        public string? PublicationDemographic { get; set; } // "shounen", "shoujo", "josei", "seinen"

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!; // "completed", "ongoing", "cancelled", "hiatus"

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("contentRating")]
        public string ContentRating { get; set; } = default!; // "safe", "suggestive", "erotica", "pornographic"

        [JsonPropertyName("chapterNumbersResetOnNewVolume")]
        public bool ChapterNumbersResetOnNewVolume { get; set; }

        [JsonPropertyName("availableTranslatedLanguages")]
        public List<string>? AvailableTranslatedLanguages { get; set; }

        [JsonPropertyName("latestUploadedChapter")]
        public Guid? LatestUploadedChapter { get; set; }

        [JsonPropertyName("tags")]
        public List<Tag>? Tags { get; set; } // Sẽ được populate nếu có include=tag

        [JsonPropertyName("state")]
        public string State { get; set; } = default!; // "draft", "submitted", "published", "rejected"

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
        // Các lớp Response
    public class MangaResponse : BaseEntityResponse<Manga> { }
    public class MangaList : BaseListResponse<Manga> { }

    // Lớp cơ sở cho các response trả về một entity
    public class BaseEntityResponse<T>
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!; // "ok" or "error"

        [JsonPropertyName("response")]
        public string Response { get; set; } = default!;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    // Lớp cơ sở cho các response trả về một danh sách
    public class BaseListResponse<T>
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = default!; // "ok" or "error"

        [JsonPropertyName("response")]
        public string Response { get; set; } = default!;

        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
```

```csharp
// MangaDexLib/Models/Relationship.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Relationship
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("related")]
        public string? Related { get; set; } // Chỉ có khi type là manga_relation

        // Thuộc tính attributes sẽ được thêm khi có Reference Expansion
        // Kiểu dữ liệu có thể là MangaAttributes, AuthorAttributes, etc. tùy vào 'type'
        // Sử dụng object? để linh hoạt, hoặc tạo các lớp con nếu cần xử lý chi tiết
        [JsonPropertyName("attributes")]
        public object? Attributes { get; set; }
    }
}
```

```csharp
// MangaDexLib/Models/ScanlationGroup.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class ScanlationGroup
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public ScanlationGroupAttributes? Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship>? Relationships { get; set; }
    }

    public class ScanlationGroupAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("altNames")]
        public List<Dictionary<string, string>>? AltNames { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("ircServer")]
        public string? IrcServer { get; set; }

        [JsonPropertyName("ircChannel")]
        public string? IrcChannel { get; set; }

        [JsonPropertyName("discord")]
        public string? Discord { get; set; }

        [JsonPropertyName("contactEmail")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("mangaUpdates")]
        public string? MangaUpdates { get; set; }

        [JsonPropertyName("focusedLanguage")]

        public List<string>? FocusedLanguages { get; set; }
        [JsonPropertyName("locked")]

        public bool Locked { get; set; }

        [JsonPropertyName("official")]
        public bool Official { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("inactive")]
        public bool Inactive { get; set; }

        [JsonPropertyName("exLicensed")]
        public bool ExLicensed { get; set; }

        [JsonPropertyName("publishDelay")]
        public string? PublishDelay { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    // Các lớp Response
    public class ScanlationGroupResponse : BaseEntityResponse<ScanlationGroup> { }
    public class ScanlationGroupList : BaseListResponse<ScanlationGroup> { }
}
```

```csharp
// MangaDexLib/Models/Tag.cs
using System.Text.Json.Serialization;

namespace MangaDexLib.Models
{
    public class Tag
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("attributes")]
        public TagAttributes? Attributes { get; set; }
    }

    public class TagAttributes
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("group")]
        public string? Group { get; set; } // "content", "format", "genre", "theme"
        
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }

    // Response cho danh sách Tag
    public class TagListResponse : BaseListResponse<Tag> { }
}
```

---

### Bước 1.3: Di chuyển và Tạo Services (API Callers)

Tạo và di chuyển các service gọi API.

#### 1.3.1. Tạo các thành phần cơ sở

```csharp
// MangaDexLib/Services/APIServices/Interfaces/IApiRequestHandler.cs
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho một service xử lý các yêu cầu API HTTP.
    /// Đóng gói logic gửi yêu cầu, xử lý phản hồi, ghi log và deserialize kết quả.
    /// </summary>
    public interface IApiRequestHandler
    {
        /// <summary>
        /// Thực hiện một yêu cầu HTTP GET đến URL được chỉ định và deserialize nội dung phản hồi thành kiểu <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu mong đợi của đối tượng trả về sau khi deserialize.</typeparam>
        /// <param name="httpClient">Đối tượng <see cref="HttpClient"/> để thực hiện yêu cầu.</param>
        /// <param name="url">URL đầy đủ của endpoint API.</param>
        /// <param name="logger">Đối tượng <see cref="ILogger"/> để ghi log (thường là logger của service gọi đến).</param>
        /// <param name="options">Đối tượng <see cref="JsonSerializerOptions"/> để cấu hình quá trình deserialize.</param>
        /// <param name="cancellationToken">Token để hủy yêu cầu (tùy chọn).</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng kiểu <typeparamref name="T"/> đã được deserialize,
        /// hoặc <c>null</c> nếu yêu cầu thất bại, phản hồi không thành công, hoặc có lỗi trong quá trình deserialize.
        /// </returns>
        Task<T?> GetAsync<T>(
            HttpClient httpClient,
            string url,
            ILogger logger,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default
        ) where T : class;
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Services/ApiRequestHandler.cs
using MangaDexLib.Services.APIServices.Interfaces;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="IApiRequestHandler"/>.
    /// Đóng gói logic xử lý yêu cầu HTTP GET, bao gồm gửi yêu cầu,
    /// xử lý phản hồi thành công và lỗi, deserialize JSON, ghi log và đo thời gian thực thi.
    /// </summary>
    public class ApiRequestHandler : IApiRequestHandler
    {
        private readonly ILogger<ApiRequestHandler> _handlerLogger;

        public ApiRequestHandler(ILogger<ApiRequestHandler> handlerLogger)
        {
            _handlerLogger = handlerLogger;
        }

        public async Task<T?> GetAsync<T>(
            HttpClient httpClient,
            string url,
            ILogger logger, // Logger của service gọi đến
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            logger.LogInformation("Attempting GET request to: {Url}", url);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response == null)
                {
                    stopwatch.Stop();
                    logger.LogError("GET {Url} - Failed: HttpClient returned null response. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                    return null;
                }

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                string rawContentForErrorLog = "[Could not read stream]";

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = await JsonSerializer.DeserializeAsync<T>(contentStream, options, cancellationToken);
                        stopwatch.Stop();

                        if (result == null)
                        {
                            logger.LogWarning("GET {Url} - Success (Status: {StatusCode}) but deserialized result was null. Duration: {ElapsedMs}ms", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            logger.LogInformation("GET {Url} - Success. Status: {StatusCode}. Duration: {ElapsedMs}ms", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                        }
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        stopwatch.Stop();
                        rawContentForErrorLog = await ReadStreamSafelyAsync(contentStream, _handlerLogger);

                        logger.LogError(jsonEx, "GET {Url} - JSON Deserialization error. Status: {StatusCode}. Duration: {ElapsedMs}ms. Raw Content: {RawContent}", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, rawContentForErrorLog);
                        return null;
                    }
                }
                else
                {
                    stopwatch.Stop();
                    rawContentForErrorLog = await ReadStreamSafelyAsync(contentStream, _handlerLogger);

                    LogApiErrorHelper(logger, "GET", url, response, rawContentForErrorLog, stopwatch.ElapsedMilliseconds);
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                stopwatch.Stop();
                logger.LogError(httpEx, "GET {Url} - HTTP Request error (Network/DNS issue?). Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                logger.LogWarning("GET {Url} - Request was canceled by cancellation token. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
            catch (TaskCanceledException timeoutEx)
            {
                stopwatch.Stop();
                logger.LogError(timeoutEx, "GET {Url} - Request timed out. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "GET {Url} - Unexpected exception during API request. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
        }

        private async Task<string> ReadStreamSafelyAsync(Stream stream, ILogger localLogger)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            else if (!stream.CanRead)
            {
                return "[Stream not readable or seekable]";
            }

            try
            {
                using var reader = new StreamReader(stream, leaveOpen: true);
                char[] buffer = new char[1024];
                int charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                string content = new string(buffer, 0, charsRead);
                if (stream.Length > 1024 || (!stream.CanSeek && reader.Peek() != -1))
                {
                    content += "... (content truncated)";
                }
                return content;
            }
            catch (Exception ex)
            {
                localLogger.LogError(ex, "Error reading stream content for logging purposes.");
                return "[Error reading stream content]";
            }
        }

        private void LogApiErrorHelper(ILogger logger, string method, string url, HttpResponseMessage response, string content, long durationMs)
        {
            LogLevel logLevel = response.StatusCode >= System.Net.HttpStatusCode.InternalServerError ? LogLevel.Error : LogLevel.Warning;

            logger.Log(logLevel,
                       "{Method} {Url} - API Error. Status: {StatusCode} ({ReasonPhrase}). Duration: {ElapsedMs}ms. Response Content: {ResponseContent}",
                       method.ToUpperInvariant(),
                       url,
                       (int)response.StatusCode,
                       response.ReasonPhrase,
                       durationMs,
                       content);
        }
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/BaseApiService.cs
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho các service gọi API.
    /// Cung cấp các thành phần và phương thức dùng chung để tương tác với Backend API.
    /// </summary>
    public abstract class BaseApiService
    {
        /// <summary>
        /// HttpClient được cấu hình để gọi Backend API.
        /// Được truy cập bởi lớp kế thừa.
        /// </summary>
        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Logger dành riêng cho lớp kế thừa.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// URL cơ sở của Backend API proxy cho MangaDex.
        /// </summary>
        protected readonly string BaseUrl;

        /// <summary>
        /// Tùy chọn cấu hình cho việc deserialize JSON.
        /// </summary>
        protected readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IApiRequestHandler _apiRequestHandler;
        private readonly TimeSpan _httpClientTimeout;

        protected BaseApiService(
            HttpClient httpClient,
            ILogger logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiRequestHandler = apiRequestHandler ?? throw new ArgumentNullException(nameof(apiRequestHandler));

            // URL này trỏ đến proxy backend, không phải MangaDex trực tiếp
            BaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/') + "/mangadex"
                      ?? throw new InvalidOperationException("BackendApi:BaseUrl/mangadex is not configured.");
            
            _httpClientTimeout = SetHttpClientTimeout(httpClient);
        }

        private static TimeSpan SetHttpClientTimeout(HttpClient client)
        {
            var timeout = TimeSpan.FromSeconds(60);
            client.Timeout = timeout;
            return timeout;
        }
        
        protected async Task<T?> GetApiAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
        {
            return await _apiRequestHandler.GetAsync<T>(
                this.HttpClient,
                url,
                this.Logger,
                this.JsonOptions,
                cancellationToken
            );
        }

        protected string BuildUrlWithParams(string endpointPath, Dictionary<string, List<string>>? parameters = null)
        {
            var fullUrl = BaseUrl.TrimEnd('/') + "/" + endpointPath.TrimStart('/');

            if (parameters == null || parameters.Count == 0)
                return fullUrl;

            var sb = new StringBuilder(fullUrl);
            sb.Append('?');

            bool isFirst = true;
            foreach (var param in parameters)
            {
                if (param.Value == null) continue;

                foreach (var value in param.Value)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!isFirst)
                            sb.Append('&');
                        else
                            isFirst = false;

                        sb.Append(Uri.EscapeDataString(param.Key));
                        sb.Append('=');
                        sb.Append(Uri.EscapeDataString(value));
                    }
                }
            }
            if (isFirst && sb[sb.Length - 1] == '?')
            {
                sb.Length--;
            }
            return sb.ToString();
        }
        
        protected void AddOrUpdateParam(Dictionary<string, List<string>> parameters, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!parameters.ContainsKey(key))
                {
                    parameters[key] = new List<string>();
                }
                parameters[key].Add(value);
            }
        }
    }
}
```

#### 1.3.2. Tạo các Interfaces và Services cho API

```csharp
// MangaDexLib/Services/APIServices/Interfaces/IApiStatusService.cs
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service kiểm tra trạng thái kết nối của API.
    /// </summary>
    public interface IApiStatusService
    {
        /// <summary>
        /// Kiểm tra kết nối đến Backend API proxy.
        /// </summary>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là <c>true</c> nếu kết nối thành công (API trả về mã trạng thái thành công);
        /// ngược lại, trả về <c>false</c>.
        /// </returns>
        Task<bool> TestConnectionAsync();
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Services/ApiStatusService.cs
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Services
{
    public class ApiStatusService : BaseApiService, IApiStatusService
    {
        public ApiStatusService(
            HttpClient httpClient,
            ILogger<ApiStatusService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
        }

        public async Task<bool> TestConnectionAsync()
        {
            var url = BuildUrlWithParams("status");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var result = await GetApiAsync<object>(url, cts.Token);
            return result != null;
        }
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Interfaces/IMangaApiService.cs
using MangaDexLib.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface IMangaApiService
    {
        Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null);
        Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds);
        Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId);
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Services/MangaApiService.cs
using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Services
{
    public class MangaApiService : BaseApiService, IMangaApiService
    {
        public MangaApiService(
            HttpClient httpClient,
            ILogger<MangaApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
        }

        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            Logger.LogInformation("Fetching manga list with parameters: Limit={Limit}, Offset={Offset}, SortOptions={@SortOptions}",
                limit, offset, sortManga);
            var queryParams = new Dictionary<string, List<string>>();

            if (limit.HasValue) AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());
            if (offset.HasValue) AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

            if (sortManga != null)
            {
                var sortParams = sortManga.ToParams();
                foreach (var param in sortParams)
                {
                    if (param.Key.EndsWith("[]") && param.Value is IEnumerable<string> values)
                    {
                        if (!queryParams.ContainsKey(param.Key))
                        {
                            queryParams[param.Key] = new List<string>();
                        }
                        foreach (var val in values)
                        {
                            if (!string.IsNullOrEmpty(val)) queryParams[param.Key].Add(val);
                        }
                    }
                    else if (param.Key.StartsWith("order["))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value?.ToString() ?? string.Empty);
                    }
                    else if (param.Value != null && !string.IsNullOrEmpty(param.Value.ToString()))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value.ToString()!);
                    }
                }
                if (sortManga.ContentRating != null && sortManga.ContentRating.Any())
                {
                    if (!queryParams.ContainsKey("contentRating[]"))
                    {
                        queryParams["contentRating[]"] = new List<string>();
                    }
                    queryParams["contentRating[]"].AddRange(sortManga.ContentRating);
                }
            }
            else
            {
                AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
            }

            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "includes[]", "author");
            AddOrUpdateParam(queryParams, "includes[]", "artist");

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga list failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = limit ?? 10, Offset = offset ?? 0, Total = 0 };
            }

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga list has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga entries (Total: {Total}).", mangaList.Data.Count, mangaList.Total);
            return mangaList;
        }

        public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            Logger.LogInformation("Fetching details for manga ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "author", "artist", "cover_art", "tag" } }
            };
            var url = BuildUrlWithParams($"manga/{mangaId}", queryParams);
            Logger.LogInformation("Constructed manga details fetch URL: {Url}", url);

            var mangaResponse = await GetApiAsync<MangaResponse>(url);
            if (mangaResponse == null)
            {
                Logger.LogWarning("Fetching manga details for {MangaId} failed.", mangaId);
                return null;
            }

            if (mangaResponse.Result != "ok" || mangaResponse.Data == null)
            {
                Logger.LogWarning("API response for manga details {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaId, mangaResponse.Result, mangaResponse.Data != null, url);
                return null;
            }

            Logger.LogInformation("Successfully fetched details for manga: {MangaTitle} ({MangaId})",
                mangaResponse.Data.Attributes?.Title?.FirstOrDefault().Value ?? "N/A", mangaId);
            return mangaResponse;
        }

        public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || !mangaIds.Any())
            {
                Logger.LogWarning("FetchMangaByIdsAsync called with empty or null list of IDs.");
                return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Total = 0 };
            }

            Logger.LogInformation("Fetching manga by IDs: [{MangaIds}]", string.Join(", ", mangaIds));
            var queryParams = new Dictionary<string, List<string>>();
            foreach (var id in mangaIds)
            {
                AddOrUpdateParam(queryParams, "ids[]", id);
            }
            AddOrUpdateParam(queryParams, "includes[]", "cover_art");
            AddOrUpdateParam(queryParams, "limit", mangaIds.Count.ToString());

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch by IDs URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga by IDs failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = mangaIds.Count, Offset = 0, Total = 0 };
            }

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga by IDs has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga by IDs.", mangaList.Data.Count);
            return mangaList;
        }
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Interfaces/IChapterApiService.cs
using MangaDexLib.Models;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface IChapterApiService
    {
        Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null);
        Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId);
        Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId);
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Services/ChapterApiService.cs
using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Services
{
    public class ChapterApiService : BaseApiService, IChapterApiService
    {
        private readonly string _imageProxyBaseUrl;

        public ChapterApiService(
            HttpClient httpClient,
            ILogger<ChapterApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
            _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        }

        public async Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
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
                Logger.LogWarning("No valid languages specified for fetching chapters.");
                return new ChapterList { Data = new List<Chapter>(), Result = "ok", Response = "collection" };
            }

            Logger.LogInformation("Fetching chapters for manga {MangaId} with languages [{Languages}]. Max chapters: {MaxChapters}",
                mangaId, string.Join(", ", validLanguages), maxChapters?.ToString() ?? "All");

            do
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[chapter]", order);
                AddOrUpdateParam(queryParams, "order[volume]", order);
                AddOrUpdateParam(queryParams, "includes[]", "scanlation_group");
                AddOrUpdateParam(queryParams, "includes[]", "user");

                foreach (var lang in validLanguages)
                {
                    AddOrUpdateParam(queryParams, "translatedLanguage[]", lang);
                }

                var url = BuildUrlWithParams($"manga/{mangaId}/feed", queryParams);
                Logger.LogDebug("Fetching chapter page: {Url}", url);

                var chapterListResponse = await GetApiAsync<ChapterList>(url);

                if (chapterListResponse == null)
                {
                    Logger.LogWarning("API call failed when fetching chapters for manga {MangaId} at offset {Offset}. Stopping pagination.", mangaId, offset);
                    break;
                }
                
                if (chapterListResponse.Result != "ok" || chapterListResponse.Data == null || !chapterListResponse.Data.Any())
                {
                    Logger.LogInformation("No more chapters found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                    if (totalAvailable == 0 && offset == 0) totalAvailable = chapterListResponse.Total;
                    break;
                }

                allChapters.AddRange(chapterListResponse.Data);
                totalFetched += chapterListResponse.Data.Count;
                if (totalAvailable == 0) totalAvailable = chapterListResponse.Total; 
                offset += limit;

                Logger.LogDebug("Fetched {Count} chapters. Total fetched: {TotalFetched}. Total available: {TotalAvailable}. Next offset: {NextOffset}",
                    chapterListResponse.Data.Count, totalFetched, totalAvailable, offset);

                if ((maxChapters.HasValue && totalFetched >= maxChapters.Value) || totalFetched >= totalAvailable)
                {
                    Logger.LogInformation("Reached chapter limit or fetched all available chapters for manga {MangaId}. Stopping.", mangaId);
                    break;
                }

            } while (offset < totalAvailable);

            if (maxChapters.HasValue && allChapters.Count > maxChapters.Value)
            {
                allChapters = allChapters.Take(maxChapters.Value).ToList();
                Logger.LogInformation("Truncated chapter list to {MaxChapters} for manga {MangaId}.", maxChapters.Value, mangaId);
            }

            Logger.LogInformation("Successfully fetched {Count} chapters for manga ID: {MangaId}.", allChapters.Count, mangaId);
            return new ChapterList
            {
                Result = "ok",
                Response = "collection",
                Data = allChapters,
                Limit = limit,
                Offset = 0,
                Total = totalAvailable
            };
        }
        
        public async Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            var queryParams = new Dictionary<string, List<string>> {
                { "includes[]", new List<string> { "scanlation_group", "manga", "user" } }
            };
            var url = BuildUrlWithParams($"chapter/{chapterId}", queryParams);
            Logger.LogInformation("Fetching info for chapter ID: {ChapterId}", chapterId);

            var chapterResponse = await GetApiAsync<ChapterResponse>(url);
            if (chapterResponse == null) return null;
            
            if (chapterResponse.Result != "ok" || chapterResponse.Data == null)
            {
                Logger.LogWarning("API response for chapter info {ChapterId} has invalid format or missing data. Result: {Result}, HasData: {HasData}",
                    chapterId, chapterResponse.Result, chapterResponse.Data != null);
                return null;
            }

            Logger.LogInformation("Successfully fetched info for chapter ID: {ChapterId}", chapterId);
            return chapterResponse;
        }

        public async Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            var url = BuildUrlWithParams($"at-home/server/{chapterId}");
            Logger.LogInformation("Fetching chapter pages info for chapter ID: {ChapterId}", chapterId);

            var atHomeResponse = await GetApiAsync<AtHomeServerResponse>(url);
            if (atHomeResponse == null) return null;
            
            if (atHomeResponse.Result != "ok" || string.IsNullOrEmpty(atHomeResponse.BaseUrl) || atHomeResponse.Chapter?.Data == null || !atHomeResponse.Chapter.Data.Any())
            {
                Logger.LogWarning("API response for chapter pages {ChapterId} has invalid format or missing data. Result: {Result}, BaseUrl: {HasBaseUrl}, ChapterData: {HasChapterData}, HasPages: {HasPages}",
                    chapterId, atHomeResponse.Result, !string.IsNullOrEmpty(atHomeResponse.BaseUrl), atHomeResponse.Chapter?.Data != null, atHomeResponse.Chapter?.Data?.Any() ?? false);
                return null;
            }

            Logger.LogInformation("Successfully fetched page info for chapter ID: {ChapterId}", chapterId);
            return atHomeResponse;
        }
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Interfaces/ICoverApiService.cs
using MangaDexLib.Models;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface ICoverApiService
    {
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);
        string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512);
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Services/CoverApiService.cs
using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Services
{
    public class CoverApiService : BaseApiService, ICoverApiService
    {
        private readonly string _imageProxyBaseUrl;
        private readonly TimeSpan _apiDelay;

        public CoverApiService(
            HttpClient httpClient,
            ILogger<CoverApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
            _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
            _apiDelay = TimeSpan.FromMilliseconds(configuration?.GetValue<int>("ApiRateLimitDelayMs", 250) ?? 250);
        }

        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            var allCovers = new List<Cover>();
            int offset = 0;
            const int limit = 100;
            int totalAvailable = 0;

            Logger.LogInformation("Fetching ALL covers for manga ID: {MangaId} with pagination...", mangaId);

            do
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "manga[]", mangaId);
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[volume]", "asc");

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogDebug("Fetching covers page: {Url}", url);

                try
                {
                    var coverListResponse = await GetApiAsync<CoverList>(url);
                    
                    if (coverListResponse == null)
                    {
                        Logger.LogWarning("Error fetching covers for manga {MangaId} at offset {Offset}. Retrying...", mangaId, offset);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        coverListResponse = await GetApiAsync<CoverList>(url);

                        if (coverListResponse == null)
                        {
                            Logger.LogError("Failed to fetch covers for manga {MangaId} at offset {Offset} after retry. Stopping pagination.", mangaId, offset);
                            break;
                        }
                    }

                    if (coverListResponse.Data == null || !coverListResponse.Data.Any())
                    {
                        Logger.LogInformation("No more covers found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                        if (totalAvailable == 0 && offset == 0) totalAvailable = coverListResponse.Total;
                        break;
                    }

                    allCovers.AddRange(coverListResponse.Data);
                    if (totalAvailable == 0) totalAvailable = coverListResponse.Total;
                    offset += limit;
                    Logger.LogDebug("Fetched {Count} covers. Offset now: {Offset}. Total available: {TotalAvailable}",
                        coverListResponse.Data.Count, offset, totalAvailable);

                    if (offset < totalAvailable && totalAvailable > 0)
                    {
                        await Task.Delay(_apiDelay);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unexpected exception during cover pagination for manga ID: {MangaId}", mangaId);
                    return null;
                }

            } while (offset < totalAvailable && totalAvailable > 0);

            Logger.LogInformation("Finished fetching. Total covers retrieved: {RetrievedCount} for manga ID: {MangaId}. API reported total: {ApiTotal}",
                allCovers.Count, mangaId, totalAvailable);

            return new CoverList
            {
                Result = "ok",
                Response = "collection",
                Data = allCovers,
                Limit = allCovers.Count,
                Offset = 0,
                Total = totalAvailable
            };
        }

        public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
        {
            var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.{size}.jpg";
            return $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
        }
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Interfaces/ITagApiService.cs
using MangaDexLib.Models;
using System.Threading.Tasks;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface ITagApiService
    {
        Task<TagListResponse?> FetchTagsAsync();
    }
}
```

```csharp
// MangaDexLib/Services/APIServices/Services/TagApiService.cs
using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
```

---

### Bước 1.4: Di chuyển Mappers và Utilities

Di chuyển và tạo lại các services xử lý dữ liệu (Mappers, Extractors, Utilities).

#### 1.4.1. Tạo Utility Services

```csharp
// MangaDexLib/Services/UtilityServices/JsonConversionService.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace MangaDexLib.Services.UtilityServices
{
    public class JsonConversionService
    {
        public Dictionary<string, object> ConvertJsonElementToDict(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            if (element.ValueKind != JsonValueKind.Object)
            {
                return dict;
            }

            foreach (var property in element.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        dict[property.Name] = ConvertJsonElementToDict(property.Value);
                        break;
                    case JsonValueKind.Array:
                        dict[property.Name] = ConvertJsonElementToList(property.Value);
                        break;
                    case JsonValueKind.String:
                        dict[property.Name] = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (property.Value.TryGetInt32(out int intValue))
                        {
                            dict[property.Name] = intValue;
                        }
                        else if (property.Value.TryGetInt64(out long longValue))
                        {
                            dict[property.Name] = longValue;
                        }
                        else
                        {
                            dict[property.Name] = property.Value.GetDouble();
                        }
                        break;
                    case JsonValueKind.True:
                        dict[property.Name] = true;
                        break;
                    case JsonValueKind.False:
                        dict[property.Name] = false;
                        break;
                    case JsonValueKind.Null:
                        dict[property.Name] = null;
                        break;
                    default:
                        dict[property.Name] = property.Value.ToString();
                        break;
                }
            }
            return dict;
        }
        
        public List<object> ConvertJsonElementToList(JsonElement element)
        {
            var list = new List<object>();
            if (element.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var item in element.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                        list.Add(ConvertJsonElementToDict(item));
                        break;
                    case JsonValueKind.Array:
                        list.Add(ConvertJsonElementToList(item));
                        break;
                    case JsonValueKind.String:
                        list.Add(item.GetString());
                        break;
                    case JsonValueKind.Number:
                        if (item.TryGetInt32(out int intValue))
                        {
                            list.Add(intValue);
                        }
                        else if (item.TryGetInt64(out long longValue))
                        {
                            list.Add(longValue);
                        }
                        else
                        {
                            list.Add(item.GetDouble());
                        }
                        break;
                    case JsonValueKind.True:
                        list.Add(true);
                        break;
                    case JsonValueKind.False:
                        list.Add(false);
                        break;
                    case JsonValueKind.Null:
                        list.Add(null);
                        break;
                    default:
                        list.Add(item.ToString());
                        break;
                }
            }
            return list;
        }
    }
}
```

```csharp
// MangaDexLib/Services/UtilityServices/LocalizationService.cs
using MangaDexLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace MangaDexLib.Services.UtilityServices
{
    public class LocalizationService
    {
        public string GetLocalizedTitle(string titleJson)
        {
            try
            {
                if (string.IsNullOrEmpty(titleJson))
                    return "Không có tiêu đề";
                
                try
                {
                    var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);
                    
                    if (titles == null || titles.Count == 0)
                        return "Không có tiêu đề";
                        
                    if (titles.ContainsKey("vi"))
                        return titles["vi"];
                    if (titles.ContainsKey("en"))
                        return titles["en"];
                        
                    var firstItem = titles.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "Không có tiêu đề" : firstItem.Value;
                }
                catch (JsonException)
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(titleJson);
                        
                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            if (jsonElement.TryGetProperty("vi", out var viTitle))
                                return viTitle.GetString() ?? "Không có tiêu đề";
                            if (jsonElement.TryGetProperty("en", out var enTitle))
                                return enTitle.GetString() ?? "Không có tiêu đề";
                            
                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "Không có tiêu đề";
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                
                return "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xử lý tiêu đề truyện: {ex.Message}");
                return "Không có tiêu đề";
            }
        }
        
        public string GetLocalizedDescription(string descriptionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(descriptionJson))
                    return "";
                
                try
                {
                    var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);
                    
                    if (descriptions == null || descriptions.Count == 0)
                        return "";
                        
                    if (descriptions.ContainsKey("vi"))
                        return descriptions["vi"];
                    if (descriptions.ContainsKey("en"))
                        return descriptions["en"];
                        
                    var firstItem = descriptions.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "" : firstItem.Value;
                }
                catch (JsonException)
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(descriptionJson);
                        
                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            if (jsonElement.TryGetProperty("vi", out var viDescription))
                                return viDescription.GetString() ?? "";
                            if (jsonElement.TryGetProperty("en", out var enDescription))
                                return enDescription.GetString() ?? "";
                            
                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "";
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                
                return "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xử lý mô tả truyện: {ex.Message}");
                return "";
            }
        }

        public string GetStatus(string status)
        {
            return status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }

        public string GetStatus(MangaAttributes? attributes)
        {
            if (attributes == null || string.IsNullOrEmpty(attributes.Status)) return "Không rõ";

            return attributes.Status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }

        public string GetStatus(Dictionary<string, object> attributesDict)
        {
            string status = attributesDict.ContainsKey("status") ? attributesDict["status"]?.ToString() ?? "unknown" : "unknown";
            return GetStatus(status);
        }
    }
}
```

#### 1.4.2. Tạo Data Extractor

```csharp
// MangaDexLib/DataProcessing/Interfaces/IMangaDataExtractor.cs
using MangaDexLib.Models;
using System.Collections.Generic;

namespace MangaDexLib.DataProcessing.Interfaces
{
    public interface IMangaDataExtractor
    {
        string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList);
        string ExtractMangaDescription(Dictionary<string, string>? descriptionDict);
        List<string> ExtractAndTranslateTags(List<Tag>? tagsList);
        (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships);
        string ExtractCoverUrl(string mangaId, List<Relationship>? relationships);
        string ExtractAndTranslateStatus(string? status);
        string ExtractChapterDisplayTitle(ChapterAttributes attributes);
        string ExtractChapterNumber(ChapterAttributes attributes);
        Dictionary<string, List<string>> ExtractAlternativeTitles(List<Dictionary<string, string>>? altTitlesList);
        string ExtractPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary);
    }
}
```

```csharp
// MangaDexLib/DataProcessing/Services/MangaDataExtractorService.cs
using MangaDexLib.Models;
using MangaDexLib.Services.APIServices.Services;
using MangaDexLib.Services.UtilityServices;
using MangaDexLib.DataProcessing.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MangaDexLib.DataProcessing.Services
{
    public class MangaDataExtractorService : IMangaDataExtractor
    {
        private readonly ILogger<MangaDataExtractorService> _logger;
        private readonly LocalizationService _localizationService;
        private readonly IConfiguration _configuration;
        private readonly string _backendApiBaseUrl;

        private static readonly Dictionary<string, string> _tagTranslations = InitializeTagTranslations();
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public MangaDataExtractorService(
            ILogger<MangaDataExtractorService> logger,
            LocalizationService localizationService,
            IConfiguration configuration)
        {
            _logger = logger;
            _localizationService = localizationService;
            _configuration = configuration;
            _backendApiBaseUrl = _configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                                ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured in MangaDataExtractorService.");
        }

        public string ExtractCoverUrl(string mangaId, List<Relationship>? relationships)
        {
            try
            {
                if (relationships == null || !relationships.Any())
                {
                    return "/images/cover-placeholder.jpg";
                }

                var coverRelationship = relationships.FirstOrDefault(r => r != null && r.Type == "cover_art");

                if (coverRelationship == null)
                {
                    return "/images/cover-placeholder.jpg";
                }

                string? fileName = null;
                if (coverRelationship.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                {
                    if (attributesElement.TryGetProperty("fileName", out var fileNameElement) && fileNameElement.ValueKind == JsonValueKind.String)
                    {
                        fileName = fileNameElement.GetString();
                    }
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    return "/images/cover-placeholder.jpg";
                }

                var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.512.jpg";
                return $"{_backendApiBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi trích xuất Cover URL cho manga ID: {mangaId}");
                return "/images/cover-placeholder.jpg";
            }
        }

        public string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
        {
            try
            {
                if (altTitlesList != null)
                {
                    foreach (var altTitleDict in altTitlesList)
                    {
                        if (altTitleDict != null && altTitleDict.TryGetValue("vi", out var viTitle) && !string.IsNullOrEmpty(viTitle))
                        {
                            return viTitle;
                        }
                    }
                }

                if (titleDict != null && titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
                {
                    return enTitle;
                }

                if (titleDict != null && titleDict.TryGetValue("vi", out var mainViTitle) && !string.IsNullOrEmpty(mainViTitle))
                {
                    return mainViTitle;
                }

                if (titleDict != null && titleDict.Any())
                {
                    return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
                }

                if (altTitlesList != null)
                {
                    foreach (var altTitleDict in altTitlesList)
                    {
                        if (altTitleDict != null && altTitleDict.Any())
                        {
                            return altTitleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
                        }
                    }
                }

                return "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất tiêu đề manga.");
                return "Lỗi tiêu đề";
            }
        }

        public string ExtractMangaDescription(Dictionary<string, string>? descriptionDict)
        {
            if (descriptionDict == null || !descriptionDict.Any())
            {
                return "";
            }

            try
            {
                if (descriptionDict.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
                {
                    return viDesc;
                }
                if (descriptionDict.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
                {
                    return enDesc;
                }

                return descriptionDict.FirstOrDefault().Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất mô tả manga.");
                return "";
            }
        }

        public List<string> ExtractAndTranslateTags(List<Tag>? tagsList)
        {
            var translatedTags = new List<string>();
            if (tagsList == null || !tagsList.Any())
            {
                return translatedTags;
            }

            try
            {
                foreach (var tag in tagsList)
                {
                    if (tag?.Attributes?.Name == null) continue;

                    if (tag.Attributes.Name.TryGetValue("en", out var enTagName) && !string.IsNullOrEmpty(enTagName))
                    {
                        if (_tagTranslations.TryGetValue(enTagName, out var translation))
                        {
                            translatedTags.Add(translation);
                        }
                        else
                        {
                            translatedTags.Add(enTagName);
                            _logger.LogDebug($"Không tìm thấy bản dịch cho tag: {enTagName}");
                        }
                    }
                    else if (tag.Attributes.Name.Any())
                    {
                        translatedTags.Add(tag.Attributes.Name.First().Value);
                    }
                }

                return translatedTags.Distinct().OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), false)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất và dịch tags manga.");
                return new List<string>();
            }
        }

        public (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships)
        {
            string author = "Không rõ";
            string artist = "Không rõ";

            if (relationships == null || !relationships.Any())
            {
                return (author, artist);
            }

            try
            {
                foreach (var rel in relationships)
                {
                    if (rel == null) continue;
                    string relType = rel.Type;
                    string name = "Không rõ";

                    if (relType == "author" || relType == "artist")
                    {
                        if (rel.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                        {
                            if (attributesElement.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                            {
                                name = nameElement.GetString() ?? "Không rõ";
                            }
                        }

                        if (relType == "author")
                            author = name;
                        else
                            artist = name;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất tác giả/họa sĩ từ relationships.");
            }

            return (author, artist);
        }

        public string ExtractAndTranslateStatus(string? status)
        {
            return _localizationService.GetStatus(status);
        }

        public string ExtractChapterDisplayTitle(ChapterAttributes attributes)
        {
            string chapterNumberString = attributes.ChapterNumber ?? "?";
            string specificChapterTitle = attributes.Title?.Trim() ?? "";

            if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
            {
                return !string.IsNullOrEmpty(specificChapterTitle) ? specificChapterTitle : "Oneshot";
            }

            string patternChapterVn = $"^Chương\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";
            string patternChapterEn = $"^Chapter\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";

            bool startsWithChapterInfo = Regex.IsMatch(specificChapterTitle, patternChapterVn, RegexOptions.IgnoreCase) ||
                                         Regex.IsMatch(specificChapterTitle, patternChapterEn, RegexOptions.IgnoreCase);

            if (startsWithChapterInfo)
            {
                return specificChapterTitle;
            }
            else if (!string.IsNullOrEmpty(specificChapterTitle))
            {
                return $"Chương {chapterNumberString}: {specificChapterTitle}";
            }
            else
            {
                return $"Chương {chapterNumberString}";
            }
        }

        public string ExtractChapterNumber(ChapterAttributes attributes)
        {
            return attributes.ChapterNumber ?? "?";
        }

        public Dictionary<string, List<string>> ExtractAlternativeTitles(List<Dictionary<string, string>>? altTitlesList)
        {
            var altTitlesDictionary = new Dictionary<string, List<string>>();
            if (altTitlesList == null) return altTitlesDictionary;

            try
            {
                foreach (var altTitleDict in altTitlesList)
                {
                    if (altTitleDict != null && altTitleDict.Any())
                    {
                        var langKey = altTitleDict.Keys.First();
                        var titleText = altTitleDict[langKey];

                        if (!string.IsNullOrEmpty(titleText))
                        {
                            if (!altTitlesDictionary.ContainsKey(langKey))
                            {
                                altTitlesDictionary[langKey] = new List<string>();
                            }
                            altTitlesDictionary[langKey].Add(titleText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tiêu đề thay thế từ List.");
            }

            return altTitlesDictionary;
        }

        public string ExtractPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary)
        {
            if (altTitlesDictionary == null || !altTitlesDictionary.Any()) return "";

            if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
            if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First();
            return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
        }

        private static Dictionary<string, string> InitializeTagTranslations()
        {
            return new Dictionary<string, string>
            {
                { "Oneshot", "Oneshot" }, { "Thriller", "Hồi hộp" }, { "Award Winning", "Đạt giải thưởng" },
                { "Reincarnation", "Chuyển sinh" }, { "Sci-Fi", "Khoa học viễn tưởng" }, { "Time Travel", "Du hành thời gian" },
                { "Genderswap", "Chuyển giới" }, { "Loli", "Loli" }, { "Traditional Games", "Trò chơi truyền thống" },
                { "Official Colored", "Bản màu chính thức" }, { "Historical", "Lịch sử" }, { "Monsters", "Quái vật" },
                { "Action", "Hành động" }, { "Demons", "Ác quỷ" }, { "Psychological", "Tâm lý" }, { "Ghosts", "Ma" },
                { "Animals", "Động vật" }, { "Long Strip", "Truyện dài" }, { "Romance", "Lãng mạn" }, { "Ninja", "Ninja" },
                { "Comedy", "Hài hước" }, { "Mecha", "Robot" }, { "Anthology", "Tuyển tập" }, { "Boys' Love", "Tình yêu nam giới" },
                { "Incest", "Loạn luân" }, { "Crime", "Tội phạm" }, { "Survival", "Sinh tồn" }, { "Zombies", "Zombie" },
                { "Reverse Harem", "Harem đảo" }, { "Sports", "Thể thao" }, { "Superhero", "Siêu anh hùng" },
                { "Martial Arts", "Võ thuật" }, { "Fan Colored", "Bản màu fanmade" }, { "Samurai", "Samurai" },
                { "Magical Girls", "Ma pháp thiếu nữ" }, { "Mafia", "Mafia" }, { "Adventure", "Phiêu lưu" },
                { "Self-Published", "Tự xuất bản" }, { "Virtual Reality", "Thực tế ảo" }, { "Office Workers", "Nhân viên văn phòng" },
                { "Video Games", "Trò chơi điện tử" }, { "Post-Apocalyptic", "Hậu tận thế" }, { "Sexual Violence", "Bạo lực tình dục" },
                { "Crossdressing", "Giả trang khác giới" }, { "Magic", "Phép thuật" }, { "Girls' Love", "Tình yêu nữ giới" },
                { "Harem", "Harem" }, { "Military", "Quân đội" }, { "Wuxia", "Võ hiệp" }, { "Isekai", "Dị giới" },
                { "4-Koma", "4-Koma" }, { "Doujinshi", "Doujinshi" }, { "Philosophical", "Triết học" }, { "Gore", "Bạo lực" },
                { "Drama", "Kịch tính" }, { "Medical", "Y học" }, { "School Life", "Học đường" }, { "Horror", "Kinh dị" },
                { "Fantasy", "Kỳ ảo" }, { "Villainess", "Nữ phản diện" }, { "Vampires", "Ma cà rồng" },
                { "Delinquents", "Học sinh cá biệt" }, { "Monster Girls", "Monster Girls" }, { "Shota", "Shota" },
                { "Police", "Cảnh sát" }, { "Web Comic", "Web Comic" }, { "Slice of Life", "Đời thường" },
                { "Aliens", "Người ngoài hành tinh" }, { "Cooking", "Nấu ăn" }, { "Supernatural", "Siêu nhiên" },
                { "Mystery", "Bí ẩn" }, { "Adaptation", "Chuyển thể" }, { "Music", "Âm nhạc" }, { "Full Color", "Bản màu đầy đủ" },
                { "Tragedy", "Bi kịch" }, { "Gyaru", "Gyaru" }
            };
        }
    }
}
```

---

### Bước 1.6: Tạo file README.md cho `MangaDexLib`

Tạo một file `README.md` trong thư mục gốc của `MangaDexLib` để mô tả project.

```markdown
// MangaDexLib/README.md
# MangaDexLib

`MangaDexLib` là một thư viện .NET độc lập được thiết kế để đóng gói tất cả các tương tác với API MangaDex, được tách ra từ dự án `MangaReader_WebUI`.

## Mục đích

-   **Tách biệt logic:** Tách biệt hoàn toàn logic gọi API MangaDex khỏi logic giao diện người dùng của ứng dụng chính.
-   **Tái sử dụng:** Tạo ra một thành phần có thể tái sử dụng trong các dự án khác nếu cần tương tác với MangaDex.
-   **Tổ chức code:** Giữ cho project chính (`MangaReader_WebUI`) gọn gàng và tập trung vào nguồn dữ liệu của `MangaReaderLib`.

## Cấu trúc

-   **`Models/`**: Chứa các lớp C# (DTOs) đại diện cho cấu trúc dữ liệu JSON trả về từ MangaDex API (ví dụ: `Manga`, `Chapter`, `Cover`...).
-   **`Services/APIServices/`**: Chứa các lớp service chịu trách nhiệm thực hiện các cuộc gọi HTTP đến backend proxy và deserialize phản hồi.
-   **`DataProcessing/`**: Chứa các lớp service xử lý và chuyển đổi dữ liệu thô từ API thành các định dạng dễ sử dụng hơn (Mappers, Extractors).
-   **`api.yaml`**: Tài liệu OpenAPI Specification của MangaDex API để tham khảo.

## Cách Hoạt Động

Thư viện này không gọi trực tiếp đến `api.mangadex.org`. Thay vào đó, nó tương tác với một **Backend API** (được cấu hình qua `BackendApi:BaseUrl`) đóng vai trò là một proxy. Điều này giúp:

-   Ẩn API key (nếu có).
-   Xử lý rate-limiting phía server.
-   Cache các phản hồi từ MangaDex để tăng hiệu suất.
-   Đơn giản hóa logic phía client.
```

Sau khi hoàn thành các bước trên, bạn đã tách thành công toàn bộ mã nguồn liên quan đến MangaDex ra một project library độc lập và có thể build được.
````