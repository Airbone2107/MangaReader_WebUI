Chào bạn,

Bạn đã phân tích rất chính xác! Log lỗi cho thấy server `MangaReader_ManagerUI` khi gọi đến API backend (`https://localhost:7262`) đã bị từ chối với lỗi `401 Unauthorized`. Điều này có nghĩa là, mặc dù React Client đã gửi token lên server `MangaReader_ManagerUI`, nhưng server này đã **không gửi tiếp** token đó đến API backend khi nó hoạt động như một proxy.

Chúng ta cần sửa `MangaReader_ManagerUI.Server` để nó lấy `Authorization` header từ request đến và gắn nó vào request đi.

Dưới đây là file `TODO.md` chi tiết để hướng dẫn bạn khắc phục.

***

# TODO.md: Sửa lỗi 401 Unauthorized và Truyền Token Xác Thực

Hướng dẫn này sẽ giúp bạn khắc phục lỗi `401 Unauthorized` bằng cách cấu hình cho proxy server (`MangaReader_ManagerUI.Server`) tự động chuyển tiếp token xác thực (JWT) đến API backend.

## Phân tích nguyên nhân

1.  **React Client -> Proxy Server:** Luồng này đã đúng. `apiClient.js` trong React app đã đính kèm `Authorization: Bearer <token>` vào request gửi đến `/api/users`.
2.  **Proxy Server -> Backend API:** Đây là nơi xảy ra lỗi. `HttpClient` được sử dụng bởi `MangaReaderLib` trong server proxy đã không tự động sao chép `Authorization` header từ request gốc. Do đó, request đến backend API (`https://localhost:7262/api/Users`) không có token và bị từ chối với lỗi 401.

## Giải pháp

Chúng ta sẽ sử dụng một `DelegatingHandler` trong ASP.NET Core. Đây là một phương pháp chuẩn và hiệu quả để chặn các `HttpRequestMessage` đi ra, đọc `Authorization` header từ `HttpContext` của request gốc, và gắn nó vào request đang được gửi đi.

### Bước 1: Tạo `AuthenticationHeaderHandler`

Tạo một file mới trong project `MangaReader_ManagerUI.Server` để xử lý việc chuyển tiếp header.

<!-- file path="MangaReader_ManagerUI\MangaReader_ManagerUI.Server\AuthenticationHeaderHandler.cs" -->
```csharp
using System.Net.Http.Headers;

namespace MangaReader_ManagerUI.Server
{
    /// <summary>
    /// Một DelegatingHandler để chặn các yêu cầu HTTP đi ra,
    /// đọc token xác thực từ HttpContext của yêu cầu đến,
    /// và gắn nó vào yêu cầu đang được gửi đến API backend.
    /// </summary>
    public class AuthenticationHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationHeaderHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Lấy HttpContext của request hiện tại từ client (React) đến server này.
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) == true)
            {
                // Sao chép header "Authorization" từ request gốc sang request đang được gửi đi.
                // Điều này đảm bảo token được chuyển tiếp đến API backend.
                if (AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
                {
                    request.Headers.Authorization = headerValue;
                }
            }

            // Tiếp tục gửi request đi sau khi đã thêm header (nếu có).
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
```

### Bước 2: Cập nhật `Program.cs` để sử dụng `AuthenticationHeaderHandler`

Bây giờ, chúng ta cần đăng ký `AuthenticationHeaderHandler` và `IHttpContextAccessor` vào DI container, và cấu hình `HttpClient` của `ApiClient` để sử dụng handler này.

