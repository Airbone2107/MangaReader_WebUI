using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;

namespace manga_reader_web.Models
{
    // Các model cho dữ liệu người dùng
    public class User
    {
        public string Id { get; set; }
        public string GoogleId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PhotoURL { get; set; }
        public List<string> Following { get; set; } = new List<string>();
        public List<ReadingProgress> ReadingProgress { get; set; } = new List<ReadingProgress>();
        public DateTime CreatedAt { get; set; }
    }

    public class ReadingProgress
    {
        public string MangaId { get; set; }
        public string LastChapter { get; set; }
        public DateTime LastReadAt { get; set; }
    }

    // Các model cho dữ liệu Manga
    public class Chapter
    {
        public string MangaId { get; set; }
        public string ChapterId { get; set; }
        public string ChapterName { get; set; }
        public List<object> ChapterList { get; set; } = new List<object>();
    }

    // Model quản lý lọc và sắp xếp Manga
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

        // Chuyển đổi thành Dictionary để tạo query parameters
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

    // Các model cho việc hiển thị Manga
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
        public string Publisher { get; set; }
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

    public class ChapterViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Number { get; set; }
        public string Language { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public class MangaDetailViewModel
    {
        public MangaViewModel Manga { get; set; }
        public List<ChapterViewModel> Chapters { get; set; } = new List<ChapterViewModel>();
    }

    public class MangaListViewModel
    {
        public List<MangaViewModel> Mangas { get; set; } = new List<MangaViewModel>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int MaxPages { get; set; } // Số trang tối đa có thể truy cập dựa trên giới hạn API
        public SortManga SortOptions { get; set; } = new SortManga();
    }

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
    }
} 