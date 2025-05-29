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
        [ProducesResponseType(typeof(LibApiCollectionResponse<LibResourceObject<LibTagGroupAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTagGroups(
            [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? nameFilter,
            [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting list of tag groups.");
            var result = await _tagGroupClient.GetTagGroupsAsync(offset, limit, nameFilter, orderBy, ascending, HttpContext.RequestAborted);
            if (result == null) return StatusCode(500, "Error fetching tag groups.");
            return Ok(result);
        }

        // POST: api/TagGroups
        [HttpPost]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTagGroup([FromBody] LibCreateTagGroupRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create tag group: {Name}", createDto.Name);
            if (!ModelState.IsValid) 
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new LibApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new LibApiErrorResponse(errors));
            }
             try
            {
                var result = await _tagGroupClient.CreateTagGroupAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Creation Failed", "Could not create tag group.")));
                }
                // Cần endpoint GetTagGroupById
                return CreatedAtAction(nameof(GetTagGroupById), new { id = Guid.Parse(result.Data.Id) }, result);
            }
            // ... error handling ...
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag group");
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // GET: api/TagGroups/{id} (Thêm endpoint này)
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTagGroupById(Guid id)
        {
            _logger.LogInformation("API: Requesting tag group by ID: {TagGroupId}", id);
            var result = await _tagGroupClient.GetTagGroupByIdAsync(id, HttpContext.RequestAborted);
            if (result == null || result.Data == null)
            {
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"TagGroup with ID {id} not found.")));
            }
            return Ok(result);
        }
    }
} 