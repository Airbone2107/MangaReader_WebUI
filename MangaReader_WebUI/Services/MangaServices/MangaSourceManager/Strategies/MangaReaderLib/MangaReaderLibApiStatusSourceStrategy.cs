using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibApiStatusSourceStrategy : IApiStatusSourceStrategy
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaReaderLibApiStatusSourceStrategy> _logger;


        public MangaReaderLibApiStatusSourceStrategy(IMangaReaderLibMangaClient mangaClient, ILogger<MangaReaderLibApiStatusSourceStrategy> logger)
        {
            _mangaClient = mangaClient;
            _logger = logger;
        }

        public async Task<bool> TestConnectionAsync()
        {
            _logger.LogInformation("[MRLib Strategy->TestConnectionAsync] Testing connection.");
            try
            {
                var result = await _mangaClient.GetMangasAsync(limit: 1);
                return result?.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MRLib Strategy->TestConnectionAsync] Error testing connection.");
                return false;
            }
        }
    }
} 