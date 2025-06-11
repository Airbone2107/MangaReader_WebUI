# TODO.md

## Mục 1: Cập nhật Mapping DTOs (MangaReaderLib -> Cấu trúc trung gian MangaDex)

Cập nhật các file mappers trong `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\` để phù hợp với DTO mới của `MangaReaderLib` và các thay đổi trong cấu trúc response API (ví dụ: `attributes` trong `RelationshipObject`, `tags` nhúng trong `MangaAttributesDto`).

### 1.1. Cập nhật `IMangaReaderLibToMangaViewModelMapper` và `MangaReaderLibToMangaViewModelMapperService.cs`

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToMangaViewModelMapper.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToMangaViewModelMapper.cs
using MangaReader.WebUI.Models;           // Cho MangaViewModel
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Mangas;        // Cho MangaAttributesDto
using System.Threading.Tasks;           // Cho Task

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers
{
    public interface IMangaReaderLibToMangaViewModelMapper
    {
        Task<MangaViewModel> MapToMangaViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, bool isFollowing = false);
    }
}
```

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToMangaViewModelMapperService.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToMangaViewModelMapperService.cs
using MangaReader.WebUI.Models;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.Authors; // Cần cho AuthorAttributesDto (nếu dùng cho relationship attributes)
using MangaReaderLib.DTOs.CoverArts; // Cần cho CoverArtAttributesDto (nếu dùng cho relationship attributes)
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.Extensions.Configuration; // Thêm IConfiguration
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json; // Cần cho JsonSerializer và JsonElement

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaViewModelMapperService : IMangaReaderLibToMangaViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToMangaViewModelMapperService> _logger;
        private readonly IMangaReaderLibCoverApiService _coverApiService; // Vẫn giữ lại nếu cần cho trường hợp GetCoverArtByIdAsync
        private readonly IUserService _userService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly LocalizationService _localizationService;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibToMangaViewModelMapperService(
            ILogger<MangaReaderLibToMangaViewModelMapperService> logger,
            IMangaReaderLibCoverApiService coverApiService,
            IUserService userService,
            IMangaFollowService mangaFollowService,
            LocalizationService localizationService,
            IConfiguration configuration) // Inject IConfiguration
        {
            _logger = logger;
            _coverApiService = coverApiService;
            _userService = userService;
            _mangaFollowService = mangaFollowService;
            _localizationService = localizationService;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') 
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
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
                string description = ""; // Sẽ được lấy từ TranslatedManga, ở đây MangaAttributesDto của Lib không có.
                string coverUrl = "/images/cover-placeholder.jpg";
                string author = "Không rõ";
                string artist = "Không rõ";

                // Cover Art: Nếu `includes[]=cover_art`, `RelationshipObject.Id` sẽ là PublicId.
                var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
                if (coverRelationship != null && !string.IsNullOrEmpty(coverRelationship.Id))
                {
                    // Id của relationship giờ là PublicId
                    coverUrl = $"{_cloudinaryBaseUrl}/{coverRelationship.Id}";
                    _logger.LogDebug("MangaReaderLib Mapper: Cover URL set to {CoverUrl} using PublicId from relationship.", coverUrl);
                }
                else
                {
                    _logger.LogWarning("MangaReaderLib Mapper: No cover_art relationship with PublicId found for manga {MangaId}. Using placeholder.", id);
                }

                // Author/Artist: Nếu `includes[]=author`, `RelationshipObject.Attributes` sẽ chứa name/biography.
                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel.Attributes != null)
                        {
                            // Attributes giờ là object, cần parse
                            try
                            {
                                var relAttributes = JsonSerializer.Deserialize<AuthorAttributesDto>(JsonSerializer.Serialize(rel.Attributes)); // Chuyển từ object sang DTO
                                if (relAttributes != null)
                                {
                                    if (rel.Type == "author") author = relAttributes.Name ?? author;
                                    else if (rel.Type == "artist") artist = relAttributes.Name ?? artist;
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
                
                // Tags: Lấy từ `attributes.Tags`
                List<string> tags = new List<string>();
                if (attributes.Tags != null && attributes.Tags.Any())
                {
                    tags = attributes.Tags
                        .Where(t => t.Attributes != null && !string.IsNullOrEmpty(t.Attributes.Name))
                        .Select(t => t.Attributes.Name) // TagInMangaAttributesDto.Name là string
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
                    PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "",
                    ContentRating = attributes.ContentRating.ToString() ?? "",
                    AlternativeTitles = "", // MangaReaderLib không có altTitles trực tiếp trong MangaAttributesDto
                    LastUpdated = attributes.UpdatedAt,
                    IsFollowing = isFollowing,
                };
            }
            // ... phần catch giữ nguyên
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

### 1.2. Cập nhật `IMangaReaderLibToMangaDetailViewModelMapper` và `MangaReaderLibToMangaDetailViewModelMapperService.cs`

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToMangaDetailViewModelMapper.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToMangaDetailViewModelMapper.cs
// ... giữ nguyên
```

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToMangaDetailViewModelMapperService.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToMangaDetailViewModelMapperService.cs
using MangaReader.WebUI.Models;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.Authors; // Thêm using
using MangaReaderLib.DTOs.CoverArts; // Thêm using
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
// ...
using System.Text.Json; // Thêm using

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaDetailViewModelMapper : IMangaReaderLibToMangaDetailViewModelMapper
    {
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;
        private readonly ILogger<MangaReaderLibToMangaDetailViewModelMapper> _logger;
        private readonly IMangaReaderLibAuthorClient _authorClient; // Giữ nguyên, không cần thiết nếu API trả attributes
        private readonly LocalizationService _localizationService;
        private readonly IMangaReaderLibCoverApiService _coverApiService; // Thêm dịch vụ này
        private readonly IConfiguration _configuration; // Thêm IConfiguration
        private readonly string _cloudinaryBaseUrl;


        public MangaReaderLibToMangaDetailViewModelMapper(
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper,
            ILogger<MangaReaderLibToMangaDetailViewModelMapper> logger,
            IMangaReaderLibAuthorClient authorClient,
            LocalizationService localizationService,
            IMangaReaderLibCoverApiService coverApiService, // Inject
            IConfiguration configuration) // Inject
        {
            _mangaViewModelMapper = mangaViewModelMapper;
            _logger = logger;
            _authorClient = authorClient;
            _localizationService = localizationService;
            _coverApiService = coverApiService; // Gán
            _configuration = configuration;     // Gán
            _cloudinaryBaseUrl = _configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') 
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, List<ChapterViewModel> chapters)
        {
            // ...
            Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaDetailViewModel.");
            Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaDetailViewModel.");
            Debug.Assert(chapters != null, "chapters không được null khi mapping thành MangaDetailViewModel.");

            var attributes = mangaData.Attributes!;
            var relationships = mangaData.Relationships;

            var mangaViewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaData);

            // Description: MangaReaderLib không có description trong MangaAttributesDto.
            // Nó sẽ được lấy từ TranslatedManga bởi service gọi mapper này.
            // Hoặc mapper này có thể gọi API để lấy, nhưng tốt hơn là service chuẩn bị trước.
            // Hiện tại, mangaViewModel.Description sẽ rỗng, và view sẽ xử lý.

            // Author/Artist: API `/mangas/{id}?includes[]=author` sẽ trả về attributes trong relationship.
            string authorName = "Không rõ";
            string artistName = "Không rõ";
            if (relationships != null)
            {
                foreach (var rel in relationships)
                {
                    if (rel.Attributes != null && (rel.Type == "author" || rel.Type == "artist"))
                    {
                        try
                        {
                            // Attributes của relationship đã chứa name, biography
                            var staffAttributes = JsonSerializer.Deserialize<AuthorAttributesDto>(JsonSerializer.Serialize(rel.Attributes));
                            if (staffAttributes != null && !string.IsNullOrEmpty(staffAttributes.Name))
                            {
                                if (rel.Type == "author") authorName = staffAttributes.Name;
                                else if (rel.Type == "artist") artistName = staffAttributes.Name;
                            }
                        }
                        catch (JsonException ex)
                        {
                             _logger.LogWarning(ex, "MangaReaderLib Detail Mapper: Failed to deserialize relationship attributes for manga {MangaId}, type {RelType}.", mangaData.Id, rel.Type);
                        }
                    }
                }
            }
             // Cập nhật lại author/artist nếu mapper cha chưa lấy được (ví dụ không include ở list)
            if (mangaViewModel.Author == "Không rõ" && authorName != "Không rõ") mangaViewModel.Author = authorName;
            if (mangaViewModel.Artist == "Không rõ" && artistName != "Không rõ") mangaViewModel.Artist = artistName;

            // Cover Art cho Detail: `RelationshipObject.Id` là GUID của CoverArt.
            // Cần gọi API để lấy PublicId.
            var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelationship != null && Guid.TryParse(coverRelationship.Id, out Guid coverArtGuid))
            {
                try
                {
                    var coverArtDetailsResponse = await _coverApiService.GetCoverArtByIdAsync(coverArtGuid);
                    if (coverArtDetailsResponse?.Data?.Attributes?.PublicId != null)
                    {
                        mangaViewModel.CoverUrl = $"{_cloudinaryBaseUrl}/{coverArtDetailsResponse.Data.Attributes.PublicId}";
                        _logger.LogDebug("MangaReaderLib Detail Mapper: Cover URL set to {CoverUrl} using PublicId from CoverArt entity {CoverArtGuid}.", mangaViewModel.CoverUrl, coverArtGuid);
                    }
                    else
                    {
                         _logger.LogWarning("MangaReaderLib Detail Mapper: Could not get PublicId for coverArtId {CoverArtId} for manga {MangaId}.", coverArtGuid, mangaData.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MangaReaderLib Detail Mapper: Error fetching cover art details for ID {CoverArtId} for manga {MangaId}.", coverArtGuid, mangaData.Id);
                }
            }
            else
            {
                _logger.LogDebug("MangaReaderLib Detail Mapper: No cover_art relationship with GUID or invalid ID for manga {MangaId}", mangaData.Id);
            }


            // ... (phần còn lại của mapper)
            // MangaReaderLib không có alternative titles, nên dictionary sẽ rỗng
            var alternativeTitlesByLanguage = new Dictionary<string, List<string>>();

            // Convert enums to Vietnamese strings for display
            mangaViewModel.Status = _localizationService.GetStatus(attributes.Status.ToString());
            mangaViewModel.PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "";
            mangaViewModel.ContentRating = attributes.ContentRating.ToString() ?? "";

            // Tags đã được xử lý trong _mangaViewModelMapper

            return new MangaDetailViewModel
            {
                Manga = mangaViewModel,
                Chapters = chapters ?? new List<ChapterViewModel>(),
                AlternativeTitlesByLanguage = alternativeTitlesByLanguage
            };
        }
    }
}
```

### 1.3. Cập nhật `IMangaReaderLibToAtHomeServerResponseMapper` và `MangaReaderLibToAtHomeServerResponseMapperService.cs`

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToAtHomeServerResponseMapper.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToAtHomeServerResponseMapper.cs
// ... giữ nguyên
```

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToAtHomeServerResponseMapperService.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToAtHomeServerResponseMapperService.cs
using MangaReader.WebUI.Models.Mangadex; // Cho AtHomeServerResponse
using MangaReaderLib.DTOs.Common;        // Cho ApiCollectionResponse, ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterPageAttributesDto
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Thêm using
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToAtHomeServerResponseMapper : IMangaReaderLibToAtHomeServerResponseMapper
    {
        private readonly ILogger<MangaReaderLibToAtHomeServerResponseMapper> _logger;
        private readonly string _cloudinaryBaseUrl; // Thêm Cloudinary base URL

        public MangaReaderLibToAtHomeServerResponseMapper(
            ILogger<MangaReaderLibToAtHomeServerResponseMapper> logger,
            IConfiguration configuration) // Inject IConfiguration
        {
            _logger = logger;
            // Lấy Cloudinary base URL từ configuration
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') 
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public AtHomeServerResponse MapToAtHomeServerResponse(
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored) // mangaReaderLibBaseUrl không còn cần thiết
        {
            Debug.Assert(chapterPagesData != null, "chapterPagesData không được null khi mapping.");
            Debug.Assert(!string.IsNullOrEmpty(chapterId), "chapterId không được rỗng.");

            var pages = new List<string>();
            if (chapterPagesData.Data != null && chapterPagesData.Data.Any())
            {
                // Sắp xếp các trang theo PageNumber
                var sortedPages = chapterPagesData.Data.OrderBy(p => p.Attributes.PageNumber);

                foreach (var pageDto in sortedPages)
                {
                    if (pageDto?.Attributes?.PublicId != null)
                    {
                        // Tạo URL đầy đủ từ Cloudinary base URL và PublicId
                        var imageUrl = $"{_cloudinaryBaseUrl}/{pageDto.Attributes.PublicId}";
                        pages.Add(imageUrl);
                        _logger.LogDebug("Mapped MangaReaderLib page: ChapterId={ChapterId}, PageNumber={PageNumber}, PublicId={PublicId} to Cloudinary URL: {ImageUrl}",
                            chapterId, pageDto.Attributes.PageNumber, pageDto.Attributes.PublicId, imageUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Skipping page due to missing PublicId. ChapterId={ChapterId}, PageDtoId={PageDtoId}", chapterId, pageDto?.Id);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No page data found in chapterPagesData for ChapterId={ChapterId}", chapterId);
            }

            return new AtHomeServerResponse
            {
                Result = "ok",
                // BaseUrl không còn cần thiết vì pages đã là URL đầy đủ
                BaseUrl = "", 
                Chapter = new AtHomeChapterData
                {
                    Hash = chapterId, // Sử dụng chapterId làm hash
                    Data = pages,
                    DataSaver = pages // Hiện tại dùng chung, có thể cần logic khác nếu có dataSaver
                }
            };
        }
    }
}
```

### 1.4. Cập nhật `IMangaReaderLibToTagListResponseMapper` và `MangaReaderLibToTagListResponseMapperService.cs`

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToTagListResponseMapper.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers\IMangaReaderLibToTagListResponseMapper.cs
// ... giữ nguyên
```

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToTagListResponseMapperService.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToTagListResponseMapperService.cs
using MangaReader.WebUI.Models.Mangadex; // Cho TagListResponse, Tag, TagAttributes
using MangaReaderLib.DTOs.Common;        // Cho ApiCollectionResponse, ResourceObject
using MangaReaderLib.DTOs.Tags;          // Cho TagAttributesDto
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;                      // Cho Any
using System.Collections.Generic;       // Cho Dictionary, List

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToTagListResponseMapper : IMangaReaderLibToTagListResponseMapper
    {
        private readonly ILogger<MangaReaderLibToTagListResponseMapper> _logger;

        public MangaReaderLibToTagListResponseMapper(ILogger<MangaReaderLibToTagListResponseMapper> logger)
        {
            _logger = logger;
        }

        public TagListResponse MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsDataFromLib)
        {
            Debug.Assert(tagsDataFromLib != null, "tagsDataFromLib không được null khi mapping thành TagListResponse.");

            var tagListResponse = new TagListResponse
            {
                Result = tagsDataFromLib.Result,
                Response = tagsDataFromLib.ResponseType, // Hoặc "collection"
                Limit = tagsDataFromLib.Limit,
                Offset = tagsDataFromLib.Offset,
                Total = tagsDataFromLib.Total,
                Data = new List<Tag>()
            };

            if (tagsDataFromLib.Data != null)
            {
                foreach (var libTagResource in tagsDataFromLib.Data)
                {
                    if (libTagResource?.Attributes != null)
                    {
                        try
                        {
                            var libTagAttributes = libTagResource.Attributes;
                            
                            // Tạo MangaDex.TagAttributes
                            var dexTagAttributes = new TagAttributes
                            {
                                // MangaReaderLib TagAttributesDto.Name là string
                                // MangaDex TagAttributes.Name là Dictionary<string, string>
                                // Giả sử tên từ MangaReaderLib là tiếng Anh (en)
                                Name = new Dictionary<string, string> { { "en", libTagAttributes.Name } },
                                Description = new Dictionary<string, string>(), // MangaReaderLib TagAttributesDto không có description
                                Group = libTagAttributes.TagGroupName?.ToLowerInvariant() ?? "other", // Map TagGroupName sang Group
                                Version = 1 // Giá trị mặc định
                            };

                            var dexTag = new Tag
                            {
                                Id = Guid.Parse(libTagResource.Id),
                                Type = "tag", // Loại cố định cho MangaDex
                                Attributes = dexTagAttributes
                            };
                            tagListResponse.Data.Add(dexTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi mapping MangaReaderLib Tag ID {TagId} sang MangaDex Tag ViewModel.", libTagResource.Id);
                            continue;
                        }
                    }
                }
            }
            return tagListResponse;
        }
    }
}
```

## Mục 2: Cập nhật Hàm Gọi API và Strategies

### 2.1. Cập nhật `IMangaClient` (MangaReaderLib)

File: `MangaReaderLib\Services\Interfaces\IMangaClient.cs`
```csharp
// MangaReaderLib\Services\Interfaces\IMangaClient.cs
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Enums;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Manga API endpoints
    /// </summary>
    public interface IMangaClient
    {
        /// <summary>
        /// Lấy danh sách manga với các tùy chọn lọc và phân trang
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, 
            int? limit = null, 
            string? titleFilter = null, 
            string? statusFilter = null, // Sẽ là string thay vì MangaStatus enum để linh hoạt
            string? contentRatingFilter = null, // Sẽ là string thay vì ContentRating enum
            List<PublicationDemographic>? publicationDemographicsFilter = null, // Giữ lại List<Enum>
            string? originalLanguageFilter = null,
            int? yearFilter = null,
            List<Guid>? authorIdsFilter = null,
            List<Guid>? includedTags = null,     // Thêm
            string? includedTagsMode = null,   // Thêm
            List<Guid>? excludedTags = null,     // Thêm
            string? excludedTagsMode = null,   // Thêm
            string? orderBy = null, 
            bool? ascending = null,
            List<string>? includes = null,       // Thêm
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một manga dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId,
            List<string>? includes = null,       // Thêm
            CancellationToken cancellationToken = default);
        
        // ... các phương thức khác giữ nguyên
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(
            CreateMangaRequestDto request, 
            CancellationToken cancellationToken = default);
        
        Task UpdateMangaAsync(
            Guid mangaId, 
            UpdateMangaRequestDto request, 
            CancellationToken cancellationToken = default);
        
        Task DeleteMangaAsync(
            Guid mangaId, 
            CancellationToken cancellationToken = default);

        Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(
            Guid mangaId, 
            int? offset = null, 
            int? limit = null, 
            CancellationToken cancellationToken = default);
        
        Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(
            Guid mangaId, 
            Stream imageStream, 
            string fileName, 
            string? volume = null, 
            string? description = null, 
            CancellationToken cancellationToken = default);
        
        Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(
            Guid mangaId, 
            int? offset = null, 
            int? limit = null,
            string? orderBy = null, 
            bool? ascending = null,
            CancellationToken cancellationToken = default);
    }
}
```

### 2.2. Cập nhật `MangaClient.cs` (MangaReaderLib)

File: `MangaReaderLib\Services\Implementations\MangaClient.cs`
```csharp
// MangaReaderLib\Services\Implementations\MangaClient.cs
// ...
using MangaReaderLib.Enums;

namespace MangaReaderLib.Services.Implementations
{
    public class MangaClient : IMangaClient
    {
        // ... constructor và helper methods giữ nguyên

        public async Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, int? limit = null, string? titleFilter = null,
            string? statusFilter = null, string? contentRatingFilter = null, // Đã đổi sang string
            List<PublicationDemographic>? publicationDemographicsFilter = null,
            string? originalLanguageFilter = null, int? yearFilter = null,
            List<Guid>? authorIdsFilter = null,
            List<Guid>? includedTags = null,     // Thêm
            string? includedTagsMode = null,   // Thêm
            List<Guid>? excludedTags = null,     // Thêm
            string? excludedTagsMode = null,   // Thêm
            string? orderBy = null, bool? ascending = null,
            List<string>? includes = null,       // Thêm
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mangas with various filters.");

            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "titleFilter", titleFilter);
            AddQueryParam(queryParams, "statusFilter", statusFilter); // Truyền trực tiếp string
            AddQueryParam(queryParams, "contentRatingFilter", contentRatingFilter); // Truyền trực tiếp string

            if (publicationDemographicsFilter != null && publicationDemographicsFilter.Any())
            {
                 AddListQueryParam(queryParams, "publicationDemographicsFilter[]", publicationDemographicsFilter.Select(e => e.ToString()).ToList());
            }
            
            AddQueryParam(queryParams, "originalLanguageFilter", originalLanguageFilter);
            AddQueryParam(queryParams, "yearFilter", yearFilter?.ToString());

            if (authorIdsFilter != null && authorIdsFilter.Any())
            {
                AddListQueryParam(queryParams, "authorIdsFilter[]", authorIdsFilter.Select(id => id.ToString()).ToList());
            }

            // Xử lý filters tag nâng cao
            if (includedTags != null && includedTags.Any())
            {
                AddListQueryParam(queryParams, "includedTags[]", includedTags.Select(id => id.ToString()).ToList());
                if (!string.IsNullOrEmpty(includedTagsMode))
                {
                    AddQueryParam(queryParams, "includedTagsMode", includedTagsMode);
                }
            }
            if (excludedTags != null && excludedTags.Any())
            {
                 AddListQueryParam(queryParams, "excludedTags[]", excludedTags.Select(id => id.ToString()).ToList());
                if (!string.IsNullOrEmpty(excludedTagsMode))
                {
                    AddQueryParam(queryParams, "excludedTagsMode", excludedTagsMode);
                }
            }

            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());

            // Xử lý includes
            if (includes != null && includes.Any())
            {
                AddListQueryParam(queryParams, "includes[]", includes);
            }

            string requestUri = BuildQueryString("Mangas", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId, 
            List<string>? includes = null, // Thêm includes
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting manga by ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            if (includes != null && includes.Any())
            {
                AddListQueryParam(queryParams, "includes[]", includes);
            }
            string requestUri = BuildQueryString($"Mangas/{mangaId}", queryParams);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        // ... các phương thức khác giữ nguyên
        // ...
        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(CreateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new manga: {Title}", request.Title);
            return await _apiClient.PostAsync<CreateMangaRequestDto, ApiResponse<ResourceObject<MangaAttributesDto>>>("Mangas", request, cancellationToken);
        }

        public async Task UpdateMangaAsync(Guid mangaId, UpdateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating manga with ID: {MangaId}", mangaId);
            await _apiClient.PutAsync($"Mangas/{mangaId}", request, cancellationToken);
        }

        public async Task DeleteMangaAsync(Guid mangaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting manga with ID: {MangaId}", mangaId);
            await _apiClient.DeleteAsync($"Mangas/{mangaId}", cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(Guid mangaId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting covers for manga with ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            string requestUri = BuildQueryString($"mangas/{mangaId}/covers", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(Guid mangaId, Stream imageStream, string fileName, string? volume = null, string? description = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading cover for manga with ID: {MangaId}, Filename: {FileName}", mangaId, fileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "file", fileName);
            if (!string.IsNullOrEmpty(volume))
            {
                content.Add(new StringContent(volume), "volume");
            }
            if (!string.IsNullOrEmpty(description))
            {
                content.Add(new StringContent(description), "description");
            }
            
            return await _apiClient.PostAsync<ApiResponse<ResourceObject<CoverArtAttributesDto>>>($"mangas/{mangaId}/covers", content, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(Guid mangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting translations for manga with ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            string requestUri = BuildQueryString($"mangas/{mangaId}/translations", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>>(requestUri, cancellationToken);
        }
    }
}
```

### 2.3. Cập nhật `MangaReaderLibMangaSourceStrategy.cs`

File: `MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibMangaSourceStrategy.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibMangaSourceStrategy.cs
// ...
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.Enums; // Cho PublicationDemographic
using MangaReaderLib.DTOs.Authors; // Cho AuthorAttributesDto
using System.Text.Json; // Cho JsonSerializer

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibMangaSourceStrategy : IMangaApiSourceStrategy
    {
        // ... constructor và các dependencies khác giữ nguyên

        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] TitleFilter: {TitleFilter}", sortManga?.Title);
            
            List<PublicationDemographic>? publicationDemographics = null;
            if (sortManga?.Demographic != null && sortManga.Demographic.Any())
            {
                publicationDemographics = new List<PublicationDemographic>();
                foreach (var demoStr in sortManga.Demographic)
                {
                    if (Enum.TryParse<PublicationDemographic>(demoStr, true, out var pubDemo))
                    {
                        publicationDemographics.Add(pubDemo);
                    }
                }
            }

            // Map ContentRating của MangaDex (List<string>) sang string của MangaReaderLib (lấy phần tử đầu tiên nếu có)
            string? contentRatingFilter = null;
            if (sortManga?.ContentRating != null && sortManga.ContentRating.Any())
            {
                // MangaReaderLib ContentRating enum không có "None", "All".
                // Nếu UI gửi "safe", "suggestive", "erotica", "pornographic" thì map.
                var validRatings = sortManga.ContentRating
                    .Where(r => Enum.TryParse<ContentRating>(r, true, out _))
                    .ToList();
                if (validRatings.Any())
                {
                     // MangaReaderLib API chỉ nhận 1 content rating filter
                    contentRatingFilter = validRatings.First();
                }
            }


            var libResult = await _mangaClient.GetMangasAsync(
                offset: offset,
                limit: limit,
                titleFilter: sortManga?.Title,
                statusFilter: sortManga?.Status?.FirstOrDefault(),
                contentRatingFilter: contentRatingFilter, // Sử dụng contentRatingFilter đã map
                publicationDemographicsFilter: publicationDemographics,
                originalLanguageFilter: sortManga?.OriginalLanguage?.FirstOrDefault(),
                yearFilter: sortManga?.Year,
                authorIdsFilter: sortManga?.Authors?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(), // authors là ID trong SortManga
                includedTags: sortManga?.IncludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                includedTagsMode: sortManga?.IncludedTagsMode,
                excludedTags: sortManga?.ExcludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                excludedTagsMode: sortManga?.ExcludedTagsMode,
                orderBy: sortManga?.SortBy, // Cần mapping logic nếu SortBy của UI khác API
                ascending: sortManga?.SortBy switch { "title" => true, _ => false }, // Mặc định desc cho các trường hợp khác
                includes: new List<string> { "cover_art", "author" } // Luôn yêu cầu includes
            );

            // ... (phần còn lại của hàm FetchMangaAsync giữ nguyên, bao gồm cả mapping)
            if (libResult?.Data == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchMangaAsync] API returned null or null Data.");
                return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Total = 0 };
            }
             _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] Received {Count} manga DTOs from MangaReaderLib.", libResult.Data.Count);


            var mappedData = new List<Manga>();
            foreach (var dto in libResult.Data)
            {
                if (dto == null || dto.Attributes == null) continue;
                var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(dto);
                if (mangaDexModel != null)
                {
                    mappedData.Add(mangaDexModel);
                }
            }
             _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] Successfully mapped {MappedCount} manga DTOs.", mappedData.Count);

            return new MangaList
            {
                Result = "ok",
                Response = "collection",
                Data = mappedData,
                Limit = libResult.Limit,
                Offset = libResult.Offset,
                Total = libResult.Total
            };
        }

        public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaDetailsAsync] ID: {MangaId}", mangaId);
            if (!Guid.TryParse(mangaId, out var guidMangaId)) return null;

            // Yêu cầu include author, artist
            var libResponse = await _mangaClient.GetMangaByIdAsync(guidMangaId, new List<string> { "author" });
            if (libResponse?.Data == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchMangaDetailsAsync] Failed to get manga details for ID {MangaId}", mangaId);
                return null;
            }

            var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(libResponse.Data, true); // isDetails = true
            if (mangaDexModel == null) return null;
            
            return new MangaResponse { Result = "ok", Response = "entity", Data = mangaDexModel };
        }
        
        // Hàm MapMangaReaderLibDtoToMangaDexModel cần được cập nhật để xử lý attributes trong relationship
        private async Task<Manga?> MapMangaReaderLibDtoToMangaDexModel(global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Mangas.MangaAttributesDto> dto, bool isDetails = false)
        {
            if (dto == null || dto.Attributes == null) return null;

            _logger.LogDebug("[MRLib Strategy Mapper] Mapping MangaReaderLib DTO ID: {MangaId}, Title: {Title}. IsDetails: {IsDetails}", dto.Id, dto.Attributes.Title, isDetails);

            var relationshipsDex = new List<Relationship>();
            var mangaDexTags = new List<MangaReader.WebUI.Models.Mangadex.Tag>();

            // Cover Art
            var coverRelLib = dto.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelLib != null)
            {
                string? publicIdForCover = null;
                Guid coverArtGuid = Guid.Empty;

                if (isDetails && Guid.TryParse(coverRelLib.Id, out coverArtGuid)) // Khi get detail, ID là GUID của CoverArt
                {
                     try
                    {
                        var coverArtDetailsResponse = await _coverApiService.GetCoverArtByIdAsync(coverArtGuid);
                        publicIdForCover = coverArtDetailsResponse?.Data?.Attributes?.PublicId;
                         _logger.LogDebug("[MRLib Strategy Mapper] Cover Art (Details): Fetched PublicId '{PublicId}' for CoverArt GUID '{CoverArtGuid}'", publicIdForCover, coverArtGuid);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[MRLib Strategy Mapper] Cover Art (Details): Error fetching cover art details for GUID {CoverArtGuid}", coverArtGuid);
                    }
                }
                else if (!isDetails && !string.IsNullOrEmpty(coverRelLib.Id)) // Khi get list + include, ID là PublicId
                {
                    publicIdForCover = coverRelLib.Id;
                    _logger.LogDebug("[MRLib Strategy Mapper] Cover Art (List): Using PublicId '{PublicId}' directly from relationship ID.", publicIdForCover);
                }
                
                if (!string.IsNullOrEmpty(publicIdForCover))
                {
                    var mangaDexCoverAttributes = new CoverAttributes 
                    { 
                        FileName = publicIdForCover, // MangaDex dùng FileName, ta sẽ gán publicId vào đây
                        // Các thuộc tính khác của CoverAttributes có thể null hoặc lấy từ coverArtDetailsResponse nếu isDetails
                        Volume = isDetails && coverArtGuid != Guid.Empty ? (await _coverApiService.GetCoverArtByIdAsync(coverArtGuid))?.Data?.Attributes?.Volume : null,
                        Description = isDetails && coverArtGuid != Guid.Empty ? (await _coverApiService.GetCoverArtByIdAsync(coverArtGuid))?.Data?.Attributes?.Description : null,
                        CreatedAt = DateTimeOffset.UtcNow, // Giá trị mặc định
                        UpdatedAt = DateTimeOffset.UtcNow, // Giá trị mặc định
                        Version = 1 
                    };
                    relationshipsDex.Add(new Relationship
                    {
                        Id = coverArtGuid != Guid.Empty ? coverArtGuid : (Guid.TryParse(coverRelLib.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid()), // Dùng GUID nếu có, nếu không thì parse ID của rel, fallback NewGuid
                        Type = "cover_art",
                        Attributes = mangaDexCoverAttributes
                    });
                }
                else
                {
                     _logger.LogWarning("[MRLib Strategy Mapper] Cover Art: Could not determine PublicId for cover art for MangaId {MangaId}. Relationship ID: {RelId}", dto.Id, coverRelLib.Id);
                }
            }
            else
            {
                _logger.LogDebug("[MRLib Strategy Mapper] No cover_art relationship or invalid ID for manga {MangaId}", dto.Id);
            }

            // Author/Artist
            var staffRelsLib = dto.Relationships?.Where(r => r.Type == "author" || r.Type == "artist").ToList() ?? new();
            foreach (var staffRelLib in staffRelsLib)
            {
                if (Guid.TryParse(staffRelLib.Id, out var staffIdGuid))
                {
                    string staffName = "Không rõ";
                    string? staffBio = null;

                    if (staffRelLib.Attributes != null) // API đã include attributes
                    {
                        try
                        {
                            var libStaffAttrs = JsonSerializer.Deserialize<MangaReaderLib.DTOs.Authors.AuthorAttributesDto>(JsonSerializer.Serialize(staffRelLib.Attributes));
                            if (libStaffAttrs != null)
                            {
                                staffName = libStaffAttrs.Name ?? staffName;
                                staffBio = libStaffAttrs.Biography;
                                _logger.LogDebug("[MRLib Strategy Mapper] Staff (from included attributes): Type '{Type}', Name '{Name}' for MangaId {MangaId}", staffRelLib.Type, staffName, dto.Id);
                            }
                        }
                        catch (JsonException ex)
                        {
                             _logger.LogWarning(ex, "[MRLib Strategy Mapper] Staff: Failed to deserialize included attributes for staff ID {StaffId}, Type {StaffType}", staffRelLib.Id, staffRelLib.Type);
                        }
                    }
                    else if (isDetails) // Nếu là trang detail và API không include, thử gọi API phụ
                    {
                         try
                        {
                            var staffDetails = await _authorClient.GetAuthorByIdAsync(staffIdGuid);
                            if (staffDetails?.Data?.Attributes != null)
                            {
                                staffName = staffDetails.Data.Attributes.Name ?? staffName;
                                staffBio = staffDetails.Data.Attributes.Biography;
                                 _logger.LogDebug("[MRLib Strategy Mapper] Staff (from API call): Type '{Type}', Name '{Name}' for MangaId {MangaId}", staffRelLib.Type, staffName, dto.Id);
                            } else _logger.LogWarning("[MRLib Strategy Mapper] Staff: Could not get details for staffId {StaffId} for MangaId {MangaId}", staffIdGuid, dto.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[MRLib Strategy Mapper] Staff: Error fetching staff details for ID {StaffId} for MangaId {MangaId}", staffIdGuid, dto.Id);
                        }
                    }

                    var mangaDexStaffAttributes = new MangaReader.WebUI.Models.Mangadex.AuthorAttributes 
                    { 
                        Name = staffName,
                        Biography = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", staffBio ?? "" } },
                        CreatedAt = DateTimeOffset.UtcNow, // Mặc định
                        UpdatedAt = DateTimeOffset.UtcNow, // Mặc định
                        Version = 1 
                    };
                    relationshipsDex.Add(new Relationship
                    {
                        Id = staffIdGuid, 
                        Type = staffRelLib.Type,
                        Attributes = mangaDexStaffAttributes
                    });
                } else _logger.LogWarning("[MRLib Strategy Mapper] Invalid staff ID format: {StaffId} for MangaId {MangaId}", staffRelLib.Id, dto.Id);
            }

            // Tags: `dto.Attributes.Tags` đã chứa `List<ResourceObject<TagInMangaAttributesDto>>`
            if (dto.Attributes.Tags != null && dto.Attributes.Tags.Any())
            {
                foreach (var libTagResource in dto.Attributes.Tags)
                {
                    if (libTagResource?.Attributes != null && Guid.TryParse(libTagResource.Id, out var tagIdGuid))
                    {
                        var mdTagAttrs = new MangaReader.WebUI.Models.Mangadex.TagAttributes 
                        { 
                            Name = new Dictionary<string, string> { { "en", libTagResource.Attributes.Name } },
                            Group = libTagResource.Attributes.TagGroupName?.ToLowerInvariant() ?? "other", 
                            Version = 1 
                        };
                        // Không thêm vào relationshipsDex vì MangaDex model không có tag trong relationship
                        mangaDexTags.Add(new MangaReader.WebUI.Models.Mangadex.Tag { Id = tagIdGuid, Type = "tag", Attributes = mdTagAttrs });
                    }
                }
                 _logger.LogDebug("[MRLib Strategy Mapper] Processed {TagCount} tags for manga {MangaId}.", mangaDexTags.Count, dto.Id);
            }


            string description = "Mô tả sẽ được tải ở trang chi tiết.";
            if (isDetails) 
            {
                 description = "Không có mô tả.";
                try
                {
                    var translations = await _mangaClient.GetMangaTranslationsAsync(Guid.Parse(dto.Id));
                    var preferredTranslation = translations?.Data?.FirstOrDefault(t => 
                        (t.Attributes?.LanguageKey?.Equals("en", StringComparison.OrdinalIgnoreCase) ?? false) || 
                        (t.Attributes?.LanguageKey?.Equals(dto.Attributes.OriginalLanguage, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (t.Attributes?.LanguageKey?.Equals("vi", StringComparison.OrdinalIgnoreCase) ?? false) );
                    
                    if (preferredTranslation?.Attributes?.Description != null)
                    {
                        description = preferredTranslation.Attributes.Description;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MRLib Strategy Mapper] Error fetching translations for description for MangaId {MangaId}", dto.Id);
                }
            }

            return new Manga
            {
                Id = Guid.Parse(dto.Id),
                Type = "manga",
                Attributes = new MangaAttributes
                {
                    Title = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", dto.Attributes.Title } },
                    AltTitles = new List<Dictionary<string, string>>(),
                    Description = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", description } },
                    Status = dto.Attributes.Status.ToString(),
                    ContentRating = dto.Attributes.ContentRating.ToString(),
                    OriginalLanguage = dto.Attributes.OriginalLanguage,
                    PublicationDemographic = dto.Attributes.PublicationDemographic?.ToString(),
                    Year = dto.Attributes.Year,
                    IsLocked = dto.Attributes.IsLocked,
                    CreatedAt = dto.Attributes.CreatedAt,
                    UpdatedAt = dto.Attributes.UpdatedAt,
                    Version = 1,
                    Tags = mangaDexTags.Any() ? mangaDexTags : null,
                },
                Relationships = relationshipsDex.Any() ? relationshipsDex : null
            };
        }

        // ... các phương thức khác của strategy (FetchMangaByIdsAsync) giữ nguyên ...
    }
}
```

### 2.4. Cập nhật `MangaReaderLibChapterSourceStrategy.cs`

File: `MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibChapterSourceStrategy.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibChapterSourceStrategy.cs
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json; // Thêm using

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibChapterSourceStrategy : IChapterApiSourceStrategy
    {
        // ... constructor và dependencies giữ nguyên

        public async Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchChapterInfoAsync] Chapter ID: {ChapterId}", chapterId);
            if (!Guid.TryParse(chapterId, out var guidChapterId))
            {
                _logger.LogError("[MRLib Strategy->FetchChapterInfoAsync] Invalid Chapter ID format: {ChapterId}", chapterId);
                return null;
            }

            var libResponse = await _chapterClient.GetChapterByIdAsync(guidChapterId);
            if (libResponse?.Data?.Attributes == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] Could not get chapter details from MangaReaderLib for ID {ChapterId}. Response: {ResponseResult}", chapterId, libResponse?.Result);
                return null;
            }

            string determinedLanguage = "en"; // Fallback

            var tmRelationship = libResponse.Data.Relationships?.FirstOrDefault(r => r.Type.Equals("translated_manga", StringComparison.OrdinalIgnoreCase));
            if (tmRelationship != null && Guid.TryParse(tmRelationship.Id, out var tmGuid))
            {
                var tmDetails = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                if (!string.IsNullOrEmpty(tmDetails?.Data?.Attributes?.LanguageKey))
                {
                    determinedLanguage = tmDetails.Data.Attributes.LanguageKey;
                }
                // ... (phần fallback to original language giữ nguyên)
                else
                {
                    _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] Could not determine translated language for chapter {ChapterId} from TranslatedManga {TmId}. TM Response: {TmResponse}. Falling back to original manga language.", chapterId, tmGuid, tmDetails?.Result);
                    await FallbackToMangaOriginalLanguage(libResponse.Data, chapterId, determinedLanguage, (newLang) => determinedLanguage = newLang);
                }
            }
            else
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] No 'translated_manga' relationship found or invalid ID for chapter {ChapterId}. TM Relationship ID: {TmRelId}. Falling back to original manga language.", chapterId, tmRelationship?.Id);
                await FallbackToMangaOriginalLanguage(libResponse.Data, chapterId, determinedLanguage, (newLang) => determinedLanguage = newLang);
            }


            var chapterViewModel = _chapterViewModelMapper.MapToChapterViewModel(libResponse.Data, determinedLanguage);

            var mangaDexRelationships = new List<MangaReader.WebUI.Models.Mangadex.Relationship>();
            if (libResponse.Data.Relationships != null && libResponse.Data.Relationships.Any())
            {
                foreach (var rel in libResponse.Data.Relationships)
                {
                    if (rel == null || string.IsNullOrEmpty(rel.Type) || string.IsNullOrEmpty(rel.Id)) continue;

                    if (rel.Type.Equals("manga", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(rel.Id, out Guid mangaGuid))
                    {
                        mangaDexRelationships.Add(new MangaReader.WebUI.Models.Mangadex.Relationship { Id = mangaGuid, Type = "manga" });
                    }
                    else if (rel.Type.Equals("user", StringComparison.OrdinalIgnoreCase)) // MangaDex User ID là GUID, MRLib User ID là int
                    {
                        // Bỏ qua user relationship nếu không map được sang GUID, hoặc tạo GUID giả nếu cần
                        // Hiện tại bỏ qua để tránh lỗi
                        _logger.LogDebug("[MRLib Strategy->FetchChapterInfoAsync] Skipping 'user' relationship for MRLib mapping as UserID format differs. MRLib UserID: {UserId}", rel.Id);
                    }
                    // Các relationship khác của MangaReaderLib như translated_manga không map trực tiếp sang MangaDex Chapter relationships
                }
            }
            
            var chapterAttributes = new MangaReader.WebUI.Models.Mangadex.ChapterAttributes
            {
                Title = chapterViewModel.Title,
                Volume = libResponse.Data.Attributes.Volume,
                ChapterNumber = libResponse.Data.Attributes.ChapterNumber,
                Pages = libResponse.Data.Attributes.PagesCount,
                TranslatedLanguage = determinedLanguage, 
                PublishAt = chapterViewModel.PublishedAt,
                ReadableAt = chapterViewModel.PublishedAt, // Giả định giống PublishAt
                CreatedAt = libResponse.Data.Attributes.CreatedAt,
                UpdatedAt = libResponse.Data.Attributes.UpdatedAt,
                Version = 1 // Giá trị mặc định
            };

            if (libResponse.Data.Relationships != null)
            {
                // Ví dụ: Lấy Manga ID từ relationship để gán vào MangaDex Chapter Attributes nếu cần
                var mangaRel = libResponse.Data.Relationships.FirstOrDefault(r => r.Type == "manga");
                if (mangaRel != null && Guid.TryParse(mangaRel.Id, out Guid uploaderGuidPlaceholder)) // MRLib UserID là int, không phải GUID
                {
                    // chapterAttributes.Uploader = uploaderGuidPlaceholder; // MangaDex chapter.attributes.uploader là GUID
                }
            }


            return new ChapterResponse
            {
                Result = "ok",
                Response = "entity",
                Data = new MangaReader.WebUI.Models.Mangadex.Chapter
                {
                    Id = Guid.Parse(chapterViewModel.Id),
                    Type = "chapter",
                    Attributes = chapterAttributes,
                    Relationships = mangaDexRelationships.Any() ? mangaDexRelationships : null
                }
            };
        }
        // ... (các phương thức FetchChaptersAsync, FallbackToMangaOriginalLanguage, FetchChapterPagesAsync giữ nguyên)
        // ...
        public async Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            _logger.LogInformation("[MRLib Strategy->FetchChaptersAsync] Manga ID: {MangaId}, Languages: {Languages}", mangaId, languages);
            if (!Guid.TryParse(mangaId, out var guidMangaId))
            {
                _logger.LogError("[MRLib Strategy->FetchChaptersAsync] Invalid Manga ID format: {MangaId}", mangaId);
                return null;
            }

            var targetLanguages = languages.Split(',').Select(l => l.Trim().ToLowerInvariant()).Where(l => !string.IsNullOrEmpty(l)).ToList();
            if (!targetLanguages.Any()) return new ChapterList { Result = "ok", Data = new List<Chapter>() };

            var translatedMangasResponse = await _mangaClient.GetMangaTranslationsAsync(guidMangaId, limit: 100);
            if (translatedMangasResponse?.Data == null || !translatedMangasResponse.Data.Any())
            {
                _logger.LogWarning("[MRLib Strategy->FetchChaptersAsync] No translations found for Manga ID {MangaId}", mangaId);
                return new ChapterList { Result = "ok", Data = new List<Chapter>() };
            }

            var allChaptersFromLib = new List<global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Chapters.ChapterAttributesDto>>();
            string foundLanguageKey = "";

            var mangaDetails = await _mangaClient.GetMangaByIdAsync(guidMangaId);
            var originalLang = mangaDetails?.Data?.Attributes?.OriginalLanguage?.ToLowerInvariant();
            var languagesToTry = new List<string>();
            languagesToTry.AddRange(targetLanguages);
            if (!string.IsNullOrEmpty(originalLang) && !languagesToTry.Contains(originalLang)) languagesToTry.Add(originalLang);
            if (!languagesToTry.Contains("en")) languagesToTry.Add("en");

            _logger.LogDebug("[MRLib Strategy->FetchChaptersAsync] Languages to try for manga {MangaId}: [{Languages}]", mangaId, string.Join(",", languagesToTry));

            foreach (var langKey in languagesToTry)
            {
                var translatedManga = translatedMangasResponse.Data.FirstOrDefault(tm => tm.Attributes.LanguageKey.Equals(langKey, StringComparison.OrdinalIgnoreCase));
                if (translatedManga != null && Guid.TryParse(translatedManga.Id, out var tmGuid))
                {
                    var chaptersResponse = await _chapterClient.GetChaptersByTranslatedMangaAsync(tmGuid, orderBy: "ChapterNumber", ascending: order == "asc", limit: maxChapters ?? 500);
                    if (chaptersResponse?.Data != null && chaptersResponse.Data.Any())
                    {
                        _logger.LogInformation("[MRLib Strategy->FetchChaptersAsync] Found {Count} chapters for lang {LanguageKey}", chaptersResponse.Data.Count, langKey);
                        allChaptersFromLib.AddRange(chaptersResponse.Data);
                        foundLanguageKey = langKey;
                        if (targetLanguages.Contains(langKey)) break; 
                    }
                }
            }

            if (!allChaptersFromLib.Any())
            {
                _logger.LogWarning("[MRLib Strategy->FetchChaptersAsync] No chapters found for manga {MangaId} after trying all languages.", mangaId);
                return new ChapterList { Result = "ok", Data = new List<Chapter>(), Total = 0 };
            }
            if (string.IsNullOrEmpty(foundLanguageKey)) foundLanguageKey = "en"; 

            var mappedChapters = allChaptersFromLib
                .Select(dto => _chapterViewModelMapper.MapToChapterViewModel(dto, foundLanguageKey))
                .Select(vm => new Chapter // Map ChapterViewModel sang MangaDex.Chapter
                {
                    Id = Guid.TryParse(vm.Id, out var chapterGuid) ? chapterGuid : Guid.NewGuid(),
                    Type = "chapter",
                    Attributes = new ChapterAttributes { 
                        Title = vm.Title, 
                        Volume = vm.Number, // Hoặc logic để tách volume
                        ChapterNumber = vm.Number, 
                        Pages = 0, // MRLib không trả pages count ở list chapter
                        TranslatedLanguage = vm.Language, 
                        PublishAt = vm.PublishedAt, 
                        ReadableAt = vm.PublishedAt, 
                        CreatedAt = vm.PublishedAt, // Giả định
                        UpdatedAt = vm.PublishedAt, // Giả định
                        Version = 1 
                    },
                    Relationships = vm.Relationships?.Select(r => new Relationship { Id = Guid.TryParse(r.Id, out var relGuid) ? relGuid : Guid.NewGuid(), Type = r.Type }).ToList() ?? new List<Relationship>()
                }).ToList();
            _logger.LogInformation("[MRLib Strategy->FetchChaptersAsync] Successfully mapped {Count} chapters.", mappedChapters.Count);

            return new ChapterList { Result = "ok", Response = "collection", Data = mappedChapters, Limit = mappedChapters.Count, Total = mappedChapters.Count };
        }

        private async Task FallbackToMangaOriginalLanguage(global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Chapters.ChapterAttributesDto> chapterData, string chapterId, string currentDeterminedLanguage, Action<string> setLanguageAction)
        {
            var mangaRelationship = chapterData.Relationships?.FirstOrDefault(r => r.Type.Equals("manga", StringComparison.OrdinalIgnoreCase));
            if (mangaRelationship != null && Guid.TryParse(mangaRelationship.Id, out var mangaGuid))
            {
                _logger.LogInformation("[MRLib Strategy->FetchChapterInfoAsync] Attempting to get original language for MangaId {MangaId} as fallback for chapter {ChapterId}.", mangaGuid, chapterId);
                var mangaDetails = await _mangaClient.GetMangaByIdAsync(mangaGuid);
                if (!string.IsNullOrEmpty(mangaDetails?.Data?.Attributes?.OriginalLanguage))
                {
                    setLanguageAction(mangaDetails.Data.Attributes.OriginalLanguage);
                    _logger.LogInformation("[MRLib Strategy->FetchChapterInfoAsync] Using original manga language '{OriginalLanguage}' as fallback for chapter {ChapterId}.", mangaDetails.Data.Attributes.OriginalLanguage, chapterId);
                    return;
                }
                else
                {
                    _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] Original manga language not found for MangaId {MangaId}. Falling back to 'en' for chapter {ChapterId}.", mangaGuid, chapterId);
                }
            }
            else
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] 'manga' relationship not found or invalid for chapter {ChapterId} during language fallback. Falling back to 'en'.", chapterId);
            }
            setLanguageAction("en"); // Fallback cuối cùng
        }

        public async Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchChapterPagesAsync] Chapter ID: {ChapterId}", chapterId);
            if (!Guid.TryParse(chapterId, out var guidChapterId)) return null;

            var pagesResponse = await _chapterPageClient.GetChapterPagesAsync(guidChapterId, limit: 500); 
            if (pagesResponse == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterPagesAsync] GetChapterPagesAsync returned null for ChapterId: {ChapterId}", chapterId);
                return null;
            }
            _logger.LogInformation("[MRLib Strategy->FetchChapterPagesAsync] Received {Count} page DTOs for ChapterId: {ChapterId}", pagesResponse.Data?.Count ?? 0, chapterId);

            return _atHomeResponseMapper.MapToAtHomeServerResponse(pagesResponse, chapterId, _mangaReaderLibApiBaseUrl);
        }
    }
}
```

### 2.5. Cập nhật `MangaReaderLibCoverSourceStrategy.cs` (nếu cần)
File: `MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibCoverSourceStrategy.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibCoverSourceStrategy.cs
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibCoverSourceStrategy : ICoverApiSourceStrategy
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibCoverApiService _coverApiService; // Để dùng GetCoverArtUrl
        private readonly ILogger<MangaReaderLibCoverSourceStrategy> _logger;
        private readonly string _cloudinaryBaseUrl; // Cloudinary base URL


        public MangaReaderLibCoverSourceStrategy(
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibCoverApiService coverApiService, // Inject
            IConfiguration configuration,
            ILogger<MangaReaderLibCoverSourceStrategy> logger)
        {
            _mangaClient = mangaClient;
            _coverApiService = coverApiService; // Gán
            _logger = logger;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                 ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Manga ID: {MangaId}", mangaId);
            if (!Guid.TryParse(mangaId, out var guidMangaId)) return null;

            var libResponse = await _mangaClient.GetMangaCoversAsync(guidMangaId, limit: 100);
            if (libResponse?.Data == null) return new CoverList { Result = "ok", Data = new List<Cover>()};
            
            _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Received {Count} cover DTOs.", libResponse.Data.Count);

            var mappedCovers = libResponse.Data.Select(dto => new Cover
            {
                Id = Guid.TryParse(dto.Id, out var coverGuid) ? coverGuid : Guid.NewGuid(), 
                Type = "cover_art",
                Attributes = new CoverAttributes { 
                    FileName = dto.Attributes.PublicId, // FileName của MangaDex model sẽ chứa PublicId từ MRLib
                    Volume = dto.Attributes.Volume, 
                    Description = dto.Attributes.Description, 
                    // Locale không có trong MRLib CoverArtAttributesDto, có thể bỏ qua hoặc đặt giá trị mặc định
                    Locale = null, 
                    CreatedAt = dto.Attributes.CreatedAt, 
                    UpdatedAt = dto.Attributes.UpdatedAt, 
                    Version = 1 
                }
            }).ToList();
             _logger.LogInformation("[MRLib Strategy->GetAllCoversAsync] Successfully mapped {Count} covers.", mappedCovers.Count);

            return new CoverList { Result = "ok", Response = "collection", Data = mappedCovers, Limit = libResponse.Limit, Offset = libResponse.Offset, Total = libResponse.Total };
        }

        // fileName ở đây được truyền vào là PublicId từ logic trước đó (ví dụ: từ relationship của Manga)
        public string GetCoverUrl(string mangaIdIgnored, string publicId, int size = 512)
        {
            _logger.LogDebug("[MRLib Strategy->GetCoverUrl] PublicId: {PublicId}, Size: {Size}", publicId, size);

            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("[MRLib Strategy->GetCoverUrl] PublicId rỗng, trả về placeholder.");
                return "/images/cover-placeholder.jpg";
            }
            
            // Sử dụng _cloudinaryBaseUrl đã inject
            // Không cần gọi _coverApiService.GetCoverArtUrl nữa vì đã có PublicId
            string cloudinaryUrl = $"{_cloudinaryBaseUrl}/{publicId}"; 
            
            // Thêm transform cho size nếu cần (ví dụ: /w_512,h_auto,c_limit/)
            // Hiện tại không thêm transform, để View tự xử lý nếu cần.
            // if (size > 0)
            // {
            //     cloudinaryUrl = $"{_cloudinaryBaseUrl}/w_{size},c_limit/{publicId}";
            // }


            _logger.LogInformation("[MRLib Strategy->GetCoverUrl] Constructed Cloudinary URL: {Url}", cloudinaryUrl);
            return cloudinaryUrl;
        }
    }
}
```

### 2.6. Cập nhật `MangaReaderLibTagSourceStrategy.cs`
File: `MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibTagSourceStrategy.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibTagSourceStrategy.cs
// ... File này không cần thay đổi logic gọi API, chỉ cần mapper đã được cập nhật.
// Giữ nguyên nội dung của file này.
```

## Mục 3: Cập nhật `MangaDataExtractorService.cs`

Xem xét lại `MangaDataExtractorService.cs` để đảm bảo logic trích xuất dữ liệu (cover URL, author/artist, tags) hoạt động đúng với cấu trúc `MangaDex.Manga` (model trung gian) đã được map từ DTO của MangaReaderLib.

File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs`
```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs
// ...
using MangaReader.WebUI.Models.Mangadex; // Đảm bảo using này cho CoverAttributes, AuthorAttributes của MangaDex
// ...

public class MangaDataExtractorService : IMangaDataExtractor
{
    // ... constructor và GetCurrentMangaSource giữ nguyên

    public string ExtractCoverUrl(string mangaId, List<Relationship>? relationships)
    {
        Debug.Assert(!string.IsNullOrEmpty(mangaId), "Manga ID không được rỗng khi trích xuất Cover URL.");
        try
        {
            var currentSource = GetCurrentMangaSource();
            _logger.LogDebug("ExtractCoverUrl: Source = {Source}, MangaId = {MangaId}", currentSource, mangaId);

            if (relationships == null || !relationships.Any())
            {
                _logger.LogWarning("ExtractCoverUrl: Danh sách relationships rỗng hoặc null cho manga ID: {MangaId}. Sử dụng placeholder.", mangaId);
                return "/images/cover-placeholder.jpg";
            }

            var coverRelationship = relationships.FirstOrDefault(r => r != null && r.Type == "cover_art");

            if (coverRelationship == null)
            {
                _logger.LogWarning("ExtractCoverUrl: Không tìm thấy relationship 'cover_art' cho manga ID: {MangaId}. Sử dụng placeholder.", mangaId);
                return "/images/cover-placeholder.jpg";
            }
            
            // Logic mới: Model trung gian MangaDex.Relationship.Attributes sẽ chứa MangaDex.CoverAttributes
            // Và MangaDex.CoverAttributes.FileName sẽ chứa PublicId (nếu nguồn là MRLib) hoặc tên file (nếu nguồn là MangaDex)
            if (coverRelationship.Attributes is CoverAttributes dexCoverAttributes && !string.IsNullOrEmpty(dexCoverAttributes.FileName))
            {
                if (currentSource == MangaSource.MangaDex)
                {
                    // MangaDex: FileName là tên file thực sự
                    var originalImageUrl = $"https://uploads.mangadex.org/covers/{mangaId}/{dexCoverAttributes.FileName}.512.jpg";
                    var proxiedUrl = $"{_backendApiBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
                    _logger.LogDebug("ExtractCoverUrl (MangaDex): Proxied URL = {Url}", proxiedUrl);
                    return proxiedUrl;
                }
                else // currentSource == MangaSource.MangaReaderLib
                {
                    // MangaReaderLib: FileName (trong MangaDex.CoverAttributes) đã được gán PublicId
                    var cloudinaryUrl = $"{_cloudinaryBaseUrl}/{dexCoverAttributes.FileName}";
                    _logger.LogDebug("ExtractCoverUrl (MangaReaderLib): Direct Cloudinary URL = {Url}", cloudinaryUrl);
                    return cloudinaryUrl;
                }
            }
            // Fallback nếu Attributes không phải là CoverAttributes hoặc FileName rỗng (ít khả năng xảy ra nếu mapper đúng)
            else if (currentSource == MangaSource.MangaReaderLib && !string.IsNullOrEmpty(coverRelationship.Id.ToString()) && !Guid.TryParse(coverRelationship.Id.ToString(), out _))
            {
                // Trường hợp cho MangaReaderLib khi lấy list, ID của relationship là PublicID
                var publicIdFromRelId = coverRelationship.Id.ToString();
                 _logger.LogDebug("ExtractCoverUrl (MangaReaderLib - Fallback to Rel ID): Using PublicId '{PublicId}' from relationship ID for manga {MangaId}.", publicIdFromRelId, mangaId);
                return $"{_cloudinaryBaseUrl}/{publicIdFromRelId}";
            }


            _logger.LogWarning("ExtractCoverUrl: Không thể trích xuất filename/publicId từ attributes của cover_art cho manga ID {MangaId}. Relationship ID: {RelationshipId}. Attributes Type: {AttributeType}",
                mangaId, coverRelationship.Id, coverRelationship.Attributes?.GetType().Name ?? "null");
            return "/images/cover-placeholder.jpg";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi trích xuất Cover URL cho manga ID: {mangaId}");
            return "/images/cover-placeholder.jpg";
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

                // Logic mới: Model trung gian MangaDex.Relationship.Attributes sẽ chứa MangaDex.AuthorAttributes
                if (rel.Attributes is MangaReader.WebUI.Models.Mangadex.AuthorAttributes dexAuthorAttributes)
                {
                    name = dexAuthorAttributes.Name ?? "Không rõ";
                }
                else if (rel.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object) // Fallback cho JsonElement nếu có
                {
                     if (attributesElement.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                    {
                        name = nameElement.GetString() ?? "Không rõ";
                    }
                }


                if (relType == "author") author = name;
                else if (relType == "artist") artist = name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi trích xuất tác giả/họa sĩ từ relationships.");
        }
        return (author, artist);
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
            // Logic hiện tại đã đúng vì nó duyệt qua List<MangaDex.Tag>
            // Mapper từ MangaReaderLib DTO đã chuyển đổi tên tag thành Dictionary {"en": "TagName"}
            // Và TagGroupName thành Group.
            foreach (var tag in tagsList)
            {
                if (tag?.Attributes?.Name == null) continue;

                string tagNameEn = tag.Attributes.Name.TryGetValue("en", out var en) ? en : 
                                   tag.Attributes.Name.FirstOrDefault().Value; // Lấy tên đầu tiên nếu không có 'en'

                if (!string.IsNullOrEmpty(tagNameEn))
                {
                    if (_tagTranslations.TryGetValue(tagNameEn, out var translation))
                    {
                        translatedTags.Add(translation);
                    }
                    else
                    {
                        translatedTags.Add(tagNameEn);
                    }
                }
            }
            return translatedTags.Distinct().OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), false)).ToList();
        }
        // ... phần catch giữ nguyên
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi trích xuất và dịch tags manga.");
            return new List<string>();
        }
    }
    // ... các phương thức khác giữ nguyên
    // ...
    public string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
    {
        Debug.Assert(titleDict != null || altTitlesList != null, "Phải có ít nhất titleDict hoặc altTitlesList để trích xuất tiêu đề.");

        try
        {
            // Ưu tiên Tiếng Việt từ AltTitles trước
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

            // Nếu không có Tiếng Việt trong AltTitles, thử Tiếng Anh trong Title chính
            if (titleDict != null && titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
            {
                return enTitle;
            }

            // Nếu không có Tiếng Anh, thử Tiếng Việt trong Title chính
             if (titleDict != null && titleDict.TryGetValue("vi", out var mainViTitle) && !string.IsNullOrEmpty(mainViTitle))
            {
                return mainViTitle;
            }

            // Nếu không có, lấy tiêu đề đầu tiên từ Title chính
            if (titleDict != null && titleDict.Any())
            {
                return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
            }

            // Nếu Title chính rỗng, thử lấy tiêu đề đầu tiên từ AltTitles
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
            // Ưu tiên tiếng Việt, sau đó đến tiếng Anh
            if (descriptionDict.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
            {
                return viDesc;
            }
            if (descriptionDict.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
            {
                return enDesc;
            }

            // Lấy giá trị đầu tiên nếu không có vi/en
            return descriptionDict.FirstOrDefault().Value ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi trích xuất mô tả manga.");
            return "";
        }
    }

    public string ExtractAndTranslateStatus(string? status)
    {
        // Sử dụng LocalizationService để dịch
        return _localizationService.GetStatus(status);
    }

     public string ExtractChapterDisplayTitle(ChapterAttributes attributes)
    {
        Debug.Assert(attributes != null, "ChapterAttributes không được null khi trích xuất Display Title.");

        string chapterNumber = attributes.ChapterNumber ?? "?"; // Lấy số chapter, mặc định là "?"
        string chapterTitle = attributes.Title ?? ""; // Lấy tiêu đề, mặc định là rỗng

        // Trường hợp đặc biệt cho Oneshot hoặc khi không có số chapter
        if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
        {
            return !string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot";
        }

        // Format tiêu đề chuẩn
        string displayTitle = $"Chương {chapterNumber}";
        if (!string.IsNullOrEmpty(chapterTitle) && chapterTitle != chapterNumber)
        {
            displayTitle += $": {chapterTitle}";
        }

        return displayTitle;
    }

    public string ExtractChapterNumber(ChapterAttributes attributes)
    {
         Debug.Assert(attributes != null, "ChapterAttributes không được null khi trích xuất Chapter Number.");
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
                    // Giả định mỗi dictionary con chỉ có một cặp key-value (lang-title)
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

        // Ưu tiên tiếng Anh
        if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
        // Sau đó là romaji
        if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First();
        // Lấy cái đầu tiên nếu không có
        return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
    }


    // Helper khởi tạo từ điển dịch tag
    private static Dictionary<string, string> InitializeTagTranslations()
    {
        // (Giữ nguyên từ điển dịch từ MangaTagService cũ)
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
```