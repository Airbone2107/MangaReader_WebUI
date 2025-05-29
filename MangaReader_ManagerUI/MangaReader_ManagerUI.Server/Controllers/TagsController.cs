using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
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
        [ProducesResponseType(typeof(LibApiCollectionResponse<LibResourceObject<LibTagAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTags(
            [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] Guid? tagGroupId, 
            [FromQuery] string? nameFilter, [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting list of tags.");
            var result = await _tagClient.GetTagsAsync(offset, limit, tagGroupId, nameFilter, orderBy, ascending, HttpContext.RequestAborted);
            if (result == null) return StatusCode(500, "Error fetching tags.");
            return Ok(result);
        }

        // POST: api/Tags
        [HttpPost]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibTagAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)] // Nếu TagGroupId không tồn tại
        public async Task<IActionResult> CreateTag([FromBody] LibCreateTagRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create tag: {Name}", createDto.Name);
            if (!ModelState.IsValid) 
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new LibApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new LibApiErrorResponse(errors));
            }
            try
            {
                var result = await _tagClient.CreateTagAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                     return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Creation Failed", "Could not create tag.")));
                }
                // Cần một endpoint GetTagById để trả về CreatedAtAction
                // Giả sử có action GetTagById trong controller này
                return CreatedAtAction(nameof(GetTagById), new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: TagGroup with ID {TagGroupId} not found for tag creation. Status: {StatusCode}", createDto.TagGroupId, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            // ... other error handling ...
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Tags/{id} (Thêm endpoint này để khớp với CreatedAtAction)
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibTagAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTagById(Guid id)
        {
            _logger.LogInformation("API: Requesting tag by ID: {TagId}", id);
            var result = await _tagClient.GetTagByIdAsync(id, HttpContext.RequestAborted);
            if (result == null || result.Data == null)
            {
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"Tag with ID {id} not found.")));
            }
            return Ok(result);
        }
    }
} 