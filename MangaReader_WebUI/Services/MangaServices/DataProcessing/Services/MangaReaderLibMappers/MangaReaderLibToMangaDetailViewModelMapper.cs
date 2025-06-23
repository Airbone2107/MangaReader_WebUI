using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.Extensions;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaDetailViewModelMapper : IMangaReaderLibToMangaDetailViewModelMapper
    {
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;
        private readonly ILogger<MangaReaderLibToMangaDetailViewModelMapper> _logger;
        private readonly LocalizationService _localizationService;
        private readonly IConfiguration _configuration;
        private readonly string _cloudinaryBaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public MangaReaderLibToMangaDetailViewModelMapper(
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper,
            ILogger<MangaReaderLibToMangaDetailViewModelMapper> logger,
            LocalizationService localizationService,
            IConfiguration configuration)
        {
            _mangaViewModelMapper = mangaViewModelMapper;
            _logger = logger;
            _localizationService = localizationService;
            _configuration = configuration;
            _cloudinaryBaseUrl = _configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') 
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        }

        public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, List<ChapterViewModel> chapters)
        {
            _logger.LogInformation("[LOGGING - Mapper] Bắt đầu MapToMangaDetailViewModelAsync cho Manga ID: {MangaId}", mangaData.Id);
            _logger.LogDebug("[LOGGING - Mapper] Dữ liệu đầu vào (mangaData):\n{MangaDataJson}", JsonSerializer.Serialize(mangaData, _jsonOptions));

            var attributes = mangaData.Attributes!;
            var relationships = mangaData.Relationships;

            var mangaViewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaData);
            
            string authorName = "Không rõ";
            string artistName = "Không rõ";

            if (relationships != null && relationships.Any())
            {
                var authorRelationship = relationships.FirstOrDefault(r => r.Type == "author");
                if (authorRelationship != null)
                {
                    var authorAttributes = authorRelationship.GetAttributesAs<AuthorAttributesDto>();
                    if (authorAttributes != null && !string.IsNullOrEmpty(authorAttributes.Name))
                    {
                        authorName = authorAttributes.Name;
                    }
                }

                var artistRelationship = relationships.FirstOrDefault(r => r.Type == "artist");
                if (artistRelationship != null)
                {
                     var artistAttributes = artistRelationship.GetAttributesAs<AuthorAttributesDto>();
                     if (artistAttributes != null && !string.IsNullOrEmpty(artistAttributes.Name))
                     {
                         artistName = artistAttributes.Name;
                     }
                }
            }
            mangaViewModel.Author = authorName;
            mangaViewModel.Artist = artistName;

            var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelationship != null)
            {
                var coverAttributes = coverRelationship.GetAttributesAs<CoverArtAttributesDto>();
                if (coverAttributes != null && !string.IsNullOrEmpty(coverAttributes.PublicId))
                {
                    mangaViewModel.CoverUrl = $"{_cloudinaryBaseUrl}/{coverAttributes.PublicId}";
                    _logger.LogInformation("[LOGGING - Mapper] SUCCESS: Đã lấy được Cover URL từ attributes: {CoverUrl}", mangaViewModel.CoverUrl);
                }
            }

            string statusInput = attributes.Status.ToString();
            string statusOutput = _localizationService.GetStatus(statusInput);
            mangaViewModel.Status = statusOutput;

            mangaViewModel.PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "";
            mangaViewModel.ContentRating = attributes.ContentRating.ToString() ?? "";

            return new MangaDetailViewModel
            {
                Manga = mangaViewModel,
                Chapters = chapters,
                AlternativeTitlesByLanguage = new Dictionary<string, List<string>>()
            };
        }
    }
} 