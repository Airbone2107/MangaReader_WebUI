using MangaReader.WebUI.Infrastructure;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.APIServices.Services;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers;
using Microsoft.AspNetCore.Mvc.Razor;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager;
using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Thêm cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian timeout cho session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Cấu hình logging chi tiết
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Đảm bảo HttpContextAccessor được đăng ký
builder.Services.AddHttpContextAccessor();

// Cấu hình HttpClient để gọi Backend API
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

// Đăng ký HttpClient với cấu hình nâng cao (MangaDexClient)
builder.Services.AddHttpClient("MangaDexClient", client =>
{
    // Thiết lập các tùy chọn cơ bản
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Cấu hình handler để tăng hiệu suất
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

// Đăng ký các service trong DataProcessing
builder.Services.AddScoped<IMangaDataExtractor, MangaDataExtractorService>();
builder.Services.AddScoped<IMangaToMangaViewModelMapper, MangaToMangaViewModelMapperService>();
builder.Services.AddScoped<IChapterToChapterViewModelMapper, ChapterToChapterViewModelMapperService>();
builder.Services.AddScoped<IMangaToDetailViewModelMapper, MangaToDetailViewModelMapperService>();
builder.Services.AddScoped<IChapterToSimpleInfoMapper, ChapterToSimpleInfoMapperService>();
builder.Services.AddScoped<IMangaToInfoViewModelMapper, MangaToInfoViewModelMapperService>();
builder.Services.AddScoped<IFollowedMangaViewModelMapper, FollowedMangaViewModelMapperService>();
builder.Services.AddScoped<ILastReadMangaViewModelMapper, LastReadMangaViewModelMapperService>();

// Đăng ký các MangaReaderLib Mappers đã tạo ở Bước III
builder.Services.AddScoped<IMangaReaderLibToMangaViewModelMapper, MangaReaderLibToMangaViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaDetailViewModelMapper, MangaReaderLibToMangaDetailViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterViewModelMapper, MangaReaderLibToChapterViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToSimpleChapterInfoMapper, MangaReaderLibToSimpleChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaInfoViewModelMapper, MangaReaderLibToMangaInfoViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterInfoMapper, MangaReaderLibToChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToTagListResponseMapper, MangaReaderLibToTagListResponseMapper>();
builder.Services.AddScoped<IMangaReaderLibToAtHomeServerResponseMapper, MangaReaderLibToAtHomeServerResponseMapper>();

// Đăng ký các MangaReaderLib API Clients
// IApiClient sẽ được inject với HttpClient "MangaReaderLibApiClient"
builder.Services.AddScoped<IMangaReaderLibApiClient, MangaReaderLibApiClientService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("MangaReaderLibApiClient");
    
    // Lấy ILogger cho client gốc từ MangaReaderLib
    var innerClientLogger = provider.GetRequiredService<ILogger<MangaReaderLib.Services.Implementations.ApiClient>>();
    
    // Lấy ILogger cho service wrapper
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

// Đăng ký API Request Handler (chung cho cả hai nguồn)
builder.Services.AddScoped<IApiRequestHandler, ApiRequestHandler>();

// Thay đổi đăng ký MangaDex API Services: đăng ký chúng dưới dạng concrete class
builder.Services.AddScoped<MangaReader.WebUI.Services.APIServices.Services.MangaApiService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.APIServices.Services.ChapterApiService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.APIServices.Services.CoverApiService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.APIServices.Services.TagApiService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.APIServices.Services.ApiStatusService>();

// Đăng ký các Strategy (MỚI)
// MangaDex Strategies
builder.Services.AddScoped<MangaDexMangaSourceStrategy>();
builder.Services.AddScoped<MangaDexChapterSourceStrategy>();
builder.Services.AddScoped<MangaDexCoverSourceStrategy>();
builder.Services.AddScoped<MangaDexTagSourceStrategy>();
builder.Services.AddScoped<MangaDexApiStatusSourceStrategy>();

// MangaReaderLib Strategies
builder.Services.AddScoped<MangaReaderLibMangaSourceStrategy>();
builder.Services.AddScoped<MangaReaderLibChapterSourceStrategy>();
builder.Services.AddScoped<MangaReaderLibCoverSourceStrategy>();
builder.Services.AddScoped<MangaReaderLibTagSourceStrategy>();
builder.Services.AddScoped<MangaReaderLibApiStatusSourceStrategy>();

// Đăng ký ViewRenderService
builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.ViewRenderService>();

// Đăng ký MangaSourceManagerService với tất cả các interface API chính
builder.Services.AddScoped<IMangaApiService, MangaSourceManagerService>();
builder.Services.AddScoped<IChapterApiService, MangaSourceManagerService>();
builder.Services.AddScoped<ICoverApiService, MangaSourceManagerService>();
builder.Services.AddScoped<ITagApiService, MangaSourceManagerService>();
builder.Services.AddScoped<IApiStatusService, MangaSourceManagerService>();

// Đăng ký các service mới tách từ MangaController
builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.LocalizationService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.JsonConversionService>();

// Đăng ký MangaFollowService với interface
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.IMangaFollowService, MangaReader.WebUI.Services.MangaServices.MangaFollowService>();

// Đăng ký FollowedMangaService
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.IFollowedMangaService, MangaReader.WebUI.Services.MangaServices.FollowedMangaService>();

// Đăng ký MangaInfoService
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.IMangaInfoService, MangaReader.WebUI.Services.MangaServices.MangaInfoService>();

// Giữ lại đăng ký cũ để tương thích ngược (có thể xóa sau khi đã cập nhật tất cả các thành phần khác)
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaFollowService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.MangaIdService>(sp =>
{
    var chapterApiService = sp.GetRequiredService<IChapterApiService>();
    var logger = sp.GetRequiredService<ILogger<MangaReader.WebUI.Services.MangaServices.ChapterServices.MangaIdService>>();
    return new MangaReader.WebUI.Services.MangaServices.ChapterServices.MangaIdService(chapterApiService, logger);
});

// Đăng ký ChapterLanguageServices
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterLanguageServices>(sp =>
{
    var chapterApiService = sp.GetRequiredService<IChapterApiService>();
    var logger = sp.GetRequiredService<ILogger<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterLanguageServices>>();
    return new MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterLanguageServices(chapterApiService, logger);
});

// Đăng ký ChapterReadingServices
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterReadingServices>();

// Đăng ký ReadingHistoryService
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.IReadingHistoryService, MangaReader.WebUI.Services.MangaServices.ReadingHistoryService>();

builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaPageService.MangaDetailsService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaPageService.MangaSearchService>();

// Cấu hình Razor View Engine để sử dụng View Location Expander tùy chỉnh
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    // Thêm expander vào danh sách.
    // Nó sẽ được gọi để cung cấp các đường dẫn tìm kiếm bổ sung (hoặc thay thế).
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Trong môi trường development, sử dụng trang lỗi chi tiết
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm middleware xác thực trước khi xử lý authorization
app.UseAuthentication();

// Sử dụng Session (phải đặt sau UseRouting và trước UseAuthorization)
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Thêm route đặc biệt cho callback OAuth
app.MapControllerRoute(
    name: "auth_callback",
    pattern: "auth/callback",
    defaults: new { controller = "Auth", action = "Callback" });

app.Run();
