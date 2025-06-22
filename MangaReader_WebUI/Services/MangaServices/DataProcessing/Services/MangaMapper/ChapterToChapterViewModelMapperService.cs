using MangaReader.WebUI.Models.ViewModels.Chapter; // ViewModel mới
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai IChapterToChapterViewModelMapper, chịu trách nhiệm chuyển đổi Chapter MangaDex thành ChapterViewModel.
/// </summary>
public class ChapterToChapterViewModelMapperService(
    ILogger<ChapterToChapterViewModelMapperService> logger,
    IMangaDataExtractor mangaDataExtractor
    ) : IChapterToChapterViewModelMapper
{
    public ChapterViewModel MapToChapterViewModel(MangaReader.WebUI.Models.Mangadex.Chapter chapterData)
    {
        Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành ChapterViewModel.");
        Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành ChapterViewModel.");

        var attributes = chapterData.Attributes!; // Use ! because of the assert

        try
        {
            string displayTitle = mangaDataExtractor.ExtractChapterDisplayTitle(attributes);
            string chapterNumber = mangaDataExtractor.ExtractChapterNumber(attributes);

            // Xử lý relationships (đơn giản hóa, chỉ lấy ID và Type)
            var relationships = chapterData.Relationships?
                .Where(r => r != null)
                .Select(r => new ChapterRelationshipViewModel { Id = r!.Id.ToString(), Type = r.Type })
                .ToList() ?? new List<ChapterRelationshipViewModel>();

            return new ChapterViewModel
            {
                Id = chapterData.Id.ToString(),
                Title = displayTitle,
                Number = chapterNumber,
                Volume = attributes.Volume ?? "Không rõ",
                Language = attributes.TranslatedLanguage ?? "unknown",
                PublishedAt = attributes.PublishAt.DateTime, // Convert DateTimeOffset to DateTime
                Relationships = relationships
            };
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Lỗi khi mapping ChapterData thành ChapterViewModel cho ID: {ChapterId}", chapterData?.Id);
            // Trả về ViewModel lỗi
            return new ChapterViewModel
            {
                Id = chapterData?.Id.ToString() ?? "error",
                Title = "Lỗi tải chương",
                Number = "?",
                Volume = "Lỗi",
                Language = "error",
                PublishedAt = DateTime.MinValue,
                Relationships = new List<ChapterRelationshipViewModel>()
            };
        }
    }
} 