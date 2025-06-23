Chào bạn, tôi đã phân tích yêu cầu của bạn và các tài liệu tham khảo được cung cấp. Dưới đây là tệp `TODO.md` chi tiết để hướng dẫn bạn thực hiện các thay đổi cần thiết cho `MangaReader_WebUI`.

```markdown
<!-- // TODO.md -->
# TODO: Cập nhật Frontend `MangaReader_WebUI` để phù hợp với API mới

Tài liệu này hướng dẫn chi tiết các bước cần thực hiện để refactor lại dự án `MangaReader_WebUI` nhằm tương thích với các thay đổi từ backend, cụ thể là mở rộng chức năng tìm kiếm và hiển thị chi tiết manga.

## Mục lục

1.  [Cập nhật Model](#bước-1-cập-nhật-model)
2.  [Cập nhật Service Layer](#bước-2-cập-nhật-service-layer)
3.  [Cập nhật Controller](#bước-3-cập-nhật-controller)
4.  [Cập nhật View](#bước-4-cập-nhật-view)

---

## Bước 1: Cập nhật Model

Chúng ta sẽ bắt đầu bằng việc cập nhật các model để có thể chứa các tham số tìm kiếm mới và dữ liệu mới trả về từ API.

### 1.1: Cập nhật `SortManga.cs`

Thêm các thuộc tính mới để hỗ trợ tìm kiếm theo `Artists` và `AvailableTranslatedLanguage`.

<!-- MangaReader_WebUI\Models\SortManga.cs -->
```csharp
using MangaReaderLib.Enums;

namespace MangaReader.WebUI.Models
{
    public class SortManga
    {
        public string Title { get; set; } = string.Empty;
        public List<string>? Status { get; set; }
        public List<string>? ContentRating { get; set; }
        public List<string>? PublicationDemographic { get; set; }
        public List<string>? OriginalLanguage { get; set; }
        public int? Year { get; set; }

        public List<string>? Authors { get; set; } // Tìm theo ID tác giả
        public List<string>? Artists { get; set; } // Tìm theo ID họa sĩ
        public List<string>? AvailableTranslatedLanguage { get; set; } // Tìm theo ngôn ngữ có sẵn

        public string IncludedTagsStr { get; set; } = string.Empty;
        public string ExcludedTagsStr { get; set; } = string.Empty;
        public string IncludedTagsMode { get; set; } = "AND";
        public string ExcludedTagsMode { get; set; } = "OR";
        
        public string SortBy { get; set; } = "updatedAt";
        public bool Ascending { get; set; } = false;

        // Dưới đây là các phương thức helper để lấy giá trị đã được parse
        public List<Guid>? GetIncludedTags() => 
            IncludedTagsStr?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(tagId => Guid.TryParse(tagId, out var guid) ? guid : (Guid?)null)
                            .Where(guid => guid.HasValue)
                            .Select(guid => guid!.Value)
                            .ToList();

        public List<Guid>? GetExcludedTags() =>
            ExcludedTagsStr?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .Select(tagId => Guid.TryParse(tagId, out var guid) ? guid : (Guid?)null)
                           .Where(guid => guid.HasValue)
                           .Select(guid => guid!.Value)
                           .ToList();

        public List<Guid>? GetAuthorGuids() =>
            Authors?.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();

        public List<Guid>? GetArtistGuids() =>
            Artists?.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();

        public List<PublicationDemographic>? GetPublicationDemographics() =>
            PublicationDemographic?.Select(d => Enum.TryParse<PublicationDemographic>(d, true, out var demo) ? (PublicationDemographic?)demo : null)
                                .Where(d => d.HasValue)
                                .Select(d => d.Value)
                                .ToList();
        
        public string? GetFirstStatus() => Status?.FirstOrDefault();
        
        public string? GetFirstContentRating() => ContentRating?.FirstOrDefault();
        
        public string? GetFirstOriginalLanguage() => OriginalLanguage?.FirstOrDefault();

        public SortManga()
        {
            // Giá trị mặc định đã được thiết lập ở trên
        }
    }
} 
```

### 1.2: Cập nhật `MangaViewModel.cs`

Thêm thuộc tính `AvailableTranslatedLanguages` để lưu trữ danh sách các ngôn ngữ có sẵn mà trang chi tiết sẽ sử dụng.

<!-- MangaReader_WebUI\Models\ViewModels\Manga\MangaViewModel.cs -->
```csharp
using System;
using System.Collections.Generic;

namespace MangaReader.WebUI.Models.ViewModels.Manga
{
    public class MangaViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Author { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string OriginalLanguage { get; set; } = string.Empty;
        public List<string> AvailableTranslatedLanguages { get; set; } = new List<string>(); // THÊM DÒNG NÀY
        public string PublicationDemographic { get; set; } = string.Empty;
        public string ContentRating { get; set; } = string.Empty;
        public string AlternativeTitles { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
        public bool IsFollowing { get; set; }
        public string LatestChapter { get; set; } = string.Empty;
    }
} 
```

---

## Bước 2: Cập nhật Service Layer

Cập nhật các service để xử lý logic tìm kiếm mới và lấy dữ liệu chi tiết manga.

### 2.1: Cập nhật Mapper `MangaReaderLibToMangaViewModelMapper`

Ánh xạ thuộc tính `availableTranslatedLanguages` từ DTO của `MangaReaderLib` sang `MangaViewModel`.

<!-- MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToMangaViewModelMapper.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.CoverArts;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using MangaReaderLib.Extensions;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaViewModelMapper : IMangaReaderLibToMangaViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToMangaViewModelMapper> _logger;
        private readonly IMangaReaderLibCoverApiService _coverApiService;
        private readonly IUserService _userService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly LocalizationService _localizationService;

        public MangaReaderLibToMangaViewModelMapper(
            ILogger<MangaReaderLibToMangaViewModelMapper> logger,
            IMangaReaderLibCoverApiService coverApiService,
            IUserService userService,
            IMangaFollowService mangaFollowService,
            LocalizationService localizationService)
        {
            _logger = logger;
            _coverApiService = coverApiService;
            _userService = userService;
            _mangaFollowService = mangaFollowService;
            _localizationService = localizationService;
        }

        public async Task<MangaViewModel> MapToMangaViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, bool isFollowing = false)
        {
            Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaViewModel.");
            Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaViewModel.");

            string id = mangaData.Id;
            var attributes = mangaData.Attributes!;
            var relationships = mangaData.Relationships;

            try
            {
                string title = attributes.Title;
                string description = "";
                string coverUrl = "/images/cover-placeholder.jpg"; // Giá trị mặc định
                string author = "Không rõ";
                string artist = "Không rõ";

                var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
                if (coverRelationship != null)
                {
                    var coverAttributes = coverRelationship.GetAttributesAs<CoverArtAttributesDto>();
                    if (coverAttributes != null && !string.IsNullOrEmpty(coverAttributes.PublicId))
                    {
                        // SỬ DỤNG SERVICE ĐỂ LẤY URL - ĐIỂM THAY ĐỔI CHÍNH
                        coverUrl = _coverApiService.GetCoverArtUrl(coverRelationship.Id, coverAttributes.PublicId);
                        _logger.LogDebug("MangaReaderLib Mapper: Cover URL set to {CoverUrl} using IMangaReaderLibCoverApiService.", coverUrl);
                    }
                    else
                    {
                        _logger.LogWarning("MangaReaderLib Mapper: No PublicId found in cover_art attributes for manga {MangaId}. Using placeholder.", id);
                    }
                }
                else
                {
                    _logger.LogWarning("MangaReaderLib Mapper: No cover_art relationship found for manga {MangaId}. Using placeholder.", id);
                }

                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel.Attributes != null)
                        {
                            try
                            {
                                var authorAttributes = rel.GetAttributesAs<AuthorAttributesDto>();
                                if (authorAttributes != null && !string.IsNullOrEmpty(authorAttributes.Name))
                                {
                                    if (rel.Type == "author") author = authorAttributes.Name;
                                    else if (rel.Type == "artist") artist = authorAttributes.Name;
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "MangaReaderLib Mapper: Failed to deserialize relationship attributes for manga {MangaId}, relationship type {RelType}.", id, rel.Type);
                            }
                        }
                    }
                }
                _logger.LogDebug("MangaReaderLib Mapper: Author: {Author}, Artist: {Artist} for manga {MangaId}", author, artist, id);

                string status = _localizationService.GetStatus(attributes.Status.ToString());
                
                List<string> tags = new List<string>();
                if (attributes.Tags != null && attributes.Tags.Any())
                {
                    tags = attributes.Tags
                        .Where(t => t.Attributes != null && !string.IsNullOrEmpty(t.Attributes.Name))
                        .Select(t => t.Attributes.Name)
                        .Distinct()
                        .OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), false))
                        .ToList();
                }
                _logger.LogDebug("MangaReaderLib Mapper: Tags: [{Tags}] for manga {MangaId}", string.Join(", ", tags), id);

                if (_userService.IsAuthenticated())
                {
                    try
                    {
                        isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                    }
                    catch (Exception followEx)
                    {
                        _logger.LogError(followEx, "Lỗi khi kiểm tra trạng thái theo dõi cho manga {MangaId} trong MangaReaderLib Mapper.", id);
                    }
                }

                return new MangaViewModel
                {
                    Id = id,
                    Title = title,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = status,
                    Tags = tags,
                    Author = author,
                    Artist = artist,
                    OriginalLanguage = attributes.OriginalLanguage ?? "",
                    AvailableTranslatedLanguages = attributes.AvailableTranslatedLanguages ?? new List<string>(), // CẬP NHẬT
                    PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "",
                    ContentRating = attributes.ContentRating.ToString() ?? "",
                    AlternativeTitles = "",
                    LastUpdated = attributes.UpdatedAt,
                    IsFollowing = isFollowing,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi mapping MangaReaderLib MangaData thành MangaViewModel cho ID: {MangaId}", id);
                return new MangaViewModel
                {
                    Id = id,
                    Title = $"Lỗi tải ({id})",
                    Description = "Đã xảy ra lỗi khi xử lý dữ liệu.",
                    CoverUrl = "/images/cover-placeholder.jpg",
                    Status = "Lỗi",
                    Tags = new List<string>(),
                    Author = "Lỗi",
                    Artist = "Lỗi"
                };
            }
        }
    }
} 
```

### 2.2: Cập nhật `MangaSearchService`

Chỉnh sửa phương thức `CreateSortMangaFromParameters` và `SearchMangaAsync` để sử dụng các tham số mới.

<!-- MangaReader_WebUI\Services\MangaServices\MangaPageService\MangaSearchService.cs -->
```csharp
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
```

### 2.3: Cập nhật `ChapterService`

Sửa đổi `GetChaptersAsync` để có thể lấy chapter của tất cả các ngôn ngữ có sẵn, không còn hardcode "vi,en".

<!-- MangaReader_WebUI\Services\MangaServices\ChapterServices\ChapterService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using System.Globalization;

namespace MangaReader.WebUI.Services.MangaServices.ChapterServices
{
    public class ChapterService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly ILogger<ChapterService> _logger;
        private readonly IMangaReaderLibToChapterViewModelMapper _chapterViewModelMapper;
        private readonly IMangaReaderLibToSimpleChapterInfoMapper _simpleChapterInfoMapper;

