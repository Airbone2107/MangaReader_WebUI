using MangaReader.WebUI.Models.Mangadex; // Cho TagListResponse
using MangaReaderLib.DTOs.Common;        // Cho ApiCollectionResponse, ResourceObject
using MangaReaderLib.DTOs.Tags;          // Cho TagAttributesDto

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToTagListResponseMapper
    {
        TagListResponse MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsData);
    }
} 