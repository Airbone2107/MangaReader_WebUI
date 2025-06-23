using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // Cho IMangaReaderLibCoverApiService
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto
using MangaReaderLib.DTOs.CoverArts;     // Cho CoverArtAttributesDto
using MangaReaderLib.Extensions;         // Cho GetAttributesAs extension method
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaInfoViewModelMapper : IMangaReaderLibToMangaInfoViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToMangaInfoViewModelMapper> _logger;
        private readonly IMangaReaderLibCoverApiService _coverApiService;

        public MangaReaderLibToMangaInfoViewModelMapper(
            ILogger<MangaReaderLibToMangaInfoViewModelMapper> logger,
            IMangaReaderLibCoverApiService coverApiService)
        {
            _logger = logger;
            _coverApiService = coverApiService;
        }

        public MangaInfoViewModel MapToMangaInfoViewModel(ResourceObject<MangaAttributesDto> mangaData)
        {
            Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaInfoViewModel.");

            string id = mangaData.Id;
            var attributes = mangaData.Attributes;
            var relationships = mangaData.Relationships;

            string title = attributes?.Title ?? "Lỗi tải tiêu đề";
            string coverUrl = "/images/cover-placeholder.jpg";

            var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelationship != null)
            {
                var coverAttributes = coverRelationship.GetAttributesAs<CoverArtAttributesDto>();
                if (coverAttributes != null && !string.IsNullOrEmpty(coverAttributes.PublicId))
                {
                    // Lấy URL ảnh bìa từ PublicId trong attributes
                    coverUrl = _coverApiService.GetCoverArtUrl(coverRelationship.Id, coverAttributes.PublicId);
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy attributes hoặc PublicId cho cover_art của manga ID: {MangaId}", id);
                }
            }
            else
            {
                 _logger.LogDebug("Không có relationship 'cover_art' cho manga ID: {MangaId}", id);
            }
            
            return new MangaInfoViewModel
            {
                MangaId = id,
                MangaTitle = title,
                CoverUrl = coverUrl
            };
        }
    }
} 