using MangaReader.WebUI.Models;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper; // For IMangaToMangaViewModelMapper
using System.Diagnostics;
using MangaReader.WebUI.Services.UtilityServices; // For LocalizationService
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // Cho IMangaReaderLibAuthorClient
using Microsoft.Extensions.Logging;
using System.Collections.Generic;       // Cho List
using System.Linq;                      // Cho FirstOrDefault
using System.Threading.Tasks;           // Cho Task

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaDetailViewModelMapper : IMangaReaderLibToMangaDetailViewModelMapper
    {
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;
        private readonly ILogger<MangaReaderLibToMangaDetailViewModelMapper> _logger;
        private readonly IMangaReaderLibAuthorClient _authorClient; // Sửa thành IMangaReaderLibAuthorClient
        private readonly LocalizationService _localizationService;

        public MangaReaderLibToMangaDetailViewModelMapper(
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper,
            ILogger<MangaReaderLibToMangaDetailViewModelMapper> logger,
            IMangaReaderLibAuthorClient authorClient, // Sửa thành IMangaReaderLibAuthorClient
            LocalizationService localizationService)
        {
            _mangaViewModelMapper = mangaViewModelMapper;
            _logger = logger;
            _authorClient = authorClient;
            _localizationService = localizationService;
        }

        public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, List<ChapterViewModel> chapters)
        {
            Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaDetailViewModel.");
            Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaDetailViewModel.");
            Debug.Assert(chapters != null, "chapters không được null khi mapping thành MangaDetailViewModel.");

            var attributes = mangaData.Attributes!;
            var relationships = mangaData.Relationships;

            // Map thông tin manga cơ bản bằng mapper đã có
            var mangaViewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaData);

            // Lấy mô tả từ MangaAttributesDto (nếu có)
            // MangaReaderLib's MangaAttributesDto doesn't have a description field.
            // You might need to fetch it from TranslatedManga or extend MangaAttributesDto in your backend.
            // For now, it will be empty unless you fetch it from TranslatedManga later.
            string description = ""; // Set default or fetch from TranslatedManga if available

            // Extract Author/Artist from relationships (similar to MangaViewModel mapper)
            string authorName = "Không rõ";
            string artistName = "Không rõ";
            if (relationships != null)
            {
                foreach (var rel in relationships)
                {
                    if (rel.Type == "author" || rel.Type == "artist")
                    {
                        try
                        {
                            var authorResponse = await _authorClient.GetAuthorByIdAsync(Guid.Parse(rel.Id));
                            if (authorResponse?.Data?.Attributes?.Name != null)
                            {
                                if (rel.Type == "author")
                                {
                                    authorName = authorResponse.Data.Attributes.Name;
                                }
                                else if (rel.Type == "artist")
                                {
                                    artistName = authorResponse.Data.Attributes.Name;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi lấy thông tin tác giả/họa sĩ ID {AuthorId} cho chi tiết manga {MangaId}", rel.Id, mangaData.Id);
                        }
                    }
                }
            }
            
            // Update MangaViewModel with full description and potentially author/artist if not already set
            if (mangaViewModel.Description == null) // Only update if not already set by MangaViewModel mapper
            {
                mangaViewModel.Description = description;
            }
            if (mangaViewModel.Author == "Không rõ")
            {
                mangaViewModel.Author = authorName;
            }
            if (mangaViewModel.Artist == "Không rõ")
            {
                mangaViewModel.Artist = artistName;
            }
            
            // MangaReaderLib không có alternative titles, nên dictionary sẽ rỗng
            var alternativeTitlesByLanguage = new Dictionary<string, List<string>>();

            // Convert enums to Vietnamese strings for display
            mangaViewModel.Status = _localizationService.GetStatus(attributes.Status.ToString());
            mangaViewModel.PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "";
            mangaViewModel.ContentRating = attributes.ContentRating.ToString() ?? "";

            return new MangaDetailViewModel
            {
                Manga = mangaViewModel,
                Chapters = chapters ?? new List<ChapterViewModel>(),
                AlternativeTitlesByLanguage = alternativeTitlesByLanguage
            };
        }
    }
} 