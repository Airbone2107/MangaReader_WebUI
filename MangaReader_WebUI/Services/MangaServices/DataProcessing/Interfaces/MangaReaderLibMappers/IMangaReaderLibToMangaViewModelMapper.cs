using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaViewModelMapper
    {
        Task<MangaViewModel> MapToMangaViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, bool isFollowing = false);
    }
} 