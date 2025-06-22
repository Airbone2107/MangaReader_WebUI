using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReaderLib.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaSearchService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaSearchService> _logger;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;

        public MangaSearchService(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<MangaSearchService> logger,
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper)
        {
            _mangaClient = mangaClient;
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
                // Note: MangaReaderLib API uses authorIdsFilter for both authors and artists.
                // We merge them here for simplicity. The API should handle filtering by role.
                sortManga.Authors.AddRange(artists.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)));
                sortManga.Authors = sortManga.Authors.Distinct().ToList();
                _logger.LogInformation($"Tìm kiếm với họa sĩ: {string.Join(", ", sortManga.Authors)}");
            }

            // Xử lý danh sách ngôn ngữ
            if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
            {
                sortManga.OriginalLanguage = availableTranslatedLanguage;
                _logger.LogInformation($"Tìm kiếm với ngôn ngữ: {string.Join(", ", sortManga.OriginalLanguage)}");
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
                int offset = (page - 1) * pageSize;

                List<PublicationDemographic>? demographics = null;
                if (sortManga.Demographic != null && sortManga.Demographic.Any())
                {
                    demographics = sortManga.Demographic
                        .Select(d => Enum.TryParse<PublicationDemographic>(d, true, out var demo) ? (PublicationDemographic?)demo : null)
                        .Where(d => d.HasValue)
                        .Select(d => d.Value)
                        .ToList();
                }

                var result = await _mangaClient.GetMangasAsync(
                    offset: offset,
                    limit: pageSize,
                    titleFilter: sortManga.Title,
                    statusFilter: sortManga.Status?.FirstOrDefault(),
                    contentRatingFilter: sortManga.ContentRating?.FirstOrDefault(),
                    publicationDemographicsFilter: demographics,
                    originalLanguageFilter: sortManga.OriginalLanguage?.FirstOrDefault(),
                    yearFilter: sortManga.Year,
                    authorIdsFilter: sortManga.Authors?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                    includedTags: sortManga.IncludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                    includedTagsMode: sortManga.IncludedTagsMode,
                    excludedTags: sortManga.ExcludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                    excludedTagsMode: sortManga.ExcludedTagsMode,
                    orderBy: sortManga.SortBy,
                    ascending: sortManga.SortBy == "title",
                    includes: new List<string> { "cover_art", "author", "tag" }
                );

                int totalCount = result?.Total ?? 0;
                int maxPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var mangaViewModels = new List<MangaViewModel>();
                if (result?.Data != null)
                {
                    foreach (var mangaDto in result.Data)
                    {
                        if (mangaDto != null)
                        {
                            mangaViewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaDto));
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
                _logger.LogError(ex, "Lỗi khi tải danh sách manga.");
                return new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = sortManga
                };
            }
        }
    }
}
