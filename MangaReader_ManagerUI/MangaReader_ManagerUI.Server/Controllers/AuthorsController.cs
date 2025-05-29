using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class AuthorsController : BaseApiController
    {
        private readonly IAuthorClient _authorClient;
        private readonly ILogger<AuthorsController> _logger;

        public AuthorsController(IAuthorClient authorClient, ILogger<AuthorsController> logger)
        {
            _authorClient = authorClient;
            _logger = logger;
        }

        // GET: api/Authors
        [HttpGet]
        [ProducesResponseType(typeof(LibApiCollectionResponse<LibResourceObject<LibAuthorAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuthors(
            [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? nameFilter,
            [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting list of authors.");
            try
            {
                var result = await _authorClient.GetAuthorsAsync(offset, limit, nameFilter, orderBy, ascending, HttpContext.RequestAborted);
                if (result == null) 
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new LibApiErrorResponse(new LibApiError(500, "API Error", "Failed to fetch authors from the backend API.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching authors. Status: {StatusCode}", ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching authors.");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Authors/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibAuthorAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuthorById(Guid id)
        {
            _logger.LogInformation("API: Requesting author by ID: {AuthorId}", id);
             try
            {
                var result = await _authorClient.GetAuthorByIdAsync(id, HttpContext.RequestAborted);
                if (result == null || result.Data == null) 
                {
                    return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"Author with ID {id} not found.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Author with ID {AuthorId} not found in backend. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching author {AuthorId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching author {AuthorId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // POST: api/Authors
        [HttpPost]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibAuthorAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAuthor([FromBody] LibCreateAuthorRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create author: {Name}", createDto.Name);
            if (!ModelState.IsValid) 
            {
                 var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new LibApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new LibApiErrorResponse(errors));
            }
            
            try
            {
                var result = await _authorClient.CreateAuthorAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null) 
                {
                    return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Creation Failed", "Could not create author.")));
                }
                return CreatedAtAction(nameof(GetAuthorById), new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error creating author. Status: {StatusCode}", ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while creating author.");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // PUT: api/Authors/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAuthor(Guid id, [FromBody] LibUpdateAuthorRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update author: {AuthorId}", id);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new LibApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new LibApiErrorResponse(errors));
            }
            try
            {
                await _authorClient.UpdateAuthorAsync(id, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Author with ID {AuthorId} not found for update. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error updating author {AuthorId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while updating author {AuthorId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/Authors/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAuthor(Guid id)
        {
            _logger.LogInformation("API: Request to delete author: {AuthorId}", id);
            try
            {
                await _authorClient.DeleteAuthorAsync(id, HttpContext.RequestAborted);
                return NoContent();
            }
             catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Author with ID {AuthorId} not found for deletion. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting author {AuthorId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while deleting author {AuthorId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 