# TODO: Cập nhật Client Projects theo Thay đổi API Backend

## Mục tiêu

Cập nhật các project client (`MangaReaderLib`, `MangaReader_ManagerUI`, `MangaReader_WebUI`) để tương thích với những thay đổi mới nhất từ Backend API được mô tả trong `ClientAPI_Update.md`. Đồng thời, tối ưu hóa các luồng gọi API hiện tại.

## Thứ tự cập nhật

1.  `MangaReaderLib`
2.  `MangaReader_ManagerUI`
3.  `MangaReader_WebUI`

---

## I. Cập nhật `MangaReaderLib`

### 1. Cập nhật các DTOs

#### 1.1. `MangaReaderLib\DTOs\Mangas\MangaAttributesDto.cs`

Thay đổi liên quan đến việc `tags` được nhúng trực tiếp vào `attributes`.

```csharp
// MangaReaderLib\DTOs\Mangas\MangaAttributesDto.cs
using MangaReaderLib.Enums;
using MangaReaderLib.DTOs.Tags; // Thêm using cho TagAttributesDto
using MangaReaderLib.DTOs.Common; // Thêm using cho ResourceObject

namespace MangaReaderLib.DTOs.Mangas
{
    public class MangaAttributesDto
    {
        public string Title { get; set; } = string.Empty;
        public string OriginalLanguage { get; set; } = string.Empty;
        public PublicationDemographic? PublicationDemographic { get; set; }
        public MangaStatus Status { get; set; }
        public int? Year { get; set; }
        public ContentRating ContentRating { get; set; }
        public bool IsLocked { get; set; }
        
        // Thêm thuộc tính Tags
        public List<ResourceObject<TagAttributesDto>> Tags { get; set; } = new List<ResourceObject<TagAttributesDto>>();
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
```

#### 1.2. `MangaReaderLib\DTOs\Common\RelationshipObject.cs`

Thêm trường `Attributes` để có thể chứa thông tin chi tiết của entity liên quan.

```csharp
// MangaReaderLib\DTOs\Common\RelationshipObject.cs
using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class RelationshipObject
    {
        [JsonPropertyName("id")]
        [JsonPropertyOrder(1)]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [JsonPropertyOrder(2)]
        public string Type { get; set; } = string.Empty;

        // Thêm trường Attributes
        // Kiểu `object?` cho phép chứa bất kỳ DTO attributes nào (ví dụ: AuthorAttributesDto)
        // hoặc null nếu không có attributes được include.
        [JsonPropertyName("attributes")]
        [JsonPropertyOrder(3)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Attributes { get; set; }
    }
}
```

#### 1.3. `MangaReaderLib\DTOs\Authors\AuthorAttributesDto.cs`

DTO này đã có, chỉ đảm bảo nó được sử dụng đúng khi `RelationshipObject` chứa `attributes` của `author` hoặc `artist`.

```csharp
// MangaReaderLib\DTOs\Authors\AuthorAttributesDto.cs
namespace MangaReaderLib.DTOs.Authors
{
    public class AuthorAttributesDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
// Không cần thay đổi file này.
```

### 2. Cập nhật Client Services

#### 2.1. `MangaReaderLib\Services\Interfaces\IMangaClient.cs`

Cập nhật phương thức `GetMangasAsync` để chấp nhận các tham số lọc mới.

