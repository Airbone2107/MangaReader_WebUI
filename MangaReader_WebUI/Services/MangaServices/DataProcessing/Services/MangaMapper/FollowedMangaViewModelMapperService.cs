using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.Models;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai IFollowedMangaViewModelMapper, chịu trách nhiệm chuyển đổi thông tin Manga và Chapter thành FollowedMangaViewModel.
/// </summary>
public class FollowedMangaViewModelMapperService() : IFollowedMangaViewModelMapper
{
    public FollowedMangaViewModel MapToFollowedMangaViewModel(MangaInfoViewModel mangaInfo, List<SimpleChapterInfo> latestChapters)
    {
        Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành FollowedMangaViewModel.");
        Debug.Assert(latestChapters != null, "latestChapters không được null khi mapping thành FollowedMangaViewModel.");

        return new FollowedMangaViewModel
        {
            MangaId = mangaInfo.MangaId,
            MangaTitle = mangaInfo.MangaTitle,
            CoverUrl = mangaInfo.CoverUrl,
            LatestChapters = latestChapters
        };
    }
} 