using MangaReader.WebUI.Models.ViewModels.Chapter;      // Cho ChapterAttributesDto
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToChapterViewModelMapper
    {
        ChapterViewModel MapToChapterViewModel(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage);
    }
} 