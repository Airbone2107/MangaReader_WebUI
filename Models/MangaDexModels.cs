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
        public string Status { get; set; }
        public string Safety { get; set; }
        public string Demographic { get; set; }
        public List<string> IncludedTags { get; set; } = new List<string>();
        public List<string> ExcludedTags { get; set; } = new List<string>();
        public List<string> Languages { get; set; } = new List<string>();
        public string SortBy { get; set; }
        public string TimeFrame { get; set; }
        public List<string> Genres { get; set; } = new List<string>();

        public SortManga()
        {
            // Giá trị mặc định
            Title = "";
            Status = "Tất cả";
            Safety = "Tất cả";
            Demographic = "Tất cả";
            SortBy = "Mới cập nhật";
        }

        // Chuyển đổi thành Dictionary để tạo query parameters
        public Dictionary<string, object> ToParams()
        {
            var parameters = new Dictionary<string, object>();

            // Chỉ thêm tham số nếu có dữ liệu
            if (!string.IsNullOrEmpty(Title))
                parameters["title"] = Title;

            if (Status != "Tất cả")
                parameters["status[]"] = new[] { Status.ToLower() };

            if (Safety != "Tất cả")
                parameters["contentRating[]"] = new[] { Safety.ToLower() };

            if (Demographic != "Tất cả")
                parameters["publicationDemographic[]"] = new[] { Demographic.ToLower() };

            if (IncludedTags.Count > 0)
                parameters["includedTags[]"] = IncludedTags;

            if (ExcludedTags.Count > 0)
                parameters["excludedTags[]"] = ExcludedTags;

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

            // Sắp xếp theo cập nhật mới nhất hoặc cũ nhất
            parameters["order[updatedAt]"] = SortBy == "Mới cập nhật" ? "desc" : "asc";

            // Sắp xếp theo lượt xem nếu cần
            if (SortBy == "Lượt xem")
            {
                parameters["order[followedCount]"] = "desc";
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
        public SortManga SortOptions { get; set; } = new SortManga();
    }

    public class ChapterReadViewModel
    {
        public string MangaId { get; set; }
        public string MangaTitle { get; set; }
        public string ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public string ChapterNumber { get; set; }
        public List<string> Pages { get; set; } = new List<string>();
        public string PrevChapterId { get; set; }
        public string NextChapterId { get; set; }
    }
} 