using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class ChaptersController : BaseApiController
    {
        private readonly IChapterClient _chapterClient;
        private readonly ILogger<ChaptersController> _logger;

        public ChaptersController(IChapterClient chapterClient, ILogger<ChaptersController> logger)
        {
            _chapterClient = chapterClient;
            _logger = logger;
        }

        // POST: api/Chapters
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] // Nếu TranslatedMangaId không tồn tại
        public async Task<IActionResult> CreateChapter([FromBody] CreateChapterRequestDto createDto)
        {
             _logger.LogInformation("API: Request to create chapter for TranslatedMangaId: {TranslatedMangaId}", createDto.TranslatedMangaId);
            if (!ModelState.IsValid) 
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                var result = await _chapterClient.CreateChapterAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null) 
                {
                     return BadRequest(new ApiErrorResponse(new ApiError(400, "Creation Failed", "Could not create chapter.")));
                }
                return CreatedAtAction(nameof(GetChapterById), new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: TranslatedManga with ID {TranslatedMangaId} not found for creating chapter. Status: {StatusCode}", createDto.TranslatedMangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error creating chapter");
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/translatedmangas/{translatedMangaId}/chapters
        // Đổi route để khớp với FrontendAPI.md
        [HttpGet("/api/translatedmangas/{translatedMangaId}/chapters")] 
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChaptersByTranslatedManga(Guid translatedMangaId, [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting chapters for TranslatedMangaId: {TranslatedMangaId}", translatedMangaId);
            try
            {
                var result = await _chapterClient.GetChaptersByTranslatedMangaAsync(translatedMangaId, offset, limit, orderBy, ascending, HttpContext.RequestAborted);
                if (result == null)
                {
                     return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"TranslatedManga with ID {translatedMangaId} not found or has no chapters.")));
                }
                return Ok(result);
            }
            // ... error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error fetching chapters for translated manga {id}", translatedMangaId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // GET: api/Chapters/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChapterById(Guid id)
        {
            _logger.LogInformation("API: Requesting chapter by ID: {ChapterId}", id);
            try
            {
                var result = await _chapterClient.GetChapterByIdAsync(id, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Chapter with ID {id} not found.")));
                }
                return Ok(result);
            }
            // ... error handling ...
             catch (Exception ex)
            {
                 _logger.LogError(ex, "Error fetching chapter {id}", id);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // PUT: api/Chapters/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update chapter: {ChapterId}", id);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                await _chapterClient.UpdateChapterAsync(id, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
             catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            // ... error handling ...
             catch (Exception ex)
            {
                 _logger.LogError(ex, "Error updating chapter {id}", id);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/Chapters/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            _logger.LogInformation("API: Request to delete chapter: {ChapterId}", id);
            try
            {
                await _chapterClient.DeleteChapterAsync(id, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            // ... error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting chapter {id}", id);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
    }
}