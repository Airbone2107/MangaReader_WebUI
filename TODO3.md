# TODO: Sửa Lỗi và Hoàn Tất Tái Cấu Trúc `MangaReader_WebUI`

Tài liệu này hướng dẫn sửa các lỗi biên dịch sau khi dọn dẹp mã nguồn MangaDex và hoàn thành việc chuyển đổi sang sử dụng `MangaReaderLib` làm nguồn dữ liệu duy nhất.

## Bước 1: Tạo Lại Các Models và Services Tiện Ích Đã Xóa

Do đã xóa thư mục `Models/MangaDex` và một số service, chúng ta cần tạo lại một vài model chung và service tiện ích để các phần khác của ứng dụng có thể hoạt động.

### 1.1. Tạo Model chung cho Phản hồi Trang Ảnh

Tạo một file mới để thay thế cho `AtHomeServerResponse` đã bị xóa.

<!-- MangaReader_WebUI\Models\ViewModels\Common\PageServerResponse.cs -->
```csharp
namespace MangaReader.WebUI.Models.ViewModels.Common
{
    public class PageServerResponse
    {
        public string Result { get; set; } = "ok";
        public string BaseUrl { get; set; } = string.Empty; // Sẽ rỗng cho MangaReaderLib
        public PageChapterData Chapter { get; set; } = new();
    }

    public class PageChapterData
    {
        public string Hash { get; set; } = string.Empty; // Sẽ là ChapterId
        public List<string> Data { get; set; } = new(); // Sẽ chứa URL ảnh đầy đủ
        public List<string> DataSaver { get; set; } = new();
    }
}
```

### 1.2. Tạo Model chung cho Danh sách Tag

Tạo các file mới để thay thế cho `TagListResponse` đã bị xóa.

<!-- MangaReader_WebUI\Models\ViewModels\Manga\TagViewModel.cs -->
```csharp
namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class TagViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "tag";
        public TagAttributesViewModel Attributes { get; set; } = new();
    }

    public class TagAttributesViewModel
    {
        public Dictionary<string, string> Name { get; set; } = new();
        public Dictionary<string, string> Description { get; set; } = new();
        public string Group { get; set; } = "other";
        public int Version { get; set; } = 1;
    }
}
```

<!-- MangaReader_WebUI\Models\ViewModels\Manga\TagListViewModel.cs -->
```csharp
namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class TagListViewModel
    {
        public string Result { get; set; } = "ok";
        public string Response { get; set; } = "collection";
        public List<TagViewModel> Data { get; set; } = new();
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int Total { get; set; }
    }
}
```

### 1.3. Tạo Lại Service Trích Xuất Dữ Liệu (`IMangaDataExtractor`)

Service này rất quan trọng, nó trích xuất và định dạng dữ liệu từ DTO của API sang dạng thân thiện với người dùng.

Tạo interface mới:
<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\IMangaDataExtractor.cs -->
```csharp
using MangaReaderLib.DTOs.Chapters;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces
{
    public interface IMangaDataExtractor
    {
        string ExtractChapterDisplayTitle(ChapterAttributesDto attributes);
        string ExtractChapterNumber(ChapterAttributesDto attributes);
        string ExtractAndTranslateStatus(string? status);
    }
}
```

Tạo implementation mới cho service:
<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs -->
```csharp
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.UtilityServices;
using MangaReaderLib.DTOs.Chapters;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services
{
    public class MangaDataExtractorService : IMangaDataExtractor
    {
        private readonly LocalizationService _localizationService;

        public MangaDataExtractorService(LocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public string ExtractChapterDisplayTitle(ChapterAttributesDto attributes)
        {
            Debug.Assert(attributes != null, "ChapterAttributes không được null.");

            string chapterNumberString = attributes.ChapterNumber ?? "?";
            string titleFromApi = attributes.Title?.Trim() ?? "";

            if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
            {
                return !string.IsNullOrEmpty(titleFromApi) ? titleFromApi : "Oneshot";
            }

            string patternChapterVn = $"^Chương\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";
            string patternChapterEn = $"^Chapter\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";

            bool startsWithChapterInfo = Regex.IsMatch(titleFromApi, patternChapterVn, RegexOptions.IgnoreCase) ||
                                         Regex.IsMatch(titleFromApi, patternChapterEn, RegexOptions.IgnoreCase);

            if (startsWithChapterInfo)
            {
                return titleFromApi;
            }
            else if (!string.IsNullOrEmpty(titleFromApi))
            {
                return $"Chương {chapterNumberString}: {titleFromApi}";
            }
            else
            {
                return $"Chương {chapterNumberString}";
            }
        }

        public string ExtractChapterNumber(ChapterAttributesDto attributes)
        {
            Debug.Assert(attributes != null, "ChapterAttributes không được null.");
            return attributes.ChapterNumber ?? "?";
        }

        public string ExtractAndTranslateStatus(string? status)
        {
            return _localizationService.GetStatus(status);
        }
    }
}
```

