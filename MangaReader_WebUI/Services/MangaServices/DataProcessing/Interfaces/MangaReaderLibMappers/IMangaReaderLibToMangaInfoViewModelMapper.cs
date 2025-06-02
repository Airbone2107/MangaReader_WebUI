using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto
using MangaReader.WebUI.Services.MangaServices.Models; // Cho MangaInfoViewModel

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaInfoViewModelMapper
    {
        MangaInfoViewModel MapToMangaInfoViewModel(ResourceObject<MangaAttributesDto> mangaData);
    }
} 