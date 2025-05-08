# TODO: Tạo cấu trúc và Services cho /Services/MangaServices/DataProcessing

Tài liệu này hướng dẫn các bước tạo thư mục và mã nguồn cần thiết cho việc xử lý dữ liệu chuyển đổi từ Model MangaDex sang ViewModel.

## Bước 1: Tạo cấu trúc thư mục

1.  Trong thư mục `/Services/MangaServices`, tạo một thư mục mới tên là `DataProcessing`.
2.  Bên trong `DataProcessing`, tạo hai thư mục con:
    *   `Interfaces`
    *   `Services`

Cấu trúc cuối cùng sẽ là:

```
/Services
└── /MangaServices
    └── /DataProcessing
        ├── /Interfaces
        └── /Services
```

## Bước 2: Tạo Interfaces

### 2.1. Tạo Interface `IMangaViewModelMapper.cs`

Interface này định nghĩa các phương thức trả về trực tiếp một ViewModel hoàn chỉnh.

```csharp
// Services/MangaServices/DataProcessing/Interfaces/IMangaViewModelMapper.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.Models; // Namespace cho ChapterViewModel, MangaDetailViewModel

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
    Task<MangaViewModel> MapToMangaViewModelAsync(Manga mangaData);

    /// <summary>
    /// Chuyển đổi một đối tượng Chapter (từ MangaDex) thành ChapterViewModel.
    /// </summary>
    /// <param name="chapterData">Đối tượng Chapter gốc từ API.</param>
    /// <returns>ChapterViewModel đã được điền dữ liệu.</returns>
    ChapterViewModel MapToChapterViewModel(Chapter chapterData);

    /// <summary>
    /// Chuyển đổi dữ liệu Manga và danh sách Chapter thành MangaDetailViewModel.
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc.</param>
    /// <param name="chapters">Danh sách ChapterViewModel đã được xử lý.</param>
    /// <returns>Một Task chứa MangaDetailViewModel hoàn chỉnh.</returns>
    Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(Manga mangaData, List<ChapterViewModel> chapters);

    /// <summary>
    /// Chuyển đổi một đối tượng Manga thành MangaInfoViewModel (chỉ chứa ID, Title, CoverUrl).
    /// </summary>
    /// <param name="mangaData">Đối tượng Manga gốc từ API.</param>
    /// <returns>Một Task chứa MangaInfoViewModel.</returns>
    Task<MangaInfoViewModel> MapToMangaInfoViewModelAsync(Manga mangaData);

    /// <summary>
    /// Chuyển đổi một đối tượng Chapter thành SimpleChapterInfo (dùng cho danh sách chapter mới nhất).
    /// </summary>
    /// <param name="chapterData">Đối tượng Chapter gốc từ API.</param>
    /// <returns>SimpleChapterInfo.</returns>
    SimpleChapterInfo MapToSimpleChapterInfo(Chapter chapterData);
    
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
```

### 2.2. Tạo Interface `IMangaDataExtractor.cs`

Interface này định nghĩa các phương thức để trích xuất các phần dữ liệu riêng lẻ từ Model MangaDex.

```csharp
// Services/MangaServices/DataProcessing/Interfaces/IMangaDataExtractor.cs
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
```

## Bước 3: Tạo Services

### 3.1. Tạo Service `MangaDataExtractorService.cs`

Service này sẽ triển khai `IMangaDataExtractor`, chứa logic trích xuất dữ liệu.

