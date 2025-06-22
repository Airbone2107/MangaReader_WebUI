using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToTagListResponseMapper
    {
        TagListViewModel MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsData);
    }
} 