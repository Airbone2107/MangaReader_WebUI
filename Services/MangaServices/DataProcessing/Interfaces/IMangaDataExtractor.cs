using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;

/// <summary>
/// Định nghĩa các phương thức để trích xuất các phần dữ liệu cụ thể từ Model MangaDex.
/// </summary>
public interface IMangaDataExtractor
{
    /// <summary>
    /// Trích xuất tiêu đề ưu tiên (Việt -> Anh -> Mặc định) từ thuộc tính Title và AltTitles.
    /// </summary>
    /// <param name="titleDict">Dictionary chứa các tiêu đề chính.</param>
    /// <param name="altTitlesList">Danh sách các Dictionary chứa tiêu đề thay thế.</param>
    /// <returns>Tiêu đề ưu tiên.</returns>
    string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList);

    /// <summary>
    /// Trích xuất mô tả ưu tiên (Việt -> Anh -> Mặc định) từ thuộc tính Description.
    /// </summary>
    /// <param name="descriptionDict">Dictionary chứa các mô tả.</param>
    /// <returns>Mô tả ưu tiên.</returns>
    string ExtractMangaDescription(Dictionary<string, string>? descriptionDict);

    /// <summary>
    /// Trích xuất danh sách các tags đã được dịch sang tiếng Việt và sắp xếp.
    /// </summary>
    /// <param name="tagsList">Danh sách các đối tượng Tag từ MangaAttributes.</param>
    /// <returns>Danh sách các tên tag đã dịch.</returns>
    List<string> ExtractAndTranslateTags(List<Tag>? tagsList);

    /// <summary>
    /// Trích xuất tên tác giả và họa sĩ từ danh sách Relationships.
    /// </summary>
    /// <param name="relationships">Danh sách Relationships của Manga.</param>
    /// <returns>Tuple chứa (Tên tác giả, Tên họa sĩ).</returns>
    (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships);

    /// <summary>
    /// Trích xuất URL ảnh bìa chính đã được proxy.
    /// </summary>
    /// <param name="mangaId">ID của Manga.</param>
    /// <param name="relationships">Danh sách Relationships của Manga.</param>
    /// <returns>URL ảnh bìa đã được proxy hoặc URL placeholder nếu không tìm thấy.</returns>
    string ExtractCoverUrl(string mangaId, List<Relationship>? relationships);

    /// <summary>
    /// Trích xuất trạng thái của Manga và dịch sang tiếng Việt.
    /// </summary>
    /// <param name="status">Chuỗi trạng thái gốc từ MangaAttributes.</param>
    /// <returns>Trạng thái đã được dịch.</returns>
    string ExtractAndTranslateStatus(string? status);

    /// <summary>
    /// Trích xuất tiêu đề hiển thị cho Chapter (VD: "Chương 10: Tên chương").
    /// </summary>
    /// <param name="attributes">Đối tượng ChapterAttributes.</param>
    /// <returns>Tiêu đề hiển thị.</returns>
    string ExtractChapterDisplayTitle(ChapterAttributes attributes);

    /// <summary>
    /// Trích xuất số chương từ ChapterAttributes.
    /// </summary>
    /// <param name="attributes">Đối tượng ChapterAttributes.</param>
    /// <returns>Số chương dưới dạng chuỗi, hoặc "?" nếu không có.</returns>
    string ExtractChapterNumber(ChapterAttributes attributes);

     /// <summary>
    /// Trích xuất các tiêu đề thay thế được nhóm theo ngôn ngữ.
    /// </summary>
    /// <param name="altTitlesList">Danh sách tiêu đề thay thế từ MangaAttributes.</param>
    /// <returns>Dictionary nhóm tiêu đề theo mã ngôn ngữ.</returns>
    Dictionary<string, List<string>> ExtractAlternativeTitles(List<Dictionary<string, string>>? altTitlesList);

    /// <summary>
    /// Trích xuất một tiêu đề thay thế ưu tiên (thường là tiếng Anh).
    /// </summary>
    /// <param name="altTitlesDictionary">Dictionary chứa các tiêu đề thay thế đã nhóm.</param>
    /// <returns>Tiêu đề thay thế ưu tiên hoặc chuỗi rỗng.</returns>
    string ExtractPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary);
} 