using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaDetailViewModelMapper
    {
        Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, List<ChapterViewModel> chapters);
    }
} 