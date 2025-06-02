using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Services; // Namespace của ChapterApiService
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex
{
    public class MangaDexChapterSourceStrategy : IChapterApiSourceStrategy
    {
        private readonly ChapterApiService _mangaDexChapterApiService; // Sử dụng concrete class

        public MangaDexChapterSourceStrategy(ChapterApiService mangaDexChapterApiService)
        {
            _mangaDexChapterApiService = mangaDexChapterApiService;
        }

        public Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            return _mangaDexChapterApiService.FetchChaptersAsync(mangaId, languages, order, maxChapters);
        }

        public Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            return _mangaDexChapterApiService.FetchChapterInfoAsync(chapterId);
        }

        public Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            return _mangaDexChapterApiService.FetchChapterPagesAsync(chapterId);
        }
    }
} 