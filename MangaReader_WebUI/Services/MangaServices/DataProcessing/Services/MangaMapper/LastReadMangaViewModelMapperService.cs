using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai ILastReadMangaViewModelMapper, chịu trách nhiệm chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel.
/// </summary>
public class LastReadMangaViewModelMapperService() : ILastReadMangaViewModelMapper
{
    public LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfoViewModel chapterInfo, DateTime lastReadAt)
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