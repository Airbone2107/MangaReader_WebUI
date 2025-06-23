using MangaReaderLib.Services.Implementations;
using MangaReaderLib.Services.Interfaces;
using System.Text.Json.Serialization;

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
// Đăng ký implementation cụ thể
builder.Services.AddScoped<AuthorClient>();
builder.Services.AddScoped<MangaClient>();
builder.Services.AddScoped<TagClient>();
builder.Services.AddScoped<TagGroupClient>();
builder.Services.AddScoped<CoverArtClient>();
builder.Services.AddScoped<TranslatedMangaClient>();
builder.Services.AddScoped<ChapterClient>();
builder.Services.AddScoped<ChapterPageClient>();

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
