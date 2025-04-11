using manga_reader_web.Services.AuthServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Cấu hình xác thực JWT Bearer token
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Đặt true trong môi trường sản xuất
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
            builder.Configuration["Authentication:Jwt:Secret"])),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

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

// Đăng ký MangaDexService với HttpClient được cấu hình
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaDexService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<manga_reader_web.Services.MangaServices.MangaDexService>>();
    var httpClient = httpClientFactory.CreateClient("MangaDexClient");
    return new manga_reader_web.Services.MangaServices.MangaDexService(httpClient, logger);
});

// Đăng ký ViewRenderService
builder.Services.AddScoped<manga_reader_web.Services.UtilityServices.ViewRenderService>();

// Đăng ký các service mới tách từ MangaController
builder.Services.AddScoped<manga_reader_web.Services.UtilityServices.LocalizationService>();
builder.Services.AddScoped<manga_reader_web.Services.UtilityServices.JsonConversionService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaInformation.MangaUtilityService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaInformation.MangaTitleService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaInformation.MangaTagService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaInformation.MangaRelationshipService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaInformation.MangaDescription>();
// Đăng ký MangaFollowService với interface
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.IMangaFollowService, manga_reader_web.Services.MangaServices.MangaFollowService>();
// Đăng ký FollowedMangaService
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.IFollowedMangaService, manga_reader_web.Services.MangaServices.FollowedMangaService>();
// Giữ lại đăng ký cũ để tương thích ngược (có thể xóa sau khi đã cập nhật tất cả các thành phần khác)
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaFollowService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterServices.ChapterService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterServices.MangaIdService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<manga_reader_web.Services.MangaServices.ChapterServices.MangaIdService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpClient = httpClientFactory.CreateClient("MangaDexClient");
    return new manga_reader_web.Services.MangaServices.ChapterServices.MangaIdService(httpClient, configuration, logger);
});

// Đăng ký ChapterLanguageServices
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterServices.ChapterLanguageServices>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<manga_reader_web.Services.MangaServices.ChapterServices.ChapterLanguageServices>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpClient = httpClientFactory.CreateClient("MangaDexClient");
    return new manga_reader_web.Services.MangaServices.ChapterServices.ChapterLanguageServices(httpClient, configuration, logger);
});

// Đăng ký ChapterReadingServices
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterServices.ChapterReadingServices>();

builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaPageService.MangaDetailsService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaPageService.MangaSearchService>();

// Đăng ký HttpContextAccessor để các service có thể truy cập HttpContext
builder.Services.AddHttpContextAccessor();

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
