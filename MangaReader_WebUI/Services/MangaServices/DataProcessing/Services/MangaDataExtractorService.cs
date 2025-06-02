using MangaReader.WebUI.Enums; // Cần cho MangaSource
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Services; // Cần cho CoverApiService static helper
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.UtilityServices; // Cần cho LocalizationService
using Microsoft.Extensions.Configuration; // Thêm using này
using Microsoft.AspNetCore.Http;        // Thêm using này
using System.Diagnostics;
using System.Text.Json; // Cần cho JsonException và JsonSerializer

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;

/// <summary>
/// Triển khai IMangaDataExtractor, chịu trách nhiệm trích xuất dữ liệu cụ thể từ Model MangaDex.
/// </summary>
public class MangaDataExtractorService : IMangaDataExtractor
{
    private readonly ILogger<MangaDataExtractorService> _logger;
    private readonly LocalizationService _localizationService;
    private readonly IConfiguration _configuration; // Thêm IConfiguration
    private readonly IHttpContextAccessor _httpContextAccessor; // Thêm IHttpContextAccessor

    private readonly string _backendApiBaseUrl;
    private readonly string _mangaReaderLibApiBaseUrl;
    private readonly string _cloudinaryBaseUrl;

    // Từ điển dịch tag (có thể chuyển ra file config hoặc service riêng nếu lớn)
    private static readonly Dictionary<string, string> _tagTranslations = InitializeTagTranslations();
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MangaDataExtractorService(
        ILogger<MangaDataExtractorService> logger,
        LocalizationService localizationService,
        IConfiguration configuration, // Inject IConfiguration
        IHttpContextAccessor httpContextAccessor) // Inject IHttpContextAccessor
    {
        _logger = logger;
        _localizationService = localizationService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        _backendApiBaseUrl = _configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                            ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured in MangaDataExtractorService.");
        _mangaReaderLibApiBaseUrl = _configuration["MangaReaderApiSettings:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured in MangaDataExtractorService.");
        _cloudinaryBaseUrl = _configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                            ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
    }

