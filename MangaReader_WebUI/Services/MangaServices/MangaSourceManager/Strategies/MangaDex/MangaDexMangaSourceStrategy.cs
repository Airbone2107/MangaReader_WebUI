using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Services; // Namespace của MangaApiService
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex
{
    public class MangaDexMangaSourceStrategy : IMangaApiSourceStrategy
    {
        private readonly MangaApiService _mangaDexApiService; // Sử dụng concrete class

        public MangaDexMangaSourceStrategy(MangaApiService mangaDexApiService)
        {
            _mangaDexApiService = mangaDexApiService;
        }

        public Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            return _mangaDexApiService.FetchMangaAsync(limit, offset, sortManga);
        }

        public Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            return _mangaDexApiService.FetchMangaByIdsAsync(mangaIds);
        }

        public Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            return _mangaDexApiService.FetchMangaDetailsAsync(mangaId);
        }
    }
} 