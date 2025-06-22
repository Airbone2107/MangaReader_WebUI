using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai IChapterToSimpleInfoMapper, chịu trách nhiệm chuyển đổi Chapter thành SimpleChapterInfo.
/// </summary>
public class ChapterToSimpleInfoMapperService(
    IMangaDataExtractor mangaDataExtractor
    ) : IChapterToSimpleInfoMapper
{
    public SimpleChapterInfoViewModel MapToSimpleChapterInfo(MangaReader.WebUI.Models.Mangadex.Chapter chapterData)
    {
        Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành SimpleChapterInfoViewModel.");
        Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành SimpleChapterInfoViewModel.");

        var attributes = chapterData.Attributes!;

        string displayTitle = mangaDataExtractor.ExtractChapterDisplayTitle(attributes);

        return new SimpleChapterInfoViewModel
        {
            ChapterId = chapterData.Id.ToString(),
            DisplayTitle = displayTitle,
            PublishedAt = attributes.PublishAt.DateTime
        };
    }
} 