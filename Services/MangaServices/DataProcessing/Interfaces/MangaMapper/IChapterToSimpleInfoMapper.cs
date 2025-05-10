using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;

/// <summary>
/// Định nghĩa phương thức để chuyển đổi một đối tượng Chapter thành SimpleChapterInfo.
/// </summary>
public interface IChapterToSimpleInfoMapper
{
    /// <summary>
    /// Chuyển đổi một đối tượng Chapter thành SimpleChapterInfo (dùng cho danh sách chapter mới nhất).
    /// </summary>
    /// <param name="chapterData">Đối tượng Chapter gốc từ API.</param>
    /// <returns>SimpleChapterInfo.</returns>
    SimpleChapterInfo MapToSimpleChapterInfo(MangaReader.WebUI.Models.Mangadex.Chapter chapterData);
} 