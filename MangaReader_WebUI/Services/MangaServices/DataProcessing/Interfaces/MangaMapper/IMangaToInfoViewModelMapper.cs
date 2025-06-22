using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Models.ViewModels.Manga;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

/// <summary>
/// Định nghĩa phương thức để chuyển đổi một đối tượng Manga thành MangaInfoViewModel.
/// </summary>
public interface IMangaToInfoViewModelMapper
{
    /// <summary>
    /// Chuyển đổi một đối tượng Manga thành MangaInfoViewModel (chỉ chứa ID, Title, CoverUrl).
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc từ API.</param>
    /// <returns>Một Task chứa MangaInfoViewModel.</returns>
    MangaInfoViewModel MapToMangaInfoViewModel(Manga mangaData);
} 