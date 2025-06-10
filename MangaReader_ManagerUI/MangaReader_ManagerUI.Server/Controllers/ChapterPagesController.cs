using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MangaReaderLib.Services.Exceptions;
using System.Net;

namespace MangaReader_ManagerUI.Server.Controllers
{
    // Route base cho controller này là /api/chapterpages theo convention BaseApiController
    // Tuy nhiên, các endpoint trong FrontendAPI.md có route hơi khác.
    // Ví dụ: POST /Chapters/{chapterId}/pages/entry
    // Chúng ta sẽ sử dụng Route attributes trên từng action để khớp.
    
    public class ChapterPagesController : BaseApiController
    {
        private readonly IChapterPageClient _chapterPageClient;
        private readonly ILogger<ChapterPagesController> _logger;

        public ChapterPagesController(IChapterPageClient chapterPageClient, ILogger<ChapterPagesController> logger)
        {
            _chapterPageClient = chapterPageClient;
            _logger = logger;
        }

        // POST: api/Chapters/{chapterId}/pages/entry
        [HttpPost("/api/Chapters/{chapterId}/pages/entry")] // Custom route
        [ProducesResponseType(typeof(ApiResponse<CreateChapterPageEntryResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateChapterPageEntry(Guid chapterId, [FromBody] CreateChapterPageEntryRequestDto createDto)
        {
            _logger.LogInformation("API Proxy: Request to create page entry for ChapterId: {ChapterId}", chapterId);
            if (!ModelState.IsValid) 
            {
                 var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage, context: new { field = e.ErrorMessage }))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                var result = await _chapterPageClient.CreateChapterPageEntryAsync(chapterId, createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Creation Failed", "Could not create chapter page entry.")));
                }
                // Header Location sẽ trỏ đến endpoint upload ảnh cho trang này
                // Location: /chapterpages/{pageId}/image
                return CreatedAtAction(nameof(UploadChapterPageImage), new { pageId = result.Data.PageId }, result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API Proxy: Chapter with ID {ChapterId} not found for page entry creation. Status: {StatusCode}", chapterId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Chapter with ID {chapterId} not found.")));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during CreateChapterPageEntry. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error during CreateChapterPageEntry for chapter {id}", chapterId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // POST: api/chapterpages/{pageId}/image
        [HttpPost("{pageId}/image")] // Route này khớp với BaseApiController
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UploadChapterPageImageResponseDto>), StatusCodes.Status200OK)] // API trả về 200 OK
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadChapterPageImage(Guid pageId, [FromForm] IFormFile file)
        {
             _logger.LogInformation("API Proxy: Request to upload image for PageId: {PageId}", pageId);
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", "No file uploaded.")));
            }
            // Thêm kiểm tra file type, size
            try
            {
                using var stream = file.OpenReadStream();
                var result = await _chapterPageClient.UploadChapterPageImageAsync(pageId, stream, file.FileName, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Upload Failed", "Could not upload chapter page image.")));
                }
                return Ok(result); // Trả về 200 OK với publicId
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API Proxy: Page with ID {PageId} not found for image upload. Status: {StatusCode}", pageId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Page with ID {pageId} not found.")));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during UploadChapterPageImage. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error during UploadChapterPageImage for page {id}", pageId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/chapters/{chapterId}/pages
        [HttpGet("/api/chapters/{chapterId}/pages")] // Custom route
        [Route("/api/chapters/{chapterId}/pages", Name = "GetChapterPages")] // Named route for CreatedAtRoute
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChapterPages(Guid chapterId, [FromQuery] int? offset, [FromQuery] int? limit)
        {
             _logger.LogInformation("API Proxy: Requesting pages for ChapterId: {ChapterId}", chapterId);
            try
            {
                var result = await _chapterPageClient.GetChapterPagesAsync(chapterId, offset, limit, HttpContext.RequestAborted);
                 if (result == null)
                {
                     return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Chapter with ID {chapterId} not found or has no pages.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API Proxy: Chapter with ID {ChapterId} not found when fetching pages. Status: {StatusCode}", chapterId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during GetChapterPages. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during GetChapterPages for chapter {id}", chapterId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // PUT: api/chapterpages/{pageId}/details
        [HttpPut("{pageId}/details")] // Route này khớp với BaseApiController
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChapterPageDetails(Guid pageId, [FromBody] UpdateChapterPageDetailsRequestDto updateDto)
        {
            _logger.LogInformation("API Proxy: Request to update page details for PageId: {PageId}", pageId);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage, context: new { field = e.ErrorMessage }))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                await _chapterPageClient.UpdateChapterPageDetailsAsync(pageId, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API Proxy: Page with ID {PageId} not found for details update. Status: {StatusCode}", pageId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during UpdateChapterPageDetails. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during UpdateChapterPageDetails for page {id}", pageId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/chapterpages/{pageId}
        [HttpDelete("{pageId}")] // Route này khớp với BaseApiController
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChapterPage(Guid pageId)
        {
            _logger.LogInformation("API Proxy: Request to delete page: {PageId}", pageId);
            try
            {
                await _chapterPageClient.DeleteChapterPageAsync(pageId, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("API Proxy: Page with ID {PageId} not found for deletion. Status: {StatusCode}", pageId, ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during DeleteChapterPage. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during DeleteChapterPage for page {id}", pageId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 