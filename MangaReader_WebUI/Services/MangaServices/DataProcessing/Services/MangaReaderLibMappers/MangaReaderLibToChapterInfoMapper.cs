using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.Models; // Cho ChapterInfo
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToChapterInfoMapper : IMangaReaderLibToChapterInfoMapper
    {
        public ChapterInfo MapToChapterInfo(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage)
        {
            Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành ChapterInfo.");
            Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành ChapterInfo.");

            var attributes = chapterData.Attributes!;

            string chapterNumber = attributes.ChapterNumber ?? "?";
            string displayTitle = string.IsNullOrEmpty(attributes.Title) || attributes.Title == chapterNumber
                                ? $"Chương {chapterNumber}"
                                : $"Chương {chapterNumber}: {attributes.Title}";

            return new ChapterInfo
            {
                Id = chapterData.Id,
                Title = displayTitle,
                PublishedAt = attributes.PublishAt
            };
        }
    }
} 