<!-- file path="MangaReader_ManagerUI\MangaReader_ManagerUI.Server\Program.cs" -->
```csharp
using MangaReader_ManagerUI.Server; // Thêm using này
using MangaReaderLib.Services.Implementations;
using MangaReaderLib.Services.Interfaces;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Thêm IHttpContextAccessor để có thể truy cập HttpContext từ các service khác
builder.Services.AddHttpContextAccessor();
// Đăng ký handler như một transient service
builder.Services.AddTransient<AuthenticationHeaderHandler>();


// 1. Cấu hình HttpClientFactory và BaseAddress cho MangaReaderAPI (Backend thực sự)
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    string apiBaseUrl = builder.Configuration["MangaReaderApiSettings:BaseUrl"]
                        ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured.");
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
})
// Thêm Message Handler để tự động đính kèm token
.AddHttpMessageHandler<AuthenticationHeaderHandler>();


// 2. Đăng ký các Client Services từ MangaReaderLib
// Đăng ký implementation cụ thể
builder.Services.AddScoped<AuthorClient>();
builder.Services.AddScoped<MangaClient>();
builder.Services.AddScoped<TagClient>();
builder.Services.AddScoped<TagGroupClient>();
builder.Services.AddScoped<CoverArtClient>();
builder.Services.AddScoped<TranslatedMangaClient>();
builder.Services.AddScoped<ChapterClient>();
builder.Services.AddScoped<ChapterPageClient>();
builder.Services.AddScoped<AuthClient>();
builder.Services.AddScoped<UserClient>();
builder.Services.AddScoped<RoleClient>();

// Đăng ký các interface để DI có thể resolve chúng tới implementation cụ thể
// Author
builder.Services.AddScoped<IAuthorClient>(p => p.GetRequiredService<AuthorClient>());
builder.Services.AddScoped<IAuthorReader>(p => p.GetRequiredService<AuthorClient>());
builder.Services.AddScoped<IAuthorWriter>(p => p.GetRequiredService<AuthorClient>());

// Manga
builder.Services.AddScoped<IMangaClient>(p => p.GetRequiredService<MangaClient>());
builder.Services.AddScoped<IMangaReader>(p => p.GetRequiredService<MangaClient>());
builder.Services.AddScoped<IMangaWriter>(p => p.GetRequiredService<MangaClient>());

// Tag
builder.Services.AddScoped<ITagClient>(p => p.GetRequiredService<TagClient>());
builder.Services.AddScoped<ITagReader>(p => p.GetRequiredService<TagClient>());
builder.Services.AddScoped<ITagWriter>(p => p.GetRequiredService<TagClient>());

// TagGroup
builder.Services.AddScoped<ITagGroupClient>(p => p.GetRequiredService<TagGroupClient>());
builder.Services.AddScoped<ITagGroupReader>(p => p.GetRequiredService<TagGroupClient>());
builder.Services.AddScoped<ITagGroupWriter>(p => p.GetRequiredService<TagGroupClient>());

// CoverArt
builder.Services.AddScoped<ICoverArtClient>(p => p.GetRequiredService<CoverArtClient>());
builder.Services.AddScoped<ICoverArtReader>(p => p.GetRequiredService<CoverArtClient>());
builder.Services.AddScoped<ICoverArtWriter>(p => p.GetRequiredService<CoverArtClient>());

// TranslatedManga
builder.Services.AddScoped<ITranslatedMangaClient>(p => p.GetRequiredService<TranslatedMangaClient>());
builder.Services.AddScoped<ITranslatedMangaReader>(p => p.GetRequiredService<TranslatedMangaClient>());
builder.Services.AddScoped<ITranslatedMangaWriter>(p => p.GetRequiredService<TranslatedMangaClient>());

// Chapter
builder.Services.AddScoped<IChapterClient>(p => p.GetRequiredService<ChapterClient>());
builder.Services.AddScoped<IChapterReader>(p => p.GetRequiredService<ChapterClient>());
builder.Services.AddScoped<IChapterWriter>(p => p.GetRequiredService<ChapterClient>());

// ChapterPage
builder.Services.AddScoped<IChapterPageClient>(p => p.GetRequiredService<ChapterPageClient>());
builder.Services.AddScoped<IChapterPageReader>(p => p.GetRequiredService<ChapterPageClient>());
builder.Services.AddScoped<IChapterPageWriter>(p => p.GetRequiredService<ChapterPageClient>());

// NEW Auth/User/Role Clients
builder.Services.AddScoped<IAuthClient>(p => p.GetRequiredService<AuthClient>());
builder.Services.AddScoped<IUserClient>(p => p.GetRequiredService<UserClient>());
builder.Services.AddScoped<IRoleClient>(p => p.GetRequiredService<RoleClient>());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
```

## Kết luận

Sau khi thực hiện các thay đổi trên:

1.  Mỗi khi React client gọi một API tới server proxy (ví dụ `/api/users`), nó sẽ đính kèm token.
2.  `AuthenticationHeaderHandler` trên server proxy sẽ chặn cuộc gọi HTTP mà `UserClient` chuẩn bị gửi đi.
3.  Handler này sẽ lấy `Authorization` header từ request gốc của React và gắn vào request chuẩn bị gửi tới API backend.
4.  API backend sẽ nhận được request với token hợp lệ và trả về dữ liệu thay vì lỗi `401 Unauthorized`.

Hãy build lại project và thử lại. Lỗi 401 của bạn sẽ được khắc phục.