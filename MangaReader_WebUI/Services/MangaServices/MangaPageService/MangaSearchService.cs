using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaSearchService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ILogger<MangaSearchService> _logger;
        private readonly IMangaToMangaViewModelMapper _mangaViewModelMapper;

        public MangaSearchService(
            IMangaApiService mangaApiService,
            ILogger<MangaSearchService> logger,
            IMangaToMangaViewModelMapper mangaViewModelMapper)
        {
            _mangaApiService = mangaApiService;
            _logger = logger;
            _mangaViewModelMapper = mangaViewModelMapper;
        }

        /// <summary>
        /// Chuyển đổi tham số tìm kiếm thành đối tượng SortManga
        /// </summary>
        public SortManga CreateSortMangaFromParameters(
            string title = "",
            List<string>? status = null,
            string sortBy = "latest",
            string authors = "",
            string artists = "",
            int? year = null,
            List<string>? availableTranslatedLanguage = null,
            List<string>? publicationDemographic = null,
            List<string>? contentRating = null,
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            List<string>? genres = null,
            string includedTagsStr = "",
            string excludedTagsStr = "")
        {
            var sortManga = new SortManga
            {
                Title = title,
                Status = status ?? new List<string>(),
                SortBy = sortBy ?? "latest",
                Year = year,
                Demographic = publicationDemographic ?? new List<string>(),
                IncludedTagsMode = includedTagsMode ?? "AND",
                ExcludedTagsMode = excludedTagsMode ?? "OR",
                Genres = genres
            };

            // Xử lý danh sách tác giả
            if (!string.IsNullOrEmpty(authors))
            {
                sortManga.Authors = authors.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                _logger.LogInformation($"Tìm kiếm với tác giả: {string.Join(", ", sortManga.Authors)}");
            }

            // Xử lý danh sách họa sĩ
            if (!string.IsNullOrEmpty(artists))
            {
                sortManga.Artists = artists.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                _logger.LogInformation($"Tìm kiếm với họa sĩ: {string.Join(", ", sortManga.Artists)}");
            }

            // Xử lý danh sách ngôn ngữ
            if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
            {
                sortManga.Languages = availableTranslatedLanguage;
                _logger.LogInformation($"Tìm kiếm với ngôn ngữ: {string.Join(", ", sortManga.Languages)}");
            }

            // Xử lý danh sách đánh giá nội dung
            if (contentRating != null && contentRating.Any())
            {
                sortManga.ContentRating = contentRating;
                _logger.LogInformation($"Tìm kiếm với mức độ nội dung người dùng chọn: {string.Join(", ", sortManga.ContentRating)}");
            }
            else
            {
                // Mặc định của trang Search: chỉ "safe" cho cả hai nguồn
                sortManga.ContentRating = new List<string> { "safe" };
                _logger.LogInformation($"Tìm kiếm với mức độ nội dung mặc định (Search Page): safe");
            }

            // Xử lý danh sách includedTags từ chuỗi
            if (!string.IsNullOrEmpty(includedTagsStr))
            {
                sortManga.IncludedTags = includedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                _logger.LogInformation($"Tìm kiếm với includedTags: {string.Join(", ", sortManga.IncludedTags)}");
            }

            // Xử lý danh sách excludedTags từ chuỗi
            if (!string.IsNullOrEmpty(excludedTagsStr))
            {
                sortManga.ExcludedTags = excludedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                _logger.LogInformation($"Tìm kiếm với excludedTags: {string.Join(", ", sortManga.ExcludedTags)}");
            }

            return sortManga;
        }

        /// <summary>
        /// Thực hiện tìm kiếm manga dựa trên các tham số
        /// </summary>
        public async Task<MangaListViewModel> SearchMangaAsync(
            int page,
            int pageSize,
            SortManga sortManga)
        {
            try
            {
                // Xử lý giới hạn 10000 kết quả từ API
                const int MAX_API_RESULTS = 10000;
                int offset = (page - 1) * pageSize;
                int limit = pageSize;

                // Kiểm tra nếu đang truy cập trang cuối cùng gần với giới hạn 10000
                if (offset + limit > MAX_API_RESULTS)
                {
                    // Tính lại limit để không vượt quá 10000 kết quả
                    if (offset < MAX_API_RESULTS)
                    {
                        limit = MAX_API_RESULTS - offset;
                        _logger.LogInformation($"Đã điều chỉnh limit: {limit} cho trang cuối (offset: {offset})");
                    }
                    else
                    {
                        // Trường hợp offset đã vượt quá 10000, không thể lấy kết quả
                        _logger.LogWarning($"Offset {offset} vượt quá giới hạn API 10000 kết quả, không thể lấy dữ liệu");
                        limit = 0;
                    }
                }

                if (limit == 0)
                {
                    return new MangaListViewModel
                    {
                        Mangas = new List<MangaViewModel>(),
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = 10000,
                        MaxPages = (int)Math.Ceiling(MAX_API_RESULTS / (double)pageSize),
                        SortOptions = sortManga
                    };
                }

                var result = await _mangaApiService.FetchMangaAsync(limit: limit, offset: offset, sortManga: sortManga);

                // Lấy tổng số manga từ kết quả API (nếu có)
                int totalCount = result?.Total ?? 0;

                // Kiểm tra nếu không lấy được tổng số, sử dụng số lượng kết quả nhân với tỷ lệ page hiện tại
                if (totalCount <= 0 && result?.Data != null)
                {
                    // Ước tính tổng số manga dựa trên số lượng kết quả và offset
                    int resultCount = result.Data.Count;
                    totalCount = Math.Max(resultCount * 10, (page - 1) * pageSize + resultCount + pageSize);
                    _logger.LogInformation($"Ước tính tổng số manga: {totalCount} dựa trên {resultCount} kết quả");
                }

                // Tính toán số trang tối đa dựa trên giới hạn 10000 kết quả của API
                int maxPages = (int)Math.Ceiling(Math.Min(totalCount, MAX_API_RESULTS) / (double)pageSize);

                // Sử dụng mapper mới
                var mangaViewModels = new List<MangaViewModel>();
                if (result?.Data != null)
                {
                    foreach (var mangaData in result.Data)
                    {
                        if (mangaData != null) // Kiểm tra null cho mangaData
                        {
                            mangaViewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaData));
                        }
                    }
                }

                return new MangaListViewModel
                {
                    Mangas = mangaViewModels,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    MaxPages = maxPages,
                    SortOptions = sortManga
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tải danh sách manga: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Trả về ViewModel rỗng trong trường hợp lỗi
                return new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = sortManga
                };
            }
        }
    }
}
