using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto
using MangaReader.WebUI.Services.MangaServices.Models; // Cho ChapterInfo

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToChapterInfoMapper
    {
        ChapterInfo MapToChapterInfo(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage);
    }
} 