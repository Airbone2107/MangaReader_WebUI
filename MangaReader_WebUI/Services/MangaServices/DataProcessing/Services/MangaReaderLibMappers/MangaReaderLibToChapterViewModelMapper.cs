using MangaReader.WebUI.Models;           // Cho ChapterViewModel, ChapterRelationship
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Collections.Generic;       // Cho List

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToChapterViewModelMapper : IMangaReaderLibToChapterViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToChapterViewModelMapper> _logger;

        public MangaReaderLibToChapterViewModelMapper(ILogger<MangaReaderLibToChapterViewModelMapper> logger)
        {
            _logger = logger;
        }

        public ChapterViewModel MapToChapterViewModel(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage)
        {
            Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành ChapterViewModel.");
            Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành ChapterViewModel.");

            var attributes = chapterData.Attributes!;

            try
            {
                string chapterNumber = attributes.ChapterNumber ?? "?";
                string displayTitle = string.IsNullOrEmpty(attributes.Title) || attributes.Title == chapterNumber
                                    ? $"Chương {chapterNumber}"
                                    : $"Chương {chapterNumber}: {attributes.Title}";

                // MangaReaderLib doesn't provide scanlation group or user relationships in Chapter DTO
                // So, relationships will be empty for now.
                var relationships = new List<ChapterRelationship>();

                return new ChapterViewModel
                {
                    Id = chapterData.Id,
                    Title = displayTitle,
                    Number = chapterNumber,
                    Language = translatedLanguage, // Language from TranslatedManga, not chapter itself
                    PublishedAt = attributes.PublishAt,
                    Relationships = relationships
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi mapping MangaReaderLib ChapterData thành ChapterViewModel cho ID: {ChapterId}", chapterData?.Id);
                return new ChapterViewModel
                {
                    Id = chapterData?.Id ?? "error",
                    Title = "Lỗi tải chương",
                    Number = "?",
                    Language = translatedLanguage,
                    PublishedAt = DateTime.MinValue,
                    Relationships = new List<ChapterRelationship>()
                };
            }
        }
    }
} 