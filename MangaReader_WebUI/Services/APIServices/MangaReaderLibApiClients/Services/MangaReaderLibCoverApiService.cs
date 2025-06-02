using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    public class MangaReaderLibCoverApiService : IMangaReaderLibCoverApiService
    {
        private readonly CoverArtClient _innerClient;
        private readonly ILogger<MangaReaderLibCoverApiService> _wrapperLogger;
        private readonly string _mangaReaderLibBaseUrl; // Base URL of MangaReaderLib API

        public MangaReaderLibCoverApiService(
            IMangaReaderLibApiClient apiClient, 
            ILogger<CoverArtClient> innerClientLogger,
            ILogger<MangaReaderLibCoverApiService> wrapperLogger, 
            IConfiguration configuration)
        {
            _wrapperLogger = wrapperLogger;
            _innerClient = new CoverArtClient(apiClient, innerClientLogger);
            _mangaReaderLibBaseUrl = configuration["MangaReaderApiSettings:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured.");
        }

        public async Task DeleteCoverArtAsync(Guid coverId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibCoverApiService (Wrapper): Deleting cover art {CoverId}", coverId);
            await _innerClient.DeleteCoverArtAsync(coverId, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> GetCoverArtByIdAsync(Guid coverId, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibCoverApiService (Wrapper): Getting cover art by ID {CoverId}", coverId);
            return await _innerClient.GetCoverArtByIdAsync(coverId, cancellationToken);
        }

        /// <summary>
        /// Tạo URL ảnh bìa từ PublicId của MangaReaderLib.
        /// Cấu trúc URL dự kiến: {BaseUrl}/covers/image/{publicId}
        /// </summary>
        public string GetCoverArtUrl(string coverArtId, string publicId, int? width = null, int? height = null)
        {
            // coverArtId không thực sự cần thiết nếu publicId đã là định danh duy nhất cho ảnh.
            // API MangaReaderLib sử dụng publicId trong đường dẫn.
            // Tham số width và height có thể được thêm vào query string nếu API hỗ trợ.
            string url = $"{_mangaReaderLibBaseUrl}/covers/image/{publicId}";

            var queryParams = new List<string>();
            if (width.HasValue) queryParams.Add($"width={width.Value}");
            if (height.HasValue) queryParams.Add($"height={height.Value}");

            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }

            _wrapperLogger.LogDebug("Constructed MangaReaderLib cover URL: {Url} for PublicId: {PublicId}", url, publicId);
            return url;
        }
    }
} 