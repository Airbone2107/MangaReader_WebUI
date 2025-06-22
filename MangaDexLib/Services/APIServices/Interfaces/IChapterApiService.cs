using MangaDexLib.Models;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface IChapterApiService
    {
        Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null);
        Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId);
        Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId);
    }
} 