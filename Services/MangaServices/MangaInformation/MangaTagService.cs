using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.MangaServices.MangaInformation
{
    public class MangaTagService
    {
        private readonly ILogger<MangaTagService> _logger;
        private readonly Dictionary<string, string> _tagTranslations;

        public MangaTagService(
            ILogger<MangaTagService> logger)
        {
            _logger = logger;
            _tagTranslations = InitializeTagTranslations();
        }

        /// <summary>
        /// Khởi tạo từ điển dịch tag từ tiếng Anh sang tiếng Việt
        /// </summary>
        private Dictionary<string, string> InitializeTagTranslations()
        {
            return new Dictionary<string, string>
            {
                { "Oneshot", "Oneshot" },
                { "Thriller", "Hồi hộp" },
                { "Award Winning", "Đạt giải thưởng" },
                { "Reincarnation", "Chuyển sinh" },
                { "Sci-Fi", "Khoa học viễn tưởng" },
                { "Time Travel", "Du hành thời gian" },
                { "Genderswap", "Chuyển giới" },
                { "Loli", "Loli" },
                { "Traditional Games", "Trò chơi truyền thống" },
                { "Official Colored", "Bản màu chính thức" },
                { "Historical", "Lịch sử" },
                { "Monsters", "Quái vật" },
                { "Action", "Hành động" },
                { "Demons", "Ác quỷ" },
                { "Psychological", "Tâm lý" },
                { "Ghosts", "Ma" },
                { "Animals", "Động vật" },
                { "Long Strip", "Truyện dài" },
                { "Romance", "Lãng mạn" },
                { "Ninja", "Ninja" },
                { "Comedy", "Hài hước" },
                { "Mecha", "Robot" },
                { "Anthology", "Tuyển tập" },
                { "Boys' Love", "Tình yêu nam giới" },
                { "Incest", "Loạn luân" },
                { "Crime", "Tội phạm" },
                { "Survival", "Sinh tồn" },
                { "Zombies", "Zombie" },
                { "Reverse Harem", "Harem đảo" },
                { "Sports", "Thể thao" },
                { "Superhero", "Siêu anh hùng" },
                { "Martial Arts", "Võ thuật" },
                { "Fan Colored", "Bản màu fanmade" },
                { "Samurai", "Samurai" },
                { "Magical Girls", "Ma pháp thiếu nữ" },
                { "Mafia", "Mafia" },
                { "Adventure", "Phiêu lưu" },
                { "Self-Published", "Tự xuất bản" },
                { "Virtual Reality", "Thực tế ảo" },
                { "Office Workers", "Nhân viên văn phòng" },
                { "Video Games", "Trò chơi điện tử" },
                { "Post-Apocalyptic", "Hậu tận thế" },
                { "Sexual Violence", "Bạo lực tình dục" },
                { "Crossdressing", "Giả trang khác giới" },
                { "Magic", "Phép thuật" },
                { "Girls' Love", "Tình yêu nữ giới" },
                { "Harem", "Harem" },
                { "Military", "Quân đội" },
                { "Wuxia", "Võ hiệp" },
                { "Isekai", "Dị giới" },
                { "4-Koma", "4-Koma" },
                { "Doujinshi", "Doujinshi" },
                { "Philosophical", "Triết học" },
                { "Gore", "Bạo lực" },
                { "Drama", "Kịch tính" },
                { "Medical", "Y học" },
                { "School Life", "Học đường" },
                { "Horror", "Kinh dị" },
                { "Fantasy", "Kỳ ảo" },
                { "Villainess", "Nữ phản diện" },
                { "Vampires", "Ma cà rồng" },
                { "Delinquents", "Học sinh cá biệt" },
                { "Monster Girls", "Monster Girls" },
                { "Shota", "Shota" },
                { "Police", "Cảnh sát" },
                { "Web Comic", "Web Comic" },
                { "Slice of Life", "Đời thường" },
                { "Aliens", "Người ngoài hành tinh" },
                { "Cooking", "Nấu ăn" },
                { "Supernatural", "Siêu nhiên" },
                { "Mystery", "Bí ẩn" },
                { "Adaptation", "Chuyển thể" },
                { "Music", "Âm nhạc" },
                { "Full Color", "Bản màu đầy đủ" },
                { "Tragedy", "Bi kịch" },
                { "Gyaru", "Gyaru" }
            };
        }

        /// <summary>
        /// Xử lý và lấy danh sách tags từ MangaAttributes
        /// </summary>
        /// <param name="attributes">MangaAttributes chứa thông tin tags</param>
        /// <returns>Danh sách các tag đã được xử lý và sắp xếp</returns>
        public List<string> GetMangaTags(MangaAttributes? attributes)
        {
            var tags = new List<string>();
            if (attributes?.Tags == null) return tags;

            try
            {
                // Lấy tags tiếng Anh từ model
                var englishTags = GetTagsFromModel(attributes.Tags);

                // Dịch sang tiếng Việt
                tags = TranslateTagsToVietnamese(englishTags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tags từ MangaAttributes.");
            }

            // Sắp xếp tags theo thứ tự alphabet cho dễ đọc
            return tags.Distinct().OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Dịch danh sách tag từ tiếng Anh sang tiếng Việt
        /// </summary>
        /// <param name="englishTags">Danh sách tag tiếng Anh</param>
        /// <returns>Danh sách tag đã được dịch sang tiếng Việt</returns>
        private List<string> TranslateTagsToVietnamese(List<string> englishTags)
        {
            var vietnameseTags = new List<string>();
            
            foreach (var tag in englishTags)
            {
                if (_tagTranslations.TryGetValue(tag, out var translation))
                {
                    vietnameseTags.Add(translation);
                }
                else
                {
                    // Nếu không có bản dịch, giữ nguyên tag tiếng Anh
                    vietnameseTags.Add(tag);
                    _logger.LogInformation($"Không tìm thấy bản dịch cho tag: {tag}");
                }
            }
            
            return vietnameseTags;
        }

        /// <summary>
        /// Lấy danh sách tags từ danh sách Tag model
        /// </summary>
        /// <param name="tagsList">Danh sách các Tag model</param>
        /// <returns>Danh sách tên tag tiếng Anh</returns>
        private List<string> GetTagsFromModel(List<Tag>? tagsList)
        {
            var tags = new List<string>();
            if (tagsList == null) return tags;

            try
            {
                foreach (var tag in tagsList)
                {
                    if (tag?.Attributes != null)
                    {
                        var tagName = ExtractTagName(tag.Attributes);
                        if (!string.IsNullOrEmpty(tagName) && tagName != "Không rõ")
                        {
                             tags.Add(tagName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tags từ List<Tag>.");
            }

            return tags;
        }

        /// <summary>
        /// Trích xuất tên tag từ TagAttributes
        /// </summary>
        /// <param name="tagAttributes">TagAttributes chứa thông tin tên tag</param>
        /// <returns>Tên tag tiếng Anh</returns>
        private string ExtractTagName(TagAttributes? tagAttributes)
        {
             if (tagAttributes?.Name == null) return "Không rõ";

            try
            {
                // Ưu tiên lấy tên tiếng Anh
                if (tagAttributes.Name.TryGetValue("en", out var enName) && !string.IsNullOrEmpty(enName))
                {
                    return enName;
                }
                // Nếu không có tiếng Anh, lấy tên đầu tiên tìm thấy
                return tagAttributes.Name.FirstOrDefault().Value ?? "Không rõ";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất tên tag từ TagAttributes.");
            }
            return "Không rõ";
        }

        // Giữ lại phương thức cũ để tương thích ngược
        public List<string> GetMangaTags(Dictionary<string, object> mangaDict)
        {
            var tags = new List<string>();

            try
            {
                // Truy cập trực tiếp vào attributes
                var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                
                // Lấy tags tiếng Anh
                var englishTags = GetTagsFromAttributes(attributesDict);
                
                // Dịch sang tiếng Việt
                tags = TranslateTagsToVietnamese(englishTags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi truy cập attributes manga");
            }

            // Sắp xếp tags theo thứ tự alphabet cho dễ đọc
            return tags.Distinct().OrderBy(t => t).ToList();
        }

        // Giữ lại phương thức cũ để tương thích ngược
        private List<string> GetTagsFromAttributes(Dictionary<string, object> attributesDict)
        {
            var tags = new List<string>();

            try
            {
                // Truy cập trực tiếp vào thuộc tính tags
                if (!attributesDict.ContainsKey("tags") || attributesDict["tags"] == null)
                    return tags;

                var tagsList = (List<object>)attributesDict["tags"];
                
                // Xử lý List<object>
                foreach (var tag in tagsList)
                {
                    if (tag is Dictionary<string, object> tagDict)
                    {
                        var tagName = ExtractTagName(tagDict);
                        if (!string.IsNullOrEmpty(tagName) && tagName != "Không rõ")
                        {
                            tags.Add(tagName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý tags từ attributes");
            }

            return tags;
        }

        // Giữ lại phương thức cũ để tương thích ngược
        private string ExtractTagName(Dictionary<string, object> tagDict)
        {
            try
            {
                // Truy cập trực tiếp vào attributes
                if (!tagDict.ContainsKey("attributes") || tagDict["attributes"] == null)
                    return "Không rõ";

                var tagAttrs = (Dictionary<string, object>)tagDict["attributes"];
                if (!tagAttrs.ContainsKey("name") || tagAttrs["name"] == null)
                    return "Không rõ";

                var nameObj = (Dictionary<string, object>)tagAttrs["name"];
                
                // Chuyển đổi dictionary sang kiểu Dictionary<string, string>
                var tagNameDict = nameObj.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value?.ToString() ?? ""
                );

                // Lấy tên tag tiếng Anh
                if (tagNameDict.TryGetValue("en", out var enName) && !string.IsNullOrEmpty(enName))
                    return enName;

                return tagNameDict.FirstOrDefault().Value ?? "Không rõ";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi trích xuất tên tag");
            }
            return "Không rõ";
        }
    }
}
