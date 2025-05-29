using MangaReaderLib.Services.Interfaces;
using MangaReaderLib.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Cấu hình HttpClientFactory và BaseAddress cho MangaReaderAPI (Backend thực sự)
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    string apiBaseUrl = builder.Configuration["MangaReaderApiSettings:BaseUrl"] 
                        ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured.");
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"); 
});

// 2. Đăng ký các Client Services từ MangaReaderLib
builder.Services.AddScoped<IMangaClient, MangaClient>();
builder.Services.AddScoped<IAuthorClient, AuthorClient>();
builder.Services.AddScoped<ITagClient, TagClient>();
builder.Services.AddScoped<ITagGroupClient, TagGroupClient>();
builder.Services.AddScoped<ICoverArtClient, CoverArtClient>();
builder.Services.AddScoped<ITranslatedMangaClient, TranslatedMangaClient>();
builder.Services.AddScoped<IChapterClient, ChapterClient>();
builder.Services.AddScoped<IChapterPageClient, ChapterPageClient>();

builder.Services.AddControllers();
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