### 1.4. Đăng ký các Service mới trong `Program.cs`

Mở file `MangaReader_WebUI\Program.cs` và đảm bảo các service mới được đăng ký.

<!-- MangaReader_WebUI\Program.cs -->
```csharp
using MangaReader.WebUI.Infrastructure;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces; // Thêm using này
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services; // Thêm using này
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaPageService;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc.Razor;
using MangaReaderLib.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Thêm cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Cấu hình logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Đảm bảo HttpContextAccessor được đăng ký
builder.Services.AddHttpContextAccessor();

// Cấu hình HttpClient để gọi Backend API (CHỈ DÙNG CHO XÁC THỰC)
builder.Services.AddHttpClient("BackendApiClient", client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Thêm HttpClient cho MangaReaderLib API
builder.Services.AddHttpClient("MangaReaderLibApiClient", client =>
{
    var baseUrl = builder.Configuration["MangaReaderApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Đăng ký các MangaReaderLib Mappers
builder.Services.AddScoped<IMangaReaderLibToMangaViewModelMapper, MangaReaderLibToMangaViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaDetailViewModelMapper, MangaReaderLibToMangaDetailViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterViewModelMapper, MangaReaderLibToChapterViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToSimpleChapterInfoMapper, MangaReaderLibToSimpleChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaInfoViewModelMapper, MangaReaderLibToMangaInfoViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterInfoMapper, MangaReaderLibToChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToTagListResponseMapper, MangaReaderLibToTagListResponseMapper>();
builder.Services.AddScoped<IMangaReaderLibToAtHomeServerResponseMapper, MangaReaderLibToAtHomeServerResponseMapper>();

// Đăng ký MangaDataExtractor
builder.Services.AddScoped<IMangaDataExtractor, MangaDataExtractorService>();

// Đăng ký các MangaReaderLib API Clients
builder.Services.AddScoped<IMangaReaderLibApiClient, MangaReaderLibApiClientService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("MangaReaderLibApiClient");
    var innerClientLogger = provider.GetRequiredService<ILogger<ApiClient>>();
    var wrapperLoggerForApiClient = provider.GetRequiredService<ILogger<MangaReaderLibApiClientService>>();
    return new MangaReaderLibApiClientService(httpClient, innerClientLogger, wrapperLoggerForApiClient);
});
builder.Services.AddScoped<IMangaReaderLibAuthorClient, MangaReaderLibAuthorClientService>();
builder.Services.AddScoped<IMangaReaderLibChapterClient, MangaReaderLibChapterClientService>();
builder.Services.AddScoped<IMangaReaderLibChapterPageClient, MangaReaderLibChapterPageClientService>();
builder.Services.AddScoped<IMangaReaderLibCoverApiService, MangaReaderLibCoverApiService>();
builder.Services.AddScoped<IMangaReaderLibMangaClient, MangaReaderLibMangaClientService>();
builder.Services.AddScoped<IMangaReaderLibTagClient, MangaReaderLibTagClientService>();
builder.Services.AddScoped<IMangaReaderLibTagGroupClient, MangaReaderLibTagGroupClientService>();
builder.Services.AddScoped<IMangaReaderLibTranslatedMangaClient, MangaReaderLibTranslatedMangaClientService>();

// Đăng ký các service liên quan đến xác thực
builder.Services.AddScoped<IUserService, UserService>();

// Đăng ký các service tiện ích
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<JsonConversionService>();
builder.Services.AddScoped<ViewRenderService>();

// Đăng ký các service tầng cao của ứng dụng
builder.Services.AddScoped<IMangaFollowService, MangaFollowService>();
builder.Services.AddScoped<IFollowedMangaService, FollowedMangaService>();
builder.Services.AddScoped<IMangaInfoService, MangaInfoService>();
builder.Services.AddScoped<IReadingHistoryService, ReadingHistoryService>();
builder.Services.AddScoped<ChapterService>();
builder.Services.AddScoped<MangaIdService>();
builder.Services.AddScoped<ChapterLanguageServices>();
builder.Services.AddScoped<ChapterReadingServices>();
builder.Services.AddScoped<MangaDetailsService>();
builder.Services.AddScoped<MangaSearchService>();

// Cấu hình Razor View Engine để sử dụng View Location Expander tùy chỉnh
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "auth_callback",
    pattern: "auth/callback",
    defaults: new { controller = "Auth", action = "Callback" });

app.Run();
```

