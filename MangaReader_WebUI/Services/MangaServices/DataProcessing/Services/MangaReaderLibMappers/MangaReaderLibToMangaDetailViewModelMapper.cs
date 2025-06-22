using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // Cho IMangaReaderLibAuthorClient
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices; // For LocalizationService
using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using System.Diagnostics;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaDetailViewModelMapper : IMangaReaderLibToMangaDetailViewModelMapper
    {
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;
        private readonly ILogger<MangaReaderLibToMangaDetailViewModelMapper> _logger;
        private readonly IMangaReaderLibAuthorClient _authorClient; // Giữ nguyên, không cần thiết nếu API trả attributes
        private readonly LocalizationService _localizationService;
        private readonly IMangaReaderLibCoverApiService _coverApiService;
        private readonly IConfiguration _configuration;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibToMangaDetailViewModelMapper(
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper,
            ILogger<MangaReaderLibToMangaDetailViewModelMapper> logger,
            IMangaReaderLibAuthorClient authorClient,
            LocalizationService localizationService,
            IMangaReaderLibCoverApiService coverApiService,
            IConfiguration configuration)
        {
            _mangaViewModelMapper = mangaViewModelMapper;
            _logger = logger;
            _authorClient = authorClient;
            _localizationService = localizationService;
            _coverApiService = coverApiService;
            _configuration = configuration;
            _cloudinaryBaseUrl = _configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') 
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, List<ChapterViewModel> chapters)
        {
            Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaDetailViewModel.");
            Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaDetailViewModel.");
            Debug.Assert(chapters != null, "chapters không được null khi mapping thành MangaDetailViewModel.");

            var attributes = mangaData.Attributes!;
            var relationships = mangaData.Relationships;

            var mangaViewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaData);

            // Description: MangaReaderLib không có description trong MangaAttributesDto.
            // Nó sẽ được lấy từ TranslatedManga bởi service gọi mapper này.
            // Hoặc mapper này có thể gọi API để lấy, nhưng tốt hơn là service chuẩn bị trước.
            // Hiện tại, mangaViewModel.Description sẽ rỗng, và view sẽ xử lý.

            // Author/Artist: API `/mangas/{id}?includes[]=author` sẽ trả về attributes trong relationship.
            string authorName = "Không rõ";
            string artistName = "Không rõ";
            if (relationships != null)
            {
                foreach (var rel in relationships)
                {
                    if (rel.Attributes != null && (rel.Type == "author" || rel.Type == "artist"))
                    {
                        try
                        {
                            // Attributes của relationship đã chứa name, biography
                            var staffAttributes = JsonSerializer.Deserialize<AuthorAttributesDto>(JsonSerializer.Serialize(rel.Attributes));
                            if (staffAttributes != null && !string.IsNullOrEmpty(staffAttributes.Name))
                            {
                                if (rel.Type == "author") authorName = staffAttributes.Name;
                                else if (rel.Type == "artist") artistName = staffAttributes.Name;
                            }
                        }
                        catch (JsonException ex)
                        {
                             _logger.LogWarning(ex, "MangaReaderLib Detail Mapper: Failed to deserialize relationship attributes for manga {MangaId}, type {RelType}.", mangaData.Id, rel.Type);
                        }
                    }
                }
            }
             // Cập nhật lại author/artist nếu mapper cha chưa lấy được (ví dụ không include ở list)
            if (mangaViewModel.Author == "Không rõ" && authorName != "Không rõ") mangaViewModel.Author = authorName;
            if (mangaViewModel.Artist == "Không rõ" && artistName != "Không rõ") mangaViewModel.Artist = artistName;

            // Cover Art cho Detail: `RelationshipObject.Id` là GUID của CoverArt.
            // Cần gọi API để lấy PublicId.
            var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelationship != null && Guid.TryParse(coverRelationship.Id, out Guid coverArtGuid))
            {
                try
                {
                    var coverArtDetailsResponse = await _coverApiService.GetCoverArtByIdAsync(coverArtGuid);
                    if (coverArtDetailsResponse?.Data?.Attributes?.PublicId != null)
                    {
                        mangaViewModel.CoverUrl = $"{_cloudinaryBaseUrl}/{coverArtDetailsResponse.Data.Attributes.PublicId}";
                        _logger.LogDebug("MangaReaderLib Detail Mapper: Cover URL set to {CoverUrl} using PublicId from CoverArt entity {CoverArtGuid}.", mangaViewModel.CoverUrl, coverArtGuid);
                    }
                    else
                    {
                         _logger.LogWarning("MangaReaderLib Detail Mapper: Could not get PublicId for coverArtId {CoverArtId} for manga {MangaId}.", coverArtGuid, mangaData.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MangaReaderLib Detail Mapper: Error fetching cover art details for ID {CoverArtId} for manga {MangaId}.", coverArtGuid, mangaData.Id);
                }
            }
            else
            {
                _logger.LogDebug("MangaReaderLib Detail Mapper: No cover_art relationship with GUID or invalid ID for manga {MangaId}", mangaData.Id);
            }

            // MangaReaderLib không có alternative titles, nên dictionary sẽ rỗng
            var alternativeTitlesByLanguage = new Dictionary<string, List<string>>();

            // Convert enums to Vietnamese strings for display
            mangaViewModel.Status = _localizationService.GetStatus(attributes.Status.ToString());
            mangaViewModel.PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "";
            mangaViewModel.ContentRating = attributes.ContentRating.ToString() ?? "";

            // Tags đã được xử lý trong _mangaViewModelMapper

            return new MangaDetailViewModel
            {
                Manga = mangaViewModel,
                Chapters = chapters ?? new List<ChapterViewModel>(),
                AlternativeTitlesByLanguage = alternativeTitlesByLanguage
            };
        }
    }
} 