namespace MangaReader.WebUI.Models
{
    /// <summary>
    /// Model đại diện cho một chương của manga.
    /// Được sử dụng trong các service liên quan đến việc quản lý và hiển thị thông tin chương.
    /// Sử dụng chủ yếu trong ChapterService và MangaDexService để xử lý dữ liệu chương từ API MangaDex.
    /// </summary>
    public class Chapter    
    {
        public string MangaId { get; set; }
        public string ChapterId { get; set; }
        public string ChapterName { get; set; }
        public List<object> ChapterList { get; set; } = new List<object>();
    }

    /// <summary>
    /// Model quản lý các tùy chọn lọc và sắp xếp khi tìm kiếm manga.
    /// Được sử dụng trong MangaDexService để xây dựng các tham số truy vấn khi gọi API MangaDex.
    /// Sử dụng trong MangaController để xử lý các yêu cầu tìm kiếm và lọc manga từ người dùng.
    /// </summary>
    public class SortManga
    {
        public string Title { get; set; }
        public List<string> Status { get; set; } = new List<string>();
        public string Safety { get; set; }
        public List<string> Demographic { get; set; } = new List<string>();
        public List<string> IncludedTags { get; set; } = new List<string>();
        public List<string> ExcludedTags { get; set; } = new List<string>();
        public List<string> Languages { get; set; } = new List<string>();
        public string SortBy { get; set; }
        public string TimeFrame { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public string IncludedTagsMode { get; set; } = "AND";
        public string ExcludedTagsMode { get; set; } = "OR";
        public List<string> Authors { get; set; } = new List<string>();
        public List<string> Artists { get; set; } = new List<string>();
        public int? Year { get; set; }
        public List<string> ContentRating { get; set; } = new List<string>();
        public List<string> OriginalLanguage { get; set; } = new List<string>();
        public List<string> ExcludedOriginalLanguage { get; set; } = new List<string>();
        public string CreatedAtSince { get; set; }
        public string UpdatedAtSince { get; set; }
        public bool? HasAvailableChapters { get; set; }
        public string Group { get; set; }

        public SortManga()
        {
            // Giá trị mặc định
            Title = "";
            Status = new List<string>();
            Safety = "";
            Demographic = new List<string>();
            SortBy = "latest";
            IncludedTags = new List<string>();
            ExcludedTags = new List<string>();
            Languages = new List<string>();
            Genres = new List<string>();
            ContentRating = new List<string>() { "safe", "suggestive", "erotica"};
            Authors = new List<string>();
            Artists = new List<string>();
            OriginalLanguage = new List<string>();
            ExcludedOriginalLanguage = new List<string>();
        }

        /// <summary>
        /// Chuyển đổi các thuộc tính của model thành Dictionary để tạo query parameters cho API MangaDex.
        /// Được sử dụng trong MangaDexService khi gọi API để tìm kiếm và lọc manga.
        /// </summary>
        public Dictionary<string, object> ToParams()
        {
            var parameters = new Dictionary<string, object>();

            // Chỉ thêm tham số nếu có dữ liệu
            if (!string.IsNullOrEmpty(Title))
                parameters["title"] = Title;
                
            // Danh sách tác giả
            if (Authors != null && Authors.Count > 0)
                parameters["authors[]"] = Authors;
                
            // Danh sách họa sĩ
            if (Artists != null && Artists.Count > 0)
                parameters["artists[]"] = Artists;
                
            // Năm phát hành
            if (Year.HasValue && Year > 0)
                parameters["year"] = Year.Value;

            // Tham số trạng thái - đảm bảo dùng status[] 
            if (Status != null && Status.Count > 0)
                parameters["status[]"] = Status;

            // Xử lý tham số includedTags[] - đúng định dạng API yêu cầu
            if (IncludedTags != null && IncludedTags.Count > 0)
            {
                parameters["includedTags[]"] = IncludedTags;
                parameters["includedTagsMode"] = IncludedTagsMode;
            }

            // Xử lý tham số excludedTags[] - đúng định dạng API yêu cầu
            if (ExcludedTags != null && ExcludedTags.Count > 0)
            {
                parameters["excludedTags[]"] = ExcludedTags;
                parameters["excludedTagsMode"] = ExcludedTagsMode;
            }

            // Chỉ thêm tham số ngôn ngữ nếu danh sách không rỗng
            if (Languages != null && Languages.Count > 0)
            {
                // Kiểm tra mã ngôn ngữ hợp lệ
                var validLanguages = Languages.Where(lang => 
                    !string.IsNullOrWhiteSpace(lang) && 
                    System.Text.RegularExpressions.Regex.IsMatch(lang.Trim(), @"^[a-z]{2}(-[a-z]{2})?$")
                ).ToList();
                
                if (validLanguages.Count > 0)
                    parameters["availableTranslatedLanguage[]"] = validLanguages;
            }
            
            // Ngôn ngữ gốc
            if (OriginalLanguage != null && OriginalLanguage.Count > 0)
                parameters["originalLanguage[]"] = OriginalLanguage;
                
            // Loại trừ ngôn ngữ gốc
            if (ExcludedOriginalLanguage != null && ExcludedOriginalLanguage.Count > 0)
                parameters["excludedOriginalLanguage[]"] = ExcludedOriginalLanguage;
                
            // Thời gian tạo và cập nhật
            if (!string.IsNullOrEmpty(CreatedAtSince))
                parameters["createdAtSince"] = CreatedAtSince;
                
            if (!string.IsNullOrEmpty(UpdatedAtSince))
                parameters["updatedAtSince"] = UpdatedAtSince;
                
            // Manga có chương sẵn có
            if (HasAvailableChapters.HasValue)
                parameters["hasAvailableChapters"] = HasAvailableChapters.Value ? "true" : "false";
                
            // Nhóm
            if (!string.IsNullOrEmpty(Group))
                parameters["group"] = Group;

            // Chuyển đổi các giá trị sortBy sang các tham số order tương ứng
            if (SortBy == "latest")
            {
                parameters["order[updatedAt]"] = "desc";
            }
            else if (SortBy == "title")
            {
                parameters["order[title]"] = "asc";
            }
            else if (SortBy == "popular")
            {
                parameters["order[followedCount]"] = "desc";
            }
            else if (SortBy == "relevance")
            {
                parameters["order[relevance]"] = "desc";
            }
            else if (SortBy == "rating")
            {
                parameters["order[rating]"] = "desc";
            }
            else if (SortBy == "createdAt")
            {
                parameters["order[createdAt]"] = "desc";
            }
            else if (SortBy == "year")
            {
                parameters["order[year]"] = "desc";
            }
            else
            {
                // Mặc định sắp xếp theo chương mới nhất nếu không có giá trị sortBy hợp lệ
                parameters["order[latestUploadedChapter]"] = "desc";
            }

            // Thêm tham số contentRating[] để lọc nội dung an toàn
            if (ContentRating != null && ContentRating.Count > 0)
            {
                parameters["contentRating[]"] = ContentRating;
            }
            else if (string.IsNullOrEmpty(Safety) || Safety == "Tất cả")
            {
                parameters["contentRating[]"] = new[] { "safe", "suggestive" };
            }
            else
            {
                parameters["contentRating[]"] = new[] { Safety.ToLower() };
            }

            // Thêm tham số demographic nếu có chỉ định
            if (Demographic != null && Demographic.Count > 0)
            {
                parameters["publicationDemographic[]"] = Demographic;
            }
            else if (!string.IsNullOrEmpty(Safety) && Safety != "Tất cả")
            {
                parameters["publicationDemographic[]"] = new[] { Safety.ToLower() };
            }

            return parameters;
        }
    }

