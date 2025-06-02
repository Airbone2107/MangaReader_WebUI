using MangaReader.WebUI.Models;           // Cho ChapterViewModel
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToChapterViewModelMapper
    {
        ChapterViewModel MapToChapterViewModel(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage);
    }
} 