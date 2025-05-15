using MangaReader.WebUI.Models;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

/// <summary>
/// Định nghĩa phương thức để chuyển đổi dữ liệu Manga và danh sách Chapter thành MangaDetailViewModel.
/// </summary>
public interface IMangaToDetailViewModelMapper
{
    /// <summary>
    /// Chuyển đổi dữ liệu Manga và danh sách Chapter thành MangaDetailViewModel.
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc.</param>
    /// <param name="chapters">Danh sách ChapterViewModel đã được xử lý.</param>
    /// <returns>Một Task chứa MangaDetailViewModel hoàn chỉnh.</returns>
    Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(MangaReader.WebUI.Models.Mangadex.Manga mangaData, List<ChapterViewModel> chapters);
} 