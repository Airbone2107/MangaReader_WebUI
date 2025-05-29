using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Implementations
{
    public class CoverArtClient : ICoverArtClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<CoverArtClient> _logger;

        public CoverArtClient(IApiClient apiClient, ILogger<CoverArtClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DeleteCoverArtAsync(Guid coverId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting cover art with ID: {CoverId}", coverId);
            await _apiClient.DeleteAsync($"CoverArts/{coverId}", cancellationToken);
        }
    }
} 