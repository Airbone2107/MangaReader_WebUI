using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;

/// <summary>
/// Định nghĩa các phương thức để chuyển đổi dữ liệu MangaDex thành các ViewModel hoàn chỉnh.
/// </summary>
public interface IMangaViewModelMapper
{
    /// <summary>
    /// Chuyển đổi một đối tượng Manga (từ MangaDex) thành MangaViewModel.
    /// Bao gồm cả việc lấy thông tin bổ sung như trạng thái theo dõi nếu cần.
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc từ API.</param>
    /// <returns>Một Task chứa MangaViewModel đã được điền dữ liệu.</returns>
    Task<MangaViewModel> MapToMangaViewModelAsync(MangaReader.WebUI.Models.Mangadex.Manga mangaData);

    /// <summary>
    /// Chuyển đổi một đối tượng Chapter (từ MangaDex) thành ChapterViewModel.
    /// </summary>
    /// <param name="chapterData">Đối tượng Chapter gốc từ API.</param>
    /// <returns>ChapterViewModel đã được điền dữ liệu.</returns>
    ChapterViewModel MapToChapterViewModel(MangaReader.WebUI.Models.Mangadex.Chapter chapterData);

    /// <summary>
    /// Chuyển đổi dữ liệu Manga và danh sách Chapter thành MangaDetailViewModel.
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc.</param>
    /// <param name="chapters">Danh sách ChapterViewModel đã được xử lý.</param>
    /// <returns>Một Task chứa MangaDetailViewModel hoàn chỉnh.</returns>
    Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(MangaReader.WebUI.Models.Mangadex.Manga mangaData, List<ChapterViewModel> chapters);

    /// <summary>
    /// Chuyển đổi một đối tượng Manga thành MangaInfoViewModel (chỉ chứa ID, Title, CoverUrl).
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc từ API.</param>
    /// <returns>Một Task chứa MangaInfoViewModel.</returns>
    MangaInfoViewModel MapToMangaInfoViewModel(Manga mangaData);

    /// <summary>
    /// Chuyển đổi một đối tượng Chapter thành SimpleChapterInfo (dùng cho danh sách chapter mới nhất).
    /// </summary>
    /// <param name="chapterData">Đối tượng Chapter gốc từ API.</param>
    /// <returns>SimpleChapterInfo.</returns>
    SimpleChapterInfo MapToSimpleChapterInfo(MangaReader.WebUI.Models.Mangadex.Chapter chapterData);

    /// <summary>
    /// Chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel (dùng cho lịch sử đọc).
    /// </summary>
    /// <param name="mangaInfo">Thông tin cơ bản của Manga.</param>
    /// <param name="chapterInfo">Thông tin cơ bản của Chapter.</param>
    /// <param name="lastReadAt">Thời điểm đọc cuối.</param>
    /// <returns>LastReadMangaViewModel.</returns>
    LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfo chapterInfo, DateTime lastReadAt);

    /// <summary>
    /// Chuyển đổi thông tin Manga và danh sách Chapter thành FollowedMangaViewModel (dùng cho trang đang theo dõi).
    /// </summary>
    /// <param name="mangaInfo">Thông tin cơ bản của Manga.</param>
    /// <param name="latestChapters">Danh sách các chapter mới nhất (dạng SimpleChapterInfo).</param>
    /// <returns>FollowedMangaViewModel.</returns>
    FollowedMangaViewModel MapToFollowedMangaViewModel(MangaInfoViewModel mangaInfo, List<SimpleChapterInfo> latestChapters);
} 