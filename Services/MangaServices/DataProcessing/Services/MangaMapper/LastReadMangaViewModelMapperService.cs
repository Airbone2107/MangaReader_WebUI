using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.Models;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai ILastReadMangaViewModelMapper, chịu trách nhiệm chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel.
/// </summary>
public class LastReadMangaViewModelMapperService() : ILastReadMangaViewModelMapper
{
    public LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfo chapterInfo, DateTime lastReadAt)
    {
        Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành LastReadMangaViewModel.");
        Debug.Assert(chapterInfo != null, "chapterInfo không được null khi mapping thành LastReadMangaViewModel.");

        return new LastReadMangaViewModel
        {
            MangaId = mangaInfo.MangaId,
            MangaTitle = mangaInfo.MangaTitle,
            CoverUrl = mangaInfo.CoverUrl,
            ChapterId = chapterInfo.Id,
            ChapterTitle = chapterInfo.Title,
            ChapterPublishedAt = chapterInfo.PublishedAt,
            LastReadAt = lastReadAt
        };
    }
} 