```csharp
// Services/MangaServices/DataProcessing/Services/MangaDataExtractorService.cs
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces; // Cần cho ICoverApiService
using MangaReader.WebUI.Services.APIServices.Services; // Cần cho CoverApiService static helper
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.UtilityServices; // Cần cho LocalizationService
using System.Diagnostics;
using System.Text.Json; // Cần cho JsonException và JsonSerializer

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;

/// <summary>
/// Triển khai IMangaDataExtractor, chịu trách nhiệm trích xuất dữ liệu cụ thể từ Model MangaDex.
/// </summary>
public class MangaDataExtractorService(
    ILogger<MangaDataExtractorService> logger,
    ICoverApiService coverApiService, // Để tạo URL ảnh bìa proxy
    LocalizationService localizationService // Để dịch status
    ) : IMangaDataExtractor
{
    // Từ điển dịch tag (có thể chuyển ra file config hoặc service riêng nếu lớn)
    private static readonly Dictionary<string, string> _tagTranslations = InitializeTagTranslations();
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
    {
        Debug.Assert(titleDict != null || altTitlesList != null, "Phải có ít nhất titleDict hoặc altTitlesList để trích xuất tiêu đề.");

        try
        {
            // Ưu tiên Tiếng Việt từ AltTitles trước
            if (altTitlesList != null)
            {
                foreach (var altTitleDict in altTitlesList)
                {
                    if (altTitleDict != null && altTitleDict.TryGetValue("vi", out var viTitle) && !string.IsNullOrEmpty(viTitle))
                    {
                        return viTitle;
                    }
                }
            }

            // Nếu không có Tiếng Việt trong AltTitles, thử Tiếng Anh trong Title chính
            if (titleDict != null && titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
            {
                return enTitle;
            }

            // Nếu không có Tiếng Anh, thử Tiếng Việt trong Title chính
             if (titleDict != null && titleDict.TryGetValue("vi", out var mainViTitle) && !string.IsNullOrEmpty(mainViTitle))
            {
                return mainViTitle;
            }

            // Nếu không có, lấy tiêu đề đầu tiên từ Title chính
            if (titleDict != null && titleDict.Any())
            {
                return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
            }

            // Nếu Title chính rỗng, thử lấy tiêu đề đầu tiên từ AltTitles
            if (altTitlesList != null)
            {
                foreach (var altTitleDict in altTitlesList)
                {
                    if (altTitleDict != null && altTitleDict.Any())
                    {
                        return altTitleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
                    }
                }
            }

            return "Không có tiêu đề";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi trích xuất tiêu đề manga.");
            return "Lỗi tiêu đề";
        }
    }

    public string ExtractMangaDescription(Dictionary<string, string>? descriptionDict)
    {
        if (descriptionDict == null || !descriptionDict.Any())
        {
            return "";
        }

        try
        {
            // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
            if (descriptionDict.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
            {
                return viDesc;
            }
            if (descriptionDict.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
            {
                return enDesc;
            }

            // Lấy giá trị đầu tiên nếu không có vi/en
            return descriptionDict.FirstOrDefault().Value ?? "";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi trích xuất mô tả manga.");
            return "";
        }
    }

    public List<string> ExtractAndTranslateTags(List<Tag>? tagsList)
    {
        var translatedTags = new List<string>();
        if (tagsList == null || !tagsList.Any())
        {
            return translatedTags;
        }

        try
        {
            foreach (var tag in tagsList)
            {
                if (tag?.Attributes?.Name == null) continue;

                // Ưu tiên lấy tên tiếng Anh để dịch
                if (tag.Attributes.Name.TryGetValue("en", out var enTagName) && !string.IsNullOrEmpty(enTagName))
                {
                    if (_tagTranslations.TryGetValue(enTagName, out var translation))
                    {
                        translatedTags.Add(translation);
                    }
                    else
                    {
                        // Nếu không có bản dịch, giữ nguyên tag tiếng Anh
                        translatedTags.Add(enTagName);
                        logger.LogDebug($"Không tìm thấy bản dịch cho tag: {enTagName}");
                    }
                }
                // Nếu không có tiếng Anh, lấy tên đầu tiên và không dịch
                else if (tag.Attributes.Name.Any())
                {
                     translatedTags.Add(tag.Attributes.Name.First().Value);
                }
            }

            // Sắp xếp tags theo thứ tự alphabet tiếng Việt
            return translatedTags.Distinct().OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), false)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi trích xuất và dịch tags manga.");
            return new List<string>();
        }
    }

    public (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships)
    {
        string author = "Không rõ";
        string artist = "Không rõ";

        if (relationships == null) return (author, artist);

        try
        {
            foreach (var rel in relationships)
            {
                if (rel == null) continue;
                string relType = rel.Type;

                if (relType == "author" || relType == "artist")
                {
                    string name = "Không rõ";
                    // Kiểm tra xem attributes có được include và có phải là object JSON không
                    if (rel.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                    {
                        if (attributesElement.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                        {
                            name = nameElement.GetString() ?? "Không rõ";
                        }
                        else
                        {
                            logger.LogWarning($"Attributes của relationship {rel.Id} (type: {relType}) không chứa 'name' hoặc không phải string.");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"Relationship {rel.Id} (type: {relType}) không có attributes hoặc attributes không phải object. Đảm bảo có includes trong API call.");
                    }

                    if (relType == "author")
                        author = name;
                    else // relType == "artist"
                        artist = name;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi trích xuất tác giả/họa sĩ từ relationships.");
        }

        return (author, artist);
    }

    public string ExtractCoverUrl(string mangaId, List<Relationship>? relationships)
    {
         Debug.Assert(!string.IsNullOrEmpty(mangaId), "Manga ID không được rỗng khi trích xuất Cover URL.");
        try
        {
            // Sử dụng helper tĩnh từ CoverApiService để trích xuất filename
            // Truyền logger vào hàm helper
            var coverFileName = CoverApiService.ExtractCoverFileNameFromRelationships(relationships, logger);
            if (!string.IsNullOrEmpty(coverFileName))
            {
                // Sử dụng instance coverApiService để tạo URL proxy
                return coverApiService.GetProxiedCoverUrl(mangaId, coverFileName);
            }
            else
            {
                logger.LogDebug($"Không tìm thấy cover filename cho manga ID {mangaId} từ relationships.");
                return "/images/cover-placeholder.jpg"; // Placeholder
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Lỗi khi trích xuất Cover URL cho manga ID: {mangaId}");
            return "/images/cover-placeholder.jpg"; // Placeholder
        }
    }

    public string ExtractAndTranslateStatus(string? status)
    {
        // Sử dụng LocalizationService để dịch
        return localizationService.GetStatus(status);
    }

     public string ExtractChapterDisplayTitle(ChapterAttributes attributes)
    {
        Debug.Assert(attributes != null, "ChapterAttributes không được null khi trích xuất Display Title.");

        string chapterNumber = attributes.ChapterNumber ?? "?"; // Lấy số chapter, mặc định là "?"
        string chapterTitle = attributes.Title ?? ""; // Lấy tiêu đề, mặc định là rỗng

        // Trường hợp đặc biệt cho Oneshot hoặc khi không có số chapter
        if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
        {
            return !string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot";
        }

        // Format tiêu đề chuẩn
        string displayTitle = $"Chương {chapterNumber}";
        if (!string.IsNullOrEmpty(chapterTitle) && chapterTitle != chapterNumber)
        {
            displayTitle += $": {chapterTitle}";
        }

        return displayTitle;
    }

    public string ExtractChapterNumber(ChapterAttributes attributes)
    {
         Debug.Assert(attributes != null, "ChapterAttributes không được null khi trích xuất Chapter Number.");
         return attributes.ChapterNumber ?? "?";
    }

    public Dictionary<string, List<string>> ExtractAlternativeTitles(List<Dictionary<string, string>>? altTitlesList)
    {
        var altTitlesDictionary = new Dictionary<string, List<string>>();
        if (altTitlesList == null) return altTitlesDictionary;

        try
        {
            foreach (var altTitleDict in altTitlesList)
            {
                if (altTitleDict != null && altTitleDict.Any())
                {
                    // Giả định mỗi dictionary con chỉ có một cặp key-value (lang-title)
                    var langKey = altTitleDict.Keys.First();
                    var titleText = altTitleDict[langKey];

                    if (!string.IsNullOrEmpty(titleText))
                    {
                        if (!altTitlesDictionary.ContainsKey(langKey))
                        {
                            altTitlesDictionary[langKey] = new List<string>();
                        }
                        altTitlesDictionary[langKey].Add(titleText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi xử lý tiêu đề thay thế từ List.");
        }

        return altTitlesDictionary;
    }

    public string ExtractPreferredAlternativeTitle(Dictionary<string, List<string>> altTitlesDictionary)
    {
        if (altTitlesDictionary == null || !altTitlesDictionary.Any()) return "";

        // Ưu tiên tiếng Anh
        if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
        // Sau đó là romaji
        if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First();
        // Lấy cái đầu tiên nếu không có
        return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
    }


    // Helper khởi tạo từ điển dịch tag
    private static Dictionary<string, string> InitializeTagTranslations()
    {
        // (Giữ nguyên từ điển dịch từ MangaTagService cũ)
        return new Dictionary<string, string>
        {
            { "Oneshot", "Oneshot" }, { "Thriller", "Hồi hộp" }, { "Award Winning", "Đạt giải thưởng" },
            { "Reincarnation", "Chuyển sinh" }, { "Sci-Fi", "Khoa học viễn tưởng" }, { "Time Travel", "Du hành thời gian" },
            { "Genderswap", "Chuyển giới" }, { "Loli", "Loli" }, { "Traditional Games", "Trò chơi truyền thống" },
            { "Official Colored", "Bản màu chính thức" }, { "Historical", "Lịch sử" }, { "Monsters", "Quái vật" },
            { "Action", "Hành động" }, { "Demons", "Ác quỷ" }, { "Psychological", "Tâm lý" }, { "Ghosts", "Ma" },
            { "Animals", "Động vật" }, { "Long Strip", "Truyện dài" }, { "Romance", "Lãng mạn" }, { "Ninja", "Ninja" },
            { "Comedy", "Hài hước" }, { "Mecha", "Robot" }, { "Anthology", "Tuyển tập" }, { "Boys' Love", "Tình yêu nam giới" },
            { "Incest", "Loạn luân" }, { "Crime", "Tội phạm" }, { "Survival", "Sinh tồn" }, { "Zombies", "Zombie" },
            { "Reverse Harem", "Harem đảo" }, { "Sports", "Thể thao" }, { "Superhero", "Siêu anh hùng" },
            { "Martial Arts", "Võ thuật" }, { "Fan Colored", "Bản màu fanmade" }, { "Samurai", "Samurai" },
            { "Magical Girls", "Ma pháp thiếu nữ" }, { "Mafia", "Mafia" }, { "Adventure", "Phiêu lưu" },
            { "Self-Published", "Tự xuất bản" }, { "Virtual Reality", "Thực tế ảo" }, { "Office Workers", "Nhân viên văn phòng" },
            { "Video Games", "Trò chơi điện tử" }, { "Post-Apocalyptic", "Hậu tận thế" }, { "Sexual Violence", "Bạo lực tình dục" },
            { "Crossdressing", "Giả trang khác giới" }, { "Magic", "Phép thuật" }, { "Girls' Love", "Tình yêu nữ giới" },
            { "Harem", "Harem" }, { "Military", "Quân đội" }, { "Wuxia", "Võ hiệp" }, { "Isekai", "Dị giới" },
            { "4-Koma", "4-Koma" }, { "Doujinshi", "Doujinshi" }, { "Philosophical", "Triết học" }, { "Gore", "Bạo lực" },
            { "Drama", "Kịch tính" }, { "Medical", "Y học" }, { "School Life", "Học đường" }, { "Horror", "Kinh dị" },
            { "Fantasy", "Kỳ ảo" }, { "Villainess", "Nữ phản diện" }, { "Vampires", "Ma cà rồng" },
            { "Delinquents", "Học sinh cá biệt" }, { "Monster Girls", "Monster Girls" }, { "Shota", "Shota" },
            { "Police", "Cảnh sát" }, { "Web Comic", "Web Comic" }, { "Slice of Life", "Đời thường" },
            { "Aliens", "Người ngoài hành tinh" }, { "Cooking", "Nấu ăn" }, { "Supernatural", "Siêu nhiên" },
            { "Mystery", "Bí ẩn" }, { "Adaptation", "Chuyển thể" }, { "Music", "Âm nhạc" }, { "Full Color", "Bản màu đầy đủ" },
            { "Tragedy", "Bi kịch" }, { "Gyaru", "Gyaru" }
        };
    }
}
```