```csharp
// MangaReaderLib\Services\Interfaces\IMangaClient.cs
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;

namespace MangaReaderLib.Services.Interfaces
{
    public interface IMangaClient
    {
        Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null,
            int? limit = null,
            string? titleFilter = null,
            string? statusFilter = null,
            string? contentRatingFilter = null,
            // Loại bỏ demographicFilter, thay bằng publicationDemographicsFilter
            // string? demographicFilter = null, 
            List<string>? publicationDemographicsFilter = null, // MỚI: Cho phép nhiều giá trị
            string? originalLanguageFilter = null,
            int? yearFilter = null,
            List<Guid>? includedTags = null,      // MỚI
            string? includedTagsMode = null,   // MỚI ("AND" | "OR")
            List<Guid>? excludedTags = null,      // MỚI
            string? excludedTagsMode = null,   // MỚI ("AND" | "OR")
            List<Guid>? authorIdsFilter = null,
            string? orderBy = null,
            bool? ascending = null,
            List<string>? includes = null, // MỚI: để request author, artist, cover_art attributes
            CancellationToken cancellationToken = default);

        // ... các phương thức khác giữ nguyên ...
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId,
            List<string>? includes = null, // MỚI: để request author, artist attributes
            CancellationToken cancellationToken = default);
        
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

#### 2.2. `MangaReaderLib\Services\Implementations\MangaClient.cs`

Cập nhật triển khai của `GetMangasAsync` và `GetMangaByIdAsync`.

```csharp
// MangaReaderLib\Services\Implementations\MangaClient.cs
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MangaReaderLib.Services.Implementations
{
    public class MangaClient : IMangaClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<MangaClient> _logger;

        public MangaClient(IApiClient apiClient, ILogger<MangaClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ... (BuildQueryString và AddQueryParam giữ nguyên) ...
        private string BuildQueryString(string baseUri, Dictionary<string, List<string>> queryParams)
        {
            var queryString = new StringBuilder();
            if (queryParams != null && queryParams.Any())
            {
                bool firstParam = true;
                foreach (var param in queryParams)
                {
                    if (param.Value != null && param.Value.Any())
                    {
                        foreach (var value in param.Value)
                        {
                            if (string.IsNullOrEmpty(value)) continue;

                            if (firstParam)
                            {
                                queryString.Append("?");
                                firstParam = false;
                            }
                            else
                            {
                                queryString.Append("&");
                            }
                            queryString.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
                        }
                    }
                }
            }
            return $"{baseUri}{queryString}";
        }
        
        private void AddQueryParam(Dictionary<string, List<string>> queryParams, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].Add(value);
            }
        }

        public async Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, int? limit = null, string? titleFilter = null,
            string? statusFilter = null, string? contentRatingFilter = null,
            List<string>? publicationDemographicsFilter = null, // THAY ĐỔI
            string? originalLanguageFilter = null, int? yearFilter = null,
            List<Guid>? includedTags = null, string? includedTagsMode = null, // MỚI
            List<Guid>? excludedTags = null, string? excludedTagsMode = null, // MỚI
            List<Guid>? authorIdsFilter = null,
            string? orderBy = null, bool? ascending = null,
            List<string>? includes = null, // MỚI
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mangas with filters: Title={TitleFilter}, Status={StatusFilter}, ContentRating={ContentRatingFilter}",
                titleFilter, statusFilter, contentRatingFilter);

            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "titleFilter", titleFilter);
            AddQueryParam(queryParams, "statusFilter", statusFilter);
            AddQueryParam(queryParams, "contentRatingFilter", contentRatingFilter);
            AddQueryParam(queryParams, "originalLanguageFilter", originalLanguageFilter);
            AddQueryParam(queryParams, "yearFilter", yearFilter?.ToString());

            // Xử lý publicationDemographicsFilter (array)
            if (publicationDemographicsFilter != null && publicationDemographicsFilter.Any())
            {
                queryParams["publicationDemographicsFilter[]"] = publicationDemographicsFilter;
            }

            // Xử lý includedTags (array) và includedTagsMode
            if (includedTags != null && includedTags.Any())
            {
                queryParams["includedTags[]"] = includedTags.Select(id => id.ToString()).ToList();
                if (!string.IsNullOrEmpty(includedTagsMode))
                {
                    AddQueryParam(queryParams, "includedTagsMode", includedTagsMode);
                }
            }

            // Xử lý excludedTags (array) và excludedTagsMode
            if (excludedTags != null && excludedTags.Any())
            {
                queryParams["excludedTags[]"] = excludedTags.Select(id => id.ToString()).ToList();
                if (!string.IsNullOrEmpty(excludedTagsMode))
                {
                    AddQueryParam(queryParams, "excludedTagsMode", excludedTagsMode);
                }
            }
            
            if (authorIdsFilter != null && authorIdsFilter.Any())
            {
                // API dùng authorIdsFilter[] nhưng client gửi là List<Guid> và HttpClient sẽ xử lý
                queryParams["authorIdsFilter[]"] = authorIdsFilter.Select(id => id.ToString()).ToList();
            }
            
            if (includes != null && includes.Any())
            {
                 queryParams["includes[]"] = includes;
            }

            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());

            string requestUri = BuildQueryString("Mangas", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId, 
            List<string>? includes = null, // MỚI
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting manga by ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            if (includes != null && includes.Any())
            {
                queryParams["includes[]"] = includes;
            }
            string requestUri = BuildQueryString($"Mangas/{mangaId}", queryParams);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        // ... (CreateMangaAsync, UpdateMangaAsync, DeleteMangaAsync, GetMangaCoversAsync, UploadMangaCoverAsync, GetMangaTranslationsAsync giữ nguyên) ...
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

---

## II. Cập nhật `MangaReader_ManagerUI`

### 1. Cập nhật Types (`src/types/manga.ts`)

Phản ánh thay đổi trong `MangaAttributes` và `RelationshipObject`.

```typescript
// src/types/manga.ts
import { RelationshipObject as ApiRelationshipObject, ResourceObject as ApiResourceObject } from './api' // Giả sử bạn đã có ApiResourceObject
import { TagAttributes } from './manga'; // Thêm TagAttributes từ chính nó

// ... (các interface khác giữ nguyên) ...

export interface MangaAttributes {
  title: string
  originalLanguage: string
  publicationDemographic: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null // Cho phép null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  tags: ApiResourceObject<TagAttributes>[] // THAY ĐỔI: tags giờ là mảng các ResourceObject<TagAttributes>
  createdAt: string
  updatedAt: string
}

// Cập nhật RelationshipObject để có thể chứa attributes
export interface RelationshipObject extends ApiRelationshipObject {
  // id: string; // Đã có từ ApiRelationshipObject
  // type: string; // Đã có từ ApiRelationshipObject
  attributes?: any; // Hoặc cụ thể hơn: AuthorAttributes | ArtistAttributes | null
  name?: string;    // Thêm name để dễ dàng hiển thị, sẽ được populate bởi store/component
}

// ... (các interface còn lại) ...

export interface AuthorAttributes {
  name: string
  biography?: string
  createdAt: string
  updatedAt: string
}

export interface TagAttributes {
  name: string
  tagGroupId: string 
  tagGroupName: string 
  createdAt: string
  updatedAt: string
}

// ... (giữ nguyên các DTOs khác: TagGroupAttributes, TranslatedMangaAttributes, ChapterAttributes, ...)
// ... (ChapterPageAttributes, CoverArtAttributes, Manga, Author, Tag, ...)
// ... (CreateMangaRequest, UpdateMangaRequest, MangaAuthorInput, ...)
// ... (CreateAuthorRequest, UpdateAuthorRequest, CreateTagRequest, UpdateTagRequest, ...)
// ... (CreateTagGroupRequest, UpdateTagGroupRequest, CreateTranslatedMangaRequest, UpdateTranslatedMangaRequest, ...)
// ... (CreateChapterRequest, UpdateChapterRequest, CreateChapterPageEntryRequest, UpdateChapterPageDetailsRequest, ...)
// ... (UploadCoverArtRequest, SelectedRelationship)

// Đảm bảo các interface sau được giữ lại hoặc cập nhật nếu cần:
export interface TagGroup {
  id: string
  type: 'tag_group'
  attributes: TagGroupAttributes
  relationships?: RelationshipObject[]
}

export interface TagGroupAttributes {
  name: string
  createdAt: string
  updatedAt: string
}

export interface TranslatedManga {
  id: string
  type: 'translated_manga'
  attributes: TranslatedMangaAttributes
  relationships?: RelationshipObject[]
}

export interface TranslatedMangaAttributes {
  languageKey: string
  title: string
  description?: string
  createdAt: string
  updatedAt: string
}

export interface Chapter {
  id: string
  type: 'chapter'
  attributes: ChapterAttributes
  relationships?: RelationshipObject[]
}

export interface ChapterAttributes {
  volume?: string
  chapterNumber?: string
  title?: string
  pagesCount: number
  publishAt: string
  readableAt: string
  createdAt: string
  updatedAt: string
}

export interface ChapterPage {
  id: string
  type: 'chapter_page'
  attributes: ChapterPageAttributes
  relationships?: RelationshipObject[]
}

export interface ChapterPageAttributes {
  pageNumber: number
  publicId: string
}

export interface CoverArt {
  id: string
  type: 'cover_art'
  attributes: CoverArtAttributes
  relationships?: RelationshipObject[]
}

export interface CoverArtAttributes {
  volume?: string
  publicId: string
  description?: string
  createdAt: string
  updatedAt: string
}

export interface Manga {
  id: string
  type: 'manga'
  attributes: MangaAttributes
  relationships?: RelationshipObject[]
  coverArtPublicId?: string
}

export interface Author {
  id: string
  type: 'author' | 'artist' // Có thể là author hoặc artist
  attributes: AuthorAttributes
  relationships?: RelationshipObject[]
}

export interface Tag { // Đây là MangaDex Tag, không phải TagAttributesDto từ backend
  id: string
  type: 'tag'
  attributes: { // MangaDex TagAttributes có cấu trúc khác
    name: { [key: string]: string }; // Ví dụ: { en: "Action", vi: "Hành động" }
    description: { [key: string]: string };
    group: string; // "content", "format", "genre", "theme"
    version: number;
  }
  // MangaDex Tag không có relationships trong cấu trúc trả về thông thường
}


// Request DTOs
export interface CreateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  tagIds?: string[]
  authors?: MangaAuthorInput[]
}

export interface UpdateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  tagIds?: string[]
  authors?: MangaAuthorInput[]
}

export interface MangaAuthorInput {
  authorId: string
  role: 'Author' | 'Artist'
}

export interface CreateAuthorRequest {
  name: string
  biography?: string
}

export interface UpdateAuthorRequest {
  name: string
  biography?: string
}

export interface CreateTagRequest {
  name: string
  tagGroupId: string
}

export interface UpdateTagRequest {
  name: string
  tagGroupId: string
}

export interface CreateTagGroupRequest {
  name: string
}

export interface UpdateTagGroupRequest {
  name: string
}

export interface CreateTranslatedMangaRequest {
  mangaId: string
  languageKey: string
  title: string
  description?: string
}

export interface UpdateTranslatedMangaRequest {
  languageKey: string
  title: string
  description?: string
}

export interface CreateChapterRequest {
  translatedMangaId: string
  uploadedByUserId: number
  volume?: string
  chapterNumber?: string
  title?: string
  publishAt: string // ISO 8601 datetime string
  readableAt: string // ISO 8601 datetime string
}

export interface UpdateChapterRequest {
  volume?: string
  chapterNumber?: string
  title?: string
  publishAt: string // ISO 8601 datetime string
  readableAt: string // ISO 8601 datetime string
}

export interface CreateChapterPageEntryRequest {
  pageNumber: number
}

export interface UpdateChapterPageDetailsRequest {
  pageNumber: number
}

export interface UploadCoverArtRequest {
  file: File;
  volume?: string;
  description?: string;
}

export interface SelectedRelationship {
  id: string;
  name: string;
  role?: 'Author' | 'Artist';
  tagGroupName?: string; // Thêm cho tags
}
```

### 2. Cập nhật API Client (`src/api/mangaApi.js`)

Điều chỉnh hàm `getMangas` để gửi các tham số filter mới.

```javascript
// src/api/mangaApi.js
import apiClient from './apiClient';

// ... (typedefs giữ nguyên hoặc cập nhật nếu cần) ...
/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').Manga>} MangaCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').Manga>} MangaSingleResponse
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').CoverArt>} CoverArtCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').CoverArt>} CoverArtSingleResponse
 * @typedef {import('../types/manga').CreateMangaRequest} CreateMangaRequest
 * @typedef {import('../types/manga').UpdateMangaRequest} UpdateMangaRequest
 * @typedef {import('../types/manga').UploadCoverArtRequest} UploadCoverArtRequest
 */

const BASE_URL = '/Mangas';

const mangaApi = {
  /**
   * Lấy danh sách manga.
   * @param {object} params - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.titleFilter]
   * @param {string} [params.statusFilter]
   * @param {string} [params.contentRatingFilter]
   * // @param {string} [params.demographicFilter] - ĐÃ BỎ
   * @param {string[]} [params.publicationDemographicsFilter] - MỚI: array of strings
   * @param {string} [params.originalLanguageFilter]
   * @param {number} [params.yearFilter]
   * // @param {string[]} [params.tagIdsFilter] - ĐÃ BỎ, thay bằng includedTags/excludedTags
   * @param {string[]} [params.includedTags] - MỚI: array of GUIDs
   * @param {'AND' | 'OR'} [params.includedTagsMode] - MỚI
   * @param {string[]} [params.excludedTags] - MỚI: array of GUIDs
   * @param {'AND' | 'OR'} [params.excludedTagsMode] - MỚI
   * @param {string[]} [params.authorIdsFilter] - array of GUIDs
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @param {string[]} [params.includes] - MỚI: array of strings (e.g., ['author', 'artist', 'cover_art'])
   * @returns {Promise<MangaCollectionResponse>}
   */
  getMangas: async (params) => {
    // Xử lý params cho array (axios sẽ tự động thêm [] nếu cần cho một số backend)
    // Tuy nhiên, ASP.NET Core Model Binder có thể xử lý params lặp lại.
    // Đối với publicationDemographicsFilter, includedTags, excludedTags, authorIdsFilter, includes
    // Client sẽ truyền ví dụ: publicationDemographicsFilter=Shounen&publicationDemographicsFilter=Seinen
    // Hoặc nếu thư viện HTTP client hỗ trợ, nó có thể gửi dưới dạng publicationDemographicsFilter[]=Shounen&publicationDemographicsFilter[]=Seinen
    // Trong MangaReaderLib, MangaClient đã xử lý việc này bằng cách thêm [] vào tên key khi giá trị là List.
    // => Ở đây, chỉ cần đảm bảo truyền đúng tên key mà không cần thêm [] ở client JS này, 
    //    vì ASP.NET Core Proxy Controller (MangaReader_ManagerUI.Server) sẽ nhận đúng và MangaReaderLib sẽ xử lý.

    const response = await apiClient.get(BASE_URL, { params });
    return response.data;
  },

  /**
   * Lấy thông tin manga theo ID.
   * @param {string} id - ID của manga.
   * @param {object} [queryParams] - Tham số truy vấn tùy chọn.
   * @param {string[]} [queryParams.includes] - MỚI: array of strings (e.g., ['author', 'artist'])
   * @returns {Promise<MangaSingleResponse>}
   */
  getMangaById: async (id, queryParams) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`, { params: queryParams });
    return response.data;
  },

  // ... (các phương thức khác giữ nguyên) ...
  createManga: async (data) => {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  updateManga: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  deleteManga: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },
  
  getMangaCovers: async (mangaId, params) => {
    const response = await apiClient.get(`/mangas/${mangaId}/covers`, { params });
    return response.data;
  },

  uploadMangaCover: async (mangaId, data) => {
    const formData = new FormData();
    formData.append('file', data.file);
    if (data.volume) {
      formData.append('volume', data.volume);
    }
    if (data.description) {
      formData.append('description', data.description);
    }

    const response = await apiClient.post(`/mangas/${mangaId}/covers`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

export default mangaApi;
```

### 3. Cập nhật Zustand Store (`src/stores/mangaStore.js`)

#### 3.1. Định nghĩa State và Filters

```javascript
// src/stores/mangaStore.js
import { create } from 'zustand'
// ... (các imports khác)
import { RELATIONSHIP_TYPES } from '../constants/appConstants' // Thêm nếu chưa có
import coverArtApi from '../api/coverArtApi'; // Thêm nếu chưa có

// ...

const useMangaStore = create(persistStore((set, get) => ({
  // ... (state hiện tại)
  mangas: [],
  totalMangas: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    titleFilter: '',
    statusFilter: '',
    contentRatingFilter: '',
    // demographicFilter: '', // BỎ
    publicationDemographicsFilter: [], // MỚI: array
    originalLanguageFilter: '',
    yearFilter: null,
    // tagIdsFilter: [], // BỎ
    includedTags: [], // MỚI: array of tag IDs
    includedTagsMode: 'AND', // MỚI: default 'AND'
    excludedTags: [], // MỚI: array of tag IDs
    excludedTagsMode: 'OR', // MỚI: default 'OR'
    authorIdsFilter: [],
  },
  sort: {
    orderBy: 'updatedAt',
    ascending: false,
  },
  // Thêm state để lưu trữ danh sách includes cho API
  mangaIncludes: ['author', 'artist', 'cover_art'], // Mặc định lấy thông tin này

  fetchMangas: async (resetPagination = false) => {
    const { page, rowsPerPage, filters, sort, mangaIncludes } = get(); // Lấy mangaIncludes
    const offset = resetPagination ? 0 : page * rowsPerPage;

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      titleFilter: filters.titleFilter || undefined,
      statusFilter: filters.statusFilter || undefined,
      contentRatingFilter: filters.contentRatingFilter || undefined,
      // publicationDemographicFilter sẽ được xử lý bởi backend nếu là array
      publicationDemographicsFilter: filters.publicationDemographicsFilter?.length > 0 ? filters.publicationDemographicsFilter : undefined,
      originalLanguageFilter: filters.originalLanguageFilter || undefined,
      yearFilter: filters.yearFilter || undefined,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
      includes: mangaIncludes?.length > 0 ? mangaIncludes : undefined, // Thêm includes
    };

    // Xử lý các filter dạng array
    if (filters.includedTags && filters.includedTags.length > 0) {
      queryParams.includedTags = filters.includedTags;
      queryParams.includedTagsMode = filters.includedTagsMode;
    }
    if (filters.excludedTags && filters.excludedTags.length > 0) {
      queryParams.excludedTags = filters.excludedTags;
      queryParams.excludedTagsMode = filters.excludedTagsMode;
    }
    if (filters.authorIdsFilter && filters.authorIdsFilter.length > 0) {
      queryParams.authorIdsFilter = filters.authorIdsFilter; // Server sẽ hiểu là authorIdsFilter[]
    }
    
    try {
      const response = await mangaApi.getMangas(queryParams);
      
      // Xử lý `relationships` để chuẩn bị dữ liệu cho UI
      const processedMangas = response.data.map(manga => {
        let coverArtPublicId = null;
        const authors = [];
        const artists = [];

        if (manga.relationships && Array.isArray(manga.relationships)) {
          manga.relationships.forEach(rel => {
            if (rel.type === RELATIONSHIP_TYPES.COVER_ART && rel.id) {
              // API mới: id của cover_art là publicId
              coverArtPublicId = rel.id; 
            } else if (rel.type === RELATIONSHIP_TYPES.AUTHOR && rel.attributes) {
              authors.push({ id: rel.id, name: rel.attributes.name });
            } else if (rel.type === RELATIONSHIP_TYPES.ARTIST && rel.attributes) {
              artists.push({ id: rel.id, name: rel.attributes.name });
            }
          });
        }
        
        // Tags đã được nhúng vào manga.attributes.tags
        // Không cần xử lý tags từ relationships nữa.

        return {
          ...manga,
          // attributes.tags đã có sẵn, không cần map lại ở đây nếu backend trả đúng cấu trúc ResourceObject<TagAttributesDto>
          coverArtPublicId, // Gán publicId đã lấy
          // Thêm thông tin tác giả/họa sĩ đã xử lý nếu cần truy cập trực tiếp trong component
          // Hoặc component có thể tự xử lý từ manga.relationships
          processedAuthors: authors,
          processedArtists: artists,
        };
      });

      set({
        mangas: processedMangas,
        totalMangas: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      });
    } catch (error) {
      console.error('Failed to fetch mangas:', error);
      set({ mangas: [], totalMangas: 0 });
    }
  },
  
  // ... (các actions khác: setPage, setRowsPerPage, setSort) ...

  setFilter: (filterName, value) => {
    set(state => ({
      filters: { ...state.filters, [filterName]: value }
    }));
  },
  
  applyFilters: (newFilters) // Hàm này có thể không cần thay đổi nếu nó chỉ set state filters
    => {
    set((state) => ({
      filters: { ...state.filters, ...newFilters },
      page: 0, 
    }));
    // `fetchMangas(true)` sẽ được gọi từ component sau khi applyFilters
  },

  resetFilters: () => {
    set({
      filters: { // Reset về giá trị mặc định mới
        titleFilter: '',
        statusFilter: '',
        contentRatingFilter: '',
        publicationDemographicsFilter: [], // Mặc định mảng rỗng
        originalLanguageFilter: '',
        yearFilter: null,
        includedTags: [], // Mặc định mảng rỗng
        includedTagsMode: 'AND',
        excludedTags: [], // Mặc định mảng rỗng
        excludedTagsMode: 'OR',
        authorIdsFilter: [],
      },
      page: 0,
    });
    // `fetchMangas(true)` sẽ được gọi từ component sau khi resetFilters
  },

  // ... (deleteManga giữ nguyên) ...
  deleteManga: async (id) => {
    try {
      await mangaApi.deleteManga(id)
      showSuccessToast('Xóa manga thành công!')
      get().fetchMangas() // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete manga:', error)
      // Error is handled by apiClient interceptor
    }
  },
}), 'manga'))

export default useMangaStore
```

#### 3.2. Giải thích thay đổi trong `fetchMangas`:

*   **`publicationDemographicsFilter`**: Sẽ được gửi dưới dạng mảng các string. ASP.NET Core sẽ bind đúng nếu client gửi dạng lặp lại (VD: `?publicationDemographicsFilter=Shounen&publicationDemographicsFilter=Seinen`).
*   **`includedTags`, `excludedTags`**: Tương tự, là mảng các GUIDs.
*   **`includedTagsMode`, `excludedTagsMode`**: Là các string "AND" hoặc "OR".
*   **`includes`**: Mảng các string để yêu cầu thông tin nhúng.
*   **Xử lý `relationships` và `attributes.tags`**:
    *   **Cover Art**: Trong `GET /mangas`, `relationships` cho `cover_art` sẽ có `id` là `PublicId`. Chúng ta trích xuất `PublicId` này.
    *   **Author/Artist**: Nếu `includes` có `author` hoặc `artist`, thì `relationships` sẽ chứa các đối tượng này với trường `attributes` là `AuthorAttributesDto`.
    *   **Tags**: Thông tin tags (bao gồm `id`, `type`, `attributes` của tag) đã được nhúng sẵn trong `manga.attributes.tags`. Không cần phải xử lý từ `relationships` nữa.

### 4. Cập nhật Giao diện Lọc (`src/features/manga/pages/MangaListPage.jsx`)

#### 4.1. `PUBLICATION_DEMOGRAPHIC_OPTIONS` trong `appConstants.js`

Đảm bảo các giá trị value khớp với Enum `PublicationDemographic` của backend. (Có vẻ đã đúng).

#### 4.2. Giao diện lọc Demographic (Multiselect)

```jsx
// src/features/manga/pages/MangaListPage.jsx
// ... (imports)
import { Checkbox, ListItemText } from '@mui/material'; // Thêm cho multiselect

