using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaReaderLib.Services.Implementations
{
    public class TranslatedMangaClient : ITranslatedMangaClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<TranslatedMangaClient> _logger;

        public TranslatedMangaClient(IApiClient apiClient, ILogger<TranslatedMangaClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> CreateTranslatedMangaAsync(
            CreateTranslatedMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new translated manga for language: {LanguageKey}", request.LanguageKey);
            return await _apiClient.PostAsync<CreateTranslatedMangaRequestDto, ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>>("TranslatedMangas", request, cancellationToken);
        }
        
        public async Task<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(
            Guid translatedMangaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting translated manga by ID: {TranslatedMangaId}", translatedMangaId);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>>($"TranslatedMangas/{translatedMangaId}", cancellationToken);
        }

        public async Task UpdateTranslatedMangaAsync(
            Guid translatedMangaId, UpdateTranslatedMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating translated manga with ID: {TranslatedMangaId}", translatedMangaId);
            await _apiClient.PutAsync($"TranslatedMangas/{translatedMangaId}", request, cancellationToken);
        }

        public async Task DeleteTranslatedMangaAsync(Guid translatedMangaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting translated manga with ID: {TranslatedMangaId}", translatedMangaId);
            await _apiClient.DeleteAsync($"TranslatedMangas/{translatedMangaId}", cancellationToken);
        }
    }
} 