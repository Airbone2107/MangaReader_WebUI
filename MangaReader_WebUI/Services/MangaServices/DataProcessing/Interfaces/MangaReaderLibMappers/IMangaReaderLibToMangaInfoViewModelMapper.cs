using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaInfoViewModelMapper
    {
        MangaInfoViewModel MapToMangaInfoViewModel(ResourceObject<MangaAttributesDto> mangaData);
    }
} 