// ...

function MangaListPage() {
  // ... (state và hooks)
  const {
    // ...
    filters = {}, // Đảm bảo filters được khởi tạo
    // ...
    setFilter, // Lấy setFilter từ store
    applyFilters, // Lấy applyFilters
    resetFilters, // Lấy resetFilters
  } = useMangaStore();

  // ...

  const handleApplyFilters = () => {
    // Gọi applyFilters của store, sau đó fetchMangas
    applyFilters(filters); // Truyền filters hiện tại từ store
    fetchMangas(true);
  };

  const handleResetFilters = () => {
    resetFilters(); // Action này trong store đã tự fetch lại
  };

  // Handler cho publicationDemographicsFilter (multiselect)
  const handleDemographicChange = (event) => {
    const {
      target: { value },
    } = event;
    setFilter(
      'publicationDemographicsFilter',
      // On autofill we get a stringified value.
      typeof value === 'string' ? value.split(',') : value,
    );
  };
  
  // Handler cho các Tag Autocomplete (ví dụ)
  const handleIncludedTagsChange = (event, newValue) => {
    setFilter('includedTags', newValue.map(tag => tag.id)); // Giả sử newValue là mảng các object tag {id, name}
  };
  const handleExcludedTagsChange = (event, newValue) => {
    setFilter('excludedTags', newValue.map(tag => tag.id));
  };

  // ... (render)
  return (
    <Box className="manga-list-page">
      {/* ... (Typography) */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-start">
          {/* ... (các filter khác) ... */}
          <Grid item xs={12} sm={6} md={4} lg={3}> {/* Điều chỉnh kích thước cột nếu cần */}
            <TextField
              select
              label="Đối tượng"
              variant="outlined"
              fullWidth
              SelectProps={{
                multiple: true,
                value: filters.publicationDemographicsFilter || [],
                onChange: handleDemographicChange,
                renderValue: (selected) => 
                  (selected).map(val => PUBLICATION_DEMOGRAPHIC_OPTIONS.find(opt => opt.value === val)?.label).join(', '),
              }}
              sx={{ minWidth: '180px' }}
            >
              {PUBLICATION_DEMOGRAPHIC_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  <Checkbox checked={(filters.publicationDemographicsFilter || []).indexOf(option.value) > -1} />
                  <ListItemText primary={option.label} />
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          {/* Included Tags Filter */}
          <Grid item xs={12} sm={6} md={4} lg={3}>
            <Autocomplete
              multiple
              options={availableTags} // Giả sử availableTags là state chứa [{id, name}, ...]
              getOptionLabel={(option) => option.name}
              value={availableTags.filter(tag => (filters.includedTags || []).includes(tag.id))}
              onChange={handleIncludedTagsChange}
              renderInput={(params) => (
                <TextField {...params} label="Chứa Tags (Bắt buộc)" variant="outlined" />
              )}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
            />
          </Grid>
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Chế độ Tags Bắt buộc"
              value={filters.includedTagsMode || 'AND'}
              onChange={(e) => setFilter('includedTagsMode', e.target.value)}
              variant="outlined"
              fullWidth
              disabled={(filters.includedTags || []).length === 0}
            >
              <MenuItem value="AND">VÀ (Tất cả)</MenuItem>
              <MenuItem value="OR">HOẶC (Bất kỳ)</MenuItem>
            </TextField>
          </Grid>

          {/* Excluded Tags Filter */}
          <Grid item xs={12} sm={6} md={4} lg={3}>
            <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              value={availableTags.filter(tag => (filters.excludedTags || []).includes(tag.id))}
              onChange={handleExcludedTagsChange}
              renderInput={(params) => (
                <TextField {...params} label="Không chứa Tags" variant="outlined" />
              )}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
            />
          </Grid>
           <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Chế độ Tags Loại trừ"
              value={filters.excludedTagsMode || 'OR'}
              onChange={(e) => setFilter('excludedTagsMode', e.target.value)}
              variant="outlined"
              fullWidth
              disabled={(filters.excludedTags || []).length === 0}
            >
              <MenuItem value="OR">HOẶC (Bất kỳ)</MenuItem>
              <MenuItem value="AND">VÀ (Tất cả)</MenuItem>
            </TextField>
          </Grid>

          {/* ... (Nút Áp dụng, Đặt lại) ... */}
            <Grid item xs={12} sm={6} md={2} lg={2} sx={{ display: 'flex', alignItems: 'center' }}>
                <Button
                variant="contained"
                color="primary"
                startIcon={<SearchIcon />}
                onClick={handleApplyFilters}
                fullWidth
                sx={{ height: '56px' }} 
                >
                Áp dụng
                </Button>
            </Grid>
            <Grid item xs={12} sm={6} md={2} lg={2} sx={{ display: 'flex', alignItems: 'center' }}>
                <Button
                variant="outlined"
                color="inherit"
                startIcon={<ClearIcon />}
                onClick={handleResetFilters}
                fullWidth
                sx={{ height: '56px' }}
                >
                Đặt lại
                </Button>
            </Grid>
        </Grid>
      </Box>
      {/* ... (Table và các phần khác) ... */}
    </Box>
  );
}

export default MangaListPage;
```

#### 4.3. Hiển thị Tags trong `MangaTable.jsx`

```jsx
// src/features/manga/components/MangaTable.jsx
// ... (imports)

// ... (columns definition)
    // Cột Tags được cập nhật để đọc từ manga.attributes.tags
    {
      id: 'tags', // Sử dụng một key không trùng với attributes.tags để tránh nhầm lẫn
      label: 'Tags',
      minWidth: 150,
      sortable: false, // Việc sort tags có thể phức tạp
      format: (value, row) => { // `value` ở đây sẽ là undefined nếu id là 'tags'
        const tagsToDisplay = row.attributes?.tags || []; // Lấy từ attributes.tags
        return (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {tagsToDisplay.slice(0, 3).map((tagResource) => ( // Hiển thị tối đa 3 tags
              <Chip key={tagResource.id} label={tagResource.attributes.name} size="small" />
            ))}
            {tagsToDisplay.length > 3 && (
              <Chip label={`+${tagsToDisplay.length - 3}`} size="small" />
            )}
          </Box>
        );
      },
    },
// ...

// Trong formatMangaDataForTable:
// Không cần xử lý đặc biệt cho tags nữa vì nó đã có trong `row.attributes.tags`
// Tuy nhiên, cần đảm bảo `coverArtPublicId` vẫn được lấy đúng từ `relationships`
const formatMangaDataForTable = (mangasData) => {
  if (!mangasData) return [];
  return mangasData.map(manga => {
    // coverArtPublicId đã được xử lý trong store rồi
    return {
      ...manga.attributes, // Bao gồm cả `tags`
      id: manga.id,
      relationships: manga.relationships, // Vẫn giữ relationships nếu cần cho Author/Artist hoặc CoverArt (nếu logic lấy coverArtPublicId không ở store)
      coverArtPublicId: manga.coverArtPublicId,
    };
  });
};

// ...
```

#### 4.4. Hiển thị Author/Artist trong `MangaTable.jsx` (Nếu muốn hiển thị trực tiếp từ `processedAuthors/Artists` của Store)

```jsx
// src/features/manga/components/MangaTable.jsx

// ... (columns definition)
    {
      id: 'authors',
      label: 'Tác giả',
      minWidth: 120,
      sortable: false,
      format: (value, row) => {
        const authors = row.processedAuthors || []; // Lấy từ trường đã xử lý trong store
        return authors.map(a => a.name).join(', ');
      }
    },
    {
      id: 'artists',
      label: 'Họa sĩ',
      minWidth: 120,
      sortable: false,
      format: (value, row) => {
        const artists = row.processedArtists || []; // Lấy từ trường đã xử lý trong store
        return artists.map(a => a.name).join(', ');
      }
    },
// ...

// Trong formatMangaDataForTable:
const formatMangaDataForTable = (mangasData) => {
  if (!mangasData) return [];
  return mangasData.map(manga => {
    return {
      ...manga.attributes,
      id: manga.id,
      relationships: manga.relationships, 
      coverArtPublicId: manga.coverArtPublicId,
      processedAuthors: manga.processedAuthors, // Đảm bảo trường này được truyền vào
      processedArtists: manga.processedArtists, // Đảm bảo trường này được truyền vào
    };
  });
};
```

### 5. Cập nhật `MangaForm.jsx`

*   **Tags:** Sử dụng Autocomplete để chọn tags. `tagIds` trong form data vẫn là mảng GUID.
*   **Authors/Artists:** UI hiện tại có vẻ đã xử lý `authors` là một mảng các `MangaAuthorInput`. Không cần thay đổi lớn ở đây, trừ khi muốn hiển thị tên tác giả/họa sĩ trong quá trình chọn.

```jsx
// src/features/manga/components/MangaForm.jsx
// ... (imports)
// import tagApi from '../../../api/tagApi'; // Đã có
// import authorApi from '../../../api/authorApi'; // Đã có

function MangaForm({ initialData, onSubmit, isEditMode }) {
  // ... (useFormWithZod và các state khác)

  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
    getValues, // Thêm getValues
    reset, // Thêm reset
  } = useFormWithZod({
    schema: isEditMode ? updateMangaSchema : createMangaSchema,
    defaultValues: initialData
      ? {
          title: initialData.attributes.title || '',
          originalLanguage: initialData.attributes.originalLanguage || '',
          publicationDemographic: initialData.attributes.publicationDemographic || null,
          status: initialData.attributes.status || 'Ongoing',
          year: initialData.attributes.year || null,
          contentRating: initialData.attributes.contentRating || 'Safe',
          isLocked: initialData.attributes.isLocked || false,
          // Lấy tagIds từ attributes.tags
          tagIds: initialData.attributes.tags?.map(tagResource => tagResource.id) || [],
          authors: initialData.relationships
            ?.filter((rel) => rel.type === 'author' || rel.type === 'artist')
            .map((rel) => ({
              authorId: rel.id,
              // Lấy role từ type, attributes.name sẽ được lấy từ availableAuthors khi hiển thị
              role: rel.type === RELATIONSHIP_TYPES.AUTHOR ? 'Author' : 'Artist',
            })) || [],
          tempAuthor: null, // Sẽ là object {id, name} khi chọn từ Autocomplete
          tempAuthorRole: 'Author',
        }
      : { // Giá trị mặc định cho form tạo mới
          title: '',
          originalLanguage: 'ja',
          publicationDemographic: null,
          status: 'Ongoing',
          year: new Date().getFullYear(),
          contentRating: 'Safe',
          isLocked: false,
          tagIds: [],
          authors: [],
          tempAuthor: null,
          tempAuthorRole: 'Author',
        },
  });
  
  // ... (state for availableAuthors, availableTags) ...
  // ... (useEffect for fetching dropdown data) ...

  // Cập nhật selectedTagsVisual khi tagIds thay đổi hoặc availableTags được tải
  useEffect(() => {
    if (availableTags.length > 0) {
      const currentTagIds = getValues('tagIds') || [];
      const hydratedTags = currentTagIds
        .map((tagId) => {
          const tagDetails = availableTags.find(t => t.id === tagId);
          return tagDetails ? { id: tagDetails.id, name: tagDetails.name, tagGroupName: tagDetails.tagGroupName } : null;
        })
        .filter(Boolean);
      setSelectedTagsVisual(hydratedTags);
    }
  }, [getValues('tagIds'), availableTags]);


  // Cập nhật selectedAuthorsVisual khi authors (form value) thay đổi hoặc availableAuthors được tải
  useEffect(() => {
    if (availableAuthors.length > 0) {
        const currentAuthorsForm = getValues('authors') || [];
        const hydratedAuthors = currentAuthorsForm
            .map((formAuthor) => {
                const authorDetails = availableAuthors.find(a => a.id === formAuthor.authorId);
                return authorDetails ? { id: authorDetails.id, name: authorDetails.name, role: formAuthor.role } : null;
            })
            .filter(Boolean);
        setSelectedAuthorsVisual(hydratedAuthors);
    }
  }, [getValues('authors'), availableAuthors]);


  // ... (các handlers khác) ...
  const currentTagIdsFormValue = watch('tagIds'); // Theo dõi sự thay đổi của tagIds trong form

    useEffect(() => {
        // Đồng bộ selectedTagsVisual với giá trị từ form
        if (availableTags.length > 0 && currentTagIdsFormValue) {
            const newSelectedVisuals = currentTagIdsFormValue
                .map(id => availableTags.find(tag => tag.id === id))
                .filter(Boolean); // Lọc ra những tag thực sự tìm thấy trong availableTags
            setSelectedTagsVisual(newSelectedVisuals);
        }
    }, [currentTagIdsFormValue, availableTags]);


  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ mt: 1 }}>
      <Grid container spacing={2} columns={{ xs: 4, sm: 6, md: 12 }}>
        {/* ... (các fields khác) ... */}
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="publicationDemographic"
            label="Đối tượng xuất bản"
            type="select"
            options={[{value: '', label: 'Không chọn'}, ...PUBLICATION_DEMOGRAPHIC_OPTIONS]} // Thêm option "Không chọn"
          />
        </Grid>
        {/* ... */}
        {/* Tags Section */}
        <Grid item xs={12}>
          <Autocomplete
            multiple
            id="manga-tags-autocomplete"
            options={availableTags.sort((a, b) => 
                (a.tagGroupName || 'zzzz').localeCompare(b.tagGroupName || 'zzzz') || a.name.localeCompare(b.name)
            )} // Sắp xếp theo group rồi đến tên
            groupBy={(option) => option.tagGroupName || 'Khác'}
            getOptionLabel={(option) => option.name}
            value={selectedTagsVisual} // Sử dụng state trực quan
            onChange={(event, newValue) => {
              setSelectedTagsVisual(newValue); // Cập nhật state trực quan
              setValue('tagIds', newValue.map(tag => tag.id)); // Cập nhật giá trị form
            }}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            renderOption={(props, option, { selected }) => (
              <Box component="li" {...props} sx={{ width: '100%', justifyContent: 'flex-start', px: 1, py: 0.5 }}>
                <Checkbox
                  icon={<CheckBoxOutlineBlankIcon fontSize="small" />}
                  checkedIcon={<CheckBoxIcon fontSize="small" />}
                  style={{ marginRight: 8 }}
                  checked={selected}
                />
                <Chip 
                  label={option.name} 
                  size="small" 
                  variant="outlined" 
                  sx={{ cursor: 'pointer', flexGrow: 1, justifyContent: 'flex-start' }} 
                />
              </Box>
            )}
            // PaperComponent={HorizontalTagPaper} // Tùy chọn
            renderInput={(params) => (
              <TextField
                {...params}
                variant="outlined"
                label="Tags"
                placeholder="Chọn tags"
                margin="normal"
                error={!!errors.tagIds}
                helperText={errors.tagIds ? errors.tagIds.message : null}
              />
            )}
            renderTags={(value, getTagProps) =>
              value.map((option, index) => (
                <Chip
                  key={option.id}
                  label={option.name}
                  {...getTagProps({ index })}
                  color="secondary" // Hoặc màu khác tùy ý
                  variant="outlined"
                />
              ))
            }
            fullWidth
          />
        </Grid>
        {/* ... (các fields còn lại và nút submit) ... */}
      </Grid>
    </Box>
  );
}
```

### 6. Cập nhật `CoverArtManager.jsx`

*   API `GET /mangas/{mangaId}/covers` giờ trả về `CoverArt` entities với `id` là GUID, `attributes` chứa `publicId`.
*   `CLOUDINARY_BASE_URL` cần được sử dụng với `publicId` từ `cover.attributes.publicId`.

```javascript
// src/features/manga/components/CoverArtManager.jsx
// ... (imports)