        public ChapterService(
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibChapterClient chapterClient,
            ILogger<ChapterService> logger,
            IMangaReaderLibToChapterViewModelMapper chapterViewModelMapper,
            IMangaReaderLibToSimpleChapterInfoMapper simpleChapterInfoMapper)
        {
            _mangaClient = mangaClient;
            _chapterClient = chapterClient;
            _logger = logger;
            _chapterViewModelMapper = chapterViewModelMapper;
            _simpleChapterInfoMapper = simpleChapterInfoMapper;
        }
        
        public async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, List<string>? languages = null)
        {
            try
            {
                if (!Guid.TryParse(mangaId, out var mangaGuid))
                {
                    _logger.LogError("MangaId không hợp lệ: {MangaId}", mangaId);
                    return new List<ChapterViewModel>();
                }

                var translationsResponse = await _mangaClient.GetMangaTranslationsAsync(mangaGuid);
                if (translationsResponse?.Data == null || !translationsResponse.Data.Any())
                {
                    _logger.LogWarning("Không tìm thấy bản dịch nào cho manga {MangaId}", mangaId);
                    return new List<ChapterViewModel>();
                }
                
                var allChapterViewModels = new List<ChapterViewModel>();
                var targetTranslations = translationsResponse.Data;

                // Nếu có danh sách ngôn ngữ được chỉ định, lọc các bản dịch
                if (languages != null && languages.Any())
                {
                    targetTranslations = targetTranslations
                        .Where(t => languages.Contains(t.Attributes.LanguageKey, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }

                foreach(var translation in targetTranslations)
                {
                    if (Guid.TryParse(translation.Id, out var tmGuid))
                    {
                        var chapterListResponse = await _chapterClient.GetChaptersByTranslatedMangaAsync(tmGuid, limit: 5000);
                        if(chapterListResponse?.Data != null)
                        {
                            foreach (var chapterDto in chapterListResponse.Data)
                            {
                                allChapterViewModels.Add(_chapterViewModelMapper.MapToChapterViewModel(chapterDto, translation.Attributes.LanguageKey));
                            }
                        }
                    }
                }
                
                return SortChaptersByNumberDescending(allChapterViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chapters cho manga {MangaId}", mangaId);
                return new List<ChapterViewModel>();
            }
        }

        private List<ChapterViewModel> SortChaptersByNumberDescending(List<ChapterViewModel> chapters)
        {
            return chapters
                .OrderByDescending(c => ParseChapterNumber(c.Number) ?? double.MinValue)
                .ThenByDescending(c => c.PublishedAt)
                .ToList();
        }

        public Dictionary<string, List<ChapterViewModel>> GetChaptersByLanguage(List<ChapterViewModel> chapters)
        {
            var chaptersByLanguage = chapters.GroupBy(c => c.Language)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var language in chaptersByLanguage.Keys)
            {
                chaptersByLanguage[language] = SortChaptersByNumberAscending(chaptersByLanguage[language]);
            }

            return chaptersByLanguage;
        }

        private List<ChapterViewModel> SortChaptersByNumberAscending(List<ChapterViewModel> chapters)
        {
            return chapters
                .OrderBy(c => ParseChapterNumber(c.Number) ?? double.MaxValue)
                .ThenBy(c => c.PublishedAt)
                .ToList();
        }

        public async Task<List<SimpleChapterInfoViewModel>> GetLatestChaptersAsync(string mangaId, int limit, List<string>? languages = null)
        {
            var allChapters = await GetChaptersAsync(mangaId, languages);
            return allChapters
                .OrderByDescending(c => c.PublishedAt)
                .Take(limit)
                .Select(vm => new SimpleChapterInfoViewModel { ChapterId = vm.Id, DisplayTitle = vm.Title, PublishedAt = vm.PublishedAt})
                .ToList();
        }

        private double? ParseChapterNumber(string chapterNumber)
        {
            if (double.TryParse(chapterNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                return number;
            }
            return null;
        }
    }
}
```

### 2.4: Cập nhật `MangaDetailsService`

Sửa đổi `GetChaptersAsync` để gọi `ChapterService` mà không cần chỉ định ngôn ngữ, cho phép nó lấy tất cả các chapter có sẵn.

<!-- MangaReader_WebUI\Services\MangaServices\MangaPageService\MangaDetailsService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaDetailsService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaDetailsService> _logger;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly ChapterService _chapterService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMangaReaderLibToMangaDetailViewModelMapper _mangaDetailViewModelMapper;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public MangaDetailsService(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<MangaDetailsService> logger,
            IMangaFollowService mangaFollowService,
            ChapterService chapterService,
            IHttpContextAccessor httpContextAccessor,
            IMangaReaderLibToMangaDetailViewModelMapper mangaDetailViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaFollowService = mangaFollowService;
            _chapterService = chapterService;
            _httpContextAccessor = httpContextAccessor;
            _mangaDetailViewModelMapper = mangaDetailViewModelMapper;
            _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        }

        /// <summary>
        /// Lấy thông tin chi tiết manga từ API
        /// </summary>
        public async Task<MangaDetailViewModel> GetMangaDetailsAsync(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var mangaGuid))
                {
                    _logger.LogError("[LOGGING] MangaId không hợp lệ: {MangaId}", id);
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "ID Manga không hợp lệ" } };
                }

                var includes = new List<string> { "author", "artist", "cover_art" };
                _logger.LogInformation("[LOGGING] Chuẩn bị gọi GetMangaByIdAsync cho ID: {MangaId} với includes: {Includes}", id, string.Join(", ", includes));
                
                var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid, includes);

                // LOG DỮ LIỆU THÔ TỪ API
                if (mangaResponse != null)
                {
                    _logger.LogInformation("[LOGGING] Dữ liệu JSON thô trả về từ API cho manga ID {MangaId}:\n{JsonResponse}", 
                        id, JsonSerializer.Serialize(mangaResponse, _jsonSerializerOptions));
                }
                else
                {
                    _logger.LogWarning("[LOGGING] API không trả về dữ liệu (null) cho manga ID {MangaId}", id);
                }

                if (mangaResponse?.Data == null)
                {
                    _logger.LogError("[LOGGING] Không thể lấy chi tiết manga {id}. API không trả về dữ liệu trong 'data' field.", id);
                    return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" } };
                }

                var mangaData = mangaResponse.Data;
                
                // Lấy tất cả các chapter có sẵn
                var chapterViewModels = await GetChaptersAsync(id, mangaData.Attributes.AvailableTranslatedLanguages);

                _logger.LogInformation("[LOGGING] Bắt đầu quá trình mapping dữ liệu cho manga ID: {MangaId}", id);
                var mangaDetailViewModel = await _mangaDetailViewModelMapper.MapToMangaDetailViewModelAsync(mangaData, chapterViewModels);
                _logger.LogInformation("[LOGGING] Hoàn tất quá trình mapping dữ liệu cho manga ID: {MangaId}", id);

                if (mangaDetailViewModel.Manga != null)
                {
                    mangaDetailViewModel.Manga.IsFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                }

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && mangaDetailViewModel.Manga != null && !string.IsNullOrEmpty(mangaDetailViewModel.Manga.Title))
                {
                    httpContext.Session.SetString($"Manga_{id}_Title", mangaDetailViewModel.Manga.Title);
                }

                return mangaDetailViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LOGGING] Lỗi nghiêm trọng khi lấy chi tiết manga {id}", id);
                return new MangaDetailViewModel { Manga = new MangaViewModel { Id = id, Title = "Lỗi tải thông tin" } };
            }
        }

        /// <summary>
        /// Lấy danh sách chapters của manga
        /// </summary>
        private async Task<List<ChapterViewModel>> GetChaptersAsync(string mangaId, List<string>? availableLanguages)
        {
            try
            {
                // Truyền danh sách ngôn ngữ có sẵn vào ChapterService
                var chapterViewModels = await _chapterService.GetChaptersAsync(mangaId, availableLanguages);

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && chapterViewModels.Any())
                {
                    var chaptersByLanguage = _chapterService.GetChaptersByLanguage(chapterViewModels);
                    foreach (var kvp in chaptersByLanguage)
                    {
                        httpContext.Session.SetString($"Manga_{mangaId}_Chapters_{kvp.Key.ToLower()}", JsonSerializer.Serialize(kvp.Value));
                    }
                }
                return chapterViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chapters cho manga {mangaId}", mangaId);
                return new List<ChapterViewModel>();
            }
        }
    }
}
```

---

## Bước 3: Cập nhật Controller

Chỉnh sửa `MangaController` để nhận các tham số tìm kiếm mới từ request và truyền chúng vào service.

### 3.1: Cập nhật `MangaController.cs`

<!-- MangaReader_WebUI\Controllers\MangaController.cs -->
```csharp
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.MangaPageService;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Controllers
{
    public class MangaController : Controller
    {
        private readonly IMangaReaderLibTagClient _tagClient;
        private readonly IMangaReaderLibAuthorClient _authorClient;
        private readonly IMangaReaderLibToTagListResponseMapper _tagListResponseMapper;
        private readonly ILogger<MangaController> _logger;
        private readonly MangaDetailsService _mangaDetailsService;
        private readonly MangaSearchService _mangaSearchService;
        private readonly ViewRenderService _viewRenderService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFollowedMangaService _followedMangaService;
        private readonly IReadingHistoryService _readingHistoryService;

        public MangaController(
            IMangaReaderLibTagClient tagClient,
            IMangaReaderLibAuthorClient authorClient,
            IMangaReaderLibToTagListResponseMapper tagListResponseMapper,
            ILogger<MangaController> logger,
            MangaDetailsService mangaDetailsService,
            MangaSearchService mangaSearchService,
            ViewRenderService viewRenderService,
            IMangaFollowService mangaFollowService,
            IUserService userService,
            IHttpClientFactory httpClientFactory,
            IFollowedMangaService followedMangaService,
            IReadingHistoryService readingHistoryService)
        {
            _tagClient = tagClient;
            _authorClient = authorClient;
            _tagListResponseMapper = tagListResponseMapper;
            _logger = logger;
            _mangaDetailsService = mangaDetailsService;
            _mangaSearchService = mangaSearchService;
            _viewRenderService = viewRenderService;
            _mangaFollowService = mangaFollowService;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _followedMangaService = followedMangaService;
            _readingHistoryService = readingHistoryService;
        }

        private static class SessionKeys
        {
            public const string CurrentSearchResultData = "CurrentSearchResultData";
        }

        [HttpGet]
        [Route("api/manga/tags")]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                _logger.LogInformation("Đang lấy danh sách tags từ MangaReaderLib API");
                var tagsDataFromLib = await _tagClient.GetTagsAsync(limit: 500); // Lấy tối đa 500 tags
                if (tagsDataFromLib == null)
                {
                    throw new Exception("API không trả về dữ liệu tags.");
                }

                // Map kết quả từ MangaReaderLib DTO sang MangaDex DTO mà frontend đang sử dụng
                var tagsForFrontend = _tagListResponseMapper.MapToTagListResponse(tagsDataFromLib);

                return Json(new { success = true, data = tagsForFrontend });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tags từ MangaReaderLib API.");
                return Json(new { success = false, error = "Không thể tải danh sách tags." });
            }
        }

        // GET: Manga/Details/5
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                ViewData["PageType"] = "manga-details";
                var viewModel = await _mangaDetailsService.GetMangaDetailsAsync(id);

                if (_userService.IsAuthenticated())
                {
                    bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                    if (viewModel.Manga != null)
                    {
                        viewModel.Manga.IsFollowing = isFollowing;
                    }
                }
                else
                {
                    if (viewModel.Manga != null)
                    {
                        viewModel.Manga.IsFollowing = false;
                    }
                }

                if (viewModel.AlternativeTitlesByLanguage != null && viewModel.AlternativeTitlesByLanguage.Any())
                {
                    ViewData["AlternativeTitlesByLanguage"] = viewModel.AlternativeTitlesByLanguage;
                }

                return _viewRenderService.RenderViewBasedOnRequest(this, "Details", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết manga: {Message}", ex.Message);
                ViewBag.ErrorMessage = "Không thể tải chi tiết manga. Vui lòng thử lại sau.";
                return View("Details", new MangaDetailViewModel { AlternativeTitlesByLanguage = new Dictionary<string, List<string>>() });
            }
        }

        // GET: Manga/Search
        public async Task<IActionResult> Search(
            string title = "",
            List<string>? status = null,
            string sortBy = "updatedAt",
            string? authors = null,
            string? artists = null,
            int? year = null,
            List<string>? publicationDemographic = null,
            List<string>? contentRating = null,
            List<string>? availableTranslatedLanguage = null, // THÊM THAM SỐ MỚI
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            string includedTagsStr = "",
            string excludedTagsStr = "",
            int page = 1,
            int pageSize = 24)
        {
            try
            {
                _logger.LogInformation("[SEARCH_VIEW] Bắt đầu action Search với page={Page}, pageSize={PageSize}", page, pageSize);
                ViewData["PageType"] = "home";

                var authorIdList = authors?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                var artistIdList = artists?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, status, sortBy, authorIdList, artistIdList, year,
                    publicationDemographic, contentRating, availableTranslatedLanguage,
                    includedTagsMode, excludedTagsMode, includedTagsStr, excludedTagsStr);

                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas != null && viewModel.Mangas.Any())
                {
                    HttpContext.Session.SetString(SessionKeys.CurrentSearchResultData,
                        JsonSerializer.Serialize(viewModel.Mangas));
                }

                string initialViewMode = Request.Cookies.TryGetValue("MangaViewMode", out string? cookieViewMode) && (cookieViewMode == "grid" || cookieViewMode == "list")
                    ? cookieViewMode : "grid";

                ViewData["InitialViewMode"] = initialViewMode;

                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    string hxTarget = Request.Headers["HX-Target"].FirstOrDefault() ?? "";
                    string referer = Request.Headers["Referer"].FirstOrDefault() ?? "";

                    if (!string.IsNullOrEmpty(referer) && !referer.Contains("/Manga/Search"))
                    {
                        return PartialView("Search", viewModel);
                    }

                    if (hxTarget == "search-results-and-pagination" || hxTarget == "main-content")
                    {
                        return PartialView("_SearchResultsWrapperPartial", viewModel);
                    }
                    else
                    {
                        return PartialView("Search", viewModel);
                    }
                }

                return View("Search", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách manga.");
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga. Chi tiết: {ex.Message}";
                return View("Search", new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = new SortManga { Title = title, Status = status, SortBy = sortBy }
                });
            }
        }

        [HttpPost]
        [Route("api/proxy/toggle-follow")]
        public async Task<IActionResult> ToggleFollowProxy([FromBody] MangaActionRequest request)
        {
            if (string.IsNullOrEmpty(request?.MangaId))
            {
                return BadRequest(new { success = false, message = "Manga ID không hợp lệ" });
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Toggle follow attempt failed: User not authenticated.");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
            }

            string backendEndpoint;
            bool isCurrentlyFollowing;

            try
            {
                var checkClient = _httpClientFactory.CreateClient("BackendApiClient");
                var checkToken = _userService.GetToken();
                checkClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", checkToken);
                var checkResponse = await checkClient.GetAsync($"/api/users/user/following/{request.MangaId}");

                if (checkResponse.IsSuccessStatusCode)
                {
                    var checkContent = await checkResponse.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<FollowingStatusResponse>(checkContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    isCurrentlyFollowing = statusResponse?.IsFollowing ?? false;
                }
                else if (checkResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _userService.RemoveToken();
                    return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Không thể kiểm tra trạng thái theo dõi hiện tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking following status for manga {MangaId}", request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi khi kiểm tra trạng thái theo dõi." });
            }

            backendEndpoint = isCurrentlyFollowing ? "/api/users/unfollow" : "/api/users/follow";
            bool targetFollowingState = !isCurrentlyFollowing;
            string successMessage = targetFollowingState ? "Đã theo dõi truyện" : "Đã hủy theo dõi truyện";

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { mangaId = request.MangaId };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(backendEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true, isFollowing = targetFollowingState, message = successMessage });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                        return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                    }
                    return StatusCode((int)response.StatusCode, new { success = false, message = $"Lỗi từ backend: {response.ReasonPhrase}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong proxy action {Endpoint} cho manga {MangaId}", backendEndpoint, request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi xử lý yêu cầu" });
            }
        }

        public async Task<IActionResult> GetSearchResultsPartial(
            string title = "", 
            List<string>? status = null, 
            string sortBy = "updatedAt",
            string? authors = null, 
            string? artists = null,
            int? year = null,
            List<string>? publicationDemographic = null, 
            List<string>? contentRating = null,
            List<string>? availableTranslatedLanguage = null, // THÊM THAM SỐ MỚI
            string includedTagsMode = "AND", 
            string excludedTagsMode = "OR",
            string includedTagsStr = "", 
            string excludedTagsStr = "",
            int page = 1, 
            int pageSize = 24)
        {
            try
            {
                var authorIdList = authors?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                var artistIdList = artists?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, status, sortBy, authorIdList, artistIdList, year,
                    publicationDemographic, contentRating, availableTranslatedLanguage,
                    includedTagsMode, excludedTagsMode, includedTagsStr, excludedTagsStr);

                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas.Count == 0)
                {
                    return PartialView("_NoResultsPartial");
                }
                return PartialView("_SearchResultsWrapperPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải kết quả tìm kiếm.");
                return PartialView("_NoResultsPartial");
            }
        }

        [HttpGet]
        public IActionResult GetMangaViewPartial(string viewMode = "grid")
        {
            try
            {
                var mangasJson = HttpContext.Session.GetString(SessionKeys.CurrentSearchResultData);
                if (string.IsNullOrEmpty(mangasJson))
                {
                    return PartialView("_NoResultsPartial");
                }

                var mangas = JsonSerializer.Deserialize<List<MangaViewModel>>(mangasJson);
                ViewData["InitialViewMode"] = viewMode;

                var viewModel = new MangaListViewModel
                {
                    Mangas = mangas ?? new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = mangas?.Count ?? 0,
                    TotalCount = mangas?.Count ?? 0,
                    MaxPages = 1
                };

                return PartialView("_SearchResultsPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu manga từ Session.");
                return PartialView("_NoResultsPartial");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Followed()
        {
            if (!_userService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Followed", "Manga") });
            }

            try
            {
                var followedMangas = await _followedMangaService.GetFollowedMangaListAsync();
                return _viewRenderService.RenderViewBasedOnRequest(this, "Followed", followedMangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang truyện đang theo dõi.");
                ViewBag.ErrorMessage = "Không thể tải danh sách truyện đang theo dõi. Vui lòng thử lại sau.";
                return View("Followed", new List<FollowedMangaViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            if (!_userService.IsAuthenticated())
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_UnauthorizedPartial");
                }
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("History", "Manga") });
            }

            try
            {
                var history = await _readingHistoryService.GetReadingHistoryAsync();
                return _viewRenderService.RenderViewBasedOnRequest(this, "History", history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang lịch sử đọc truyện.");
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                    return PartialView("_ErrorPartial");
                }
                ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                return View("History", new List<LastReadMangaViewModel>());
            }
        }

        [HttpGet("api/manga/search-authors")]
        public async Task<IActionResult> SearchAuthors([FromQuery] string nameFilter)
        {
            if (string.IsNullOrWhiteSpace(nameFilter) || nameFilter.Length < 2)
            {
                return Ok(new List<AuthorSearchResultViewModel>());
            }

            try
            {
                var authorResponse = await _authorClient.GetAuthorsAsync(nameFilter: nameFilter, limit: 10);
                if (authorResponse?.Data == null)
                {
                    return Ok(new List<AuthorSearchResultViewModel>());
                }

                var results = authorResponse.Data.Select(a => new AuthorSearchResultViewModel
                {
                    Id = Guid.Parse(a.Id),
                    Name = a.Attributes.Name
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm tác giả với filter: {NameFilter}", nameFilter);
                return StatusCode(500, "Lỗi máy chủ khi tìm kiếm tác giả.");
            }
        }
    }

    public class MangaActionRequest
    {
        public string? MangaId { get; set; }
    }

    public class FollowingStatusResponse
    {
        public bool IsFollowing { get; set; }
    }
}
```

---

## Bước 4: Cập nhật View

Cập nhật giao diện người dùng để thêm bộ lọc mới và hiển thị thông tin ngôn ngữ một cách linh động.

### 4.1: Cập nhật `_SearchFormPartial.cshtml`

Thêm hai bộ lọc mới: Họa sĩ (Artist) và Ngôn ngữ có sẵn (Available Language).

<!-- MangaReader_WebUI\Views\MangaSearch\_SearchFormPartial.cshtml -->
```html
@model MangaReader.WebUI.Models.ViewModels.Manga.MangaListViewModel

<div class="card search-card">
    <div class="card-body">
        <form asp-action="Search" method="get" id="searchForm" hx-get="@Url.Action("Search", "Manga")" hx-target="#search-results-and-pagination" hx-push-url="true">
            <div class="row g-3">
                <div class="col-md-12">
                    <div class="input-group mb-3">
                        <span class="input-group-text"><i class="bi bi-search"></i></span>
                        <input type="text" name="title" value="@Model.SortOptions.Title" class="form-control form-control-lg" placeholder="Nhập tên manga cần tìm...">
                        <button type="submit" class="btn btn-primary">
                            <i class="bi bi-search me-2"></i>Tìm kiếm
                        </button>
                    </div>
                </div>
                
                <div class="col-md-12">
                    <button type="button" class="filter-toggle-btn w-100 py-2 text-start" id="filterToggle">
                        <i class="bi bi-funnel me-2"></i>
                        <span>Bộ lọc nâng cao</span>
                        <span class="chevron-icon float-end mt-1"></span>
                    </button>
                    
                    <div class="filter-content p-3 border rounded mt-2" id="filterContainer" style="display: none;">
                        <div class="row g-3">
                            <div class="col-md-12">
                                <label class="form-label mb-2">Thẻ và thể loại 
                                    <span class="tags-help-icon ms-1">
                                        <i class="bi bi-info-circle-fill"></i>
                                        <div class="custom-tooltip">
                                            <strong>Sử dụng tag:</strong><br>
                                            • Click 1 lần: <span class='badge bg-primary'>Bắt buộc có</span> - Manga phải chứa tag này.<br>
                                            • Click 2 lần: <span class='badge bg-danger'>Loại trừ</span> - Manga không được chứa tag này.<br>
                                            • Click 3 lần: <span class='badge bg-secondary'>Không áp dụng</span> - Bỏ tag khỏi bộ lọc.<br><br>
                                            <strong>Chế độ thẻ bắt buộc:</strong><br>
                                            <strong>VÀ</strong>: Manga phải chứa tất cả thẻ bắt buộc đã chọn.<br>
                                            <strong>HOẶC</strong>: Manga chỉ cần chứa ít nhất một trong các thẻ bắt buộc đã chọn.<br><br>
                                            <strong>Chế độ thẻ loại trừ:</strong><br>
                                            <strong>VÀ</strong>: Manga không được chứa tất cả thẻ loại trừ đã chọn (chặn chỉ khi có đủ tất cả thẻ).<br>
                                            <strong>HOẶC</strong>: Manga không được chứa bất kỳ thẻ loại trừ nào đã chọn (chặn khi có bất kỳ thẻ nào).
                                        </div>
                                    </span>
                                </label>
                                <div class="d-flex gap-2 mb-2">
                                    <div class="tag-mode-box-container">
                                        <div class="tag-mode-label">Thẻ bắt buộc:</div>
                                        <div class="tag-mode-box @(Model.SortOptions.IncludedTagsMode == "OR" ? "tag-mode-or" : "tag-mode-and")" id="includedTagsModeBox">
                                            <span id="includedTagsModeText">@(Model.SortOptions.IncludedTagsMode == "OR" ? "HOẶC" : "VÀ")</span>
                                        </div>
                                        <input type="hidden" name="includedTagsMode" id="includedTagsMode" value="@Model.SortOptions.IncludedTagsMode">
                                    </div>
                                    <div class="tag-mode-box-container">
                                        <div class="tag-mode-label">Thẻ loại trừ:</div>
                                        <div class="tag-mode-box @(Model.SortOptions.ExcludedTagsMode == "AND" ? "tag-mode-and" : "tag-mode-or")" id="excludedTagsModeBox">
                                            <span id="excludedTagsModeText">@(Model.SortOptions.ExcludedTagsMode == "AND" ? "VÀ" : "HOẶC")</span>
                                        </div>
                                        <input type="hidden" name="excludedTagsMode" id="excludedTagsMode" value="@Model.SortOptions.ExcludedTagsMode">
                                    </div>
                                </div>
                                <div class="manga-tags-container">
                                    <div class="manga-tags-selection" id="mangaTagsSelection" tabindex="0">
                                        <div class="manga-selected-tags" id="selectedTagsDisplay">
                                            @if (string.IsNullOrEmpty(Model.SortOptions.IncludedTagsStr) && string.IsNullOrEmpty(Model.SortOptions.ExcludedTagsStr))
                                            {
                                                <span class="manga-tags-empty" id="emptyTagsMessage">Chưa có thẻ nào được chọn. Bấm để chọn thẻ.</span>
                                            }
                                        </div>
                                    </div>
                                    <div class="manga-tags-dropdown" id="mangaTagsDropdown">
                                        <div class="manga-tag-search">
                                            <div class="input-group">
                                                <span class="input-group-text"><i class="bi bi-search"></i></span>
                                                <input type="text" class="form-control form-control-sm" id="tagSearchInput" placeholder="Tìm kiếm thẻ...">
                                            </div>
                                        </div>
                                        <div id="tagsContainer" class="manga-tags-groups">
                                            <div class="text-center py-3">
                                                <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                                                <span>Đang tải danh sách thẻ...</span>
                                            </div>
                                        </div>
                                        <div class="manga-tags-footer">
                                            <button type="button" class="btn btn-sm btn-light" id="closeTagsDropdown">
                                                <i class="bi bi-x"></i> Đóng
                                            </button>
                                        </div>
                                    </div>
                                </div>
                                <input type="hidden" id="selectedTags" name="includedTagsStr" value="@Model.SortOptions.IncludedTagsStr" />
                                <input type="hidden" id="excludedTags" name="excludedTagsStr" value="@Model.SortOptions.ExcludedTagsStr" />
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Tác giả</label>
                                <div class="author-search-container" id="authorSearchContainer">
                                    <div class="author-search-input-wrapper">
                                        <div class="selected-authors-list"></div>
                                        <input type="text" class="form-control author-search-input" placeholder="Tìm tên tác giả...">
                                    </div>
                                    <div class="author-search-results"></div>
                                    <input type="hidden" name="authors" value="@(Model.SortOptions.Authors != null ? string.Join(",", Model.SortOptions.Authors) : "")" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Họa sĩ</label>
                                 <div class="author-search-container" id="artistSearchContainer">
                                    <div class="author-search-input-wrapper">
                                        <div class="selected-authors-list"></div>
                                        <input type="text" class="form-control author-search-input" placeholder="Tìm tên họa sĩ...">
                                    </div>
                                    <div class="author-search-results"></div>
                                    <input type="hidden" name="artists" value="@(Model.SortOptions.Artists != null ? string.Join(",", Model.SortOptions.Artists) : "")" />
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="filter-dropdown-label">Sắp xếp theo</label>
                                <div class="filter-dropdown">
                                    <button type="button" class="filter-toggle-btn">
                                        <span class="selected-text">
                                            @{
                                                var sortByText = Model.SortOptions.SortBy switch
                                                {
                                                    "title" => "Tên (A-Z)",
                                                    "createdAt" => "Thời gian tạo",
                                                    "year" => "Năm xuất bản",
                                                    _ => "Mới nhất" // Mặc định là updatedAt
                                                };
                                            }
                                            @sortByText
                                        </span>
                                        <span class="toggle-icon"></span>
                                    </button>
                                    <div class="filter-menu-content">
                                        <div class="filter-menu-padding">
                                            <div class="filter-option">
                                                <input type="radio" name="sortBy" id="sortUpdatedAt" value="updatedAt" @(Model.SortOptions.SortBy == "updatedAt" || string.IsNullOrEmpty(Model.SortOptions.SortBy) ? "checked" : "")>
                                                <label class="filter-option-label" for="sortUpdatedAt">Mới nhất</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="sortBy" id="sortTitle" value="title" @(Model.SortOptions.SortBy == "title" ? "checked" : "")>
                                                <label class="filter-option-label" for="sortTitle">Tên (A-Z)</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="sortBy" id="sortCreatedAt" value="createdAt" @(Model.SortOptions.SortBy == "createdAt" ? "checked" : "")>
                                                <label class="filter-option-label" for="sortCreatedAt">Thời gian tạo</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="sortBy" id="sortYear" value="year" @(Model.SortOptions.SortBy == "year" ? "checked" : "")>
                                                <label class="filter-option-label" for="sortYear">Năm xuất bản</label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="filter-dropdown-label">Mức độ nội dung</label>
                                <div class="filter-dropdown">
                                    <button type="button" class="filter-toggle-btn">
                                        <span class="selected-text">
                                            @(Model.SortOptions.ContentRating != null && Model.SortOptions.ContentRating.Any() ? string.Join(", ", Model.SortOptions.ContentRating.Select(TranslateContentRating)) : "Tất cả")
                                        </span>
                                        <span class="toggle-icon"></span>
                                    </button>
                                    <div class="filter-menu-content">
                                        <div class="filter-menu-padding">
                                            <div class="filter-option">
                                                <input type="checkbox" name="contentRating" id="contentSafe" value="Safe" @(Model.SortOptions.ContentRating == null || Model.SortOptions.ContentRating.Contains("Safe") ? "checked" : "")>
                                                <label class="filter-option-label" for="contentSafe">An Toàn</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="contentRating" id="contentSuggestive" value="Suggestive" @(Model.SortOptions.ContentRating != null && Model.SortOptions.ContentRating.Contains("Suggestive") ? "checked" : "")>
                                                <label class="filter-option-label" for="contentSuggestive">Nhạy cảm</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="contentRating" id="contentErotica" value="Erotica" @(Model.SortOptions.ContentRating != null && Model.SortOptions.ContentRating.Contains("Erotica") ? "checked" : "")>
                                                <label class="filter-option-label" for="contentErotica">R18</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="contentRating" id="contentPornographic" value="Pornographic" @(Model.SortOptions.ContentRating != null && Model.SortOptions.ContentRating.Contains("Pornographic") ? "checked" : "")>
                                                <label class="filter-option-label" for="contentPornographic">NSFW</label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="filter-dropdown-label">Số kết quả mỗi trang</label>
                                <div class="filter-dropdown">
                                    <button type="button" class="filter-toggle-btn">
                                        <span class="selected-text">@Model.PageSize</span>
                                        <span class="toggle-icon"></span>
                                    </button>
                                    <div class="filter-menu-content">
                                        <div class="filter-menu-padding">
                                            <div class="filter-option">
                                                <input type="radio" name="pageSize" id="pageSize12" value="12" @(Model.PageSize == 12 ? "checked" : "")>
                                                <label class="filter-option-label" for="pageSize12">12</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="pageSize" id="pageSize24" value="24" @(Model.PageSize == 24 ? "checked" : "")>
                                                <label class="filter-option-label" for="pageSize24">24</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="pageSize" id="pageSize36" value="36" @(Model.PageSize == 36 ? "checked" : "")>
                                                <label class="filter-option-label" for="pageSize36">36</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="pageSize" id="pageSize48" value="48" @(Model.PageSize == 48 ? "checked" : "")>
                                                <label class="filter-option-label" for="pageSize48">48</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="radio" name="pageSize" id="pageSize100" value="100" @(Model.PageSize == 100 ? "checked" : "")>
                                                <label class="filter-option-label" for="pageSize100">100</label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="filter-dropdown-label">Đối tượng độc giả</label>
                                <div class="filter-dropdown">
                                    <button type="button" class="filter-toggle-btn">
                                        <span class="selected-text">
                                            @(Model.SortOptions.PublicationDemographic != null && Model.SortOptions.PublicationDemographic.Any() ? string.Join(", ", Model.SortOptions.PublicationDemographic.Select(d => d.ToUpperInvariant())) : "Tất cả")
                                        </span>
                                        <span class="toggle-icon"></span>
                                    </button>
                                    <div class="filter-menu-content">
                                        <div class="filter-menu-padding">
                                            <div class="filter-option">
                                                <input type="checkbox" name="publicationDemographic" id="demoShounen" value="Shounen" @(Model.SortOptions.PublicationDemographic != null && Model.SortOptions.PublicationDemographic.Contains("Shounen") ? "checked" : "")>
                                                <label class="filter-option-label" for="demoShounen">Shounen</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="publicationDemographic" id="demoShoujo" value="Shoujo" @(Model.SortOptions.PublicationDemographic != null && Model.SortOptions.PublicationDemographic.Contains("Shoujo") ? "checked" : "")>
                                                <label class="filter-option-label" for="demoShoujo">Shoujo</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="publicationDemographic" id="demoSeinen" value="Seinen" @(Model.SortOptions.PublicationDemographic != null && Model.SortOptions.PublicationDemographic.Contains("Seinen") ? "checked" : "")>
                                                <label class="filter-option-label" for="demoSeinen">Seinen</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="publicationDemographic" id="demoJosei" value="Josei" @(Model.SortOptions.PublicationDemographic != null && Model.SortOptions.PublicationDemographic.Contains("Josei") ? "checked" : "")>
                                                <label class="filter-option-label" for="demoJosei">Josei</label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="filter-dropdown-label">Trạng thái</label>
                                <div class="filter-dropdown">
                                    <button type="button" class="filter-toggle-btn">
                                        <span class="selected-text">
                                            @(Model.SortOptions.Status != null && Model.SortOptions.Status.Any() ? string.Join(", ", Model.SortOptions.Status.Select(s => TranslateStatus(s))) : "Tất cả")
                                        </span>
                                        <span class="toggle-icon"></span>
                                    </button>
                                    <div class="filter-menu-content">
                                        <div class="filter-menu-padding">
                                            <div class="filter-option">
                                                <input type="checkbox" name="status" id="statusOngoing" value="Ongoing" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("Ongoing") ? "checked" : "")>
                                                <label class="filter-option-label" for="statusOngoing">Đang tiến hành</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="status" id="statusCompleted" value="Completed" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("Completed") ? "checked" : "")>
                                                <label class="filter-option-label" for="statusCompleted">Hoàn thành</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="status" id="statusHiatus" value="Hiatus" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("Hiatus") ? "checked" : "")>
                                                <label class="filter-option-label" for="statusHiatus">Tạm ngưng</label>
                                            </div>
                                            <div class="filter-option">
                                                <input type="checkbox" name="status" id="statusCancelled" value="Cancelled" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("Cancelled") ? "checked" : "")>
                                                <label class="filter-option-label" for="statusCancelled">Đã hủy</label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="filter-dropdown-label">Ngôn ngữ có sẵn</label>
                                <div class="filter-dropdown">
                                    <button type="button" class="filter-toggle-btn">
                                        <span class="selected-text">
                                            @(Model.SortOptions.AvailableTranslatedLanguage != null && Model.SortOptions.AvailableTranslatedLanguage.Any() ? string.Join(", ", Model.SortOptions.AvailableTranslatedLanguage.Select(l => l.ToUpper())) : "Tất cả")
                                        </span>
                                        <span class="toggle-icon"></span>
                                    </button>
                                    <div class="filter-menu-content">
                                        <div class="filter-menu-padding">
                                            <div class="filter-option">
                                                <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langVi" value="vi" @(Model.SortOptions.AvailableTranslatedLanguage != null && Model.SortOptions.AvailableTranslatedLanguage.Contains("vi") ? "checked" : "")>
                                                <label class="filter-option-label" for="langVi">Tiếng Việt</label>
                                            </div>
                                            <div class="filter-option">
                                                <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langEnAvail" value="en" @(Model.SortOptions.AvailableTranslatedLanguage != null && Model.SortOptions.AvailableTranslatedLanguage.Contains("en") ? "checked" : "")>
                                                <label class="filter-option-label" for="langEnAvail">Tiếng Anh</label>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Năm phát hành</label>
                                <input type="number" name="year" class="form-control" placeholder="Nhập năm..." min="1900" max="@DateTime.Now.Year" value="@Model.SortOptions.Year">
                            </div>
                            <div class="col-md-12 text-end">
                                <button type="button" class="filter-reset-btn me-2" id="resetFilters">
                                    <i class="bi bi-x-circle me-2"></i>Xóa bộ lọc
                                </button>
                                <button type="submit" class="filter-apply-btn">
                                    <i class="bi bi-filter me-2"></i>Áp dụng bộ lọc
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <input type="hidden" name="page" value="1" />
        </form>
    </div>
</div>

<div class="d-flex justify-content-end mb-2">
    <div class="btn-group view-mode-toggle" role="group">
        <button type="button" class="btn btn-sm btn-outline-primary active" data-mode="grid" 
                hx-get="@Url.Action("GetMangaViewPartial", "Manga", new { viewMode = "grid" })"
                hx-target="#search-results-container"
                hx-swap="innerHTML"
                hx-push-url="false"
                title="Grid View">
            <i class="bi bi-grid-fill"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-primary" data-mode="list" 
                hx-get="@Url.Action("GetMangaViewPartial", "Manga", new { viewMode = "list" })"
                hx-target="#search-results-container"
                hx-swap="innerHTML"
                hx-push-url="false"
                title="List View">
            <i class="bi bi-list-ul"></i>
        </button>
    </div>
</div>

@functions {
    public string TranslateStatus(string status)
    {
        return status switch
        {
            "Ongoing" => "Đang tiến hành",
            "Completed" => "Hoàn thành",
            "Hiatus" => "Tạm ngưng",
            "Cancelled" => "Đã hủy",
            _ => status
        };
    }

    public string TranslateContentRating(string rating)
    {
        return rating switch
        {
            "Safe" => "An Toàn",
            "Suggestive" => "Nhạy cảm",
            "Erotica" => "R18",
            "Pornographic" => "NSFW",
            _ => rating
        };
    }
}
```

### 4.2: Cập nhật `_SearchPaginationPartial.cshtml`

Cập nhật logic tạo link phân trang để bao gồm các tham số tìm kiếm mới.

<!-- MangaReader_WebUI\Views\MangaSearch\_SearchPaginationPartial.cshtml -->
```html
@model MangaReader.WebUI.Models.ViewModels.Manga.MangaListViewModel

@if (Model.MaxPages > 1)
{
    <div class="d-flex justify-content-center mt-4">
        <nav aria-label="Page navigation">
            <ul class="pagination">
                @{
                    var totalPages = Model.MaxPages;
                    var startPage = Math.Max(2, Model.CurrentPage - 2);
                    var endPage = Math.Min(totalPages - 1, Model.CurrentPage + 2);
                }
                
                <li class="page-item @(Model.CurrentPage == 1 ? "disabled" : "")">
                    <a class="page-link" 
                       hx-get="@Url.Action("Search", "Manga", new { 
                           page = Math.Max(1, Model.CurrentPage - 1), 
                           title = Model.SortOptions.Title, 
                           status = Model.SortOptions.Status, 
                           publicationDemographic = Model.SortOptions.PublicationDemographic,
                           contentRating = Model.SortOptions.ContentRating,
                           availableTranslatedLanguage = Model.SortOptions.AvailableTranslatedLanguage,
                           sortBy = Model.SortOptions.SortBy, 
                           authors = string.Join(",", Model.SortOptions.Authors ?? new List<string>()),
                           artists = string.Join(",", Model.SortOptions.Artists ?? new List<string>()),
                           year = Model.SortOptions.Year,
                           includedTagsMode = Model.SortOptions.IncludedTagsMode,
                           excludedTagsMode = Model.SortOptions.ExcludedTagsMode,
                           pageSize = Model.PageSize,
                           includedTagsStr = Model.SortOptions.IncludedTagsStr,
                           excludedTagsStr = Model.SortOptions.ExcludedTagsStr
                       })" 
                       hx-target="#search-results-and-pagination" 
                       hx-push-url="true" 
                       aria-label="Previous">
                        <i class="bi bi-chevron-left"></i>
                    </a>
                </li>
                
                <li class="page-item @(Model.CurrentPage == 1 ? "active" : "")">
                    <a class="page-link" 
                       hx-get="@Url.Action("Search", "Manga", new { 
                           page = 1, 
                           title = Model.SortOptions.Title, 
                           status = Model.SortOptions.Status, 
                           publicationDemographic = Model.SortOptions.PublicationDemographic,
                           contentRating = Model.SortOptions.ContentRating,
                           availableTranslatedLanguage = Model.SortOptions.AvailableTranslatedLanguage,
                           sortBy = Model.SortOptions.SortBy, 
                           authors = string.Join(",", Model.SortOptions.Authors ?? new List<string>()),
                           artists = string.Join(",", Model.SortOptions.Artists ?? new List<string>()),
                           year = Model.SortOptions.Year,
                           includedTagsMode = Model.SortOptions.IncludedTagsMode,
                           excludedTagsMode = Model.SortOptions.ExcludedTagsMode,
                           pageSize = Model.PageSize,
                           includedTagsStr = Model.SortOptions.IncludedTagsStr,
                           excludedTagsStr = Model.SortOptions.ExcludedTagsStr
                       })" 
                       hx-target="#search-results-and-pagination" 
                       hx-push-url="true">
                        1
                    </a>
                </li>
                
                @if (startPage > 2)
                {
                    <li class="page-item">
                        <span class="page-link dots" data-page-goto="left">...</span>
                    </li>
                }
                
                @for (var i = startPage; i <= endPage; i++)
                {
                    <li class="page-item @(i == Model.CurrentPage ? "active" : "")">
                        <a class="page-link" 
                           hx-get="@Url.Action("Search", "Manga", new { 
                               page = i, 
                               title = Model.SortOptions.Title, 
                               status = Model.SortOptions.Status, 
                               publicationDemographic = Model.SortOptions.PublicationDemographic,
                               contentRating = Model.SortOptions.ContentRating,
                               availableTranslatedLanguage = Model.SortOptions.AvailableTranslatedLanguage,
                               sortBy = Model.SortOptions.SortBy, 
                               authors = string.Join(",", Model.SortOptions.Authors ?? new List<string>()),
                               artists = string.Join(",", Model.SortOptions.Artists ?? new List<string>()),
                               year = Model.SortOptions.Year,
                               includedTagsMode = Model.SortOptions.IncludedTagsMode,
                               excludedTagsMode = Model.SortOptions.ExcludedTagsMode,
                               pageSize = Model.PageSize,
                               includedTagsStr = Model.SortOptions.IncludedTagsStr,
                               excludedTagsStr = Model.SortOptions.ExcludedTagsStr
                           })" 
                           hx-target="#search-results-and-pagination" 
                           hx-push-url="true">
                            @i
                        </a>
                    </li>
                }
                
                @if (endPage < totalPages - 1)
                {
                    <li class="page-item">
                        <span class="page-link dots" data-page-goto="right">...</span>
                    </li>
                }
                
                @if (totalPages > 1)
                {
                    <li class="page-item @(Model.CurrentPage == totalPages ? "active" : "")">
                        <a class="page-link" 
                           hx-get="@Url.Action("Search", "Manga", new { 
                               page = totalPages, 
                               title = Model.SortOptions.Title, 
                               status = Model.SortOptions.Status, 
                               publicationDemographic = Model.SortOptions.PublicationDemographic,
                               contentRating = Model.SortOptions.ContentRating,
                               availableTranslatedLanguage = Model.SortOptions.AvailableTranslatedLanguage,
                               sortBy = Model.SortOptions.SortBy, 
                               authors = string.Join(",", Model.SortOptions.Authors ?? new List<string>()),
                               artists = string.Join(",", Model.SortOptions.Artists ?? new List<string>()),
                               year = Model.SortOptions.Year,
                               includedTagsMode = Model.SortOptions.IncludedTagsMode,
                               excludedTagsMode = Model.SortOptions.ExcludedTagsMode,
                               pageSize = Model.PageSize,
                               includedTagsStr = Model.SortOptions.IncludedTagsStr,
                               excludedTagsStr = Model.SortOptions.ExcludedTagsStr
                           })" 
                           hx-target="#search-results-and-pagination" 
                           hx-push-url="true">
                            @totalPages
                        </a>
                    </li>
                }
                
                <li class="page-item @(Model.CurrentPage == totalPages ? "disabled" : "")">
                    <a class="page-link" 
                       hx-get="@Url.Action("Search", "Manga", new { 
                           page = Math.Min(totalPages, Model.CurrentPage + 1), 
                           title = Model.SortOptions.Title, 
                           status = Model.SortOptions.Status, 
                           publicationDemographic = Model.SortOptions.PublicationDemographic,
                           contentRating = Model.SortOptions.ContentRating,
                           availableTranslatedLanguage = Model.SortOptions.AvailableTranslatedLanguage,
                           sortBy = Model.SortOptions.SortBy, 
                           authors = string.Join(",", Model.SortOptions.Authors ?? new List<string>()),
                           artists = string.Join(",", Model.SortOptions.Artists ?? new List<string>()),
                           year = Model.SortOptions.Year,
                           includedTagsMode = Model.SortOptions.IncludedTagsMode,
                           excludedTagsMode = Model.SortOptions.ExcludedTagsMode,
                           pageSize = Model.PageSize,
                           includedTagsStr = Model.SortOptions.IncludedTagsStr,
                           excludedTagsStr = Model.SortOptions.ExcludedTagsStr
                       })" 
                       hx-target="#search-results-and-pagination" 
                       hx-push-url="true" 
                       aria-label="Next">
                        <i class="bi bi-chevron-right"></i>
                    </a>
                </li>
            </ul>
        </nav>
    </div>
    
    <div class="text-center mt-2 text-muted">
        <small>Hiển thị @((Model.CurrentPage - 1) * Model.PageSize + 1) - @Math.Min(Model.CurrentPage * Model.PageSize, Model.TotalCount) trong tổng số @Model.TotalCount manga</small>
        @if (Model.TotalCount > 10000)
        {
            <br><small class="text-warning">API giới hạn chỉ hiển thị tối đa 10000 kết quả, tương ứng với trang @Model.MaxPages</small>
        }
    </div>
} 
```

### 4.3: Cập nhật `Details.cshtml`

Thay đổi giao diện trang chi tiết để hiển thị các nút ngôn ngữ một cách linh động dựa trên dữ liệu từ `Model.Manga.AvailableTranslatedLanguages`.

<!-- MangaReader_WebUI\Views\Manga\MangaDetails\Details.cshtml -->
```html
@model MangaReader.WebUI.Models.ViewModels.Manga.MangaDetailViewModel
@inject ILogger<Program> Logger

@{
    ViewData["Title"] = Model.Manga?.Title ?? "Chi tiết manga";
    
    var availableLangs = Model.Manga?.AvailableTranslatedLanguages
        .Select(lang => lang.ToLower())
        .OrderBy(lang => lang == "vi" ? 0 : (lang == "en" ? 1 : 2))
        .ThenBy(lang => lang)
        .ToList() ?? new List<string>();
        
    Logger.LogInformation("[LOGGING - VIEW] Dữ liệu nhận được tại Details.cshtml: Author='{Author}', Artist='{Artist}', Status='{Status}', AvailableLangs='{Langs}'", 
        Model.Manga?.Author, Model.Manga?.Artist, Model.Manga?.Status, string.Join(",", availableLangs));
}

<div class="details-manga-header-background" style="background-image: url('@Model.Manga?.CoverUrl')"></div>
<div class="details-manga-details-container">
    @if (ViewBag.ErrorMessage != null)
    {
        <div class="alert alert-danger mb-4">@ViewBag.ErrorMessage</div>
    }
    
    @if (Model.Manga != null)
    {
        <div class="details-manga-header-container">
            <div class="details-manga-header">
                <div class="container py-4">
                    <div class="row">
                        <div class="col-md-3">
                            <div class="detail-manga-cover-container">
                                <img src="@Model.Manga.CoverUrl" class="detail-manga-cover" alt="@Model.Manga.Title" data-bs-toggle="modal" data-bs-target="#coverModal">
                            </div>
                        </div>
                        <div class="col-md-9 theme-text ps-md-0">
                            <div class="details-manga-info-row details-manga-info-title">
                                <h1 class="details-manga-title mb-2">@Model.Manga.Title</h1>
                                @if (!string.IsNullOrEmpty(Model.Manga.AlternativeTitles))
                                {
                                    <p class="details-manga-alt-title mb-3">@Model.Manga.AlternativeTitles</p>
                                }
                                
                                <div class="mb-3">
                                    <div class="d-flex flex-wrap align-items-center author-artist-row">
                                        <p class="mb-2 me-4"><strong><i class="bi bi-person-fill me-2"></i>Tác giả:</strong> @Model.Manga.Author</p>
                                        @if (!string.IsNullOrEmpty(Model.Manga.Artist) && Model.Manga.Artist != Model.Manga.Author)
                                        {
                                            <p class="mb-2"><strong><i class="bi bi-brush me-2"></i>Họa sĩ:</strong> @Model.Manga.Artist</p>
                                        }
                                    </div>
                                </div>
                            </div>
                            
                            <div class="details-manga-info-row details-manga-info-meta">
                                <div class="d-flex gap-2 mb-3">
                                    @if (Model.Chapters.Any())
                                    {
                                        var chapters = Model.Chapters.OrderBy(c => c.Number).ToList();
                                        var firstChapter = availableLangs
                                            .Select(lang => chapters.FirstOrDefault(c => c.Language.Equals(lang, StringComparison.OrdinalIgnoreCase)))
                                            .FirstOrDefault(c => c != null);
                                        
                                        var newestChapter = Model.Chapters
                                            .OrderByDescending(c => double.TryParse(c.Number, out var num) ? num : 0)
                                            .ThenByDescending(c => c.PublishedAt)
                                            .FirstOrDefault();

                                        if (firstChapter != null)
                                        {
                                            <a asp-controller="Chapter" asp-action="Read" asp-route-id="@firstChapter.Id" class="btn btn-primary"
                                               hx-get="@Url.Action("Read", "Chapter", new { id = firstChapter.Id })"
                                               hx-target="#main-content"
                                               hx-push-url="true">
                                                <i class="bi bi-book-fill me-2"></i>Đọc từ đầu
                                            </a>
                                        }
                                        if (newestChapter != null)
                                        {
                                            <a asp-controller="Chapter" asp-action="Read" asp-route-id="@newestChapter.Id" class="btn btn-success"
                                               hx-get="@Url.Action("Read", "Chapter", new { id = newestChapter.Id })"
                                               hx-target="#main-content"
                                               hx-push-url="true">
                                                <i class="bi bi-lightning-fill me-2"></i>Đọc mới nhất
                                            </a>
                                        }
                                    }
                                    
                                    <button class="btn btn-theme-outline" id="followBtn" data-id="@Model.Manga.Id" data-following="@Model.Manga.IsFollowing.ToString().ToLower()">
                                        @if (Model.Manga.IsFollowing)
                                        {
                                            <i class="bi bi-bookmark-check-fill me-2"></i><span>Đang theo dõi</span>
                                        }
                                        else
                                        {
                                            <i class="bi bi-bookmark-plus me-2"></i><span>Theo dõi</span>
                                        }
                                    </button>
                                </div>
                                <div class="mb-3">
                                    <span class="badge bg-primary me-2">@Model.Manga.Status</span>
                                    @foreach (var tag in Model.Manga.Tags)
                                    {
                                        <span class="badge details-manga-tag me-2 mb-1">@tag</span>
                                    }
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            @if (!string.IsNullOrEmpty(Model.Manga.Description))
            {
                <div class="details-manga-description-section">
                    <div class="container py-3">
                        <div class="row">
                            <div class="col-12">
                                <div class="details-manga-description-container">
                                    <p class="details-manga-short-description">
                                        @Model.Manga.Description
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            }
        </div>
        
        <div class="modal fade" id="coverModal" tabindex="-1" aria-labelledby="coverModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="coverModalLabel">@Model.Manga.Title</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body text-center">
                        <img src="@Model.Manga.CoverUrl" class="img-fluid" alt="@Model.Manga.Title">
                    </div>
                </div>
            </div>
        </div>
        
        <div class="container mt-4 mb-5 pb-5">
            <ul class="nav nav-tabs mb-4" id="mangaDetailTabs" role="tablist">
                <li class="nav-item" role="presentation">
                    <button class="nav-link active" id="chapters-tab" data-bs-toggle="tab" data-bs-target="#chapters" type="button" role="tab" aria-controls="chapters" aria-selected="true">
                        <i class="bi bi-list-ol me-2"></i>Danh sách chương (@Model.Chapters.Count)
                    </button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="info-tab" data-bs-toggle="tab" data-bs-target="#info" type="button" role="tab" aria-controls="info" aria-selected="false">
                        <i class="bi bi-info-circle me-2"></i>Thông tin chi tiết
                    </button>
                </li>
            </ul>
            
            <div class="tab-content" id="mangaDetailTabsContent">
                <div class="tab-pane fade show active" id="chapters" role="tabpanel" aria-labelledby="chapters-tab">
                    
                    @if (availableLangs.Any())
                    {
                        <div class="mb-4">
                            <div class="custom-language-filter">
                                <button class="language-filter-btn active" data-lang="all">Tất cả</button>
                                @foreach(var lang in availableLangs)
                                {
                                    <button class="language-filter-btn" data-lang="@lang">@TranslateLanguage(lang)</button>
                                }
                            </div>
                        </div>
                    }
                    
                    @{
                        var chaptersByLanguage = Model.Chapters
                            .GroupBy(c => c.Language.ToLower())
                            .ToDictionary(g => g.Key, g => g.ToList());
                    }
                    
                    <div class="custom-chapters-container">
                        @foreach (var language in availableLangs)
                        {
                            if (!chaptersByLanguage.ContainsKey(language)) continue;

                            var langChapters = chaptersByLanguage[language];
                            var langId = $"lang-{language}";
                            var newestChapter = langChapters.OrderByDescending(c => {
                                _ = double.TryParse(c.Number, out var num);
                                return num;
                            }).FirstOrDefault();
                            var newestChapterNumber = newestChapter != null ? newestChapter.Number : "N/A";
                            var chaptersByVolume = langChapters
                                .GroupBy(c => c.Volume ?? "Không rõ")
                                .OrderByDescending(g => 
                                {
                                    if (g.Key == "Không rõ") return int.MinValue;
                                    return int.TryParse(g.Key, out int volNum) ? volNum : int.MinValue;
                                })
                                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => {
                                    _ = double.TryParse(c.Number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num);
                                    return num;
                                }).ToList());
                            
                            <div class="custom-language-section" data-language="@language" id="lang-section-@language">
                                <div class="custom-language-header" data-lang-id="@langId">
                                    <div class="custom-language-info">
                                        <i class="bi bi-translate me-2"></i>
                                        <span class="fw-bold">@TranslateLanguage(language)</span>
                                        <span class="badge bg-@(language == "vi" ? "success" : "info") ms-2">@langChapters.Count chương</span>
                                        <span class="badge bg-primary ms-2">Mới nhất: Chương @newestChapterNumber</span>
                                    </div>
                                    <i class="bi bi-chevron-down language-toggle-icon"></i>
                                </div>
                                <div class="custom-language-content" id="lang-content-@langId">
                                    <div class="custom-volumes-container">
                                        @foreach (var volumeKey in chaptersByVolume.Keys)
                                        {
                                            var volumeChapters = chaptersByVolume[volumeKey];
                                            var volumeIdSafe = $"{langId}-vol-{System.Text.RegularExpressions.Regex.Replace(volumeKey, @"[^a-zA-Z0-9\-_]", "-")}"; 
                                            var minChapterNum = volumeChapters.LastOrDefault()?.Number ?? "?";
                                            var maxChapterNum = volumeChapters.FirstOrDefault()?.Number ?? "?";
                                            
                                            <div class="custom-volume-dropdown" id="volume-@volumeIdSafe">
                                                <div class="custom-volume-header" data-volume-id="@volumeIdSafe">
                                                    <div class="custom-volume-title">
                                                        <i class="bi bi-journal-bookmark me-2"></i>
                                                        @(volumeKey == "Không rõ" ? "Tập không rõ" : $"Tập {volumeKey}")
                                                    </div>
                                                    <div class="custom-volume-chapters-info">
                                                        @if (minChapterNum != "?" && maxChapterNum != "?" && minChapterNum != maxChapterNum) {
                                                            @($"Chương {minChapterNum} - {maxChapterNum}")
                                                        } else if (maxChapterNum != "?") {
                                                            @($"Chương {maxChapterNum}")
                                                        }
                                                    </div>
                                                    <i class="bi bi-chevron-down volume-toggle-icon"></i>
                                                </div>
                                                <div class="custom-volume-chapters" id="chapters-@volumeIdSafe">
                                                    @foreach (var chapter in volumeChapters)
                                                    {
                                                        <a asp-controller="Chapter" asp-action="Read" 
                                                           asp-route-id="@chapter.Id"
                                                           class="custom-chapter-item chapter-link"
                                                           hx-get="@Url.Action("Read", "Chapter", new { id = chapter.Id })"
                                                           hx-target="#main-content"
                                                           hx-push-url="true">
                                                            <div class="custom-chapter-info">
                                                                <h6 class="mb-0">@chapter.Title</h6>
                                                                <small class="text-muted">@chapter.PublishedAt.ToString("dd/MM/yyyy")</small>
                                                            </div>
                                                        </a>
                                                    }
                                                </div>
                                            </div>
                                        }
                                    </div>
                                </div>
                            </div>
                        }
                    </div>
                </div>
                <div class="tab-pane fade" id="info" role="tabpanel" aria-labelledby="info-tab">
                    <div class="card details-manga-details-card">
                        <div class="card-body">
                            <h5 class="card-title mb-3">Mô tả đầy đủ</h5>
                            <p class="card-text details-manga-description">@Model.Manga.Description</p>
                            <h5 class="card-title mt-4 mb-3">Thông tin chi tiết</h5>
                            <ul class="list-group list-group-flush theme-aware-list">
                                <li class="list-group-item d-flex justify-content-between align-items-center">
                                    <span><i class="bi bi-person-fill me-2"></i>Tác giả</span>
                                    <span>@Model.Manga.Author</span>
                                </li>
                                @if (!string.IsNullOrEmpty(Model.Manga.Artist) && Model.Manga.Artist != Model.Manga.Author)
                                {
                                    <li class="list-group-item d-flex justify-content-between align-items-center">
                                        <span><i class="bi bi-brush me-2"></i>Họa sĩ</span>
                                        <span>@Model.Manga.Artist</span>
                                    </li>
                                }
                                <li class="list-group-item d-flex justify-content-between align-items-center">
                                    <span><i class="bi bi-flag-fill me-2"></i>Trạng thái</span>
                                    <span class="badge bg-primary">@Model.Manga.Status</span>
                                </li>
                                @if (!string.IsNullOrEmpty(Model.Manga.OriginalLanguage))
                                {
                                    <li class="list-group-item d-flex justify-content-between align-items-center">
                                        <span><i class="bi bi-globe me-2"></i>Ngôn ngữ gốc</span>
                                        <span>@TranslateLanguage(Model.Manga.OriginalLanguage)</span>
                                    </li>
                                }
                                <li class="list-group-item d-flex justify-content-between align-items-center">
                                    <span><i class="bi bi-translate me-2"></i>Bản dịch có sẵn</span>
                                    <span>
                                        @foreach (var lang in availableLangs)
                                        {
                                            <span class="badge bg-@(lang == "vi" ? "success" : "info") me-1">@TranslateLanguage(lang)</span>
                                        }
                                    </span>
                                </li>
                                <li class="list-group-item d-flex justify-content-between align-items-center">
                                    <span><i class="bi bi-list-ol me-2"></i>Số chương</span>
                                    <span>@Model.Chapters.Count</span>
                                </li>
                                @if (!string.IsNullOrEmpty(Model.Manga.PublicationDemographic))
                                {
                                    <li class="list-group-item d-flex justify-content-between align-items-center">
                                        <span><i class="bi bi-people-fill me-2"></i>Đối tượng</span>
                                        <span>@TranslateDemographic(Model.Manga.PublicationDemographic)</span>
                                    </li>
                                }
                                @if (!string.IsNullOrEmpty(Model.Manga.ContentRating))
                                {
                                    <li class="list-group-item d-flex justify-content-between align-items-center">
                                        <span><i class="bi bi-shield-fill me-2"></i>Xếp hạng nội dung</span>
                                        <span>@TranslateContentRating(Model.Manga.ContentRating)</span>
                                    </li>
                                }
                                @if (Model.Manga.LastUpdated.HasValue)
                                {
                                    <li class="list-group-item d-flex justify-content-between align-items-center">
                                        <span><i class="bi bi-clock-fill me-2"></i>Cập nhật lần cuối</span>
                                        <span>@Model.Manga.LastUpdated.Value.ToString("dd/MM/yyyy HH:mm")</span>
                                    </li>
                                }
                            </ul>
                            <h5 class="card-title mt-4 mb-3">Thể loại</h5>
                            <div class="details-manga-tags">
                                @foreach (var tag in Model.Manga.Tags)
                                {
                                    <span class="badge details-manga-tag me-2 mb-2">@tag</span>
                                }
                            </div>
                            @if (Model.AlternativeTitlesByLanguage != null && Model.AlternativeTitlesByLanguage.Any())
                            {
                                <h5 class="card-title mt-4 mb-3">Tên khác</h5>
                                <div class="card-text details-manga-alt-titles">
                                    @{
                                        var altTitleLangs = Model.AlternativeTitlesByLanguage.Keys
                                            .OrderBy(lang => lang == "vi" ? 0 : (lang == "en" ? 1 : 2))
                                            .ThenBy(lang => lang)
                                            .ToList();

                                        foreach (var lang in altTitleLangs)
                                        {
                                            var titles = Model.AlternativeTitlesByLanguage[lang];
                                            foreach (var title in titles)
                                            {
                                                <div class="mb-1 alt-title-item">
                                                    <span class="alt-title-lang me-2">
                                                        <i class="bi bi-translate me-1"></i>
                                                        <strong>@GetLanguageName(lang):</strong>
                                                    </span>
                                                    <span>@title</span>
                                                </div>
                                            }
                                        }
                                    }
                                </div>
                            }
                        </div>
                    </div>
                </div>
            </div>
        </div>
    }
    else
    {
        <div class="container">
            <div class="text-center py-5">
                <i class="bi bi-emoji-frown display-1 text-muted"></i>
                <h3 class="mt-3">Không tìm thấy thông tin manga</h3>
                <p class="text-muted">Manga bạn đang tìm kiếm không tồn tại hoặc đã bị xóa.</p>
                <a asp-controller="Home" asp-action="Index" class="btn btn-primary mt-3" hx-get="@Url.Action("Index", "Home")" hx-target="#main-content" hx-push-url="true">
                    <i class="bi bi-house-door me-2"></i>Quay lại trang chủ
                </a>
            </div>
        </div>
    }
</div>

@functions {
    public string TranslateStatus(string status)
    {
        return status.ToLower() switch
        {
            "ongoing" => "Đang tiến hành",
            "completed" => "Hoàn thành",
            "hiatus" => "Tạm ngưng",
            "cancelled" => "Đã hủy",
            _ => "Không rõ"
        };
    }
    
    public string TranslateLanguage(string lang)
    {
        return lang.ToLower() switch
        {
            "vi" => "Tiếng Việt",
            "en" => "Tiếng Anh",
            "ja" => "Tiếng Nhật",
            "zh" => "Tiếng Trung",
            "ko" => "Tiếng Hàn",
            "fr" => "Tiếng Pháp",
            "es" => "Tiếng Tây Ban Nha",
            "de" => "Tiếng Đức",
            "it" => "Tiếng Ý",
            "ru" => "Tiếng Nga",
            "pt-br" => "Tiếng Bồ Đào Nha (Brazil)",
            "id" => "Tiếng Indonesia",
            "th" => "Tiếng Thái",
            "unknown" => "Không rõ ngôn ngữ",
            "additionalprop1" => "Ngôn ngữ khác (1)",
            "additionalprop2" => "Ngôn ngữ khác (2)",
            "additionalprop3" => "Ngôn ngữ khác (3)",
            _ => lang?.ToUpper() ?? "Không rõ"
        };
    }
    
    public string GetLanguageName(string langCode)
    {
        return TranslateLanguage(langCode);
    }
    
    public string TranslateDemographic(string demographic)
    {
        return demographic.ToLower() switch
        {
            "shounen" => "Shounen (Nam thiếu niên)",
            "shoujo" => "Shoujo (Nữ thiếu niên)",
            "seinen" => "Seinen (Nam thanh niên)",
            "josei" => "Josei (Nữ thanh niên)",
            _ => demographic
        };
    }
    
    public string TranslateContentRating(string rating)
    {
        return rating.ToLower() switch
        {
            "safe" => "An toàn",
            "suggestive" => "Gợi cảm",
            "erotica" => "Khiêu dâm",
            "pornographic" => "Người lớn",
            _ => rating
        };
    }
}
```

---

## Kết luận

Sau khi hoàn thành các bước trên, ứng dụng `MangaReader_WebUI` của bạn sẽ được cập nhật hoàn chỉnh:
- **Form tìm kiếm** sẽ có các bộ lọc cho Tác giả, Họa sĩ và Ngôn ngữ dịch.
- **Trang chi tiết manga** sẽ hiển thị linh động các ngôn ngữ có sẵn và danh sách chương tương ứng.
- **Luồng dữ liệu** từ API đến UI được đảm bảo chính xác với các model và mapper đã được cập nhật.

Hãy kiểm tra kỹ lại các file đã thay đổi và chạy thử ứng dụng để đảm bảo mọi chức năng hoạt động đúng như mong đợi.
```