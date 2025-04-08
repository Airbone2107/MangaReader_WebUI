using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using manga_reader_web.Services.AuthServices;

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
builder.Services.AddScoped<manga_reader_web.Services.MangaDexService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<manga_reader_web.Services.MangaDexService>>();
    var httpClient = httpClientFactory.CreateClient("MangaDexClient");
    return new manga_reader_web.Services.MangaDexService(httpClient, logger);
});

// Đăng ký các service mới tách từ MangaController
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.LocalizationService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.JsonConversionService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaUtilityService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaTitleService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaTagService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaRelationshipService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.MangaFollowService>();
builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterService>();

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
