using MangaReader.WebUI.Models;
using MangaReader.WebUI.Services.MangaServices.MangaInformation;
using MangaReader.WebUI.Services.UtilityServices;
using MangaReader.WebUI.Services.APIServices;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaSearchService
    {
        private readonly IMangaApiService _mangaApiService;
        private readonly ICoverApiService _coverApiService;
        private readonly ILogger<MangaSearchService> _logger;
        private readonly LocalizationService _localizationService;
        private readonly JsonConversionService _jsonConversionService;
        private readonly MangaTitleService _mangaTitleService;
        private readonly MangaTagService _mangaTagService;
        private readonly MangaDescription _mangaDescriptionService;
        private readonly MangaRelationshipService _mangaRelationshipService;

        public MangaSearchService(
            IMangaApiService mangaApiService,
            ICoverApiService coverApiService,
            ILogger<MangaSearchService> logger,
            LocalizationService localizationService,
            JsonConversionService jsonConversionService,
            MangaTitleService mangaTitleService,
            MangaTagService mangaTagService,
            MangaDescription mangaDescriptionService,
            MangaRelationshipService mangaRelationshipService)
        {
            _mangaApiService = mangaApiService;
            _coverApiService = coverApiService;
            _logger = logger;
            _localizationService = localizationService;
            _jsonConversionService = jsonConversionService;
            _mangaTitleService = mangaTitleService;
            _mangaTagService = mangaTagService;
            _mangaDescriptionService = mangaDescriptionService;
            _mangaRelationshipService = mangaRelationshipService;
        }

        /// <summary>
        /// Chuyển đổi tham số tìm kiếm thành đối tượng SortManga
        /// </summary>
        public SortManga CreateSortMangaFromParameters(
            string title = "",
            List<string> status = null,
            string sortBy = "latest",
            string authors = "",
            string artists = "",
            int? year = null,
            List<string> availableTranslatedLanguage = null,
            List<string> publicationDemographic = null,
            List<string> contentRating = null,
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            List<string> genres = null,
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
                _logger.LogInformation($"Tìm kiếm với mức độ nội dung: {string.Join(", ", sortManga.ContentRating)}");
            }
            else
            {
                // Mặc định: nội dung an toàn
                sortManga.ContentRating = new List<string> { "safe", "suggestive", "erotica" };
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
                        MaxPages = 0,
                        SortOptions = sortManga
                    };
                }

                var result = await _mangaApiService.FetchMangaAsync(limit: limit, offset: offset, sortManga: sortManga);

                // Lấy tổng số manga từ kết quả API (nếu có)
                int totalCount = 0;

                // Kiểm tra nếu kết quả trả về là JsonElement (có thể chứa thông tin totalCount)
                if (result != null && result.Count > 0)
                {
                    try
                    {
                        // Thử lấy totalCount từ response metadata
                        var firstItem = result[0];
                        if (firstItem is JsonElement element)
                        {
                            // Kiểm tra xem có property total không
                            if (element.TryGetProperty("total", out JsonElement totalElement))
                            {
                                totalCount = totalElement.GetInt32();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi lấy tổng số manga: {ex.Message}");
                    }
                }

                // Nếu không lấy được tổng số, sử dụng số lượng kết quả nhân với tỷ lệ page hiện tại
                if (totalCount <= 0)
                {
                    // Ước tính tổng số manga dựa trên số lượng kết quả và offset
                    totalCount = Math.Max(result.Count * 10, (page - 1) * pageSize + result.Count + pageSize);
                }

                // Tính toán số trang tối đa dựa trên giới hạn 10000 kết quả của API
                int maxPages = (int)Math.Ceiling(Math.Min(totalCount, MAX_API_RESULTS) / (double)pageSize);

                var mangaViewModels = await ConvertToMangaViewModelsAsync(result);

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

        /// <summary>
        /// Chuyển đổi kết quả từ API sang danh sách MangaViewModel
        /// </summary>
        private async Task<List<MangaViewModel>> ConvertToMangaViewModelsAsync(List<object> result)
        {
            var mangaViewModels = new List<MangaViewModel>();

            foreach (var manga in result)
            {
                try
                {
                    // Sử dụng JsonSerializer để chuyển đổi chính xác
                    var mangaElement = JsonSerializer.Deserialize<JsonElement>(manga.ToString());
                    // Chuyển đổi JsonElement thành Dictionary
                    var mangaDict = _jsonConversionService.ConvertJsonElementToDict(mangaElement);

                    if (mangaDict.ContainsKey("total"))
                    {
                        continue;
                    }

                    if (!mangaDict.ContainsKey("id") || mangaDict["id"] == null)
                    {
                        _logger.LogWarning("Manga không có ID");
                        continue;
                    }

                    string id = mangaDict["id"].ToString();
                    var attributesDict = (Dictionary<string, object>)mangaDict["attributes"];
                    
                    // Lấy title từ MangaTitleService
                    string title = _mangaTitleService.GetMangaTitle(attributesDict["title"], attributesDict["altTitles"]);
                    
                    // Lấy author/artist từ MangaRelationshipService
                    var (author, artist) = _mangaRelationshipService.GetAuthorArtist(mangaDict);
                    
                    // Lấy description từ MangaDescriptionService
                    string description = _mangaDescriptionService.GetDescription(attributesDict);
                    
                    // Lấy status từ LocalizationService
                    string status = _localizationService.GetStatus(attributesDict);
                    
                    // Xử lý DateTime?
                    DateTime? lastUpdated = null;
                    if (attributesDict.ContainsKey("updatedAt") && attributesDict["updatedAt"] != null)
                    {
                        if (DateTime.TryParse(attributesDict["updatedAt"].ToString(), out DateTime updatedAt))
                        {
                            lastUpdated = updatedAt;
                        }
                        else
                        {
                            _logger.LogWarning($"Không thể parse ngày cập nhật '{attributesDict["updatedAt"]}' cho manga {id}");
                        }
                    }

                    string coverUrl = "";

                    // Tải cover art
                    try
                    {
                        coverUrl = await _coverApiService.FetchCoverUrlAsync(id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi tải cover cho manga ID {id}: {ex.Message}");
                        coverUrl = "/images/no-cover.png"; // Sử dụng ảnh mặc định nếu không lấy được
                    }

                    var viewModel = new MangaViewModel
                    {
                        Id = id,
                        Title = title,
                        Author = author,
                        Artist = artist,
                        Description = description,
                        CoverUrl = coverUrl,
                        Status = status,
                        LastUpdated = lastUpdated
                    };

                    mangaViewModels.Add(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi khi chuyển đổi manga: {ex.Message}");
                    continue;
                }
            }

            return mangaViewModels;
        }
    }
}