    private MangaSource GetCurrentMangaSource()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Cookies.TryGetValue("MangaSource", out var sourceString))
        {
            if (Enum.TryParse(sourceString, true, out MangaSource source))
            {
                return source;
            }
        }
        return MangaSource.MangaDex; // Mặc định
    }

    public string ExtractCoverUrl(string mangaId, List<Relationship>? relationships)
    {
        Debug.Assert(!string.IsNullOrEmpty(mangaId), "Manga ID không được rỗng khi trích xuất Cover URL.");
        try
        {
            var currentSource = GetCurrentMangaSource();
            _logger.LogDebug("ExtractCoverUrl: Source = {Source}, MangaId = {MangaId}", currentSource, mangaId);

            if (relationships == null || !relationships.Any())
            {
                _logger.LogWarning("ExtractCoverUrl: Danh sách relationships rỗng hoặc null cho manga ID: {MangaId}. Sử dụng placeholder.", mangaId);
                return "/images/cover-placeholder.jpg";
            }

            var coverRelationship = relationships.FirstOrDefault(r => r != null && r.Type == "cover_art");

            if (coverRelationship == null)
            {
                _logger.LogWarning("ExtractCoverUrl: Không tìm thấy relationship 'cover_art' cho manga ID: {MangaId}. Sử dụng placeholder.", mangaId);
                return "/images/cover-placeholder.jpg";
            }

            string? publicIdOrFileName = null;

            // Lấy publicId (hoặc fileName chứa publicId) từ attributes
            if (coverRelationship.Attributes is CoverAttributes coverAttributesMangaDex)
            {
                // Đối với MangaDex, FileName chứa tên file thực sự
                // Đối với MangaReaderLib, sau khi map ở Strategy, FileName sẽ chứa PublicId
                publicIdOrFileName = coverAttributesMangaDex.FileName;
                 _logger.LogDebug("ExtractCoverUrl: Extracted publicIdOrFileName '{PublicIdOrFileName}' from CoverAttributes object.", publicIdOrFileName);
            }
            else if (coverRelationship.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
            {
                if (attributesElement.TryGetProperty("fileName", out var fileNameElement) && fileNameElement.ValueKind == JsonValueKind.String)
                {
                    publicIdOrFileName = fileNameElement.GetString();
                    _logger.LogDebug("ExtractCoverUrl: Extracted publicIdOrFileName '{PublicIdOrFileName}' from JsonElement.fileName.", publicIdOrFileName);
                }
                else if (attributesElement.TryGetProperty("publicId", out var publicIdElement) && publicIdElement.ValueKind == JsonValueKind.String) // Fallback nếu có trường publicId trực tiếp
                {
                    publicIdOrFileName = publicIdElement.GetString();
                     _logger.LogDebug("ExtractCoverUrl: Extracted publicId '{PublicId}' from JsonElement.publicId.", publicIdOrFileName);
                }
            }

            if (string.IsNullOrEmpty(publicIdOrFileName))
            {
                _logger.LogWarning("ExtractCoverUrl: Không thể trích xuất publicId hoặc fileName cho cover_art của manga ID {MangaId}. Relationship ID: {RelationshipId}. Attributes Type: {AttributeType}",
                    mangaId, coverRelationship.Id, coverRelationship.Attributes?.GetType().Name ?? "null");
                return "/images/cover-placeholder.jpg";
            }

            if (currentSource == MangaSource.MangaDex)
            {
                // MangaDex: publicIdOrFileName là fileName thực sự
                var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{publicIdOrFileName}.512.jpg"; // Size mặc định 512px
                var proxiedUrl = $"{_backendApiBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
                _logger.LogDebug("ExtractCoverUrl (MangaDex): Proxied URL = {Url}", proxiedUrl);
                return proxiedUrl;
            }
            else // currentSource == MangaSource.MangaReaderLib
            {
                // MangaReaderLib: publicIdOrFileName giờ đây chính là PublicId của Cloudinary
                // Xây dựng URL Cloudinary trực tiếp
                var cloudinaryUrl = $"{_cloudinaryBaseUrl}/{publicIdOrFileName}"; // Không thêm transform ở đây
                _logger.LogDebug("ExtractCoverUrl (MangaReaderLib): Direct Cloudinary URL = {Url}", cloudinaryUrl);
                return cloudinaryUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi trích xuất Cover URL cho manga ID: {mangaId}");
            return "/images/cover-placeholder.jpg";
        }
    }

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
            _logger.LogError(ex, "Lỗi khi trích xuất tiêu đề manga.");
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
            _logger.LogError(ex, "Lỗi khi trích xuất mô tả manga.");
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
                        _logger.LogDebug($"Không tìm thấy bản dịch cho tag: {enTagName}");
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
            _logger.LogError(ex, "Lỗi khi trích xuất và dịch tags manga.");
            return new List<string>();
        }
    }

    public (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships)
    {
        string author = "Không rõ";
        string artist = "Không rõ";

        if (relationships == null || !relationships.Any())
        {
            _logger.LogDebug("ExtractAuthorArtist: Danh sách relationships rỗng hoặc null.");
            return (author, artist);
        }

        try
        {
            var currentSource = GetCurrentMangaSource(); // Lấy nguồn hiện tại

            foreach (var rel in relationships)
            {
                if (rel == null) continue;
                string relType = rel.Type;
                string name = "Không rõ";

                if (relType == "author" || relType == "artist")
                {
                    // Kiểm tra xem attributes có phải là MangaDex.AuthorAttributes không (đã được map từ Strategy)
                    if (rel.Attributes is MangaReader.WebUI.Models.Mangadex.AuthorAttributes authorAttributes)
                    {
                        name = authorAttributes.Name ?? "Không rõ";
                        _logger.LogDebug("ExtractAuthorArtist (Source: {Source}): Lấy Name '{Name}' từ AuthorAttributes cho relationship type '{RelType}', ID '{RelId}'.", currentSource, name, relType, rel.Id);
                    }
                    // Fallback kiểm tra JsonElement (cho MangaDex hoặc trường hợp MRLib chưa map hoàn chỉnh)
                    else if (rel.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                    {
                        if (attributesElement.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                        {
                            name = nameElement.GetString() ?? "Không rõ";
                             _logger.LogDebug("ExtractAuthorArtist (Source: {Source}): Lấy Name '{Name}' từ JsonElement cho relationship type '{RelType}', ID '{RelId}'.", currentSource, name, relType, rel.Id);
                        }
                        else
                        {
                            _logger.LogWarning("ExtractAuthorArtist (Source: {Source}): Attributes của relationship {RelId} (type: {RelType}) không chứa 'name' hoặc không phải string.", currentSource, rel.Id, relType);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("ExtractAuthorArtist (Source: {Source}): Relationship {RelId} (type: {RelType}) không có attributes hoặc attributes không phải object/AuthorAttributes. Attributes Type: {AttributeType}. Đảm bảo có includes trong API call hoặc mapper đã làm giàu attributes.", 
                            currentSource, rel.Id, relType, rel.Attributes?.GetType().Name ?? "null");
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
            _logger.LogError(ex, "Lỗi khi trích xuất tác giả/họa sĩ từ relationships.");
        }

        return (author, artist);
    }

    public string ExtractAndTranslateStatus(string? status)
    {
        // Sử dụng LocalizationService để dịch
        return _localizationService.GetStatus(status);
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
            _logger.LogError(ex, "Lỗi khi xử lý tiêu đề thay thế từ List.");
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