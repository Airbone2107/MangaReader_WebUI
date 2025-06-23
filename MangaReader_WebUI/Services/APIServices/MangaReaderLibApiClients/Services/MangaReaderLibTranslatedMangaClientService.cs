using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibTranslatedMangaClientService : IMangaReaderLibTranslatedMangaClient
    {
        private readonly ITranslatedMangaReader _innerReader;
        private readonly ILogger<MangaReaderLibTranslatedMangaClientService> _wrapperLogger;

        public MangaReaderLibTranslatedMangaClientService(
            ITranslatedMangaReader translatedMangaReader,
            ILogger<MangaReaderLibTranslatedMangaClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = translatedMangaReader;
        }

        public Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(Guid translatedMangaId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTranslatedMangaClientService (Wrapper): Getting translated manga by ID {TranslatedMangaId}", translatedMangaId);
            return _innerReader.GetTranslatedMangaByIdAsync(translatedMangaId, cancellationToken);
        }
    }
} 