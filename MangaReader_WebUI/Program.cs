using MangaReader.WebUI.Infrastructure;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;
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

// Đăng ký các Mappers cấp cao (ViewModel-to-ViewModel)
builder.Services.AddScoped<IFollowedMangaViewModelMapper, FollowedMangaViewModelMapperService>();
builder.Services.AddScoped<ILastReadMangaViewModelMapper, LastReadMangaViewModelMapperService>();

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
