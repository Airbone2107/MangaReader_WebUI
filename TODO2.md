# TODO: Cập nhật Frontend để hỗ trợ tìm kiếm Manga nâng cao

Tài liệu này hướng dẫn các bước cần thiết để cập nhật dự án `MangaReader_ManagerUI` nhằm hỗ trợ các tính năng tìm kiếm mới cho Manga, bao gồm lọc theo Tác giả (Author), Họa sĩ (Artist) và Ngôn ngữ dịch có sẵn (Available Translated Languages).

Các thay đổi sẽ được thực hiện ở cả tầng Server (proxy) và Client (React) của `MangaReader_ManagerUI`.

---

## Phần 1: Cập nhật Backend Proxy (`MangaReader_ManagerUI.Server`)

Bước đầu tiên là cập nhật `MangasController` để nó có thể nhận và chuyển tiếp các tham số query mới (`authors`, `artists`, `availableTranslatedLanguage`) đến `MangaReaderLib`.

### Bước 1.1: Cập nhật `MangasController.cs`

Chỉnh sửa phương thức `GetMangas` để thay thế tham số `authorIdsFilter` cũ bằng các tham số `authors`, `artists`, và `availableTranslatedLanguage` mới.

<!-- file path="MangaReader_ManagerUI\MangaReader_ManagerUI.Server\Controllers\MangasController.cs" -->
```csharp
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MangaReaderLib.Services.Exceptions;
using System.Net;
using MangaReaderLib.Enums;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class MangasController : BaseApiController
    {
        private readonly IMangaClient _mangaClient;
        private readonly ILogger<MangasController> _logger;

        public MangasController(IMangaClient mangaClient, ILogger<MangasController> logger)
        {
            _mangaClient = mangaClient;
            _logger = logger;
        }

        // GET: api/Mangas
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangas(
            [FromQuery] int? offset, 
            [FromQuery] int? limit, 
            [FromQuery] string? titleFilter,
            [FromQuery] string? statusFilter, 
            [FromQuery] string? contentRatingFilter, 
            [FromQuery(Name = "publicationDemographicsFilter[]")] List<PublicationDemographic>? publicationDemographicsFilter,
            [FromQuery] string? originalLanguageFilter,
            [FromQuery] int? yearFilter,
            [FromQuery(Name = "authors[]")] List<Guid>? authors, // THAY ĐỔI
            [FromQuery(Name = "artists[]")] List<Guid>? artists, // THÊM MỚI
            [FromQuery(Name = "availableTranslatedLanguage[]")] List<string>? availableTranslatedLanguage, // THÊM MỚI
            [FromQuery(Name = "includedTags[]")] List<Guid>? includedTags,
            [FromQuery] string? includedTagsMode,
            [FromQuery(Name = "excludedTags[]")] List<Guid>? excludedTags,
            [FromQuery] string? excludedTagsMode,
            [FromQuery] string? orderBy, 
            [FromQuery] bool? ascending,
            [FromQuery(Name = "includes[]")] List<string>? includes)
        {
            _logger.LogInformation("API: Requesting list of mangas.");
            try
            {
                var result = await _mangaClient.GetMangasAsync(
                    offset, 
                    limit, 
                    titleFilter, 
                    statusFilter, 
                    contentRatingFilter, 
                    publicationDemographicsFilter, 
                    originalLanguageFilter,
                    yearFilter, 
                    authors, // THAY ĐỔI
                    artists, // THÊM MỚI
                    availableTranslatedLanguage, // THÊM MỚI
                    includedTags,
                    includedTagsMode,
                    excludedTags,
                    excludedTagsMode,
                    orderBy, 
                    ascending,
                    includes,
                    HttpContext.RequestAborted);
                
                if (result == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new ApiErrorResponse(new ApiError(500, "API Error", "Failed to fetch mangas from the backend API.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderAPI. Status: {StatusCode}", ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching mangas.");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Mangas/{mangaId}
        [HttpGet("{mangaId}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangaById(Guid mangaId, [FromQuery(Name = "includes[]")] List<string>? includes)
        {
            _logger.LogInformation("API: Requesting manga by ID: {MangaId}", mangaId);
            try
            {
                var result = await _mangaClient.GetMangaByIdAsync(mangaId, includes, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found in backend. Status: {StatusCode}", mangaId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // POST: api/Mangas
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateManga([FromBody] CreateMangaRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create manga: {Title}", createDto.Title);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage, context: new { field = e.ErrorMessage }))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                var result = await _mangaClient.CreateMangaAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null) 
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Creation Failed", "Could not create manga via backend API.")));
                }
                return CreatedAtAction(nameof(GetMangaById), new { mangaId = Guid.Parse(result.Data.Id) }, result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error creating manga. Status: {StatusCode}", ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error creating manga. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while creating manga.");
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // PUT: api/Mangas/{mangaId}
        [HttpPut("{mangaId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateManga(Guid mangaId, [FromBody] UpdateMangaRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update manga: {MangaId}", mangaId);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage, context: new { field = e.ErrorMessage }))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                await _mangaClient.UpdateMangaAsync(mangaId, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found for update. Status: {StatusCode}", mangaId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error updating manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while updating manga {MangaId}.", mangaId);
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/Mangas/{mangaId}
        [HttpDelete("{mangaId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteManga(Guid mangaId)
        {
            _logger.LogInformation("API: Request to delete manga: {MangaId}", mangaId);
            try
            {
                await _mangaClient.DeleteMangaAsync(mangaId, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found for deletion. Status: {StatusCode}", mangaId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while deleting manga {MangaId}.", mangaId);
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // POST: api/Mangas/{mangaId}/covers
        [HttpPost("{mangaId}/covers")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadMangaCover(Guid mangaId, [FromForm] IFormFile file, [FromForm] string? volume, [FromForm] string? description)
        {
            _logger.LogInformation("API: Request to upload cover for Manga ID: {MangaId}", mangaId);
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", "No file uploaded.")));
            }
            try
            {
                using var stream = file.OpenReadStream();
                var result = await _mangaClient.UploadMangaCoverAsync(mangaId, stream, file.FileName, volume, description, HttpContext.RequestAborted);
                
                if (result == null || result.Data == null)
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Upload Failed", "Could not upload cover via backend API.")));
                }
                // FrontendAPI.md trả về 201 Created cho POST /mangas/{mangaId}/covers
                return CreatedAtAction(nameof(CoverArtsController.GetCoverArtById), "CoverArts", new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found for cover upload. Status: {StatusCode}", mangaId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error uploading cover for manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while uploading cover for manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Mangas/{mangaId}/covers
        [HttpGet("{mangaId}/covers")]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangaCovers(Guid mangaId, [FromQuery] int? offset, [FromQuery] int? limit)
        {
            _logger.LogInformation("API: Requesting covers for manga ID: {MangaId}", mangaId);
            try
            {
                var result = await _mangaClient.GetMangaCoversAsync(mangaId, offset, limit, HttpContext.RequestAborted);
                if (result == null)
                {
                    // This case implies an issue with the ApiClient or a non-HTTP error before the request
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found or has no covers.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found when fetching covers. Status: {StatusCode}", mangaId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching covers for manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching covers for manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Mangas/{mangaId}/translations
        [HttpGet("{mangaId}/translations")]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangaTranslations(Guid mangaId, [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting translations for manga ID: {MangaId}", mangaId);
            try
            {
                var result = await _mangaClient.GetMangaTranslationsAsync(mangaId, offset, limit, orderBy, ascending, HttpContext.RequestAborted);
                if (result == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found or has no translations.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found when fetching translations. Status: {StatusCode}", mangaId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching translations for manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching translations for manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
    }
}
```
---