## Bước 2: Cập Nhật Các File Bị Lỗi

Bây giờ chúng ta sẽ cập nhật các file đang gây ra lỗi biên dịch.

### 2.1. Cập nhật các file Mapper và Interface

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToAtHomeServerResponseMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Common; // <-- SỬA Ở ĐÂY
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Chapters;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToAtHomeServerResponseMapper
    {
        PageServerResponse MapToAtHomeServerResponse( // <-- SỬA Ở ĐÂY
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored);
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToAtHomeServerResponseMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Common; // <-- SỬA Ở ĐÂY
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Chapters;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToAtHomeServerResponseMapper : IMangaReaderLibToAtHomeServerResponseMapper
    {
        private readonly ILogger<MangaReaderLibToAtHomeServerResponseMapper> _logger;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibToAtHomeServerResponseMapper(
            ILogger<MangaReaderLibToAtHomeServerResponseMapper> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured for AtHomeServerResponseMapper.");
        }

        public PageServerResponse MapToAtHomeServerResponse( // <-- SỬA Ở ĐÂY
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored)
        {
            Debug.Assert(chapterPagesData != null, "chapterPagesData không được null khi mapping.");
            Debug.Assert(!string.IsNullOrEmpty(chapterId), "chapterId không được rỗng.");
            
            _logger.LogInformation("[MRLib AtHome Mapper] Bắt đầu map chapter pages cho ChapterId: {ChapterId}. Dữ liệu đầu vào: {ChapterPagesDataJson}", 
                chapterId, JsonSerializer.Serialize(chapterPagesData));

            var pages = new List<string>();
            if (chapterPagesData.Data != null && chapterPagesData.Data.Any())
            {
                var sortedPages = chapterPagesData.Data.OrderBy(p => p.Attributes.PageNumber);
                 _logger.LogDebug("[MRLib AtHome Mapper] Sorted pages DTOs: {SortedPagesJson}", JsonSerializer.Serialize(sortedPages));

                foreach (var pageDto in sortedPages)
                {
                    if (pageDto?.Attributes?.PublicId != null)
                    {
                        var imageUrl = $"{_cloudinaryBaseUrl}/{pageDto.Attributes.PublicId}";
                        pages.Add(imageUrl);
                        _logger.LogDebug("[MRLib AtHome Mapper] Mapped MangaReaderLib page: ChapterId={ChapterId}, PageNumber={PageNumber}, PublicId={PublicId} to Cloudinary URL: {ImageUrl}",
                            chapterId, pageDto.Attributes.PageNumber, pageDto.Attributes.PublicId, imageUrl);
                    }
                    else
                    {
                        _logger.LogWarning("[MRLib AtHome Mapper] Skipping page due to missing PublicId. ChapterId={ChapterId}, PageDtoId={PageDtoId}", chapterId, pageDto?.Id);
                    }
                }
            }
            else
            {
                _logger.LogWarning("[MRLib AtHome Mapper] No page data found in chapterPagesData for ChapterId={ChapterId}", chapterId);
            }

            var result = new PageServerResponse // <-- SỬA Ở ĐÂY
            {
                Result = "ok",
                BaseUrl = "",
                Chapter = new PageChapterData // <-- SỬA Ở ĐÂY
                {
                    Hash = chapterId,
                    Data = pages,
                    DataSaver = pages
                }
            };
            _logger.LogInformation("[MRLib AtHome Mapper] Kết thúc map chapter pages cho ChapterId: {ChapterId}. Kết quả: {ResultJson}", 
                chapterId, JsonSerializer.Serialize(result));
            return result;
        }
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToTagListResponseMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga; // <-- SỬA Ở ĐÂY
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToTagListResponseMapper
    {
        TagListViewModel MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsData); // <-- SỬA Ở ĐÂY
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToTagListResponseMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga; // <-- SỬA Ở ĐÂY
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToTagListResponseMapper : IMangaReaderLibToTagListResponseMapper
    {
        private readonly ILogger<MangaReaderLibToTagListResponseMapper> _logger;

        public MangaReaderLibToTagListResponseMapper(ILogger<MangaReaderLibToTagListResponseMapper> logger)
        {
            _logger = logger;
        }

        public TagListViewModel MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsDataFromLib) // <-- SỬA Ở ĐÂY
        {
            Debug.Assert(tagsDataFromLib != null, "tagsDataFromLib không được null khi mapping thành TagListViewModel.");

            var tagListViewModel = new TagListViewModel // <-- SỬA Ở ĐÂY
            {
                Result = tagsDataFromLib.Result,
                Response = tagsDataFromLib.ResponseType,
                Limit = tagsDataFromLib.Limit,
                Offset = tagsDataFromLib.Offset,
                Total = tagsDataFromLib.Total,
                Data = new List<TagViewModel>()
            };

            if (tagsDataFromLib.Data != null)
            {
                foreach (var libTagResource in tagsDataFromLib.Data)
                {
                    if (libTagResource?.Attributes != null)
                    {
                        try
                        {
                            var libTagAttributes = libTagResource.Attributes;
                            
                            var dexTagAttributes = new TagAttributesViewModel // <-- SỬA Ở ĐÂY
                            {
                                Name = new Dictionary<string, string> { { "en", libTagAttributes.Name } },
                                Description = new Dictionary<string, string>(),
                                Group = libTagAttributes.TagGroupName?.ToLowerInvariant() ?? "other",
                                Version = 1
                            };

                            var dexTag = new TagViewModel // <-- SỬA Ở ĐÂY
                            {
                                Id = libTagResource.Id,
                                Type = "tag",
                                Attributes = dexTagAttributes
                            };
                            tagListViewModel.Data.Add(dexTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi mapping MangaReaderLib Tag ID {TagId} sang TagViewModel.", libTagResource.Id);
                            continue;
                        }
                    }
                }
            }
            return tagListViewModel;
        }
    }
}
```

### 2.2. Cập nhật các file Service bị lỗi

<!-- MangaReader_WebUI\Services\MangaServices\ChapterServices\ChapterLanguageServices.cs -->
```csharp
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // <-- SỬA Ở ĐÂY

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterLanguageServices
    {
        private readonly IMangaReaderLibChapterClient _chapterClient; // <-- SỬA Ở ĐÂY
        private readonly IMangaReaderLibTranslatedMangaClient _translatedMangaClient;
        private readonly ILogger<ChapterLanguageServices> _logger;

        public ChapterLanguageServices(
            IMangaReaderLibChapterClient chapterClient, // <-- SỬA Ở ĐÂY
            IMangaReaderLibTranslatedMangaClient translatedMangaClient,
            ILogger<ChapterLanguageServices> logger)
        {
            _chapterClient = chapterClient;
            _translatedMangaClient = translatedMangaClient;
            _logger = logger;
        }

        public async Task<string> GetChapterLanguageAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId) || !Guid.TryParse(chapterId, out var chapterGuid))
            {
                _logger.LogWarning("ChapterId không hợp lệ khi gọi GetChapterLanguageAsync: {ChapterId}", chapterId);
                throw new ArgumentException("ChapterId không hợp lệ", nameof(chapterId));
            }

            _logger.LogInformation("Đang lấy thông tin ngôn ngữ cho Chapter: {ChapterId}", chapterId);

            try
            {
                var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);
                if (chapterResponse?.Data?.Relationships == null)
                {
                     _logger.LogError("Không lấy được thông tin hoặc relationships cho chapter {ChapterId}.", chapterId);
                    throw new InvalidOperationException($"Không thể lấy thông tin cho chapter {chapterId}");
                }

                var tmRelationship = chapterResponse.Data.Relationships.FirstOrDefault(r => r.Type.Equals("translated_manga", StringComparison.OrdinalIgnoreCase));
                if (tmRelationship != null && Guid.TryParse(tmRelationship.Id, out var tmGuid))
                {
                    var tmDetails = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                    if (!string.IsNullOrEmpty(tmDetails?.Data?.Attributes?.LanguageKey))
                    {
                        string lang = tmDetails.Data.Attributes.LanguageKey;
                        _logger.LogInformation("Đã lấy được ngôn ngữ: {Language} cho Chapter: {ChapterId}", lang, chapterId);
                        return lang;
                    }
                }

                _logger.LogWarning("Không thể xác định ngôn ngữ cho chapter {ChapterId}, trả về 'en' mặc định.", chapterId);
                return "en"; // Fallback
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy ngôn ngữ cho chapter {ChapterId}", chapterId);
                throw;
            }
        }
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\ChapterServices\MangaIdService.cs -->
```csharp
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // <-- SỬA Ở ĐÂY

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class MangaIdService
    {
        private readonly IMangaReaderLibChapterClient _chapterClient; // <-- SỬA Ở ĐÂY
        private readonly ILogger<MangaIdService> _logger;

        public MangaIdService(
            IMangaReaderLibChapterClient chapterClient, // <-- SỬA Ở ĐÂY
            ILogger<MangaIdService> logger)
        {
            _chapterClient = chapterClient;
            _logger = logger;
        }

        public async Task<string> GetMangaIdFromChapterAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId) || !Guid.TryParse(chapterId, out var chapterGuid))
            {
                _logger.LogWarning("ChapterId không hợp lệ khi gọi GetMangaIdFromChapterAsync: {ChapterId}", chapterId);
                throw new ArgumentException("ChapterId không hợp lệ.", nameof(chapterId));
            }

            _logger.LogInformation("Đang lấy MangaID cho Chapter: {ChapterId}", chapterId);

            try
            {
                var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);

                if (chapterResponse?.Data?.Relationships != null)
                {
                    var mangaRelationship = chapterResponse.Data.Relationships
                                                .FirstOrDefault(r => r.Type.Equals("manga", StringComparison.OrdinalIgnoreCase));

                    if (mangaRelationship != null)
                    {
                        string mangaId = mangaRelationship.Id;
                        _logger.LogInformation("Đã tìm thấy MangaID: {MangaId} cho Chapter: {ChapterId}", mangaId, chapterId);
                        return mangaId;
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy relationship 'manga' cho Chapter: {ChapterId}", chapterId);
                        throw new KeyNotFoundException($"Không tìm thấy relationship 'manga' cho Chapter: {chapterId}");
                    }
                }
                else
                {
                    _logger.LogError("Không lấy được thông tin hoặc relationships cho chapter {ChapterId}. Response: {Result}", chapterId, chapterResponse?.Result);
                    throw new InvalidOperationException($"Không thể lấy thông tin relationships cho chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy MangaID cho chapter {ChapterId}", chapterId);
                throw;
            }
        }
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\ChapterServices\ChapterService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // <-- SỬA Ở ĐÂY
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers; // <-- SỬA Ở ĐÂY
using System.Globalization;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient; // <-- THAY ĐỔI
        private readonly IMangaReaderLibChapterClient _chapterClient; // <-- THAY ĐỔI
        private readonly ILogger<ChapterService> _logger;
        private readonly IMangaReaderLibToChapterViewModelMapper _chapterViewModelMapper; // <-- SỬA Ở ĐÂY
        private readonly IMangaReaderLibToSimpleChapterInfoMapper _simpleChapterInfoMapper; // <-- SỬA Ở ĐÂY

        public ChapterService(
            IMangaReaderLibMangaClient mangaClient, // <-- SỬA Ở ĐÂY
            IMangaReaderLibChapterClient chapterClient, // <-- SỬA Ở ĐÂY
            ILogger<ChapterService> logger,
            IMangaReaderLibToChapterViewModelMapper chapterViewModelMapper, // <-- SỬA Ở ĐÂY
            IMangaReaderLibToSimpleChapterInfoMapper simpleChapterInfoMapper) // <-- SỬA Ở ĐÂY
        {
            _mangaClient = mangaClient;
            _chapterClient = chapterClient;
            _logger = logger;
            _chapterViewModelMapper = chapterViewModelMapper;
            _simpleChapterInfoMapper = simpleChapterInfoMapper;
        }
        
        public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string languages = "vi,en")
        {
            try
            {
                if (!Guid.TryParse(mangaId, out var mangaGuid))
                {
                    _logger.LogError("MangaId không hợp lệ: {MangaId}", mangaId);
                    return new List<ChapterViewModel>();
                }

                var targetLanguages = languages.Split(',').Select(l => l.Trim().ToLowerInvariant()).Where(l => !string.IsNullOrEmpty(l)).ToList();
                if (!targetLanguages.Any()) return new List<ChapterViewModel>();

                var translations = await _mangaClient.GetMangaTranslationsAsync(mangaGuid);
                if (translations?.Data == null || !translations.Data.Any())
                {
                    _logger.LogWarning("Không tìm thấy bản dịch nào cho manga {MangaId}", mangaId);
                    return new List<ChapterViewModel>();
                }
                
                var allChapterViewModels = new List<ChapterViewModel>();

                foreach(var lang in targetLanguages)
                {
                    var translation = translations.Data.FirstOrDefault(t => t.Attributes.LanguageKey.Equals(lang, StringComparison.OrdinalIgnoreCase));
                    if (translation != null && Guid.TryParse(translation.Id, out var tmGuid))
                    {
                        var chapterListResponse = await _chapterClient.GetChaptersByTranslatedMangaAsync(tmGuid, limit: 5000); // Lấy nhiều chapters
                        if(chapterListResponse?.Data != null)
                        {
                            foreach (var chapterDto in chapterListResponse.Data)
                            {
                                allChapterViewModels.Add(_chapterViewModelMapper.MapToChapterViewModel(chapterDto, lang));
                            }
                        }
                    }
                }
                
                return SortChaptersByNumberDescending(allChapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chapters cho manga {MangaId}", mangaId);
                return new List<ChapterViewModel>();
            }
        }

        private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
        {
            return chapters
                .OrderByDescending(c => ParseChapterNumber(c.Number) ?? double.MinValue)
                .ThenByDescending(c => c.PublishedAt)
                .ToList();
        }

        public Dictionary<string, List<ChapterViewModel>> GetChaptersByLanguage(List<ChapterViewModel> chapters)
        {
            var chaptersByLanguage = chapters.GroupBy(c => c.Language)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var language in chaptersByLanguage.Keys)
            {
                chaptersByLanguage[language] = SortChaptersByNumberAscending(chaptersByLanguage[language]);
            }

            return chaptersByLanguage;
        }

        private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
        {
            return chapters
                .OrderBy(c => ParseChapterNumber(c.Number) ?? double.MaxValue)
                .ThenBy(c => c.PublishedAt)
                .ToList();
        }

        public async Task<List<SimpleChapterInfoViewModel>> GetLatestChaptersAsync(string mangaId, int limit, string languages = "vi,en")
        {
            // Logic này có thể được tối ưu hóa, nhưng hiện tại lấy tất cả và sort
            var allChapters = await GetChaptersAsync(mangaId, languages);
            return allChapters
                .OrderByDescending(c => c.PublishedAt)
                .Take(limit)
                .Select(vm => new SimpleChapterInfoViewModel { ChapterId = vm.Id, DisplayTitle = vm.Title, PublishedAt = vm.PublishedAt})
                .ToList();
        }

        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\ChapterServices\ChapterReadingServices.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Common; // <-- THÊM USING NÀY
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // <-- SỬA Ở ĐÂY
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterReadingServices
    {
        private readonly IMangaReaderLibChapterPageClient _chapterPageClient; // <-- SỬA Ở ĐÂY
        private readonly IMangaReaderLibMangaClient _mangaClient; // <-- SỬA Ở ĐÂY
        private readonly IMangaReaderLibToAtHomeServerResponseMapper _atHomeResponseMapper; // <-- SỬA Ở ĐÂY
        private readonly MangaIdService _mangaIdService;
        private readonly ChapterLanguageServices _chapterLanguageServices;
        private readonly ChapterService _chapterService;
        private readonly ILogger<ChapterReadingServices> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMangaDataExtractor _mangaDataExtractor;

        public ChapterReadingServices(
            IMangaReaderLibChapterPageClient chapterPageClient, // <-- SỬA Ở ĐÂY
            IMangaReaderLibMangaClient mangaClient, // <-- SỬA Ở ĐÂY
            IMangaReaderLibToAtHomeServerResponseMapper atHomeResponseMapper, // <-- SỬA Ở ĐÂY
            MangaIdService mangaIdService,
            ChapterLanguageServices chapterLanguageServices,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ChapterReadingServices> logger,
            IMangaDataExtractor mangaDataExtractor)
        {
            _chapterPageClient = chapterPageClient;
            _mangaClient = mangaClient;
            _atHomeResponseMapper = atHomeResponseMapper;
            _mangaIdService = mangaIdService;
            _chapterLanguageServices = chapterLanguageServices;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _mangaDataExtractor = mangaDataExtractor;
        }

        public async Task<ChapterReadViewModel> GetChapterReadViewModel(string chapterId)
        {
            try
            {
                if (!Guid.TryParse(chapterId, out var chapterGuid))
                {
                    throw new ArgumentException("ChapterId không hợp lệ.", nameof(chapterId));
                }

                _logger.LogInformation("Đang tải chapter {ChapterId}", chapterId);

                var pagesResponse = await _chapterPageClient.GetChapterPagesAsync(chapterGuid, limit: 500);
                if (pagesResponse?.Data == null)
                {
                    _logger.LogError("Không thể lấy thông tin trang ảnh cho chapter {ChapterId}.", chapterId);
                    throw new Exception("Không thể tải trang ảnh cho chương này.");
                }

                var pageServerResponse = _atHomeResponseMapper.MapToAtHomeServerResponse(pagesResponse, chapterId, "");
                List<string> pages = pageServerResponse.Chapter.Data;
                
                _logger.LogInformation("Đã tạo {Count} URL ảnh cho chapter {ChapterId}", pages.Count, chapterId);

                string mangaId = await _mangaIdService.GetMangaIdFromChapterAsync(chapterId);
                _logger.LogInformation("Đã xác định được mangaId: {MangaId} cho chapter {ChapterId}", mangaId, chapterId);

                string currentChapterLanguage = await _chapterLanguageServices.GetChapterLanguageAsync(chapterId);
                _logger.LogInformation("Đã lấy được ngôn ngữ {Language} cho chapter", currentChapterLanguage);

                string mangaTitle = await GetMangaTitleAsync(mangaId);
                var chaptersList = await GetChaptersAsync(mangaId, currentChapterLanguage);

                var (currentChapterViewModel, prevChapterId, nextChapterId) = FindCurrentAndAdjacentChapters(chaptersList, chapterId);

                string displayChapterTitle = currentChapterViewModel.Title;
                string displayChapterNumber = currentChapterViewModel.Number;
                
                var viewModel = new ChapterReadViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = mangaTitle,
                    ChapterId = chapterId,
                    ChapterTitle = displayChapterTitle,
                    ChapterNumber = displayChapterNumber,
                    ChapterLanguage = currentChapterLanguage,
                    Pages = pages,
                    PrevChapterId = prevChapterId,
                    NextChapterId = nextChapterId,
                    SiblingChapters = chaptersList
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chapter {ChapterId}", chapterId);
                throw;
            }
        }
        
        public async Task<string> GetMangaTitleAsync(string mangaId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string sessionTitle = httpContext?.Session.GetString($"Manga_{mangaId}_Title");
            if (!string.IsNullOrEmpty(sessionTitle))
            {
                return sessionTitle;
            }
            
            if (!Guid.TryParse(mangaId, out var mangaGuid)) return "Không có tiêu đề";

            var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid);
            string title = mangaResponse?.Data?.Attributes?.Title ?? "Không có tiêu đề";

            if (httpContext != null && !string.IsNullOrEmpty(title) && title != "Không có tiêu đề")
            {
                httpContext.Session.SetString($"Manga_{mangaId}_Title", title);
            }
            return title;
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, string language)
        {
             var httpContext = _httpContextAccessor.HttpContext;
             var sessionChaptersJson = httpContext?.Session.GetString($"Manga_{mangaId}_Chapters_{language}");

             if (!string.IsNullOrEmpty(sessionChaptersJson))
             {
                 try
                 {
                     var chaptersList = JsonSerializer.Deserialize<List<ChapterViewModel>>(sessionChaptersJson);
                     if (chaptersList != null && chaptersList.Any()) return chaptersList;
                 }
                 catch (JsonException ex)
                 {
                      _logger.LogWarning(ex, "Lỗi deserialize chapters từ session.");
                 }
             }
             return await GetChaptersFromApiAsync(mangaId, language);
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersFromApiAsync(string mangaId, string language)
        {
            var allChapters = await _chapterService.GetChaptersAsync(mangaId, language);
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                 var chaptersByLanguage = _chapterService.GetChaptersByLanguage(allChapters);
                 if (chaptersByLanguage.TryGetValue(language, out var chaptersInLanguage))
                 {
                     httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{language}", JsonSerializer.Serialize(chaptersInLanguage));
                     return chaptersInLanguage;
                 }
            }
            return new List<ChapterViewModel>();
        }
        
        private (ChapterViewModel currentChapter, string prevId, string nextId) FindCurrentAndAdjacentChapters(
            List<ChapterViewModel> chapters, string chapterId)
        {
            var currentChapter = chapters.FirstOrDefault(c => c.Id == chapterId);
            if (currentChapter == null)
            {
                return (new ChapterViewModel { Id = chapterId, Title = "Chương không xác định" }, null, null);
            }
            
            var sortedChapters = chapters
                .OrderBy(c => ParseChapterNumber(c.Number) ?? double.MaxValue)
                .ThenBy(c => c.PublishedAt)
                .ToList();

            int index = sortedChapters.FindIndex(c => c.Id == chapterId);
            string prevId = (index > 0) ? sortedChapters[index - 1].Id : null;
            string nextId = (index >= 0 && index < sortedChapters.Count - 1) ? sortedChapters[index + 1].Id : null;

            return (currentChapter, prevId, nextId);
        }

        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\MangaPageService\MangaDetailsService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // <-- SỬA Ở ĐÂY
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers; // <-- SỬA Ở ĐÂY
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient; // <-- SỬA Ở ĐÂY
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly ChapterService _chapterService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMangaReaderLibToMangaDetailViewModelMapper _mangaDetailViewModelMapper; // <-- SỬA Ở ĐÂY

        public MangaDetailsService(
            IMangaReaderLibMangaClient mangaClient, // <-- SỬA Ở ĐÂY
            ILogger<MangaDetailsService> logger,
            IMangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IMangaReaderLibToMangaDetailViewModelMapper mangaDetailViewModelMapper) // <-- SỬA Ở ĐÂY
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaDetailViewModelMapper = mangaDetailViewModelMapper;
        }

        public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
        {
            try
            {
                 if (!Guid.TryParse(id, out var mangaGuid))
                {
                    _logger.LogError("MangaId không hợp lệ: {MangaId}", id);
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "ID Manga không hợp lệ" } };
                }

                _logger.LogInformation("Đang lấy chi tiết manga ID: {id}", id);
                var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid, new List<string> { "author", "cover_art", "tag" });

                if (mangaResponse?.Data == null)
                {
                    _logger.LogError("Không thể lấy chi tiết manga {id}. API không trả về dữ liệu.", id);
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" } };
                }

                var mangaData = mangaResponse.Data;
                var chapterViewModels = await GetChaptersAsync(id);
                var mangaDetailViewModel = await _mangaDetailViewModelMapper.MapToMangaDetailViewModelAsync(mangaData, chapterViewModels);

                if (mangaDetailViewModel.Manga != null)
                {
                    mangaDetailViewModel.Manga.IsFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                }

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && mangaDetailViewModel.Manga != null && !string.IsNullOrEmpty(mangaDetailViewModel.Manga.Title))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaDetailViewModel.Manga.Title);
                }

                return mangaDetailViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy chi tiết manga {id}", id);
                return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" } };
            }
        }
        
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId)
        {
            try
            {
                var chapterViewModels = await _chapterService.GetChaptersAsync(mangaId, "vi,en");
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && chapterViewModels.Any())
                {
                    var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                    foreach (var kvp in chaptersByLanguage)
                    {
                        httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{kvp.Key}", JsonSerializer.Serialize(kvp.Value));
                    }
                }
                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chapters cho manga {mangaId}", mangaId);
                return new List<ChapterViewModel>();
            }
        }
    }
}
```

