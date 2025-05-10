using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

/// <summary>
/// Định nghĩa phương thức để chuyển đổi đối tượng Manga (từ MangaDex) thành MangaViewModel.
/// </summary>
public interface IMangaToMangaViewModelMapper
{
    /// <summary>
    /// Chuyển đổi một đối tượng Manga (từ MangaDex) thành MangaViewModel.
    /// Bao gồm cả việc lấy thông tin bổ sung như trạng thái theo dõi nếu cần.
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc từ API.</param>
    /// <returns>Một Task chứa MangaViewModel đã được điền dữ liệu.</returns>
    Task<MangaViewModel> MapToMangaViewModelAsync(MangaReader.WebUI.Models.Mangadex.Manga mangaData);
} 