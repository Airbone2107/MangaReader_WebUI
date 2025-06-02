using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibCoverSourceStrategy : ICoverApiSourceStrategy
    {
        private readonly IMangaReaderLibMangaClient _mangaClient; // Để gọi GetMangaCoversAsync
        private readonly IMangaReaderLibCoverApiService _coverApiService; // Để gọi GetCoverArtUrl
        private readonly ILogger<MangaReaderLibCoverSourceStrategy> _logger;
        private readonly string _mangaReaderLibApiBaseUrl;
        private readonly string _cloudinaryBaseUrl;


        public MangaReaderLibCoverSourceStrategy(
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibCoverApiService coverApiService,
            IConfiguration configuration,
            ILogger<MangaReaderLibCoverSourceStrategy> logger)
        {
            _mangaClient = mangaClient;
            _coverApiService = coverApiService;
            _logger = logger;
            _mangaReaderLibApiBaseUrl = configuration["MangaReaderApiSettings:BaseUrl"]?.TrimEnd('/')
                                     ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured.");
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Manga ID: {MangaId}", mangaId);
            if (!Guid.TryParse(mangaId, out var guidMangaId)) return null;

            var libResponse = await _mangaClient.GetMangaCoversAsync(guidMangaId, limit: 100); // Lấy tối đa 100 covers
            if (libResponse?.Data == null) return new CoverList { Result = "ok", Data = new List<Cover>()};
            
            _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Received {Count} cover DTOs.", libResponse.Data.Count);

            var mappedCovers = libResponse.Data.Select(dto => new Cover
            {
                Id = Guid.Parse(dto.Id), Type = "cover_art",
                Attributes = new CoverAttributes { FileName = dto.Attributes.PublicId, Volume = dto.Attributes.Volume, Description = dto.Attributes.Description, Locale = _mangaReaderLibApiBaseUrl, CreatedAt = dto.Attributes.CreatedAt, UpdatedAt = dto.Attributes.UpdatedAt, Version = 1 }
            }).ToList();
             _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Successfully mapped {Count} covers.", mappedCovers.Count);

            return new CoverList { Result = "ok", Response = "collection", Data = mappedCovers, Limit = libResponse.Limit, Offset = libResponse.Offset, Total = libResponse.Total };
        }

        public string GetCoverUrl(string mangaId, string publicId, int size = 512)
        {
            // mangaId không cần thiết cho việc xây dựng URL Cloudinary trực tiếp nếu đã có publicId.
            // fileName ở đây thực chất là publicId đã được trích xuất bởi MangaDataExtractorService.
            _logger.LogDebug("[MRLib Strategy->GetCoverUrl] MangaId (for context): {MangaId}, PublicId: {PublicId}, Size: {Size}", mangaId, publicId, size);

            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("[MRLib Strategy->GetCoverUrl] PublicId rỗng, trả về placeholder.");
                return "/images/cover-placeholder.jpg";
            }

            // Xây dựng URL Cloudinary trực tiếp
            // Mặc định không thêm transform kích thước ở đây, để logic hiển thị (View) quyết định
            // Nếu muốn thumbnail mặc định, có thể thêm transform ở đây, ví dụ: $"{_cloudinaryBaseUrl}/w_{size},h_{size*1.5},c_limit/{publicId}"
            // Tuy nhiên, để giống ManagerUI 100% khi hiển thị thumbnail, việc thêm transform nên ở View.
            string cloudinaryUrl = $"{_cloudinaryBaseUrl}/{publicId}";
            
            _logger.LogInformation("[MRLib Strategy->GetCoverUrl] Constructed Cloudinary URL: {Url}", cloudinaryUrl);
            return cloudinaryUrl;
        }
    }
} 