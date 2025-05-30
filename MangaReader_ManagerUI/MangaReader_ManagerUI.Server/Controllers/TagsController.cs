using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic; // For List
using System.Linq;
using System.Threading.Tasks;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class TagsController : BaseApiController
    {
        private readonly ITagClient _tagClient;
        private readonly ILogger<TagsController> _logger;

        public TagsController(ITagClient tagClient, ILogger<TagsController> logger)
        {
            _tagClient = tagClient;
            _logger = logger;
        }

        // GET: api/Tags
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TagAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTags(
            [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] Guid? tagGroupId, 
            [FromQuery] string? nameFilter, [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting list of tags.");
            var result = await _tagClient.GetTagsAsync(offset, limit, tagGroupId, nameFilter, orderBy, ascending, HttpContext.RequestAborted);
            if (result == null) return StatusCode(500, new ApiErrorResponse(new ApiError(500, "API Error", "Error fetching tags.")));
            return Ok(result);
        }

        // POST: api/Tags
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] // Nếu TagGroupId không tồn tại
        public async Task<IActionResult> CreateTag([FromBody] CreateTagRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create tag: {Name}", createDto.Name);
            if (!ModelState.IsValid) 
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                var result = await _tagClient.CreateTagAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                     return BadRequest(new ApiErrorResponse(new ApiError(400, "Creation Failed", "Could not create tag.")));
                }
                // Cần một endpoint GetTagById để trả về CreatedAtAction
                // Giả sử có action GetTagById trong controller này
                return CreatedAtAction(nameof(GetTagById), new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: TagGroup with ID {TagGroupId} not found for tag creation. Status: {StatusCode}", createDto.TagGroupId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Tags/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTagById(Guid id)
        {
            _logger.LogInformation("API: Requesting tag by ID: {TagId}", id);
            var result = await _tagClient.GetTagByIdAsync(id, HttpContext.RequestAborted);
            if (result == null || result.Data == null)
            {
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Tag with ID {id} not found.")));
            }
            return Ok(result);
        }

        // PUT: api/Tags/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update tag: {TagId}", id);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                await _tagClient.UpdateTagAsync(id, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Tag with ID {TagId} not found for update. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error updating tag {TagId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while updating tag {TagId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/Tags/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            _logger.LogInformation("API: Request to delete tag: {TagId}", id);
            try
            {
                await _tagClient.DeleteTagAsync(id, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Tag with ID {TagId} not found for deletion. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting tag {TagId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while deleting tag {TagId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 