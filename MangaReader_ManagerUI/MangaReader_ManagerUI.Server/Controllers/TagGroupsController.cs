using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TagGroups;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MangaReaderLib.Services.Exceptions;
using System.Net;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class TagGroupsController : BaseApiController
    {
        private readonly ITagGroupClient _tagGroupClient;
        private readonly ILogger<TagGroupsController> _logger;

        public TagGroupsController(ITagGroupClient tagGroupClient, ILogger<TagGroupsController> logger)
        {
            _tagGroupClient = tagGroupClient;
            _logger = logger;
        }

        // GET: api/TagGroups
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTagGroups(
            [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? nameFilter,
            [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting list of tag groups.");
            try
            {
                var result = await _tagGroupClient.GetTagGroupsAsync(offset, limit, nameFilter, orderBy, ascending, HttpContext.RequestAborted);
                if (result == null) return StatusCode(500, new ApiErrorResponse(new ApiError(500, "API Error", "Error fetching tag groups.")));
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderAPI. Status: {StatusCode}", ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching tag groups. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching tag groups.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // POST: api/TagGroups
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagGroupAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTagGroup([FromBody] CreateTagGroupRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create tag group: {Name}", createDto.Name);
            if (!ModelState.IsValid) 
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
             try
            {
                var result = await _tagGroupClient.CreateTagGroupAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Creation Failed", "Could not create tag group.")));
                }
                // Cần endpoint GetTagGroupById
                return CreatedAtAction(nameof(GetTagGroupById), new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderAPI. Status: {StatusCode}", ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error creating tag group. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag group");
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // GET: api/TagGroups/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagGroupAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTagGroupById(Guid id)
        {
            _logger.LogInformation("API: Requesting tag group by ID: {TagGroupId}", id);
            try
            {
                var result = await _tagGroupClient.GetTagGroupByIdAsync(id, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"TagGroup with ID {id} not found.")));
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
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching tag group. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tag group {id}", id);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // PUT: api/TagGroups/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTagGroup(Guid id, [FromBody] UpdateTagGroupRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update tag group: {TagGroupId}", id);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                await _tagGroupClient.UpdateTagGroupAsync(id, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderAPI. Status: {StatusCode}", ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: TagGroup with ID {TagGroupId} not found for update. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error updating tag group {TagGroupId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while updating tag group {TagGroupId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/TagGroups/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)] // Nếu nhóm tag còn chứa tags
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTagGroup(Guid id)
        {
            _logger.LogInformation("API: Request to delete tag group: {TagGroupId}", id);
            try
            {
                await _tagGroupClient.DeleteTagGroupAsync(id, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderAPI. Status: {StatusCode}", ex.StatusCode);
                if (ex.ApiErrorResponse != null)
                {
                    return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse);
                }
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: TagGroup with ID {TagGroupId} not found for deletion. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest) // Nếu có ràng buộc (tags con)
            {
                _logger.LogWarning("API: Cannot delete TagGroup with ID {TagGroupId} due to existing relationships. Status: {StatusCode}", id, ex.StatusCode);
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Bad Request", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting tag group {TagGroupId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while deleting tag group {TagGroupId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 