<!-- MangaReader_WebUI\Services\MangaServices\FollowedMangaService.cs -->
```csharp
using MangaReader.WebUI.Models.Auth;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers; // <-- SỬA Ở ĐÂY

namespace MangaReader.WebUI.Services.MangaServices
{
    public class FollowedMangaService : IFollowedMangaService
    {
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly ChapterService _chapterService;
        private readonly ILogger<FollowedMangaService> _logger;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(550);
        private readonly IFollowedMangaViewModelMapper _followedMangaMapper;

        public FollowedMangaService(
            IUserService userService,
            IMangaInfoService mangaInfoService,
            ChapterService chapterService,
            ILogger<FollowedMangaService> logger,
            IFollowedMangaViewModelMapper followedMangaMapper)
        {
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _chapterService = chapterService;
            _logger = logger;
            _followedMangaMapper = followedMangaMapper;
        }

        public async Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync()
        {
            var followedMangaList = new List<FollowedMangaViewModel>();

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy danh sách theo dõi.");
                return followedMangaList;
            }

            try
            {
                UserModel user = await _userService.GetUserInfoAsync();
                if (user == null || user.FollowingManga == null || !user.FollowingManga.Any())
                {
                    _logger.LogInformation("Người dùng không theo dõi manga nào.");
                    return followedMangaList;
                }

                _logger.LogInformation($"Người dùng đang theo dõi {user.FollowingManga.Count} manga. Bắt đầu lấy thông tin...");

                foreach (var mangaId in user.FollowingManga)
                {
                    try
                    {
                        await Task.Delay(_rateLimitDelay);
                        var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(mangaId);

                        if (mangaInfo == null)
                        {
                             _logger.LogWarning($"Không thể lấy thông tin cơ bản cho manga ID: {mangaId}. Bỏ qua.");
                             continue; 
                        }

                        await Task.Delay(_rateLimitDelay);
                        var latestChapters = await _chapterService.GetLatestChaptersAsync(mangaId, 3, "vi,en");

                        var followedMangaViewModel = _followedMangaMapper.MapToFollowedMangaViewModel(mangaInfo, latestChapters ?? new List<SimpleChapterInfoViewModel>());
                        followedMangaList.Add(followedMangaViewModel);
                        _logger.LogDebug($"Đã xử lý xong manga: {mangaInfo.MangaTitle}");

                    }
                    catch (Exception mangaEx)
                    {
                        _logger.LogError(mangaEx, $"Lỗi khi xử lý manga ID: {mangaId} trong danh sách theo dõi.");
                    }
                }

                _logger.LogInformation($"Hoàn tất lấy thông tin cho {followedMangaList.Count} truyện đang theo dõi.");
                return followedMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lấy danh sách truyện đang theo dõi.");
                return new List<FollowedMangaViewModel>();
            }
        }
    }
}
```

<!-- MangaReader_WebUI\Services\UtilityServices\LocalizationService.cs -->
```csharp
using System.Diagnostics;
using System.Text.Json;

namespace MangaReader.WebUI.Services.UtilityServices
{
    public class LocalizationService
    {
        public string GetStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Không rõ";

            return status.ToLowerInvariant() switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }
    }
}
```