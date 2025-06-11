using MangaReader.WebUI.Models.Mangadex; // Cho AtHomeServerResponse
using MangaReaderLib.DTOs.Common;        // Cho ApiCollectionResponse, ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterPageAttributesDto

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToAtHomeServerResponseMapper
    {
        AtHomeServerResponse MapToAtHomeServerResponse(
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored); // mangaReaderLibBaseUrl không còn cần thiết
    }
} 