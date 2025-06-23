using MangaReader.WebUI.Models.ViewModels.Common;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Chapters;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToPageServerResponseMapper
    {
        PageServerResponse MapToAtHomeServerResponse(
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored);
    }
} 