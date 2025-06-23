using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibCoverApiService : IMangaReaderLibCoverApiService
    {
        private readonly ICoverArtReader _innerReader;
        private readonly ILogger<MangaReaderLibCoverApiService> _wrapperLogger;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibCoverApiService(
            ICoverArtReader coverArtReader,
            ILogger<MangaReaderLibCoverApiService> wrapperLogger,
            IConfiguration configuration)
        {
            _wrapperLogger = wrapperLogger;
            _innerReader = coverArtReader;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> GetCoverArtByIdAsync(Guid coverId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibCoverApiService (Wrapper): Getting cover by ID {CoverId}", coverId);
            return _innerReader.GetCoverArtByIdAsync(coverId, cancellationToken);
        }

        // Phương thức bổ sung để lấy URL đầy đủ của ảnh bìa
        public string GetCoverArtUrl(string coverArtId, string publicId, int? width = null, int? height = null)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return "/images/cover-placeholder.jpg";
            }
            return $"{_cloudinaryBaseUrl}/{publicId}";
        }
    }
} 