## Phần 2: Cập nhật Frontend Client (`mangareader_managerui.client`)

Bây giờ, chúng ta sẽ cập nhật các thành phần trên giao diện người dùng React để sử dụng các tính năng tìm kiếm mới.

### Bước 2.1: Cập nhật Định nghĩa Types

Đồng bộ interface `GetMangasParams` trong `types/manga.ts` với các tham số mới của API.

<!-- file path="MangaReader_ManagerUI\mangareader_managerui.client\src\types\manga.ts" -->
```typescript
import { RelationshipObject, ResourceObject } from './api'

// DTO mới cho Tag khi được nhúng trong Manga Attributes
export interface TagInMangaAttributesDto {
  name: string;
  tagGroupName: string;
}

// Attributes DTOs
export interface MangaAttributes {
  title: string
  originalLanguage: string
  publicationDemographic: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number | null
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  createdAt: string
  updatedAt: string
  tags: ResourceObject<TagInMangaAttributesDto>[]
  availableTranslatedLanguages?: string[] // THÊM DÒNG NÀY
}

export interface AuthorAttributes {
  name: string
  biography?: string
  createdAt: string
  updatedAt: string
}

export interface TagAttributes {
  name: string
  tagGroupName: string
  createdAt: string
  updatedAt: string
}

export interface TagGroupAttributes {
  name: string
  createdAt: string
  updatedAt: string
}

export interface TranslatedMangaAttributes {
  languageKey: string
  title: string
  description?: string
  createdAt: string
  updatedAt: string
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

export interface ChapterPageAttributes {
  pageNumber: number
  publicId: string
}

export interface CoverArtAttributes {
  volume?: string
  publicId: string
  description?: string
  createdAt: string
  updatedAt: string
}

// Full Resource Objects (including ID, type, attributes, relationships)
export interface Manga {
  id: string
  type: 'manga'
  attributes: MangaAttributes
  relationships?: RelationshipObject[]
  coverArtPublicId?: string
}

export interface Author {
  id: string
  type: 'author'
  attributes: AuthorAttributes
  relationships?: RelationshipObject[]
}

export interface Tag {
  id: string
  type: 'tag'
  attributes: TagAttributes
  relationships?: RelationshipObject[]
}

export interface TagGroup {
  id: string
  type: 'tag_group'
  attributes: TagGroupAttributes
  relationships?: RelationshipObject[]
}

export interface TranslatedManga {
  id: string
  type: 'translated_manga'
  attributes: TranslatedMangaAttributes
  relationships?: RelationshipObject[]
}

export interface Chapter {
  id: string
  type: 'chapter'
  attributes: ChapterAttributes
  relationships?: RelationshipObject[]
}

export interface ChapterPage {
  id: string
  type: 'chapter_page'
  attributes: ChapterPageAttributes
  relationships?: RelationshipObject[]
}

export interface CoverArt {
  id: string
  type: 'cover_art'
  attributes: CoverArtAttributes
  relationships?: RelationshipObject[]
}

// Request DTOs and Params
export type PublicationDemographicType = 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None';
export type MangaStatusType = 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled';
export type ContentRatingType = 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic';

export interface GetMangasParams {
  offset?: number;
  limit?: number;
  titleFilter?: string;
  statusFilter?: MangaStatusType | '';
  contentRatingFilter?: ContentRatingType | '';
  publicationDemographicsFilter?: PublicationDemographicType[];
  originalLanguageFilter?: string;
  yearFilter?: number | null;
  authors?: string[]; // THAY ĐỔI
  artists?: string[]; // THÊM MỚI
  availableTranslatedLanguage?: string[]; // THÊM MỚI
  includedTags?: string[];
  includedTagsMode?: 'AND' | 'OR';
  excludedTags?: string[];
  excludedTagsMode?: 'AND' | 'OR';
  orderBy?: string;
  ascending?: boolean;
  includes?: ('cover_art' | 'author' | 'artist')[];
}

export interface CreateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number | null
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  tagIds?: string[] // Array of GUID strings
  authors?: MangaAuthorInput[]
}

export interface UpdateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number | null
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  tagIds?: string[]
  authors?: MangaAuthorInput[]
}

export interface MangaAuthorInput {
  authorId: string // GUID string
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
  publishAt: string 
  readableAt: string 
}

export interface UpdateChapterRequest {
  volume?: string
  chapterNumber?: string
  title?: string
  publishAt: string 
  readableAt: string 
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
}
```

