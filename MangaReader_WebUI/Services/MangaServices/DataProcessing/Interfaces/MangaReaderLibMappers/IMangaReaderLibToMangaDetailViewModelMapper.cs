using MangaReader.WebUI.Models;           // Cho MangaDetailViewModel, ChapterViewModel
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto
using System.Collections.Generic;       // Cho List
using System.Threading.Tasks;           // Cho Task

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaDetailViewModelMapper
    {
        Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, List<ChapterViewModel> chapters);
    }
} 