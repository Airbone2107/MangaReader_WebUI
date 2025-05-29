using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Chapters; // For DTOs specific to ChapterPage
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic; // For List
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        [ProducesResponseType(typeof(LibApiResponse<LibCreateChapterPageEntryResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateChapterPageEntry(Guid chapterId, [FromBody] LibCreateChapterPageEntryRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create page entry for ChapterId: {ChapterId}", chapterId);
            if (!ModelState.IsValid) 
            {
                 var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new LibApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new LibApiErrorResponse(errors));
            }
            try
            {
                var result = await _chapterPageClient.CreateChapterPageEntryAsync(chapterId, createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Creation Failed", "Could not create chapter page entry.")));
                }
                // Header Location sẽ trỏ đến endpoint upload ảnh cho trang này
                // Location: /chapterpages/{pageId}/image
                return CreatedAtAction(nameof(UploadChapterPageImage), new { pageId = result.Data.PageId }, result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Chapter with ID {ChapterId} not found for page entry creation. Status: {StatusCode}", chapterId, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"Chapter with ID {chapterId} not found.")));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter page entry for chapter {id}", chapterId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // POST: api/chapterpages/{pageId}/image
        [HttpPost("{pageId}/image")] // Route này khớp với BaseApiController
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LibApiResponse<LibUploadChapterPageImageResponseDto>), StatusCodes.Status200OK)] // API trả về 200 OK
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadChapterPageImage(Guid pageId, [FromForm] IFormFile file)
        {
             _logger.LogInformation("API: Request to upload image for PageId: {PageId}", pageId);
            if (file == null || file.Length == 0)
            {
                return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Validation Error", "No file uploaded.")));
            }
            // Thêm kiểm tra file type, size
            try
            {
                using var stream = file.OpenReadStream();
                var result = await _chapterPageClient.UploadChapterPageImageAsync(pageId, stream, file.FileName, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Upload Failed", "Could not upload chapter page image.")));
                }
                return Ok(result); // Trả về 200 OK với publicId
            }
             catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Page with ID {PageId} not found for image upload. Status: {StatusCode}", pageId, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"Page with ID {pageId} not found.")));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for page {id}", pageId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/chapters/{chapterId}/pages
        [HttpGet("/api/chapters/{chapterId}/pages")] // Custom route
        [ProducesResponseType(typeof(LibApiCollectionResponse<LibResourceObject<LibChapterPageAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChapterPages(Guid chapterId, [FromQuery] int? offset, [FromQuery] int? limit)
        {
             _logger.LogInformation("API: Requesting pages for ChapterId: {ChapterId}", chapterId);
            try
            {
                var result = await _chapterPageClient.GetChapterPagesAsync(chapterId, offset, limit, HttpContext.RequestAborted);
                 if (result == null)
                {
                     return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"Chapter with ID {chapterId} not found or has no pages.")));
                }
                return Ok(result);
            }
            // ... error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error fetching pages for chapter {id}", chapterId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // PUT: api/chapterpages/{pageId}/details
        [HttpPut("{pageId}/details")] // Route này khớp với BaseApiController
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChapterPageDetails(Guid pageId, [FromBody] LibUpdateChapterPageDetailsRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update page details for PageId: {PageId}", pageId);
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _chapterPageClient.UpdateChapterPageDetailsAsync(pageId, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error updating page details for page {id}", pageId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/chapterpages/{pageId}
        [HttpDelete("{pageId}")] // Route này khớp với BaseApiController
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChapterPage(Guid pageId)
        {
            _logger.LogInformation("API: Request to delete page: {PageId}", pageId);
            try
            {
                await _chapterPageClient.DeleteChapterPageAsync(pageId, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting page {id}", pageId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 