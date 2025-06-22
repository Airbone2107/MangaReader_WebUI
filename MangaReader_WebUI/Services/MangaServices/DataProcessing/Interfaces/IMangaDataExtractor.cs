using MangaReaderLib.DTOs.Chapters;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces
{
    public interface IMangaDataExtractor
    {
        string ExtractChapterDisplayTitle(ChapterAttributesDto attributes);
        string ExtractChapterNumber(ChapterAttributesDto attributes);
        string ExtractAndTranslateStatus(string? status);
    }
} 