using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto
using MangaReader.WebUI.Services.MangaServices.Models; // Cho SimpleChapterInfo

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToSimpleChapterInfoMapper
    {
        SimpleChapterInfo MapToSimpleChapterInfo(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage);
    }
} 