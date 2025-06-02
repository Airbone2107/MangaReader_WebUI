using MangaReader.WebUI.Models;           // Cho MangaViewModel
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto
using System.Threading.Tasks;           // Cho Task

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaViewModelMapper
    {
        Task<MangaViewModel> MapToMangaViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, bool isFollowing = false);
    }
} 