### Bước 2.2: Cập nhật State Management (`mangaStore`)

Điều chỉnh `filters` trong `mangaStore.js` để lưu trạng thái của các bộ lọc mới và cập nhật logic fetch dữ liệu.

<!-- file path="MangaReader_ManagerUI\mangareader_managerui.client\src\stores\mangaStore.js" -->
```javascript
import { create } from 'zustand'
import { persistStore } from '../utils/zustandPersist'
import mangaApi from '../api/mangaApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT, RELATIONSHIP_TYPES } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').Manga} Manga
 * @typedef {import('../types/api').ApiCollectionResponse<Manga>} MangaCollectionResponse
 * @typedef {import('../types/api').AuthorInRelationshipAttributes} AuthorInRelationshipAttributes
 * @typedef {import('../types/manga').CoverArtAttributes} CoverArtAttributes
 */

const useMangaStore = create(persistStore((set, get) => ({
  /** @type {Manga[]} */
  mangas: [],
  totalMangas: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    titleFilter: '',
    statusFilter: '',
    contentRatingFilter: '',
    publicationDemographicsFilter: [],
    originalLanguageFilter: '',
    yearFilter: null,
    includedTags: [],
    includedTagsMode: 'AND',
    excludedTags: [],
    excludedTagsMode: 'OR',
    authors: [], // THAY ĐỔI
    artists: [], // THÊM MỚI
    availableTranslatedLanguage: [], // THÊM MỚI
  },
  sort: {
    orderBy: 'updatedAt',
    ascending: false,
  },

  /**
   * Fetch mangas from API.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchMangas: async (resetPagination = false) => {
    const { page, rowsPerPage, filters, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    /** @type {import('../types/manga').GetMangasParams} */
    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      titleFilter: filters.titleFilter || undefined,
      statusFilter: filters.statusFilter || undefined,
      contentRatingFilter: filters.contentRatingFilter || undefined,
      publicationDemographicsFilter: filters.publicationDemographicsFilter?.length > 0 ? filters.publicationDemographicsFilter : undefined,
      originalLanguageFilter: filters.originalLanguageFilter || undefined,
      yearFilter: filters.yearFilter === null || filters.yearFilter === undefined ? undefined : filters.yearFilter,
      includedTags: filters.includedTags?.length > 0 ? filters.includedTags : undefined,
      includedTagsMode: filters.includedTags?.length > 0 ? filters.includedTagsMode : undefined,
      excludedTags: filters.excludedTags?.length > 0 ? filters.excludedTags : undefined,
      excludedTagsMode: filters.excludedTags?.length > 0 ? filters.excludedTagsMode : undefined,
      authors: filters.authors?.length > 0 ? filters.authors : undefined, // THAY ĐỔI
      artists: filters.artists?.length > 0 ? filters.artists : undefined, // THÊM MỚI
      availableTranslatedLanguage: filters.availableTranslatedLanguage?.length > 0 ? filters.availableTranslatedLanguage : undefined, // THÊM MỚI
      orderBy: sort.orderBy,
      ascending: sort.ascending,
      includes: ['cover_art', 'author', 'artist'],
    }
    
    // Xóa các trường undefined để query string sạch hơn
    Object.keys(queryParams).forEach(key => queryParams[key] === undefined && delete queryParams[key]);

    try {
      /** @type {MangaCollectionResponse} */
      const response = await mangaApi.getMangas(queryParams)
      
      const mangasWithProcessedInfo = response.data.map(manga => {
        let coverArtPublicId = null;
        const coverArtRel = manga.relationships?.find(rel => rel.type === RELATIONSHIP_TYPES.COVER_ART);
        
        if (coverArtRel && coverArtRel.attributes) {
            const coverAttributes = /** @type {CoverArtAttributes} */ (coverArtRel.attributes);
            if (coverAttributes && typeof coverAttributes.publicId === 'string') {
                coverArtPublicId = coverAttributes.publicId;
            }
        }

        return { 
          ...manga, 
          coverArtPublicId,
        };
      });

      set({
        mangas: mangasWithProcessedInfo,
        totalMangas: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch mangas:', error)
      set({ mangas: [], totalMangas: 0 })
    }
  },

  /**
   * Handle page change from DataTableMUI.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   */
  setPage: (event, newPage) => {
    set({ page: newPage });
    get().fetchMangas(false); 
  },

  /**
   * Handle rows per page change from DataTableMUI.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
    get().fetchMangas(true); 
  },

  /**
   * Handle sort change from DataTableMUI.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
    get().fetchMangas(true); 
  },

  /**
   * Update a specific filter value in the store.
   * This does NOT trigger a fetch immediately.
   * @param {string} filterName - The name of the filter property (e.g., 'titleFilter').
   * @param {any} value - The new value for the filter.
   */
  setFilter: (filterName, value) => {
    set(state => ({
      filters: { ...state.filters, [filterName]: value }
    }));
  },

  /**
   * Apply filters and refetch mangas.
   * @param {object} newFilters - New filter values.
   */
  applyFilters: (newFilters) => {
    set((state) => ({
      filters: { ...state.filters, ...newFilters },
      page: 0, 
    }));
    // fetchMangas sẽ được gọi riêng sau khi applyFilters trong component
  },

  /**
   * Reset all filters to their initial state.
   */
  resetFilters: () => {
    set({
      filters: {
        titleFilter: '',
        statusFilter: '',
        contentRatingFilter: '',
        publicationDemographicsFilter: [],
        originalLanguageFilter: '',
        yearFilter: null,
        includedTags: [],
        includedTagsMode: 'AND',
        excludedTags: [],
        excludedTagsMode: 'OR',
        authors: [], // THAY ĐỔI
        artists: [], // THÊM MỚI
        availableTranslatedLanguage: [], // THÊM MỚI
      },
      page: 0,
    });
    get().fetchMangas(true);
  },

  /**
   * Delete a manga.
   * @param {string} id - ID of the manga to delete.
   */
  deleteManga: async (id) => {
    try {
      await mangaApi.deleteManga(id)
      showSuccessToast('Xóa manga thành công!')
      get().fetchMangas() 
    } catch (error) {
      console.error('Failed to delete manga:', error)
    }
  },
}), 'manga'))

export default useMangaStore
```

