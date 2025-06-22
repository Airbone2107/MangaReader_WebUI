using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces; // Cho IMangaReaderLibCoverApiService
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto
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

            // Lấy Cover URL từ relationships (cần tìm relationship type "cover_art")
            var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelationship != null)
            {
                // MangaReaderLib's GetCoverArtUrl doesn't need publicId as path parameter
                // It only needs coverArtId and uses publicId as query param or not at all if publicId is part of CoverArtId
                // For now, let's assume _coverApiService handles the full URL based on coverArtId.
                // If MangaReaderLib's CoverArtAttributesDto has a PublicId field, we need to fetch it first.
                try
                {
                    var coverArtResponse = _coverApiService.GetCoverArtByIdAsync(Guid.Parse(coverRelationship.Id)).Result; // Sync call for simplicity in mapper, better to make mapper async
                    if (coverArtResponse?.Data?.Attributes?.PublicId != null)
                    {
                        coverUrl = _coverApiService.GetCoverArtUrl(coverRelationship.Id, coverArtResponse.Data.Attributes.PublicId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lấy cover URL cho MangaReaderLib manga ID: {MangaId}", id);
                }
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