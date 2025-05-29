using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>?> CreateTranslatedMangaAsync(
            LibCreateTranslatedMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new translated manga for language: {Language}", request.Language);
            return await _apiClient.PostAsync<LibCreateTranslatedMangaRequestDto, LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>>("TranslatedMangas", request, cancellationToken);
        }
        
        public async Task<LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>?> GetTranslatedMangaByIdAsync(
            Guid translatedMangaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting translated manga by ID: {TranslatedMangaId}", translatedMangaId);
            return await _apiClient.GetAsync<LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>>($"TranslatedMangas/{translatedMangaId}", cancellationToken);
        }

        public async Task UpdateTranslatedMangaAsync(
            Guid translatedMangaId, LibUpdateTranslatedMangaRequestDto request, CancellationToken cancellationToken = default)
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