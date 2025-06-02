using MangaReader.WebUI.Services.APIServices.Services; // Namespace của ApiStatusService
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex
{
    public class MangaDexApiStatusSourceStrategy : IApiStatusSourceStrategy
    {
        private readonly ApiStatusService _mangaDexApiStatusService; // Sử dụng concrete class

        public MangaDexApiStatusSourceStrategy(ApiStatusService mangaDexApiStatusService)
        {
            _mangaDexApiStatusService = mangaDexApiStatusService;
        }

        public Task<bool> TestConnectionAsync()
        {
            return _mangaDexApiStatusService.TestConnectionAsync();
        }
    }
} 