### Bước 2.3: Cập nhật Giao diện Tìm kiếm (`MangaListPage.jsx`)

Đây là thay đổi lớn nhất, nơi chúng ta sẽ thêm các trường `Autocomplete` và `Select` mới vào form lọc.

<!-- file path="MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\pages\MangaListPage.jsx" -->
```javascript
import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import {
    Autocomplete,
    Box,
    Button,
    Chip,
    Grid,
    MenuItem,
    TextField,
    Typography,
    FormControl,
    InputLabel,
    Select,
    OutlinedInput,
    Checkbox,
    ListItemText,
} from '@mui/material'
import React, { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import {
    CONTENT_RATING_OPTIONS,
    MANGA_STATUS_OPTIONS,
    ORIGINAL_LANGUAGE_OPTIONS,
    PUBLICATION_DEMOGRAPHIC_OPTIONS,
} from '../../../constants/appConstants'
import useMangaStore from '../../../stores/mangaStore'
import useUiStore from '../../../stores/uiStore'
import { handleApiError } from '../../../utils/errorUtils'
import MangaTable from '../components/MangaTable'

/**
 * @typedef {import('../../../types/manga').Author} AuthorForFilter
 * @typedef {import('../../../types/manga').Tag} TagForFilter
 * @typedef {import('../../../types/manga').PublicationDemographicType} PublicationDemographicType
 */

function MangaListPage() {
  const navigate = useNavigate()
  const {
    mangas,
    totalMangas,
    page,
    rowsPerPage,
    filters,
    sort,
    fetchMangas,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteManga,
  } = useMangaStore()

  const isLoading = useUiStore(state => state.isLoading);

  /** @type {[AuthorForFilter[], React.Dispatch<React.SetStateAction<AuthorForFilter[]>>]} */
  const [availableAuthors, setAvailableAuthors] = useState([])
  /** @type {[TagForFilter[], React.Dispatch<React.SetStateAction<TagForFilter[]>>]} */
  const [availableTags, setAvailableTags] = useState([])
  
  const [localFilters, setLocalFilters] = useState(filters);

  useEffect(() => {
    setLocalFilters(filters); 
  }, [filters]);

  useEffect(() => {
    fetchMangas(true); 
  }, [fetchMangas]);

  useEffect(() => {
    const fetchFilterOptions = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 });
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name, type: 'author' })))

        const tagsResponse = await tagApi.getTags({ limit: 1000 });
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name, type: 'tag' })));
      } catch (error) {
        handleApiError(error, 'Không thể tải tùy chọn lọc.');
      }
    };
    fetchFilterOptions();
  }, []);

  const handleLocalFilterChange = (filterName, value) => {
    setLocalFilters(prev => ({ ...prev, [filterName]: value }));
  };

  const handleApplyLocalFilters = () => {
    applyFilters(localFilters); 
    fetchMangas(true); 
  }

  const handleResetLocalFilters = () => {
    resetFilters(); 
    setLocalFilters(useMangaStore.getState().filters); 
  }

  const ITEM_HEIGHT = 48;
  const ITEM_PADDING_TOP = 8;
  const MenuProps = {
    PaperProps: {
      style: {
        maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
        width: 250,
      },
    },
  };
  
  const renderMultiSelectDisplay = (selectedItems, getTagProps, maxItemsToShow = 2) => {
    const numItems = selectedItems.length;
    const itemsToRender = selectedItems.slice(0, maxItemsToShow);
    
    let displayChips = itemsToRender.map((item, index) => {
      const label = typeof item === 'object' ? (item.name || item.label) : item;
      const key = typeof item === 'object' ? (item.id || item.value || index) : item;

      return (
        <Chip 
          variant="outlined" 
          label={label}
          size="small" 
          {...(getTagProps ? getTagProps({ index }) : {})} 
          key={key} 
          sx={{ 
            maxWidth: '120px', 
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
            mr: 0.5, 
            '&:last-child': {
                mr: (numItems <= maxItemsToShow && index === itemsToRender.length -1) ? 0 : 0.5,
            }
          }}
        />
      );
    });

    if (numItems > maxItemsToShow) {
      displayChips.push(
        <Chip 
          variant="outlined" 
          label={`+${numItems - maxItemsToShow}`} 
          size="small" 
          key="more-items" 
        />
      );
    }
    return displayChips;
  };


  return (
    <Box className="manga-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Manga
      </Typography>
      <Box className="filter-section">
        <Grid 
          container 
          spacing={2} 
          alignItems="stretch"
        >
          {/* Dòng 1 */}
          <Grid size={12}>
            <TextField
              label="Lọc theo Tiêu đề"
              variant="outlined"
              fullWidth
              value={localFilters.titleFilter || ''}
              onChange={(e) => handleLocalFilterChange('titleFilter', e.target.value)}
            />
          </Grid>

          {/* Dòng 2 */}
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <TextField
              select
              label="Trạng thái"
              variant="outlined"
              fullWidth
              value={localFilters.statusFilter || ''}
              onChange={(e) => handleLocalFilterChange('statusFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {MANGA_STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <TextField
              select
              label="Ngôn ngữ gốc"
              variant="outlined"
              fullWidth
              value={localFilters.originalLanguageFilter || ''}
              onChange={(e) => handleLocalFilterChange('originalLanguageFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {ORIGINAL_LANGUAGE_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <FormControl fullWidth variant="outlined">
              <InputLabel id="available-language-filter-label">Ngôn ngữ dịch</InputLabel>
              <Select
                labelId="available-language-filter-label"
                multiple
                value={localFilters.availableTranslatedLanguage || []}
                onChange={(e) => handleLocalFilterChange('availableTranslatedLanguage', e.target.value)}
                input={<OutlinedInput label="Ngôn ngữ dịch" />}
                renderValue={(selected) => ( 
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {renderMultiSelectDisplay(
                      selected.map(val => ORIGINAL_LANGUAGE_OPTIONS.find(opt => opt.value === val) || { value: val, label: val }),
                      null, 2
                    )}
                  </Box>
                )}
                MenuProps={MenuProps}
              >
                {ORIGINAL_LANGUAGE_OPTIONS.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    <Checkbox checked={(localFilters.availableTranslatedLanguage || []).indexOf(option.value) > -1} />
                    <ListItemText primary={option.label} />
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          {/* Dòng 3: Tác giả, Họa sĩ, Năm */}
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <Autocomplete
              multiple
              options={availableAuthors}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={availableAuthors.filter(a => (localFilters.authors || []).includes(a.id))}
              onChange={(event, newValue) => {
                handleLocalFilterChange('authors', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Tác giả" variant="outlined" />}
              fullWidth
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <Autocomplete
              multiple
              options={availableAuthors}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={availableAuthors.filter(a => (localFilters.artists || []).includes(a.id))}
              onChange={(event, newValue) => {
                handleLocalFilterChange('artists', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Họa sĩ" variant="outlined" />}
              fullWidth
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <TextField
              label="Năm"
              variant="outlined"
              fullWidth
              type="number"
              value={localFilters.yearFilter || ''}
              onChange={(e) => handleLocalFilterChange('yearFilter', e.target.value === '' ? null : parseInt(e.target.value, 10))}
              inputProps={{ min: 1000, max: new Date().getFullYear() + 5, step: 1 }}
            />
          </Grid>

          {/* Dòng 4: Tags */}
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
             <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={availableTags.filter(t => (localFilters.includedTags || []).includes(t.id))}
              onChange={(event, newValue) => {
                handleLocalFilterChange('includedTags', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Tags Phải Có" variant="outlined" />}
              fullWidth
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <TextField
              select
              label="Chế độ Tags Phải Có"
              variant="outlined"
              fullWidth
              value={localFilters.includedTagsMode || 'AND'}
              onChange={(e) => handleLocalFilterChange('includedTagsMode', e.target.value)}
              disabled={!localFilters.includedTags || localFilters.includedTags.length === 0}
            >
              <MenuItem value="AND">VÀ (Tất cả)</MenuItem>
              <MenuItem value="OR">HOẶC (Bất kỳ)</MenuItem>
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 4 }}>
            <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={availableTags.filter(t => (localFilters.excludedTags || []).includes(t.id))}
              onChange={(event, newValue) => {
                handleLocalFilterChange('excludedTags', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Tags Không Có" variant="outlined" />}
              fullWidth
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <TextField
              select
              label="Chế độ Tags Không Có"
              variant="outlined"
              fullWidth
              value={localFilters.excludedTagsMode || 'OR'}
              onChange={(e) => handleLocalFilterChange('excludedTagsMode', e.target.value)}
              disabled={!localFilters.excludedTags || localFilters.excludedTags.length === 0}
            >
              <MenuItem value="OR">HOẶC (Bất kỳ)</MenuItem>
            </TextField>
          </Grid>

          {/* Dòng 5: Các nút */}
          <Grid sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', mt: 1 }} size={12}>
             <Button
              variant="contained"
              color="primary"
              startIcon={<SearchIcon />}
              onClick={handleApplyLocalFilters}
              sx={{ height: '56px' }}  
            >
              Áp dụng
            </Button>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ClearIcon />}
              onClick={handleResetLocalFilters}
              sx={{ height: '56px' }} 
            >
              Đặt lại
            </Button>
          </Grid>
        </Grid>
      </Box>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2, mt: 3 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={() => navigate('/mangas/create')}
        >
          Thêm Manga mới
        </Button>
      </Box>
      <MangaTable
        mangas={mangas}
        totalMangas={totalMangas}
        page={page}
        rowsPerPage={rowsPerPage}
        onPageChange={(event, newPageVal) => setPage(event, newPageVal)}
        onRowsPerPageChange={(event) => setRowsPerPage(event)}
        onSort={(orderBy, orderDir) => setSort(orderBy, orderDir)}
        orderBy={sort.orderBy}
        order={sort.ascending ? 'asc' : 'desc'}
        onDelete={deleteManga}
        onEdit={(id) => navigate(`/mangas/edit/${id}`)}
        onViewCovers={(id) => navigate(`/mangas/${id}/covers`)}
        onViewTranslations={(id) => navigate(`/mangas/${id}/translations`)}
        isLoading={isLoading}
      />
    </Box>
  );
}

export default MangaListPage
```

---

Sau khi áp dụng các thay đổi này, ứng dụng `MangaReader_ManagerUI` của bạn sẽ có thể:
1.  Nhận diện và gửi các tham số tìm kiếm mới (`authors`, `artists`, `availableTranslatedLanguage`) từ client đến server proxy.
2.  Server proxy sẽ chuyển tiếp chính xác các tham số này đến API Backend thực sự.
3.  Giao diện người dùng sẽ có các bộ lọc riêng biệt cho Tác giả, Họa sĩ và Ngôn ngữ dịch, cho phép người dùng thực hiện các truy vấn tìm kiếm phức tạp hơn.
4.  Dữ liệu trả về sẽ được xử lý đúng cách, bao gồm cả trường `availableTranslatedLanguages` mới.

Chúc bạn thành công!
```