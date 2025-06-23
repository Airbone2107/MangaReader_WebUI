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
            string title, List<string>? status, string sortBy,
            List<string>? authors, List<string>? artists, int? year,
            List<string>? publicationDemographic, List<string>? contentRating,
            List<string>? availableTranslatedLanguage,
            string includedTagsMode, string excludedTagsMode,
            string includedTagsStr, string excludedTagsStr)
        {
            return new SortManga
            {
                Title = title ?? string.Empty,
                Status = status,
                SortBy = sortBy ?? "updatedAt",
                Year = year,
                PublicationDemographic = publicationDemographic,
                ContentRating = contentRating,
                AvailableTranslatedLanguage = availableTranslatedLanguage,
                IncludedTagsMode = includedTagsMode ?? "AND",
                ExcludedTagsMode = excludedTagsMode ?? "OR",
                IncludedTagsStr = includedTagsStr ?? string.Empty,
                ExcludedTagsStr = excludedTagsStr ?? string.Empty,
                Authors = authors,
                Artists = artists,
                Ascending = sortBy == "title"
            };
        }

        /// <summary>
        /// Thực hiện tìm kiếm manga dựa trên các tham số
        /// </summary>
        public async Task<MangaListViewModel> SearchMangaAsync(int page, int pageSize, SortManga sortManga)
        {
            try
            {
                int offset = (page - 1) * pageSize;

                var result = await _mangaClient.GetMangasAsync(
                    offset: offset,
                    limit: pageSize,
                    titleFilter: sortManga.Title,
                    statusFilter: sortManga.GetFirstStatus(),
                    contentRatingFilter: sortManga.GetFirstContentRating(),
                    publicationDemographicsFilter: sortManga.GetPublicationDemographics(),
                    originalLanguageFilter: sortManga.GetFirstOriginalLanguage(),
                    yearFilter: sortManga.Year,
                    authors: sortManga.GetAuthorGuids(),
                    artists: sortManga.GetArtistGuids(),
                    availableTranslatedLanguage: sortManga.AvailableTranslatedLanguage,
                    includedTags: sortManga.GetIncludedTags(),
                    includedTagsMode: sortManga.IncludedTagsMode,
                    excludedTags: sortManga.GetExcludedTags(),
                    excludedTagsMode: sortManga.ExcludedTagsMode,
                    orderBy: sortManga.SortBy,
                    ascending: sortManga.Ascending,
                    includes: new List<string> { "cover_art", "author", "artist" }
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
