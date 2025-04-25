using MangaReader.WebUI.Infrastructure;
using MangaReader.WebUI.Services.AuthServices;
using Microsoft.AspNetCore.Mvc.Razor;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.APIServices.Services;

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

// Đăng ký HttpClient với cấu hình nâng cao
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

// Đăng ký các service liên quan đến xác thực 
builder.Services.AddScoped<IUserService, UserService>();

// Đăng ký các API Services mới thay thế cho MangaDexService
builder.Services.AddScoped<IMangaApiService, MangaApiService>();
builder.Services.AddScoped<IChapterApiService, ChapterApiService>();
builder.Services.AddScoped<ICoverApiService, CoverApiService>();
builder.Services.AddScoped<ITagApiService, TagApiService>();
builder.Services.AddScoped<IApiStatusService, ApiStatusService>();

// Đăng ký ViewRenderService
builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.ViewRenderService>();

// Đăng ký các service mới tách từ MangaController
builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.LocalizationService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.UtilityServices.JsonConversionService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaUtilityService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaTitleService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaTagService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaRelationshipService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaInformation.MangaDescription>();
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

// Đăng ký ChapterAttributeService
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterAttributeService>();

// Đăng ký ChapterInfoService
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.ChapterServices.IChapterInfoService, MangaReader.WebUI.Services.MangaServices.ChapterServices.ChapterInfoService>();

// Đăng ký ReadingHistoryService
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.IReadingHistoryService, MangaReader.WebUI.Services.MangaServices.ReadingHistoryService>();

builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaPageService.MangaDetailsService>();
builder.Services.AddScoped<MangaReader.WebUI.Services.MangaServices.MangaPageService.MangaSearchService>();

// Đăng ký HttpContextAccessor để các service có thể truy cập HttpContext
builder.Services.AddHttpContextAccessor();

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
