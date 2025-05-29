using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class TranslatedMangasController : BaseApiController
    {
        private readonly ITranslatedMangaClient _translatedMangaClient;
        private readonly ILogger<TranslatedMangasController> _logger;

        public TranslatedMangasController(ITranslatedMangaClient translatedMangaClient, ILogger<TranslatedMangasController> logger)
        {
            _translatedMangaClient = translatedMangaClient;
            _logger = logger;
        }

        // POST: api/TranslatedMangas
        [HttpPost]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)] // Nếu MangaId không tồn tại
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTranslatedManga([FromBody] LibCreateTranslatedMangaRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create translated manga for MangaId: {MangaId}, Language: {LanguageKey}", createDto.MangaId, createDto.LanguageKey);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new LibApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new LibApiErrorResponse(errors));
            }
            try
            {
                var result = await _translatedMangaClient.CreateTranslatedMangaAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return BadRequest(new LibApiErrorResponse(new LibApiError(400, "Creation Failed", "Could not create translated manga.")));
                }
                // Theo FrontendAPI.md, endpoint này không có {id} trong route,
                // nên ta dùng nameof(GetTranslatedMangaById) và truyền id qua routeValues
                return CreatedAtAction(nameof(GetTranslatedMangaById), new { translatedMangaId = Guid.Parse(result.Data.Id) }, result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Original Manga with ID {MangaId} not found for creating translation. Status: {StatusCode}", createDto.MangaId, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                 _logger.LogError(ex, "API Error creating translated manga. Status: {StatusCode}", ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while creating translated manga.");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/TranslatedMangas/{translatedMangaId}
        [HttpGet("{translatedMangaId}")]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibTranslatedMangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTranslatedMangaById(Guid translatedMangaId)
        {
             _logger.LogInformation("API: Requesting translated manga by ID: {TranslatedMangaId}", translatedMangaId);
            try
            {
                var result = await _translatedMangaClient.GetTranslatedMangaByIdAsync(translatedMangaId, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"TranslatedManga with ID {translatedMangaId} not found.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: TranslatedManga with ID {TranslatedMangaId} not found in backend. Status: {StatusCode}", translatedMangaId, ex.StatusCode);
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            // ... error handling ...
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error fetching translated manga {id}", translatedMangaId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // PUT: api/TranslatedMangas/{translatedMangaId}
        [HttpPut("{translatedMangaId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTranslatedManga(Guid translatedMangaId, [FromBody] LibUpdateTranslatedMangaRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update translated manga: {TranslatedMangaId}", translatedMangaId);
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _translatedMangaClient.UpdateTranslatedMangaAsync(translatedMangaId, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            // ... error handling ...
             catch (Exception ex)
            {
                 _logger.LogError(ex, "Error updating translated manga {id}", translatedMangaId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/TranslatedMangas/{translatedMangaId}
        [HttpDelete("{translatedMangaId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTranslatedManga(Guid translatedMangaId)
        {
            _logger.LogInformation("API: Request to delete translated manga: {TranslatedMangaId}", translatedMangaId);
            try
            {
                await _translatedMangaClient.DeleteTranslatedMangaAsync(translatedMangaId, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", ex.Message)));
            }
            // ... error handling ...
             catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting translated manga {id}", translatedMangaId);
                return StatusCode(500, new LibApiErrorResponse(new LibApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 