using MangaReaderLib.DTOs.CoverArts; // For CoverArtAttributesDto
using MangaReaderLib.DTOs.Common;    // For ApiResponse, ApiErrorResponse
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class CoverArtsController : BaseApiController
    {
        private readonly ICoverArtClient _coverArtClient;
        private readonly IMangaClient _mangaClient; // Giữ nguyên, có thể cần cho các liên kết phức tạp sau này
        private readonly ILogger<CoverArtsController> _logger;

        public CoverArtsController(ICoverArtClient coverArtClient, IMangaClient mangaClient, ILogger<CoverArtsController> logger)
        {
            _coverArtClient = coverArtClient;
            _mangaClient = mangaClient;
            _logger = logger;
        }

        // GET: api/CoverArts/{id} - Endpoint này có trong FrontendAPI.md và OpenAPI
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCoverArtById(Guid id)
        {
            _logger.LogInformation("API: Requesting CoverArt by ID: {CoverArtId}", id);
            try
            {
                var result = await _coverArtClient.GetCoverArtByIdAsync(id, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"CoverArt with ID {id} not found.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: CoverArt with ID {CoverArtId} not found in backend. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching CoverArt {CoverArtId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching CoverArt {CoverArtId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/CoverArts/{coverId}
        [HttpDelete("{coverId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCoverArt(Guid coverId)
        {
            _logger.LogInformation("API: Request to delete cover art: {CoverId}", coverId);
            try
            {
                await _coverArtClient.DeleteCoverArtAsync(coverId, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: CoverArt with ID {CoverId} not found for deletion.", coverId);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"CoverArt with ID {coverId} not found.")));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting CoverArt {CoverId}. Status: {StatusCode}", coverId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting CoverArt {CoverId}", coverId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", "An error occurred while deleting the cover art.")));
            }
        }
    }
} 