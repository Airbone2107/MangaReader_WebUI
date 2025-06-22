using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToChapterInfoMapper
    {
        ChapterInfoViewModel MapToChapterInfo(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage);
    }
} 