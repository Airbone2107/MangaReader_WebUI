using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToSimpleChapterInfoMapper : IMangaReaderLibToSimpleChapterInfoMapper
    {
        public SimpleChapterInfoViewModel MapToSimpleChapterInfo(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage)
        {
            Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành SimpleChapterInfo.");
            Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành SimpleChapterInfo.");

            var attributes = chapterData.Attributes!;

            string chapterNumber = attributes.ChapterNumber ?? "?";
            string displayTitle = string.IsNullOrEmpty(attributes.Title) || attributes.Title == chapterNumber
                                ? $"Chương {chapterNumber}"
                                : $"Chương {chapterNumber}: {attributes.Title}";

            return new SimpleChapterInfoViewModel
            {
                ChapterId = chapterData.Id,
                DisplayTitle = displayTitle,
                PublishedAt = attributes.PublishAt
            };
        }
    }
} 