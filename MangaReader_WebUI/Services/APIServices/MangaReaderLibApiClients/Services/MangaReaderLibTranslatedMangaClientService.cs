using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibTranslatedMangaClientService : IMangaReaderLibTranslatedMangaClient
    {
        private readonly TranslatedMangaClient _innerClient;
        private readonly ILogger<MangaReaderLibTranslatedMangaClientService> _wrapperLogger;

        public MangaReaderLibTranslatedMangaClientService(
            IMangaReaderLibApiClient apiClient,
            ILogger<TranslatedMangaClient> innerClientLogger,
            ILogger<MangaReaderLibTranslatedMangaClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new TranslatedMangaClient(apiClient, innerClientLogger);
        }

        public async Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> CreateTranslatedMangaAsync(CreateTranslatedMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTranslatedMangaClientService (Wrapper): Creating translated manga for language {LanguageKey}", request.LanguageKey);
            return await _innerClient.CreateTranslatedMangaAsync(request, cancellationToken);
        }

        public async Task DeleteTranslatedMangaAsync(Guid translatedMangaId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTranslatedMangaClientService (Wrapper): Deleting translated manga {TranslatedMangaId}", translatedMangaId);
            await _innerClient.DeleteTranslatedMangaAsync(translatedMangaId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(Guid translatedMangaId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTranslatedMangaClientService (Wrapper): Getting translated manga by ID {TranslatedMangaId}", translatedMangaId);
            return await _innerClient.GetTranslatedMangaByIdAsync(translatedMangaId, cancellationToken);
        }

        public async Task UpdateTranslatedMangaAsync(Guid translatedMangaId, UpdateTranslatedMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibTranslatedMangaClientService (Wrapper): Updating translated manga {TranslatedMangaId}", translatedMangaId);
            await _innerClient.UpdateTranslatedMangaAsync(translatedMangaId, request, cancellationToken);
        }
    }
} 