// ...
function CoverArtManager({ mangaId }) {
  // ... (state và hooks khác)

  const fetchCovers = async () => {
    setLoadingCovers(true);
    try {
      // API getMangaCovers bây giờ trả về ResourceObject<CoverArtAttributesDto>
      // Mỗi cover trong response.data sẽ có attributes.publicId
      const response = await mangaApi.getMangaCovers(mangaId, { limit: 100 });
      setCovers(response.data); // response.data giờ là CoverArt[]
    } catch (error) {
      // ... (xử lý lỗi)
    } finally {
      setLoadingCovers(false);
    }
  };

  // ... (useEffect và các handlers khác)

  return (
    <Box className="cover-art-manager">
      {/* ... (Upload section) ... */}
      {loadingCovers ? (
        // ...
      ) : covers.length === 0 ? (
        // ...
      ) : (
        <Grid container spacing={2} className="cover-art-grid" /* ... */>
          {covers.map((cover) => ( // cover giờ là ResourceObject<CoverArtAttributesDto>
            <Grid item key={cover.id} /* ... */>
              <Card className="cover-art-card">
                <CardMedia
                  component="img"
                  // Sử dụng publicId từ attributes
                  image={`${CLOUDINARY_BASE_URL}${cover.attributes.publicId}`} 
                  alt={cover.attributes.description || `Cover for volume ${cover.attributes.volume}`}
                />
                {/* ... (CardContent và CardActions) ... */}
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
      {/* ... (Dialogs) ... */}
    </Box>
  );
}
// ...
```

### 7. Cập nhật `MangaEditPage.jsx`

*   Khi load manga, API `getMangaById` có thể kèm `includes` để lấy author/artist attributes.
*   `MangaForm` đã được cập nhật để xử lý `attributes.tags`.

```javascript
// src/features/manga/pages/MangaEditPage.jsx
// ... (imports)

function MangaEditPage() {
  // ... (state và hooks)
  const { id } = useParams(); // mangaId
  // ...

  useEffect(() => {
    const loadManga = async () => {
      setLoading(true);
      try {
        // Gọi API với includes để lấy thông tin author/artist nhúng vào relationships
        const response = await mangaApi.getMangaById(id, { includes: ['author', 'artist'] });
        setManga(response.data);
      } catch (error) {
        // ... (xử lý lỗi)
      } finally {
        setLoading(false);
      }
    };
    loadManga();
  }, [id, navigate]);

  // ... (handleSubmit và các phần khác)
  // MangaForm sẽ tự động xử lý việc lấy tagIds từ manga.attributes.tags
  // và authors từ manga.relationships (đã có attributes của author/artist)
  
  return (
    // ...
    <MangaForm initialData={manga} onSubmit={handleSubmit} isEditMode={true} />
    // ...
  );
}

export default MangaEditPage;
```

---

## III. Cập nhật `MangaReader_WebUI`

### 1. Cập nhật Models (`Models/MangaDex/`)

#### 1.1. `Models/MangaDex/MangaAttributes.cs`

```csharp
// MangaReader_WebUI\Models\MangaDex\MangaAttributes.cs
using System.Text.Json.Serialization; // Đảm bảo có using này

namespace MangaReader.WebUI.Models.Mangadex
{
    public class MangaAttributes
    {
        [JsonPropertyName("title")]
        public Dictionary<string, string>? Title { get; set; }

        [JsonPropertyName("altTitles")]
        public List<Dictionary<string, string>>? AltTitles { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }

        [JsonPropertyName("links")]
        public Dictionary<string, string>? Links { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string OriginalLanguage { get; set; } = default!;

        [JsonPropertyName("lastVolume")]
        public string? LastVolume { get; set; }

        [JsonPropertyName("lastChapter")]
        public string? LastChapter { get; set; }

        [JsonPropertyName("publicationDemographic")]
        public string? PublicationDemographic { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("contentRating")]
        public string ContentRating { get; set; } = default!;

        [JsonPropertyName("chapterNumbersResetOnNewVolume")]
        public bool ChapterNumbersResetOnNewVolume { get; set; }

        [JsonPropertyName("availableTranslatedLanguages")]
        public List<string>? AvailableTranslatedLanguages { get; set; }

        [JsonPropertyName("latestUploadedChapter")]
        public Guid? LatestUploadedChapter { get; set; }

        // THAY ĐỔI: `Tags` giờ là danh sách các `Tag` object (không phải string nữa)
        // Mỗi object `Tag` này sẽ có `attributes` riêng của nó.
        // Vì API MangaDex trả về cấu trúc `ResourceObject<TagAttributes>` cho mỗi tag,
        // chúng ta cần một class `Tag` tương ứng trong `Models/Mangadex/Tag.cs`
        [JsonPropertyName("tags")]
        public List<Tag>? Tags { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = default!;

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
```

#### 1.2. `Models/MangaDex/Relationship.cs`

Thêm trường `Attributes` như đã làm ở `MangaReaderLib`.

```csharp
// MangaReader_WebUI\Models\MangaDex\Relationship.cs
using System.Text.Json.Serialization;
using System.Text.Json; // Thêm using cho JsonElement

namespace MangaReader.WebUI.Models.Mangadex
{
    public class Relationship
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("related")]
        public string? Related { get; set; }

        // THAY ĐỔI: Sử dụng JsonElement để linh hoạt hơn
        // Sau đó, trong service hoặc mapper, có thể deserialize nó thành kiểu cụ thể (AuthorAttributes, ArtistAttributes, ...)
        [JsonPropertyName("attributes")]
        public JsonElement? Attributes { get; set; }
    }
}
```

#### 1.3. `Models/MangaDex/Author.cs`

Đảm bảo class `AuthorAttributes` tồn tại và khớp với dữ liệu từ API (đã có sẵn).

#### 1.4. `Models/MangaDex/Tag.cs`

Cần đảm bảo class `Tag` và `TagAttributes` (của MangaDex) đã được định nghĩa đúng để deserialize trường `manga.attributes.tags` mới.

```csharp
// MangaReader_WebUI\Models\MangaDex\Tag.cs
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Models.Mangadex
{
    public class Tag
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!; // Sẽ là "tag"

        [JsonPropertyName("attributes")]
        public TagAttributes? Attributes { get; set; }
        
        // MangaDex Tag response không có relationships trực tiếp ở đây
        // Nếu API của bạn có trả về relationship cho tag, hãy thêm vào
        // [JsonPropertyName("relationships")]
        // public List<Relationship>? Relationships { get; set; }
    }

    // Đây là TagAttributes của MangaDex, không phải của MangaReaderLib
    public class TagAttributes
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; } // Ví dụ: { "en": "Action", "vi": "Hành động" }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("group")]
        public string? Group { get; set; } // "content", "format", "genre", "theme"
        
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }

    public class TagListResponse : BaseListResponse<Tag> { }
}
```

### 2. Cập nhật API Services

#### 2.1. `Services/APIServices/Interfaces/IMangaApiService.cs`

Cập nhật `FetchMangaAsync` và `FetchMangaDetailsAsync` để hỗ trợ các tham số mới.

```csharp
// MangaReader_WebUI\Services\APIServices\Interfaces\IMangaApiService.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    public interface IMangaApiService
    {
        Task<MangaList?> FetchMangaAsync(
            int? limit = null, 
            int? offset = null, 
            SortManga? sortManga = null,
            List<string>? includes = null // MỚI
        );

        Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds);

        Task<MangaResponse?> FetchMangaDetailsAsync(
            string mangaId,
            List<string>? includes = null // MỚI
        );
    }
}
```

#### 2.2. `Services/APIServices/Services/MangaApiService.cs`

Cập nhật triển khai các phương thức trên.

```csharp
// MangaReader_WebUI\Services\APIServices\Services\MangaApiService.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    public class MangaApiService : BaseApiService, IMangaApiService
    {
        // ... (constructor và các phương thức khác)
        public MangaApiService(
            HttpClient httpClient,
            ILogger<MangaApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler) { }


        public async Task<MangaList?> FetchMangaAsync(
            int? limit = null, int? offset = null, 
            SortManga? sortManga = null,
            List<string>? includes = null // MỚI
            )
        {
            // ... (logging)
            Logger.LogInformation("Fetching manga list with parameters: Limit={Limit}, Offset={Offset}, SortOptions={@SortOptions}, Includes={@Includes}",
                limit, offset, sortManga, includes);

            var queryParams = new Dictionary<string, List<string>>();

            if (limit.HasValue) AddOrUpdateParam(queryParams, "limit", limit.Value.ToString());
            if (offset.HasValue) AddOrUpdateParam(queryParams, "offset", offset.Value.ToString());

            if (sortManga != null)
            {
                var sortParams = sortManga.ToParams(); // ToParams cần được cập nhật
                foreach (var param in sortParams)
                {
                    if (param.Key.EndsWith("[]") && param.Value is IEnumerable<string> values)
                    {
                        if (!queryParams.ContainsKey(param.Key))
                        {
                            queryParams[param.Key] = new List<string>();
                        }
                        foreach(var val in values)
                        {
                             if (!string.IsNullOrEmpty(val)) queryParams[param.Key].Add(val);
                        }
                    }
                    else if (param.Key.StartsWith("order["))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value?.ToString() ?? string.Empty);
                    }
                    else if (param.Value != null && !string.IsNullOrEmpty(param.Value.ToString()))
                    {
                        AddOrUpdateParam(queryParams, param.Key, param.Value.ToString()!);
                    }
                }
                // Xử lý ContentRating riêng nếu ToParams chưa bao gồm
                if (sortManga.ContentRating != null && sortManga.ContentRating.Any())
                {
                    if (!queryParams.ContainsKey("contentRating[]"))
                    {
                        queryParams["contentRating[]"] = new List<string>();
                    }
                     queryParams["contentRating[]"].AddRange(sortManga.ContentRating.Where(cr => !string.IsNullOrEmpty(cr)));
                }
                 // Xử lý PublicationDemographic riêng
                if (sortManga.Demographic != null && sortManga.Demographic.Any())
                {
                    if (!queryParams.ContainsKey("publicationDemographic[]")) // API dùng publicationDemographic[]
                    {
                        queryParams["publicationDemographic[]"] = new List<string>();
                    }
                    queryParams["publicationDemographic[]"].AddRange(sortManga.Demographic.Where(d => !string.IsNullOrEmpty(d)));
                }
            }
            else
            {
                AddOrUpdateParam(queryParams, "order[latestUploadedChapter]", "desc");
            }

            // Thêm includes
            if (includes != null && includes.Any())
            {
                queryParams["includes[]"] = includes;
            } 
            else // Mặc định vẫn lấy cover_art nếu không có includes nào được chỉ định
            {
                 queryParams["includes[]"] = new List<string> { "cover_art" };
            }


            var url = BuildUrlWithParams("manga", queryParams);
            // ... (phần còn lại của hàm giữ nguyên)
            Logger.LogInformation("Constructed manga fetch URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga list failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = limit ?? 10, Offset = offset ?? 0, Total = 0 };
            }

            #if DEBUG
            Debug.Assert(mangaList.Result == "ok", $"[MangaApiService] FetchMangaAsync - API returned error: {mangaList.Result}. URL: {url}");
            Debug.Assert(mangaList.Data != null, $"[MangaApiService] FetchMangaAsync - API returned null Data despite ok result. URL: {url}");
            #endif

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga list has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga entries (Total: {Total}).", mangaList.Data.Count, mangaList.Total);
            return mangaList;
        }

        public async Task<MangaResponse?> FetchMangaDetailsAsync(
            string mangaId, 
            List<string>? includes = null // MỚI
        )
        {
            Logger.LogInformation("Fetching details for manga ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            
            // Mặc định includes cho trang chi tiết
            var finalIncludes = new List<string> { "author", "artist", "cover_art" }; // tag đã tự động có trong attributes
            if (includes != null && includes.Any())
            {
                finalIncludes.AddRange(includes);
                finalIncludes = finalIncludes.Distinct().ToList(); // Loại bỏ trùng lặp
            }
            queryParams["includes[]"] = finalIncludes;
            
            var url = BuildUrlWithParams($"manga/{mangaId}", queryParams);
            // ... (phần còn lại của hàm giữ nguyên)
             Logger.LogInformation("Constructed manga details fetch URL: {Url}", url);

            var mangaResponse = await GetApiAsync<MangaResponse>(url);
            if (mangaResponse == null)
            {
                 Logger.LogWarning("Fetching manga details for {MangaId} failed.", mangaId);
                 return null; 
            }

            #if DEBUG
            Debug.Assert(mangaResponse.Result == "ok", $"[MangaApiService] FetchMangaDetailsAsync({mangaId}) - API returned error: {mangaResponse.Result}. URL: {url}");
            Debug.Assert(mangaResponse.Data != null, $"[MangaApiService] FetchMangaDetailsAsync({mangaId}) - API returned null Data despite ok result. URL: {url}");
            #endif

            if (mangaResponse.Result != "ok" || mangaResponse.Data == null)
            {
                Logger.LogWarning("API response for manga details {MangaId} has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaId, mangaResponse.Result, mangaResponse.Data != null, url);
                return null; 
            }

            Logger.LogInformation("Successfully fetched details for manga: {MangaTitle} ({MangaId})",
                mangaResponse.Data.Attributes?.Title?.FirstOrDefault().Value ?? "N/A", mangaId);
            return mangaResponse;
        }
        
        // ... (FetchMangaByIdsAsync giữ nguyên, nhưng có thể thêm includes nếu cần)
        public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            if (mangaIds == null || !mangaIds.Any())
            {
                Logger.LogWarning("FetchMangaByIdsAsync called with empty or null list of IDs.");
                return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Total = 0 };
            }

            Logger.LogInformation("Fetching manga by IDs: [{MangaIds}]", string.Join(", ", mangaIds));
            var queryParams = new Dictionary<string, List<string>>();
            foreach (var id in mangaIds)
            {
                AddOrUpdateParam(queryParams, "ids[]", id);
            }
            // Có thể thêm includes ở đây nếu cần, ví dụ để lấy author/artist khi hiển thị danh sách lịch sử/theo dõi
            queryParams["includes[]"] = new List<string> { "cover_art", "author", "artist" };
            AddOrUpdateParam(queryParams, "limit", mangaIds.Count.ToString()); 

            var url = BuildUrlWithParams("manga", queryParams);
            Logger.LogInformation("Constructed manga fetch by IDs URL: {Url}", url);

            var mangaList = await GetApiAsync<MangaList>(url);
            if (mangaList == null)
            {
                Logger.LogWarning("Fetching manga by IDs failed. Returning empty list.");
                return new MangaList { Result = "error", Response = "collection", Data = new List<Manga>(), Limit = mangaIds.Count, Offset = 0, Total = 0 };
            }

            #if DEBUG
            Debug.Assert(mangaList.Result == "ok", $"[MangaApiService] FetchMangaByIdsAsync - API returned error: {mangaList.Result}. URL: {url}");
            Debug.Assert(mangaList.Data != null, $"[MangaApiService] FetchMangaByIdsAsync - API returned null Data despite ok result. URL: {url}");
            #endif

            if (mangaList.Result != "ok" || mangaList.Data == null)
            {
                Logger.LogWarning("API response for manga by IDs has invalid format or missing data. Result: {Result}, HasData: {HasData}. URL: {Url}",
                    mangaList.Result, mangaList.Data != null, url);
                return new MangaList { Result = mangaList.Result ?? "error", Response = "collection", Data = new List<Manga>(), Limit = mangaList.Limit, Offset = mangaList.Offset, Total = mangaList.Total };
            }

            Logger.LogInformation("Successfully fetched {Count} manga by IDs.", mangaList.Data.Count);
            return mangaList;
        }
    }
}
```

#### 2.3. `Services/APIServices/Services/CoverApiService.cs`

*   `GetProxiedCoverUrl`: Đối với MangaDex, API `GET /mangas` khi có `includes[]=cover_art` sẽ trả về `PublicId` trong `RelationshipObject.id`.
    `GetProxiedCoverUrl` cần sử dụng `PublicId` này thay vì `fileName` và `mangaId`. Tuy nhiên, URL proxy image của backend hiện tại (`/mangadex/proxy-image?url=...`) vẫn cần URL đầy đủ của MangaDex.
    Vì vậy, logic xây dựng `originalImageUrl` cần thay đổi.

```csharp
// MangaReader_WebUI\Services\APIServices\Services\CoverApiService.cs
// ... (using statements)

    public class CoverApiService : BaseApiService, ICoverApiService
    {
        // ... (constructor và các phần khác)
        public CoverApiService(
            HttpClient httpClient,
            ILogger<CoverApiService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler) { }

        private readonly string _imageProxyBaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/')
                                      ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
        private readonly TimeSpan _apiDelay = TimeSpan.FromMilliseconds(configuration?.GetValue<int>("ApiRateLimitDelayMs", 250) ?? 250);
                                      
        public static string? ExtractCoverFileNameFromRelationships(List<Relationship>? relationships, ILogger? logger = null)
        {
            // ... (giữ nguyên)
            if (relationships == null || !relationships.Any())
            {
                logger?.LogDebug("ExtractCoverFileName: Danh sách relationships rỗng hoặc null.");
                return null;
            }

            var coverRelationship = relationships.FirstOrDefault(r => r != null && r.Type == "cover_art");

            if (coverRelationship == null)
            {
                logger?.LogDebug("ExtractCoverFileName: Không tìm thấy relationship có type 'cover_art'.");
                return null;
            }
            
            // API MỚI: ID của cover_art trong relationship là PUBLIC_ID
            // Nếu Attributes được include (hiếm khi cho cover_art list), nó sẽ là null.
            // Ta cần chính cái ID này (publicId) để xây URL.
            if (!string.IsNullOrEmpty(coverRelationship.Id))
            {
                // ID này chính là publicId dạng "mangas_v2/manga-guid/covers/volume_xyz"
                logger?.LogDebug("ExtractCoverFileName: Extracted PublicId '{PublicId}' from relationship ID.", coverRelationship.Id);
                return coverRelationship.Id; 
            }
            
            // Fallback nếu Attributes có chứa fileName (trường hợp cũ hoặc API khác)
            if (coverRelationship.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    if (attributesElement.TryGetProperty("fileName", out var fileNameElement) && fileNameElement.ValueKind == JsonValueKind.String)
                    {
                        var fileName = fileNameElement.GetString();
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            logger?.LogDebug("ExtractCoverFileName: Extracted fileName '{FileName}' from relationship attributes (fallback).", fileName);
                            return fileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "ExtractCoverFileName: Lỗi khi đọc attributes của relationship {RelationshipId}.", coverRelationship.Id);
                }
            }

            logger?.LogWarning("ExtractCoverFileName: Không thể trích xuất PublicId/fileName cho cover art {RelationshipId}.", coverRelationship.Id);
            return null;
        }


        public async Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            // ... (giữ nguyên, vì endpoint /cover vẫn dùng GUID của CoverArt)
            var allCovers = new List<Cover>();
            int offset = 0;
            const int limit = 100;
            int totalAvailable = 0;

            Logger.LogInformation("Fetching ALL covers for manga ID: {MangaId} with pagination...", mangaId);

            do
            {
                var queryParams = new Dictionary<string, List<string>>();
                AddOrUpdateParam(queryParams, "manga[]", mangaId);
                AddOrUpdateParam(queryParams, "limit", limit.ToString());
                AddOrUpdateParam(queryParams, "offset", offset.ToString());
                AddOrUpdateParam(queryParams, "order[volume]", "asc"); 

                var url = BuildUrlWithParams("cover", queryParams);
                Logger.LogDebug("Fetching covers page: {Url}", url);

                try
                {
                    var coverListResponse = await GetApiAsync<CoverList>(url);

                    if (coverListResponse == null)
                    {
                        Logger.LogWarning("Error fetching covers for manga {MangaId} at offset {Offset}. Retrying...", mangaId, offset);
                        await Task.Delay(TimeSpan.FromSeconds(1)); 
                        coverListResponse = await GetApiAsync<CoverList>(url);

                        if (coverListResponse == null)
                        {
                            Logger.LogError("Failed to fetch covers for manga {MangaId} at offset {Offset} after retry. Stopping pagination.", mangaId, offset);
                            break; 
                        }
                    }

                    if (coverListResponse.Data == null || !coverListResponse.Data.Any())
                    {
                        Logger.LogInformation("No more covers found or data is invalid for manga {MangaId} at offset {Offset}.", mangaId, offset);
                        if (totalAvailable == 0 && offset == 0) totalAvailable = coverListResponse.Total;
                        break; 
                    }

                    allCovers.AddRange(coverListResponse.Data);
                    if (totalAvailable == 0) totalAvailable = coverListResponse.Total;
                    offset += limit;
                    Logger.LogDebug("Fetched {Count} covers. Offset now: {Offset}. Total available: {TotalAvailable}",
                        coverListResponse.Data.Count, offset, totalAvailable);

                    if (offset < totalAvailable && totalAvailable > 0)
                    {
                        await Task.Delay(_apiDelay);
                    }
                }
                catch (Exception ex) 
                {
                    Logger.LogError(ex, "Unexpected exception during cover pagination for manga ID: {MangaId}", mangaId);
                    return null; 
                }

            } while (offset < totalAvailable && totalAvailable > 0);

            Logger.LogInformation("Finished fetching. Total covers retrieved: {RetrievedCount} for manga ID: {MangaId}. API reported total: {ApiTotal}",
                allCovers.Count, mangaId, totalAvailable);

            return new CoverList
            {
                Result = "ok",
                Response = "collection",
                Data = allCovers,
                Limit = allCovers.Count, 
                Offset = 0,
                Total = totalAvailable 
            };
        }

        // THAY ĐỔI LOGIC GetProxiedCoverUrl
        // `fileName` bây giờ sẽ là `publicId` (ví dụ: "mangas_v2/manga-guid/covers/volume_xyz")
        // `mangaId` không còn cần thiết để xây dựng URL gốc nữa.
        public string GetProxiedCoverUrl(string mangaIdIgnored, string publicId, int size = 512)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                Logger.LogWarning("GetProxiedCoverUrl: PublicId is null or empty. Returning placeholder.");
                return "/images/cover-placeholder.jpg"; // Hoặc URL placeholder của bạn
            }

            // URL gốc của ảnh trên Cloudinary (từ publicId)
            // Ví dụ publicId: "mangas_v2/0a0f3e7c-e3c7-4e3a-9e1e-3c9c8c4e9e1e/covers/volume_1_cover_art_version_1_65ddb0a1162e4.jpg"
            // CLOUDINARY_BASE_URL ví dụ: "https://res.cloudinary.com/your_cloud_name/image/upload/"
            // => originalImageUrl = "https://res.cloudinary.com/your_cloud_name/image/upload/mangas_v2/0a0f3e7c-e3c7-4e3a-9e1e-3c9c8c4e9e1e/covers/volume_1_cover_art_version_1_65ddb0a1162e4.jpg"
            // Nếu muốn thêm transform kích thước (ví dụ w_512):
            // originalImageUrl = $"{_configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]}w_{size}/{publicId}";
            // Hoặc nếu backend tự xử lý transform thì không cần:
            string originalImageUrl = $"{_configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]}{publicId}";

            // URL proxy qua backend của bạn
            string proxiedUrl = $"{_imageProxyBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
            Logger.LogDebug("GetProxiedCoverUrl: Original Cloudinary URL = {OriginalUrl}, Proxied URL = {ProxiedUrl}", originalImageUrl, proxiedUrl);
            return proxiedUrl;
        }
    }
}
```

### 3. Cập nhật Data Processing Services

#### 3.1. `Services/MangaServices/DataProcessing/Interfaces/IMangaDataExtractor.cs`

Không cần thay đổi interface, nhưng triển khai sẽ thay đổi cách lấy tags và cover.

#### 3.2. `Services/MangaServices/DataProcessing/Services/MangaDataExtractorService.cs`

*   **`ExtractCoverUrl`**:
    *   Nếu nguồn là MangaDex: Lấy `publicId` từ `relationships` (mối quan hệ `cover_art`).
    *   URL gốc sẽ là `CLOUDINARY_BASE_URL` + `publicId`.
    *   URL proxy vẫn giữ nguyên.
*   **`ExtractAndTranslateTags`**: Lấy tags từ `mangaData.Attributes.Tags` (là `List<Tag>`). Mỗi `Tag` object này có `Attributes` riêng chứa `Name` (Dictionary).

```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs
// ... (using statements)

