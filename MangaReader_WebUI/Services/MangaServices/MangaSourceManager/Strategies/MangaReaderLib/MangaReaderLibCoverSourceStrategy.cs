using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibCoverSourceStrategy : ICoverApiSourceStrategy
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibCoverApiService _coverApiService; // Để dùng GetCoverArtUrl
        private readonly ILogger<MangaReaderLibCoverSourceStrategy> _logger;
        private readonly string _cloudinaryBaseUrl; // Cloudinary base URL


        public MangaReaderLibCoverSourceStrategy(
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibCoverApiService coverApiService, // Inject
            IConfiguration configuration,
            ILogger<MangaReaderLibCoverSourceStrategy> logger)
        {
            _mangaClient = mangaClient;
            _coverApiService = coverApiService; // Gán
            _logger = logger;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                 ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Manga ID: {MangaId}", mangaId);
            if (!Guid.TryParse(mangaId, out var guidMangaId)) return null;

            var libResponse = await _mangaClient.GetMangaCoversAsync(guidMangaId, limit: 100);
            if (libResponse?.Data == null) return new CoverList { Result = "ok", Data = new List<Cover>()};
            
            _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Received {Count} cover DTOs.", libResponse.Data.Count);

            var mappedCovers = libResponse.Data.Select(dto => new Cover
            {
                Id = Guid.TryParse(dto.Id, out var coverGuid) ? coverGuid : Guid.NewGuid(), 
                Type = "cover_art",
                Attributes = new CoverAttributes { 
                    FileName = dto.Attributes.PublicId, // FileName của MangaDex model sẽ chứa PublicId từ MRLib
                    Volume = dto.Attributes.Volume, 
                    Description = dto.Attributes.Description, 
                    // Locale không có trong MRLib CoverArtAttributesDto, có thể bỏ qua hoặc đặt giá trị mặc định
                    Locale = null, 
                    CreatedAt = dto.Attributes.CreatedAt, 
                    UpdatedAt = dto.Attributes.UpdatedAt, 
                    Version = 1 
                }
            }).ToList();
             _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Successfully mapped {Count} covers.", mappedCovers.Count);

            return new CoverList { Result = "ok", Response = "collection", Data = mappedCovers, Limit = libResponse.Limit, Offset = libResponse.Offset, Total = libResponse.Total };
        }

        // fileName ở đây được truyền vào là PublicId từ logic trước đó (ví dụ: từ relationship của Manga)
        public string GetCoverUrl(string mangaIdIgnored, string publicId, int size = 512)
        {
            _logger.LogDebug("[MRLib Strategy->GetCoverUrl] PublicId: {PublicId}, Size: {Size}", publicId, size);

            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("[MRLib Strategy->GetCoverUrl] PublicId rỗng, trả về placeholder.");
                return "/images/cover-placeholder.jpg";
            }
            
            // Sử dụng _cloudinaryBaseUrl đã inject
            // Không cần gọi _coverApiService.GetCoverArtUrl nữa vì đã có PublicId
            string cloudinaryUrl = $"{_cloudinaryBaseUrl}/{publicId}"; 
            
            // Thêm transform cho size nếu cần (ví dụ: /w_512,h_auto,c_limit/)
            // Hiện tại không thêm transform, để View tự xử lý nếu cần.
            // if (size > 0)
            // {
            //     cloudinaryUrl = $"{_cloudinaryBaseUrl}/w_{size},c_limit/{publicId}";
            // }


            _logger.LogInformation("[MRLib Strategy->GetCoverUrl] Constructed Cloudinary URL: {Url}", cloudinaryUrl);
            return cloudinaryUrl;
        }
    }
} 