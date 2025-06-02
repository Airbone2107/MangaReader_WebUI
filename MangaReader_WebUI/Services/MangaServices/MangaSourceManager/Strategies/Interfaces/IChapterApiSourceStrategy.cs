// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\Interfaces\IChapterApiSourceStrategy.cs
using MangaReader.WebUI.Models.Mangadex; // Sử dụng model của MangaDex cho kiểu trả về thống nhất

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces
{
    public interface IChapterApiSourceStrategy
    {
        Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null);
        Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId);
        Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId);
    }
} 