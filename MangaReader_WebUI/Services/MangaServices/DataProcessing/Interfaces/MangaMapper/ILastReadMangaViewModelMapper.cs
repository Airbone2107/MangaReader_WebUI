using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

/// <summary>
/// Định nghĩa phương thức để chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel.
/// </summary>
public interface ILastReadMangaViewModelMapper
{
    /// <summary>
    /// Chuyển đổi thông tin Manga và Chapter thành LastReadMangaViewModel (dùng cho lịch sử đọc).
    /// </summary>
    /// <param name="mangaInfo">Thông tin cơ bản của Manga.</param>
    /// <param name="chapterInfo">Thông tin cơ bản của Chapter.</param>
    /// <param name="lastReadAt">Thời điểm đọc cuối.</param>
    /// <returns>LastReadMangaViewModel.</returns>
    LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfo chapterInfo, DateTime lastReadAt);
} 