public class MangaDataExtractorService : IMangaDataExtractor
{
    // ... (constructor và các trường giữ nguyên) ...
    private readonly string _cloudinaryBaseUrl; // Thêm trường này

    public MangaDataExtractorService(
        ILogger<MangaDataExtractorService> logger,
        LocalizationService localizationService,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _localizationService = localizationService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        _backendApiBaseUrl = _configuration["BackendApi:BaseUrl"]?.TrimEnd('/')
                            ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured in MangaDataExtractorService.");
        _mangaReaderLibApiBaseUrl = _configuration["MangaReaderApiSettings:BaseUrl"]?.TrimEnd('/')
                                  ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured in MangaDataExtractorService.");
        _cloudinaryBaseUrl = _configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') // Lấy từ config
                            ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
    }
    // ...

    public string ExtractCoverUrl(string mangaId, List<Relationship>? relationships)
    {
        // ... (logging và kiểm tra null)
        Debug.Assert(!string.IsNullOrEmpty(mangaId), "Manga ID không được rỗng khi trích xuất Cover URL.");
        try
        {
            // ... (lấy currentSource)
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

            // API MỚI: id của cover_art trong relationship là PUBLIC_ID từ Cloudinary
            // Ví dụ: "mangas_v2/0a0f3e7c-e3c7-4e3a-9e1e-3c9c8c4e9e1e/covers/volume_1_cover_art_version_1_65ddb0a1162e4.jpg"
            string? publicId = coverRelationship.Id;

            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("ExtractCoverUrl: PublicId (từ relationship.Id) rỗng cho cover_art của manga ID {MangaId}. Relationship: {@Relationship}", mangaId, coverRelationship);
                return "/images/cover-placeholder.jpg";
            }
            
            // URL gốc của ảnh trên Cloudinary (không cần size ở đây, proxy sẽ xử lý nếu cần)
            string originalImageUrl = $"{_cloudinaryBaseUrl}{publicId}";
            
            // URL proxy qua backend của bạn (nếu backend có proxy)
            // Nếu không có proxy ở backend nữa, bạn có thể trả về originalImageUrl trực tiếp
            // Hoặc nếu bạn muốn client tự thêm transform thì chỉ trả về publicId.
            // Hiện tại, frontend WebUI có proxy.
            string proxiedUrl = $"{_backendApiBaseUrl}/mangadex/proxy-image?url={Uri.EscapeDataString(originalImageUrl)}";
            _logger.LogDebug("ExtractCoverUrl (API Mới): Original Cloudinary URL = {OriginalUrl}, Proxied URL = {ProxiedUrl}", originalImageUrl, proxiedUrl);
            return proxiedUrl;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi trích xuất Cover URL cho manga ID: {mangaId}");
            return "/images/cover-placeholder.jpg";
        }
    }

    public List<string> ExtractAndTranslateTags(List<Tag>? tagsList) // Tham số tagsList giờ là List<Tag>
    {
        var translatedTags = new List<string>();
        if (tagsList == null || !tagsList.Any())
        {
            return translatedTags;
        }

        try
        {
            foreach (var tagObject in tagsList) // Duyệt qua List<Tag>
            {
                // Attributes của Tag object này là MangaDex TagAttributes
                if (tagObject?.Attributes?.Name == null || !tagObject.Attributes.Name.Any()) continue;

                string tagName = tagObject.Attributes.Name.GetValueOrDefault("en") ?? // Ưu tiên tên tiếng Anh
                                 tagObject.Attributes.Name.FirstOrDefault().Value ?? // Hoặc tên đầu tiên có
                                 tagObject.Id.ToString(); // Hoặc ID nếu không có tên

                if (_tagTranslations.TryGetValue(tagName, out var translation))
                {
                    translatedTags.Add(translation);
                }
                else
                {
                    translatedTags.Add(tagName); // Giữ nguyên nếu không có bản dịch
                    _logger.LogDebug($"Không tìm thấy bản dịch cho tag: {tagName}");
                }
            }
            // ... (sắp xếp)
            return translatedTags.Distinct().OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), false)).ToList();
        }
        // ... (catch)
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi trích xuất và dịch tags manga.");
            return new List<string>();
        }
    }
    
    public (string Author, string Artist) ExtractAuthorArtist(List<Relationship>? relationships)
    {
        // ... (logging)
        Debug.Assert(relationships != null, "Relationships không được null khi trích xuất Author/Artist.");
        string author = "Không rõ";
        string artist = "Không rõ";

        if (relationships == null || !relationships.Any())
        {
            _logger.LogDebug("ExtractAuthorArtist: Danh sách relationships rỗng hoặc null.");
            return (author, artist);
        }
        
        try
        {
            foreach (var rel in relationships)
            {
                if (rel == null || (rel.Type != "author" && rel.Type != "artist")) continue;

                string name = "Không rõ";
                // API MỚI: Attributes có thể được nhúng
                if (rel.Attributes is JsonElement attributesElement && attributesElement.ValueKind == JsonValueKind.Object)
                {
                    // Deserialize JsonElement thành AuthorAttributes của MangaDex
                    try
                    {
                        var authorAttrs = attributesElement.Deserialize<Models.Mangadex.AuthorAttributes>(_jsonOptions);
                        if (authorAttrs != null && !string.IsNullOrEmpty(authorAttrs.Name))
                        {
                            name = authorAttrs.Name;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                         _logger.LogWarning(jsonEx, "Lỗi khi deserialize AuthorAttributes từ JsonElement cho relationship type {RelType}, ID {RelId}", rel.Type, rel.Id);
                    }
                }
                else if (rel.Attributes != null) // Trường hợp khác, cố gắng ToString nếu có
                {
                     _logger.LogWarning("Attributes của relationship type {RelType}, ID {RelId} không phải JsonElement. Type: {AttributeType}", rel.Type, rel.Id, rel.Attributes.GetType().Name);
                }


                if (rel.Type == "author")
                    author = name;
                else // rel.Type == "artist"
                    artist = name;
            }
        }
        // ... (catch)
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi trích xuất tác giả/họa sĩ từ relationships.");
        }
        return (author, artist);
    }


    // ... (các phương thức khác giữ nguyên: ExtractMangaTitle, ExtractMangaDescription, ExtractAndTranslateStatus, etc.)
    // ... (InitializeTagTranslations)
    private MangaSource GetCurrentMangaSource()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Cookies.TryGetValue("MangaSource", out var sourceString))
        {
            if (Enum.TryParse(sourceString, true, out MangaSource source))
            {
                return source;
            }
        }
        return MangaSource.MangaDex; 
    }

    public string ExtractMangaTitle(Dictionary<string, string>? titleDict, List<Dictionary<string, string>>? altTitlesList)
    {
        Debug.Assert(titleDict != null || altTitlesList != null, "Phải có ít nhất titleDict hoặc altTitlesList để trích xuất tiêu đề.");

        try
        {
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

            if (titleDict != null && titleDict.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
            {
                return enTitle;
            }

             if (titleDict != null && titleDict.TryGetValue("vi", out var mainViTitle) && !string.IsNullOrEmpty(mainViTitle))
            {
                return mainViTitle;
            }

            if (titleDict != null && titleDict.Any())
            {
                return titleDict.FirstOrDefault().Value ?? "Không có tiêu đề";
            }

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
            if (descriptionDict.TryGetValue("vi", out var viDesc) && !string.IsNullOrEmpty(viDesc))
            {
                return viDesc;
            }
            if (descriptionDict.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc))
            {
                return enDesc;
            }
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
        return _localizationService.GetStatus(status);
    }

    public string ExtractChapterDisplayTitle(ChapterAttributes attributes)
    {
        Debug.Assert(attributes != null, "ChapterAttributes không được null khi trích xuất Display Title.");

        string chapterNumber = attributes.ChapterNumber ?? "?"; 
        string chapterTitle = attributes.Title ?? ""; 

        if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
        {
            return !string.IsNullOrEmpty(chapterTitle) ? chapterTitle : "Oneshot";
        }

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
        if (altTitlesDictionary.TryGetValue("en", out var enTitles) && enTitles.Any()) return enTitles.First();
        if (altTitlesDictionary.TryGetValue("ja-ro", out var jaRoTitles) && jaRoTitles.Any()) return jaRoTitles.First();
        return altTitlesDictionary.FirstOrDefault().Value?.FirstOrDefault() ?? "";
    }

    private static readonly Dictionary<string, string> _tagTranslations = InitializeTagTranslations();
    private static Dictionary<string, string> InitializeTagTranslations()
    {
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

#### 3.3. Cập nhật các Mappers khác

*   **`MangaToMangaViewModelMapperService`**: Cần gọi `ExtractCoverUrl` và `ExtractAndTranslateTags` với logic mới.
*   **`MangaToDetailViewModelMapperService`**: Tương tự, khi tạo `MangaViewModel` bên trong.

### 4. Cập nhật `Models/MangaDexModels.cs` (Lớp `SortManga`)

Cập nhật lớp `SortManga` và phương thức `ToParams()` của nó để hỗ trợ các tham số lọc mới.

```csharp
// MangaReader_WebUI\Models\MangaDexModels.cs

// ... (using statements và các model khác) ...
public class SortManga
{
    public string Title { get; set; } = "";
    public List<string> Status { get; set; } = new List<string>();
    public string Safety { get; set; } = ""; // Sẽ được chuyển thành contentRating
    
    // THAY ĐỔI: demographic -> publicationDemographic (list)
    public List<string> Demographic { get; set; } = new List<string>(); // Giữ tên này ở UI, nhưng map sang publicationDemographic[] khi gọi API

    // THAY ĐỔI: includedTags/excludedTags thay cho genres/tags
    public List<string> IncludedTags { get; set; } = new List<string>(); // List of Tag IDs
    public string IncludedTagsMode { get; set; } = "AND"; // "AND" or "OR"
    public List<string> ExcludedTags { get; set; } = new List<string>(); // List of Tag IDs
    public string ExcludedTagsMode { get; set; } = "OR";  // "AND" or "OR"

    public List<string> Languages { get; set; } = new List<string>();
    public string SortBy { get; set; } = "latest";
    public string TimeFrame { get; set; } = ""; // Không còn sử dụng trực tiếp, sẽ map sang createdAtSince/updatedAtSince nếu cần
    // public List<string> Genres { get; set; } = new List<string>(); // Bỏ, dùng IncludedTags

    public List<string> Authors { get; set; } = new List<string>(); // List of Author/Artist IDs
    public List<string> Artists { get; set; } = new List<string>(); // Sẽ gộp vào Authors khi gọi API
    public int? Year { get; set; }
    public List<string> ContentRating { get; set; } = new List<string>();
    public List<string> OriginalLanguage { get; set; } = new List<string>();
    public List<string> ExcludedOriginalLanguage { get; set; } = new List<string>();
    public string CreatedAtSince { get; set; } = "";
    public string UpdatedAtSince { get; set; } = "";
    public bool? HasAvailableChapters { get; set; }
    public string Group { get; set; } = ""; // Scanlation Group ID

    // ... (constructor giữ nguyên hoặc cập nhật nếu cần)

    public Dictionary<string, object> ToParams()
    {
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(Title))
            parameters["title"] = Title;

        // Gộp Authors và Artists vào một list cho authorOrArtist (nếu backend hỗ trợ)
        // hoặc gửi riêng nếu backend hỗ trợ authors[] và artists[]
        // Theo ClientAPI_Update, API hỗ trợ authors[], artists[] riêng.
        // Tuy nhiên, API gốc MangaDex có vẻ dùng authorOrArtist hoặc authors[] & artists[]
        // Hiện tại, MangaReaderLib client hỗ trợ authorIdsFilter.
        var allAuthorArtistIds = new List<string>();
        if (Authors != null && Authors.Any()) allAuthorArtistIds.AddRange(Authors);
        if (Artists != null && Artists.Any()) allAuthorArtistIds.AddRange(Artists.Except(Authors)); // Tránh trùng lặp nếu có

        if (allAuthorArtistIds.Any())
            parameters["authorIdsFilter[]"] = allAuthorArtistIds.Distinct().ToList(); // Gửi dưới dạng authorIdsFilter[]
            
        if (Year.HasValue && Year > 0)
            parameters["year"] = Year.Value;

        if (Status != null && Status.Any())
            parameters["status[]"] = Status;
            
        // THAY ĐỔI: Map Demographic UI sang publicationDemographic[]
        if (Demographic != null && Demographic.Any())
            parameters["publicationDemographic[]"] = Demographic;

        // THAY ĐỔI: Xử lý IncludedTags và ExcludedTags
        if (IncludedTags != null && IncludedTags.Any())
        {
            parameters["includedTags[]"] = IncludedTags;
            parameters["includedTagsMode"] = IncludedTagsMode;
        }
        if (ExcludedTags != null && ExcludedTags.Any())
        {
            parameters["excludedTags[]"] = ExcludedTags;
            parameters["excludedTagsMode"] = ExcludedTagsMode;
        }

        if (Languages != null && Languages.Any())
        {
            var validLanguages = Languages.Where(lang => 
                !string.IsNullOrWhiteSpace(lang) && 
                System.Text.RegularExpressions.Regex.IsMatch(lang.Trim(), @"^[a-z]{2}(-[a-z]{2})?$")
            ).ToList();
            if (validLanguages.Any())
                parameters["availableTranslatedLanguage[]"] = validLanguages;
        }
        
        if (OriginalLanguage != null && OriginalLanguage.Any())
            parameters["originalLanguage[]"] = OriginalLanguage;
            
        if (ExcludedOriginalLanguage != null && ExcludedOriginalLanguage.Any())
            parameters["excludedOriginalLanguage[]"] = ExcludedOriginalLanguage;
            
        if (!string.IsNullOrEmpty(CreatedAtSince))
            parameters["createdAtSince"] = CreatedAtSince;
            
        if (!string.IsNullOrEmpty(UpdatedAtSince))
            parameters["updatedAtSince"] = UpdatedAtSince;
            
        if (HasAvailableChapters.HasValue)
            parameters["hasAvailableChapters"] = HasAvailableChapters.Value ? "true" : "false";
            
        if (!string.IsNullOrEmpty(Group))
            parameters["group"] = Group;

        // Xử lý SortBy, giữ nguyên
        // ... (giữ nguyên logic của SortBy) ...
        if (SortBy == "latest") parameters["order[updatedAt]"] = "desc";
        else if (SortBy == "title") parameters["order[title]"] = "asc";
        else if (SortBy == "popular") parameters["order[followedCount]"] = "desc";
        else if (SortBy == "relevance") parameters["order[relevance]"] = "desc";
        else if (SortBy == "rating") parameters["order[rating]"] = "desc";
        else if (SortBy == "createdAt") parameters["order[createdAt]"] = "desc";
        else if (SortBy == "year") parameters["order[year]"] = "desc";
        else parameters["order[latestUploadedChapter]"] = "desc";


        // ContentRating giữ nguyên
        if (ContentRating != null && ContentRating.Any())
        {
            parameters["contentRating[]"] = ContentRating;
        }
        // Loại bỏ Safety vì nó được map vào ContentRating
        // else if (string.IsNullOrEmpty(Safety) || Safety == "Tất cả")
        // {
        //     parameters["contentRating[]"] = new[] { "safe", "suggestive" };
        // }
        // else
        // {
        //     parameters["contentRating[]"] = new[] { Safety.ToLower() };
        // }

        return parameters;
    }
}
// ...
```

### 5. Cập nhật Controller (`Controllers/MangaController.cs`)

Phương thức `Search` sẽ cần truyền các tham số mới (như `publicationDemographic`, `includedTags`, `excludedTags`, etc.) vào `CreateSortMangaFromParameters`.

```csharp
// MangaReader_WebUI\Controllers\MangaController.cs
// ... (using statements)

public class MangaController : Controller
{
    // ... (constructor và các trường) ...

    public async Task<IActionResult> Search(
        string title = "", 
        List<string> status = null, 
        string sortBy = "latest",
        string authors = "", // Giữ nguyên, sẽ được xử lý trong CreateSortMangaFromParameters
        string artists = "", // Giữ nguyên
        int? year = null,
        List<string> availableTranslatedLanguage = null,
        List<string> publicationDemographic = null, // THÊM: Cho demographic mới
        List<string> contentRating = null,
        string includedTagsMode = "AND",            // THÊM
        string excludedTagsMode = "OR",             // THÊM
        // List<string> genres = null,              // BỎ: Dùng includedTagsStr/excludedTagsStr
        string includedTagsStr = "",                // THÊM: Chuỗi ID tag, cách nhau bởi dấu phẩy
        string excludedTagsStr = "",                // THÊM: Chuỗi ID tag, cách nhau bởi dấu phẩy
        int page = 1, 
        int pageSize = 24)
    {
        // ... (logging)
        _logger.LogInformation("[SEARCH_VIEW] Bắt đầu action Search với page={Page}, pageSize={PageSize}, includedTagsStr='{IncludedTagsStr}', excludedTagsStr='{ExcludedTagsStr}'", page, pageSize, includedTagsStr, excludedTagsStr);
        ViewData["PageType"] = "home";

        var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
            title, 
            status, 
            sortBy,
            authors, 
            artists, 
            year, 
            availableTranslatedLanguage, 
            publicationDemographic, // Truyền publicationDemographic
            contentRating,
            includedTagsMode,       // Truyền includedTagsMode
            excludedTagsMode,       // Truyền excludedTagsMode
            null,                   // genres (đã bỏ)
            includedTagsStr,        // Truyền includedTagsStr
            excludedTagsStr,        // Truyền excludedTagsStr
            // Thêm các tham số khác nếu CreateSortMangaFromParameters cần
            // Ví dụ: originalLanguage, excludedOriginalLanguage, createdAtSince, updatedAtSince, hasAvailableChapters, group
            // Hiện tại các tham số này chưa có trên UI SearchForm nên để null hoặc giá trị mặc định
            originalLanguage: null, 
            excludedOriginalLanguage: null,
            createdAtSince: null,
            updatedAtSince: null,
            hasAvailableChapters: null,
            group: null
        );
        
        // ... (phần còn lại của action Search giữ nguyên)
        var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);
        // ...
        return _viewRenderService.RenderViewBasedOnRequest(this, "Search", viewModel);
    }
    
    // GetSearchResultsPartial cũng cần cập nhật tương tự
    public async Task<IActionResult> GetSearchResultsPartial(
            string title = "", 
            List<string> status = null, 
            string sortBy = "latest",
            string authors = "",
            string artists = "",
            int? year = null,
            List<string> availableTranslatedLanguage = null,
            List<string> publicationDemographic = null, // THÊM
            List<string> contentRating = null,
            string includedTagsMode = "AND",            // THÊM
            string excludedTagsMode = "OR",             // THÊM
            string includedTagsStr = "",                // THÊM
            string excludedTagsStr = "",                // THÊM
            int page = 1, 
            int pageSize = 24)
    {
        try
        {
            var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                title, status, sortBy, authors, artists, year, 
                availableTranslatedLanguage, publicationDemographic, contentRating,
                includedTagsMode, excludedTagsMode, null, includedTagsStr, excludedTagsStr,
                // Thêm các tham số khác nếu cần
                originalLanguage: null, 
                excludedOriginalLanguage: null,
                createdAtSince: null,
                updatedAtSince: null,
                hasAvailableChapters: null,
                group: null
            );
            var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);
            // ... (phần còn lại giữ nguyên)
            if (viewModel.Mangas.Count == 0)
            {
                return PartialView("_NoResultsPartial");
            }
            return PartialView("_SearchResultsWrapperPartial", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi khi tải kết quả tìm kiếm: {ex.Message}");
            return PartialView("_NoResultsPartial");
        }
    }

    // ... (các actions khác giữ nguyên)
}
```

#### 5.1. Cập nhật `MangaSearchService.CreateSortMangaFromParameters`

```csharp
// MangaReader_WebUI\Services\MangaServices\MangaPageService\MangaSearchService.cs
// ... (using)

public class MangaSearchService
{
    // ... (constructor)

    public SortManga CreateSortMangaFromParameters(
        string title = "",
        List<string>? status = null, // Thêm ? cho nullable
        string sortBy = "latest",
        string authors = "",
        string artists = "",
        int? year = null,
        List<string>? availableTranslatedLanguage = null, // Thêm ?
        List<string>? publicationDemographic = null,    // THÊM
        List<string>? contentRating = null,              // Thêm ?
        string includedTagsMode = "AND",                 // THÊM
        string excludedTagsMode = "OR",                  // THÊM
        List<string>? genres = null,                     // Sẽ bị bỏ qua, dùng includedTagsStr
        string includedTagsStr = "",                     // THÊM
        string excludedTagsStr = "",                     // THÊM
        // Thêm các tham số mới để truyền đủ
        List<string>? originalLanguage = null,
        List<string>? excludedOriginalLanguage = null,
        string? createdAtSince = null,
        string? updatedAtSince = null,
        bool? hasAvailableChapters = null,
        string? group = null
        )
    {
        var sortManga = new SortManga
        {
            Title = title,
            Status = status ?? new List<string>(),
            SortBy = sortBy ?? "latest",
            Year = year,
            Demographic = publicationDemographic ?? new List<string>(), // Map vào Demographic của SortManga
            IncludedTagsMode = includedTagsMode ?? "AND",
            ExcludedTagsMode = excludedTagsMode ?? "OR",
            // Genres = genres, // Bỏ
            ContentRating = contentRating ?? new List<string>(), // Gán contentRating
            OriginalLanguage = originalLanguage ?? new List<string>(),
            ExcludedOriginalLanguage = excludedOriginalLanguage ?? new List<string>(),
            CreatedAtSince = createdAtSince ?? "",
            UpdatedAtSince = updatedAtSince ?? "",
            HasAvailableChapters = hasAvailableChapters,
            Group = group ?? ""
        };

        // ... (Xử lý authors, artists, languages giữ nguyên)
        if (!string.IsNullOrEmpty(authors))
        {
            sortManga.Authors = authors.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
        }
        if (!string.IsNullOrEmpty(artists))
        {
            sortManga.Artists = artists.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
        }
        if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
        {
            sortManga.Languages = availableTranslatedLanguage;
        }
        
        // Mặc định content rating nếu không được cung cấp (quan trọng)
        if (sortManga.ContentRating == null || !sortManga.ContentRating.Any())
        {
             sortManga.ContentRating = new List<string> { "safe" }; // Hoặc "safe", "suggestive" tùy theo yêu cầu
        }


        // THAY ĐỔI: Xử lý includedTagsStr và excludedTagsStr
        if (!string.IsNullOrEmpty(includedTagsStr))
        {
            sortManga.IncludedTags = includedTagsStr.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id) && Guid.TryParse(id, out _)).ToList();
        }
        if (!string.IsNullOrEmpty(excludedTagsStr))
        {
            sortManga.ExcludedTags = excludedTagsStr.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id) && Guid.TryParse(id, out _)).ToList();
        }
        
        _logger.LogInformation("SortManga parameters created: Title='{Title}', Status='{Status}', SortBy='{SortBy}', Demographic='{Demographic}', IncludedTags='{IncludedTags}' (Mode: {IncludedMode}), ExcludedTags='{ExcludedTags}' (Mode: {ExcludedMode})",
            sortManga.Title, string.Join(",", sortManga.Status), sortManga.SortBy, string.Join(",", sortManga.Demographic),
            string.Join(",", sortManga.IncludedTags), sortManga.IncludedTagsMode,
            string.Join(",", sortManga.ExcludedTags), sortManga.ExcludedTagsMode);

        return sortManga;
    }
    // ... (SearchMangaAsync giữ nguyên)
}
```

### 6. Cập nhật Views

#### 6.1. `Views/MangaSearch/_SearchFormPartial.cshtml`

*   Thay đổi `select` cho "Đối tượng độc giả" thành `multiple` (nếu muốn cho phép chọn nhiều, nhưng API giờ là `publicationDemographicsFilter[]` nên có thể giữ single select và gửi giá trị đó trong mảng một phần tử). **Giữ single select cho đơn giản trên UI này.**
*   Thay thế input `genres` bằng các input ẩn cho `includedTagsStr`, `excludedTagsStr`, `includedTagsMode`, `excludedTagsMode`.
*   UI chọn tag sẽ được quản lý bởi JavaScript (`search-tags-dropdown.js`).

```html
@* Views/MangaSearch/_SearchFormPartial.cshtml *@
@model MangaReader.WebUI.Models.MangaListViewModel
@* ... (phần đầu giữ nguyên) ... *@
<div class="card search-card">
    <div class="card-body">
        <form asp-action="Search" method="get" id="searchForm" hx-get="@Url.Action("Search", "Manga")" hx-target="#search-results-and-pagination" hx-push-url="true">
            @* ... (input title, nút Tìm kiếm) ... *@
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
            </div>
            
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
                                    @* Tooltip content giữ nguyên *@
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
                                    @* ... (Nội dung sẽ được JS cập nhật) ... *@
                                    <span class="manga-tags-empty" id="emptyTagsMessage">Chưa có thẻ nào được chọn. Bấm để chọn thẻ.</span>
                                </div>
                            </div>
                            <div class="manga-tags-dropdown" id="mangaTagsDropdown">@* Nội dung dropdown được JS render *@</div>
                        </div>
                        @* Input ẩn cho includedTagsStr và excludedTagsStr *@
                        <input type="hidden" id="selectedTags" name="includedTagsStr" value="@string.Join(",", Model.SortOptions.IncludedTags ?? new List<string>())" />
                        <input type="hidden" id="excludedTags" name="excludedTagsStr" value="@string.Join(",", Model.SortOptions.ExcludedTags ?? new List<string>())" />
                    </div>

                    @* ... (Sắp xếp theo, Mức độ nội dung, Số kết quả/trang) ... *@
                     <!-- Sắp xếp theo -->
                    <div class="col-md-4">
                        <label class="filter-dropdown-label">Sắp xếp theo</label>
                        <div class="filter-dropdown">
                            <button type="button" class="filter-toggle-btn">
                                <span class="selected-text">
                                    @{
                                        var sortByText = Model.SortOptions.SortBy switch
                                        {
                                            "title" => "Tên (A-Z)",
                                            "popular" => "Phổ biến",
                                            "relevance" => "Liên quan",
                                            "rating" => "Đánh giá",
                                            "createdAt" => "Thời gian tạo",
                                            "year" => "Năm xuất bản",
                                            _ => "Mới nhất"
                                        };
                                    }
                                    @sortByText
                                </span>
                                <span class="toggle-icon"></span>
                            </button>
                            <div class="filter-menu-content">
                                <div class="filter-menu-padding">
                                    <div class="filter-option">
                                        <input type="radio" name="sortBy" id="sortLatest" value="latest" @(Model.SortOptions.SortBy == "latest" || string.IsNullOrEmpty(Model.SortOptions.SortBy) ? "checked" : "")>
                                        <label class="filter-option-label" for="sortLatest">Mới nhất</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="radio" name="sortBy" id="sortTitle" value="title" @(Model.SortOptions.SortBy == "title" ? "checked" : "")>
                                        <label class="filter-option-label" for="sortTitle">Tên (A-Z)</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="radio" name="sortBy" id="sortPopular" value="popular" @(Model.SortOptions.SortBy == "popular" ? "checked" : "")>
                                        <label class="filter-option-label" for="sortPopular">Phổ biến</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="radio" name="sortBy" id="sortRelevance" value="relevance" @(Model.SortOptions.SortBy == "relevance" ? "checked" : "")>
                                        <label class="filter-option-label" for="sortRelevance">Liên quan</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="radio" name="sortBy" id="sortRating" value="rating" @(Model.SortOptions.SortBy == "rating" ? "checked" : "")>
                                        <label class="filter-option-label" for="sortRating">Đánh giá</label>
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
                    
                    <!-- Đánh giá nội dung -->
                    <div class="col-md-4">
                        <label class="filter-dropdown-label">Mức độ nội dung</label>
                        <div class="filter-dropdown">
                            <button type="button" class="filter-toggle-btn">
                                <span class="selected-text">
                                    @{
                                        var selectedRating = Model.SortOptions.ContentRating != null && Model.SortOptions.ContentRating.Any()
                                            ? string.Join(", ", Model.SortOptions.ContentRating.Select(TranslateContentRating))
                                            : "Tất cả";
                                        
                                        var selectedCount = Model.SortOptions.ContentRating?.Count() ?? 0;
                                        var defaultThree = selectedCount == 3 && 
                                                          (Model.SortOptions.ContentRating?.Contains("safe") ?? false) &&
                                                          (Model.SortOptions.ContentRating?.Contains("suggestive") ?? false) &&
                                                          (Model.SortOptions.ContentRating?.Contains("erotica") ?? false);
                                        
                                        if (defaultThree) {
                                            selectedRating = "An Toàn, Nhạy cảm, R18"; // Hiển thị rút gọn
                                        }
                                    }
                                    @selectedRating
                                </span>
                                <span class="toggle-icon"></span>
                            </button>
                            <div class="filter-menu-content">
                                <div class="filter-menu-padding">
                                    <div class="filter-option">
                                        <input type="checkbox" name="contentRating" id="contentSafe" value="safe" 
                                               @(Model.SortOptions.ContentRating == null || Model.SortOptions.ContentRating.Contains("safe") ? "checked" : "")>
                                        <label class="filter-option-label" for="contentSafe">An Toàn</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="contentRating" id="contentSuggestive" value="suggestive" 
                                               @(Model.SortOptions.ContentRating == null || Model.SortOptions.ContentRating.Contains("suggestive") ? "checked" : "")>
                                        <label class="filter-option-label" for="contentSuggestive">Nhạy cảm</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="contentRating" id="contentErotica" value="erotica" 
                                               @(Model.SortOptions.ContentRating == null || Model.SortOptions.ContentRating.Contains("erotica") ? "checked" : "")>
                                        <label class="filter-option-label" for="contentErotica">R18</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="contentRating" id="contentPornographic" value="pornographic" 
                                               @(Model.SortOptions.ContentRating != null && Model.SortOptions.ContentRating.Contains("pornographic") ? "checked" : "")>
                                        <label class="filter-option-label" for="contentPornographic">NSFW</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Số kết quả mỗi trang -->
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
                    
                    <!-- THAY ĐỔI: Đối tượng độc giả (publicationDemographic) -->
                    <div class="col-md-4">
                        <label class="filter-dropdown-label">Đối tượng độc giả</label>
                        <div class="filter-dropdown">
                            <button type="button" class="filter-toggle-btn">
                                <span class="selected-text">
                                    @(Model.SortOptions.Demographic != null && Model.SortOptions.Demographic.Any() 
                                      ? string.Join(", ", Model.SortOptions.Demographic.Select(TranslateDemographic)) 
                                      : "Tất cả")
                                </span>
                                <span class="toggle-icon"></span>
                            </button>
                            <div class="filter-menu-content">
                                <div class="filter-menu-padding">
                                    <div class="filter-option">
                                        <input type="checkbox" name="publicationDemographic" id="demoShounen" value="shounen" @(Model.SortOptions.Demographic != null && Model.SortOptions.Demographic.Contains("shounen") ? "checked" : "")>
                                        <label class="filter-option-label" for="demoShounen">Shounen</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="publicationDemographic" id="demoShoujo" value="shoujo" @(Model.SortOptions.Demographic != null && Model.SortOptions.Demographic.Contains("shoujo") ? "checked" : "")>
                                        <label class="filter-option-label" for="demoShoujo">Shoujo</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="publicationDemographic" id="demoSeinen" value="seinen" @(Model.SortOptions.Demographic != null && Model.SortOptions.Demographic.Contains("seinen") ? "checked" : "")>
                                        <label class="filter-option-label" for="demoSeinen">Seinen</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="publicationDemographic" id="demoJosei" value="josei" @(Model.SortOptions.Demographic != null && Model.SortOptions.Demographic.Contains("josei") ? "checked" : "")>
                                        <label class="filter-option-label" for="demoJosei">Josei</label>
                                    </div>
                                    @* Thêm "None" nếu backend hỗ trợ *@
                                    @* <div class="filter-option">
                                        <input type="checkbox" name="publicationDemographic" id="demoNone" value="none" @(Model.SortOptions.Demographic != null && Model.SortOptions.Demographic.Contains("none") ? "checked" : "")>
                                        <label class="filter-option-label" for="demoNone">Khác</label>
                                    </div> *@
                                </div>
                            </div>
                        </div>
                    </div>

                    @* ... (Trạng thái, Ngôn ngữ, Tác giả, Họa sĩ, Năm, Nút Reset/Apply) ... *@
                    <!-- Trạng thái -->
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
                                        <input type="checkbox" name="status" id="statusOngoing" value="ongoing" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("ongoing") ? "checked" : "")>
                                        <label class="filter-option-label" for="statusOngoing">Đang tiến hành</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="status" id="statusCompleted" value="completed" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("completed") ? "checked" : "")>
                                        <label class="filter-option-label" for="statusCompleted">Hoàn thành</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="status" id="statusHiatus" value="hiatus" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("hiatus") ? "checked" : "")>
                                        <label class="filter-option-label" for="statusHiatus">Tạm ngưng</label>
                                    </div>
                                    <div class="filter-option">
                                        <input type="checkbox" name="status" id="statusCancelled" value="cancelled" @(Model.SortOptions.Status != null && Model.SortOptions.Status.Contains("cancelled") ? "checked" : "")>
                                        <label class="filter-option-label" for="statusCancelled">Đã hủy</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Ngôn ngữ có sẵn -->
                    <div class="col-md-4">
                        <label class="filter-dropdown-label">Ngôn ngữ</label>
                        <div class="filter-dropdown">
                            <button type="button" class="filter-toggle-btn">
                                <span class="selected-text">
                                    @(Model.SortOptions.Languages != null && Model.SortOptions.Languages.Any() ? 
                                      string.Join(", ", Model.SortOptions.Languages.Select(l => 
                                        l == "vi" ? "Tiếng Việt" : 
                                        l == "en" ? "Tiếng Anh" : 
                                        l == "ja" ? "Tiếng Nhật" : 
                                        l == "ko" ? "Tiếng Hàn" : 
                                        l == "zh" ? "Tiếng Trung" : l)) : "Tất cả")
                                </span>
                                <span class="toggle-icon"></span>
                            </button>
                            <div class="filter-menu-content">
                                <div class="filter-menu-padding">
                                    <div class="filter-option">
                                        <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langVi" value="vi" @(Model.SortOptions.Languages != null && Model.SortOptions.Languages.Contains("vi") ? "checked" : "")>
                                        <label class="filter-option-label" for="langVi">Tiếng Việt</label>
                                    </div>
                                    <div class="filter-option">
                                        <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langEn" value="en" @(Model.SortOptions.Languages != null && Model.SortOptions.Languages.Contains("en") ? "checked" : "")>
                                        <label class="filter-option-label" for="langEn">Tiếng Anh</label>
                                    </div>
                                    <div class="filter-option">
                                        <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langJp" value="ja" @(Model.SortOptions.Languages != null && Model.SortOptions.Languages.Contains("ja") ? "checked" : "")>
                                        <label class="filter-option-label" for="langJp">Tiếng Nhật</label>
                                    </div>
                                    <div class="filter-option">
                                        <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langKo" value="ko" @(Model.SortOptions.Languages != null && Model.SortOptions.Languages.Contains("ko") ? "checked" : "")>
                                        <label class="filter-option-label" for="langKo">Tiếng Hàn</label>
                                    </div>
                                    <div class="filter-option">
                                        <input class="filter-check-input" type="checkbox" name="availableTranslatedLanguage" id="langZh" value="zh" @(Model.SortOptions.Languages != null && Model.SortOptions.Languages.Contains("zh") ? "checked" : "")>
                                        <label class="filter-option-label" for="langZh">Tiếng Trung</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="col-md-4">
                        <label class="form-label">Tác giả</label>
                        <input type="text" name="authors" class="form-control" placeholder="Nhập tên tác giả..." value="@(string.Join(", ", Model.SortOptions.Authors ?? new List<string>()))">
                    </div>
                    
                    <div class="col-md-4">
                        <label class="form-label">Họa sĩ</label>
                        <input type="text" name="artists" class="form-control" placeholder="Nhập tên họa sĩ..." value="@(string.Join(", ", Model.SortOptions.Artists ?? new List<string>()))">
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
            
            <input type="hidden" name="page" value="1" />
        </form>
    </div>
</div>
@* ... (các @functions) ... *@
@functions {
    public string TranslateStatus(string status)
    {
        return status switch
        {
            "ongoing" => "Đang tiến hành",
            "completed" => "Hoàn thành",
            "hiatus" => "Tạm ngưng",
            "cancelled" => "Đã hủy",
            _ => status // Trả về giá trị gốc nếu không khớp
        };
    }

    public string TranslateContentRating(string rating)
    {
        return rating switch
        {
            "safe" => "An Toàn",
            "suggestive" => "Nhạy cảm",
            "erotica" => "R18",
            "pornographic" => "NSFW",
            _ => rating
        };
    }
    
    public string TranslateDemographic(string demographic)
    {
        return demographic switch
        {
            "shounen" => "Shounen",
            "shoujo" => "Shoujo",
            "seinen" => "Seinen",
            "josei" => "Josei",
            "none" => "Khác",
            _ => demographic
        };
    }
}
```

#### 6.2. Hiển thị tags trong `Views/Shared/_MangaGridPartial.cshtml` và `_MangaListPartial.cshtml`

Phần này không cần thay đổi vì `MangaViewModel.Tags` đã là `List<string>` (đã được xử lý ở Mapper).

#### 6.3. Hiển thị tags trong `Views/Manga/MangaDetails/Details.cshtml`

Phần này cũng không cần thay đổi vì `Model.Manga.Tags` đã được Mapper xử lý.

---

**Lưu ý quan trọng:**

*   Các thay đổi trên `MangaReader_ManagerUI` và `MangaReader_WebUI` giả định rằng `MangaReaderLib` đã được build và các project khác đã tham chiếu đến phiên bản mới nhất của nó.
*   Đảm bảo kiểm tra kỹ lưỡng các thay đổi về tên thuộc tính và kiểu dữ liệu giữa các lớp.
*   Sau khi cập nhật code, cần test lại tất cả các luồng liên quan đến lọc và hiển thị manga.
*   Đối với việc hiển thị tên Author/Artist từ `RelationshipObject.Attributes` trong `MangaReader_ManagerUI` (nếu `mangaIncludes` được sử dụng), bạn cần điều chỉnh logic trong `MangaTable.jsx` hoặc nơi hiển thị tương ứng để đọc từ `row.relationships[...].attributes.name` thay vì `row.processedAuthors` (trừ khi store đã xử lý việc này). Hiện tại, `mangaStore.js` đã xử lý `processedAuthors` và `processedArtists` nên có thể không cần thay đổi `MangaTable.jsx` nhiều.
*   Trong `MangaReader_WebUI`, `MangaDataExtractorService` cần được cập nhật để đọc `rel.Attributes` dưới dạng `JsonElement` và deserialize nó thành `AuthorAttributes` của `Models.Mangadex` nếu cần lấy tên tác giả/họa sĩ từ đó (nếu MangaDex API trả về attributes trong relationship).

Hy vọng hướng dẫn này đầy đủ và chi tiết cho bạn!
```