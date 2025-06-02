using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Services; // Namespace của CoverApiService
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex
{
    public class MangaDexCoverSourceStrategy : ICoverApiSourceStrategy
    {
        private readonly CoverApiService _mangaDexCoverApiService; // Sử dụng concrete class

        public MangaDexCoverSourceStrategy(CoverApiService mangaDexCoverApiService)
        {
            _mangaDexCoverApiService = mangaDexCoverApiService;
        }

        public Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            return _mangaDexCoverApiService.GetAllCoversForMangaAsync(mangaId);
        }

        public string GetCoverUrl(string mangaId, string fileName, int size = 512)
        {
            return _mangaDexCoverApiService.GetProxiedCoverUrl(mangaId, fileName, size);
        }
    }
} 