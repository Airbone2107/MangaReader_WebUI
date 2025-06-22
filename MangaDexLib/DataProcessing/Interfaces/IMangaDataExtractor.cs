using MangaDexLib.Models;

namespace MangaDexLib.DataProcessing.Interfaces
{
    public interface IMangaDataExtractor
    {
        string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList);
        string ExtractMangaDescription(Dictionary<string, string>? descriptionDict);
        List<string> ExtractAndTranslateTags(List<Tag>? tagsList);
        (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships);
        string ExtractCoverUrl(string mangaId, List<Relationship>? relationships);
        string ExtractAndTranslateStatus(string? status);
        string ExtractChapterDisplayTitle(ChapterAttributes attributes);
        string ExtractChapterNumber(ChapterAttributes attributes);
        Dictionary<string, List<string>> ExtractAlternativeTitles(List<Dictionary<string, string>>? altTitlesList);
        string ExtractPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary);
    }
} 