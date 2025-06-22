using MangaDexLib.DataProcessing.Interfaces;
using MangaDexLib.Models;
using MangaDexLib.Services.UtilityServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MangaDexLib.DataProcessing.Services
{
    public class MangaDataExtractorService : IMangaDataExtractor
    {
        private readonly ILogger<MangaDataExtractorService> _logger;
        private readonly LocalizationService _localizationService;
        private readonly IConfiguration _configuration;
        private readonly string _backendApiBaseUrl;

        private static readonly Dictionary<string, string> _tagTranslations = InitializeTagTranslations();
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public MangaDataExtractorService(
            ILogger<MangaDataExtractorService> logger,
            LocalizationService localizationService,
            IConfiguration configuration)
        {
            _logger = logger;
            _localizationService = localizationService;
            _configuration = configuration;
            _backendApiBaseUrl = _configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                                ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured in MangaDataExtractorService.");
        }

        public string ExtractCoverUrl(string mangaId, List<Relationship>? relationships)
        {
            try
            {
                if (relationships == null || !relationships.Any())
                {
                    return "/images/cover-placeholder.jpg";
                }

                var coverRelationship = relationships.FirstOrDefault(r => r != null && r.Type == "cover_art");

                if (coverRelationship == null)
                {
                    return "/images/cover-placeholder.jpg";
                }

                string? fileName = null;
                if (coverRelationship.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                {
                    if (attributesElement.TryGetProperty("fileName", out var fileNameElement) && fileNameElement.ValueKind == JsonValueKind.String)
                    {
                        fileName = fileNameElement.GetString();
                    }
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    return "/images/cover-placeholder.jpg";
                }

                var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}.512.jpg";
                return $"{_backendApiBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi trích xuất Cover URL cho manga ID: {mangaId}");
                return "/images/cover-placeholder.jpg";
            }
        }

        public string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
        {
            try
            {
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

                if (titleDict != null && titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
                {
                    return enTitle;
                }

                if (titleDict != null && titleDict.TryGetValue("vi", out var mainViTitle) && !string.IsNullOrEmpty(mainViTitle))
                {
                    return mainViTitle;
                }

                if (titleDict != null && titleDict.Any())
                {
                    return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
                }

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
                if (descriptionDict.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
                {
                    return viDesc;
                }
                if (descriptionDict.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
                {
                    return enDesc;
                }

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

                    if (tag.Attributes.Name.TryGetValue("en", out var enTagName) && !string.IsNullOrEmpty(enTagName))
                    {
                        if (_tagTranslations.TryGetValue(enTagName, out var translation))
                        {
                            translatedTags.Add(translation);
                        }
                        else
                        {
                            translatedTags.Add(enTagName);
                            _logger.LogDebug($"Không tìm thấy bản dịch cho tag: {enTagName}");
                        }
                    }
                    else if (tag.Attributes.Name.Any())
                    {
                        translatedTags.Add(tag.Attributes.Name.First().Value);
                    }
                }

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
                return (author, artist);
            }

            try
            {
                foreach (var rel in relationships)
                {
                    if (rel == null) continue;
                    string relType = rel.Type;
                    string name = "Không rõ";

                    if (relType == "author" || relType == "artist")
                    {
                        if (rel.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                        {
                            if (attributesElement.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                            {
                                name = nameElement.GetString() ?? "Không rõ";
                            }
                        }

                        if (relType == "author")
                            author = name;
                        else
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
            return _localizationService.GetStatus(status);
        }

        public string ExtractChapterDisplayTitle(ChapterAttributes attributes)
        {
            string chapterNumberString = attributes.ChapterNumber ?? "?";
            string specificChapterTitle = attributes.Title?.Trim() ?? "";

            if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
            {
                return !string.IsNullOrEmpty(specificChapterTitle) ? specificChapterTitle : "Oneshot";
            }

            string patternChapterVn = $"^Chương\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";
            string patternChapterEn = $"^Chapter\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";

            bool startsWithChapterInfo = Regex.IsMatch(specificChapterTitle, patternChapterVn, RegexOptions.IgnoreCase) ||
                                         Regex.IsMatch(specificChapterTitle, patternChapterEn, RegexOptions.IgnoreCase);

            if (startsWithChapterInfo)
            {
                return specificChapterTitle;
            }
            else if (!string.IsNullOrEmpty(specificChapterTitle))
            {
                return $"Chương {chapterNumberString}: {specificChapterTitle}";
            }
            else
            {
                return $"Chương {chapterNumberString}";
            }
        }

        public string ExtractChapterNumber(ChapterAttributes attributes)
        {
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

            if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
            if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First();
            return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
        }

        private static Dictionary<string, string> InitializeTagTranslations()
        {
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
}