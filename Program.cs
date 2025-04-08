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

// Sử dụng Session (phải đặt sau UseRouting và trước UseAuthorization)
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