    /// <summary>
    /// Model đại diện cho dữ liệu manga được hiển thị trong giao diện người dùng.
    /// Được sử dụng trong MangaController để hiển thị thông tin manga cho người dùng.
    /// Sử dụng trong các view liên quan đến hiển thị thông tin manga như trang danh sách, trang chi tiết.
    /// </summary>
    public class MangaViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverUrl { get; set; }
        public string Status { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Author { get; set; }
        public string Artist { get; set; }
        public string OriginalLanguage { get; set; }
        public string PublicationDemographic { get; set; }
        public string ContentRating { get; set; }
        public string AlternativeTitles { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsFollowing { get; set; }
        public string LatestChapter { get; set; }
        public double Rating { get; set; }
        public int Views { get; set; }
    }

    /// <summary>
    /// Model đại diện cho dữ liệu chương được hiển thị trong giao diện người dùng.
    /// Được sử dụng trong ChapterService để hiển thị thông tin chương cho người dùng.
    /// Sử dụng trong các view liên quan đến hiển thị danh sách chương và thông tin chương.
    /// </summary>
    public class ChapterViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Number { get; set; }
        public string Language { get; set; }
        public DateTime PublishedAt { get; set; }
        public List<ChapterRelationship> Relationships { get; set; } = new List<ChapterRelationship>();
    }

    /// <summary>
    /// Model đại diện cho mối quan hệ giữa một chương và các thực thể khác trong hệ thống MangaDex.
    /// Được sử dụng trong ChapterViewModel để lưu trữ các mối quan hệ của chương.
    /// Sử dụng trong ChapterService để xử lý các mối quan hệ giữa chương và manga, scanlation group, v.v.
    /// </summary>
    public class ChapterRelationship
    {
        public string Id { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Model kết hợp thông tin chi tiết về manga và danh sách các chương của nó.
    /// Được sử dụng trong MangaController để hiển thị trang chi tiết manga.
    /// Sử dụng trong view Details.cshtml để hiển thị thông tin đầy đủ về manga và danh sách các chương có sẵn.
    /// </summary>
    public class MangaDetailViewModel
    {
        public MangaViewModel Manga { get; set; }
        public List<ChapterViewModel> Chapters { get; set; } = new List<ChapterViewModel>();
        public Dictionary<string, List<string>> AlternativeTitlesByLanguage { get; set; } = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Model đại diện cho danh sách manga được hiển thị trong giao diện người dùng.
    /// Được sử dụng trong MangaController để hiển thị danh sách manga cho người dùng.
    /// Sử dụng trong view Index.cshtml để hiển thị danh sách manga với phân trang và các tùy chọn sắp xếp.
    /// </summary>
    public class MangaListViewModel
    {
        public List<MangaViewModel> Mangas { get; set; } = new List<MangaViewModel>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int MaxPages { get; set; } // Số trang tối đa có thể truy cập dựa trên giới hạn API
        public SortManga SortOptions { get; set; } = new SortManga();
    }

    /// <summary>
    /// Model đại diện cho dữ liệu cần thiết để hiển thị một chương để đọc.
    /// Được sử dụng trong ChapterController để hiển thị trang đọc chương cho người dùng.
    /// Sử dụng trong view Read.cshtml để hiển thị nội dung chương và điều hướng giữa các chương.
    /// </summary>
    public class ChapterReadViewModel
    {
        public string MangaId { get; set; }
        public string MangaTitle { get; set; }
        public string ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public string ChapterNumber { get; set; }
        public string ChapterLanguage { get; set; }
        public List<string> Pages { get; set; } = new List<string>();
        public string PrevChapterId { get; set; }
        public string NextChapterId { get; set; }
        public List<ChapterViewModel> SiblingChapters { get; set; } = new List<ChapterViewModel>();
    }
} 