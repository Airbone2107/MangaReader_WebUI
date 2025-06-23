using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper
{
    /// <summary>
    /// Định nghĩa phương thức để chuyển đổi thông tin Manga và danh sách Chapter thành FollowedMangaViewModel.
    /// </summary>
    public interface IFollowedMangaViewModelMapper
    {
        /// <summary>
        /// Chuyển đổi thông tin Manga và danh sách Chapter thành FollowedMangaViewModel (dùng cho trang đang theo dõi).
        /// </summary>
        /// <param name="mangaInfo">Thông tin cơ bản của Manga.</param>
        /// <param name="latestChapters">Danh sách các chapter mới nhất (dạng SimpleChapterInfo).</param>
        /// <returns>FollowedMangaViewModel.</returns>
        FollowedMangaViewModel MapToFollowedMangaViewModel(MangaInfoViewModel mangaInfo, List<SimpleChapterInfoViewModel> latestChapters);
    }
} 