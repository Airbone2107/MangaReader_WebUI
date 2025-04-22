using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices
{
    public interface IChapterApiService
    {
        Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null);
        Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId);
        Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId);
        // Có thể thêm các phương thức khác liên quan đến Chapter nếu cần
    }
} 