### 3.2. Tạo Service `MangaViewModelMapperService.cs`

Service này sẽ triển khai `IMangaViewModelMapper` và sử dụng `IMangaDataExtractor` để tạo ViewModel.

```csharp
// Services/MangaServices/DataProcessing/Services/MangaViewModelMapperService.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.AuthServices; // Cần cho IUserService
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.Models;
using MangaReader.WebUI.Services.MangaServices.MangaInformation; // Cần cho MangaUtilityService
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;

/// <summary>
/// Triển khai IMangaViewModelMapper, chịu trách nhiệm chuyển đổi Model MangaDex thành ViewModel.
/// </summary>
public class MangaViewModelMapperService(
    ILogger<MangaViewModelMapperService> logger,
    IMangaDataExtractor mangaDataExtractor, // Để lấy dữ liệu đã xử lý
    IMangaFollowService mangaFollowService, // Để kiểm tra trạng thái theo dõi
    IUserService userService,               // Để kiểm tra đăng nhập
    MangaUtilityService mangaUtilityService // Để lấy rating/views giả
    ) : IMangaViewModelMapper
{
    public async Task<MangaViewModel> MapToMangaViewModelAsync(Manga mangaData)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaViewModel.");
        Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaViewModel.");

        string id = mangaData.Id.ToString();
        var attributes = mangaData.Attributes!; // Sử dụng ! vì đã assert ở trên
        var relationships = mangaData.Relationships;

        try
        {
            string title = mangaDataExtractor.ExtractMangaTitle(attributes.Title, attributes.AltTitles);
            string description = mangaDataExtractor.ExtractMangaDescription(attributes.Description);
            string coverUrl = mangaDataExtractor.ExtractCoverUrl(id, relationships);
            string status = mangaDataExtractor.ExtractAndTranslateStatus(attributes.Status);
            List<string> tags = mangaDataExtractor.ExtractAndTranslateTags(attributes.Tags);
            var (author, artist) = mangaDataExtractor.ExtractAuthorArtist(relationships);
            var altTitlesDict = mangaDataExtractor.ExtractAlternativeTitles(attributes.AltTitles);
            string preferredAltTitle = mangaDataExtractor.ExtractPreferredAlternativeTitle(altTitlesDict);

            // Kiểm tra trạng thái follow (yêu cầu User Service và Follow Service)
            bool isFollowing = false;
            if (userService.IsAuthenticated())
            {
                // Thêm try-catch riêng cho việc kiểm tra follow để không làm crash toàn bộ mapping
                try
                {
                    isFollowing = await mangaFollowService.IsFollowingMangaAsync(id);
                }
                catch (Exception followEx)
                {
                    logger.LogError(followEx, "Lỗi khi kiểm tra trạng thái theo dõi cho manga {MangaId} trong Mapper.", id);
                    // isFollowing vẫn là false
                }
            }

            return new MangaViewModel
            {
                Id = id,
                Title = title,
                Description = description,
                CoverUrl = coverUrl,
                Status = status,
                Tags = tags,
                Author = author,
                Artist = artist,
                OriginalLanguage = attributes.OriginalLanguage ?? "",
                PublicationDemographic = attributes.PublicationDemographic ?? "",
                ContentRating = attributes.ContentRating ?? "",
                AlternativeTitles = preferredAltTitle, // Lấy tiêu đề thay thế ưu tiên
                LastUpdated = attributes.UpdatedAt.DateTime, // Chuyển DateTimeOffset thành DateTime
                IsFollowing = isFollowing,
                // Lấy dữ liệu giả từ Utility Service
                Rating = mangaUtilityService.GetMangaRating(id),
                Views = 0 // MangaDex API không cung cấp Views
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi nghiêm trọng khi mapping MangaData thành MangaViewModel cho ID: {MangaId}", id);
            // Trả về ViewModel lỗi để không làm crash trang
            return new MangaViewModel
            {
                Id = id,
                Title = $"Lỗi tải ({id})",
                Description = "Đã xảy ra lỗi khi xử lý dữ liệu.",
                CoverUrl = "/images/cover-placeholder.jpg",
                Status = "Lỗi",
                Tags = new List<string>(),
                Author = "Lỗi",
                Artist = "Lỗi"
            };
        }
    }

    public ChapterViewModel MapToChapterViewModel(Chapter chapterData)
    {
        Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành ChapterViewModel.");
        Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành ChapterViewModel.");

        var attributes = chapterData.Attributes!; // Use ! because of the assert

        try
        {
            string displayTitle = mangaDataExtractor.ExtractChapterDisplayTitle(attributes);
            string chapterNumber = mangaDataExtractor.ExtractChapterNumber(attributes);

            // Xử lý relationships (đơn giản hóa, chỉ lấy ID và Type)
            var relationships = chapterData.Relationships?
                .Where(r => r != null)
                .Select(r => new ChapterRelationship { Id = r!.Id.ToString(), Type = r.Type })
                .ToList() ?? new List<ChapterRelationship>();

            return new ChapterViewModel
            {
                Id = chapterData.Id.ToString(),
                Title = displayTitle,
                Number = chapterNumber,
                Language = attributes.TranslatedLanguage ?? "unknown",
                PublishedAt = attributes.PublishAt.DateTime, // Convert DateTimeOffset to DateTime
                Relationships = relationships
            };
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Lỗi khi mapping ChapterData thành ChapterViewModel cho ID: {ChapterId}", chapterData?.Id);
            // Trả về ViewModel lỗi
            return new ChapterViewModel
            {
                Id = chapterData?.Id.ToString() ?? "error",
                Title = "Lỗi tải chương",
                Number = "?",
                Language = "error",
                PublishedAt = DateTime.MinValue,
                Relationships = new List<ChapterRelationship>()
            };
        }
    }

     public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(Manga mangaData, List<ChapterViewModel> chapters)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaDetailViewModel.");
        Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaDetailViewModel.");
        Debug.Assert(chapters != null, "chapters không được null khi mapping thành MangaDetailViewModel.");

        // Map thông tin manga cơ bản
        var mangaViewModel = await MapToMangaViewModelAsync(mangaData);

        // Trích xuất danh sách tiêu đề thay thế đã nhóm
        var alternativeTitlesByLanguage = mangaDataExtractor.ExtractAlternativeTitles(mangaData.Attributes?.AltTitles);

        return new MangaDetailViewModel
        {
            Manga = mangaViewModel,
            Chapters = chapters ?? new List<ChapterViewModel>(), // Đảm bảo không null
            AlternativeTitlesByLanguage = alternativeTitlesByLanguage
        };
    }

    public async Task<MangaInfoViewModel> MapToMangaInfoViewModelAsync(Manga mangaData)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaInfoViewModel.");

        string id = mangaData.Id.ToString();
        var attributes = mangaData.Attributes;
        var relationships = mangaData.Relationships;

        string title = "Lỗi tải tiêu đề";
        if (attributes != null)
        {
            title = mangaDataExtractor.ExtractMangaTitle(attributes.Title, attributes.AltTitles);
        }

        string coverUrl = mangaDataExtractor.ExtractCoverUrl(id, relationships);

        // MangaInfoViewModel không cần trạng thái follow hay các thông tin phức tạp khác
        return new MangaInfoViewModel
        {
            MangaId = id,
            MangaTitle = title,
            CoverUrl = coverUrl
        };
    }

     public SimpleChapterInfo MapToSimpleChapterInfo(Chapter chapterData)
    {
        Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành SimpleChapterInfo.");
        Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành SimpleChapterInfo.");

        var attributes = chapterData.Attributes!;

        string displayTitle = mangaDataExtractor.ExtractChapterDisplayTitle(attributes);

        return new SimpleChapterInfo
        {
            ChapterId = chapterData.Id.ToString(),
            DisplayTitle = displayTitle,
            PublishedAt = attributes.PublishAt.DateTime
        };
    }
    
    public LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfo chapterInfo, DateTime lastReadAt)
    {
        Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành LastReadMangaViewModel.");
        Debug.Assert(chapterInfo != null, "chapterInfo không được null khi mapping thành LastReadMangaViewModel.");

        return new LastReadMangaViewModel
        {
            MangaId = mangaInfo.MangaId,
            MangaTitle = mangaInfo.MangaTitle,
            CoverUrl = mangaInfo.CoverUrl,
            ChapterId = chapterInfo.Id,
            ChapterTitle = chapterInfo.Title,
            ChapterPublishedAt = chapterInfo.PublishedAt,
            LastReadAt = lastReadAt
        };
    }

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
```