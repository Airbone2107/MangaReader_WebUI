using MangaReader.WebUI.Models;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

/// <summary>
/// Định nghĩa phương thức để chuyển đổi đối tượng Chapter (từ MangaDex) thành ChapterViewModel.
/// </summary>
public interface IChapterToChapterViewModelMapper
{
    /// <summary>
    /// Chuyển đổi một đối tượng Chapter (từ MangaDex) thành ChapterViewModel.
    /// </summary>
    /// <param name="chapterData">Đối tượng Chapter gốc từ API.</param>
    /// <returns>ChapterViewModel đã được điền dữ liệu.</returns>
    ChapterViewModel MapToChapterViewModel(MangaReader.WebUI.Models.Mangadex.Chapter chapterData);
} 