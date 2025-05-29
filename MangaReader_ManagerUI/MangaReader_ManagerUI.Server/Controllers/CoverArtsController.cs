using MangaReaderLib.DTOs.Attributes; // For LibCoverArtAttributesDto
using MangaReaderLib.DTOs.Common;    // For LibApiResponse, LibApiErrorResponse
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class CoverArtsController : BaseApiController
    {
        private readonly ICoverArtClient _coverArtClient;
        private readonly IMangaClient _mangaClient; // Cần IMangaClient để lấy cover art
        private readonly ILogger<CoverArtsController> _logger;

        public CoverArtsController(ICoverArtClient coverArtClient, IMangaClient mangaClient, ILogger<CoverArtsController> logger)
        {
            _coverArtClient = coverArtClient;
            _mangaClient = mangaClient;
            _logger = logger;
        }

        // GET: api/CoverArts/{id} - Endpoint này không có trong FrontendAPI.md nhưng hữu ích
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LibApiResponse<LibResourceObject<LibCoverArtAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCoverArtById(Guid id)
        {
            _logger.LogInformation("API: Requesting CoverArt by ID: {CoverArtId}", id);
            // Logic để lấy CoverArt by ID sẽ cần một phương thức trong IMangaClient hoặc ICoverArtClient (nếu có)
            // Ví dụ: var result = await _coverArtClient.GetCoverArtByIdAsync(id, HttpContext.RequestAborted);
            // Hiện tại, chúng ta chưa định nghĩa GetCoverArtByIdAsync trong ICoverArtClient hoặc IMangaClient
            // Giả sử MangaClient sẽ có GetCoverArtByIdAsync, hoặc chúng ta sẽ phải lấy tất cả cover của một manga rồi filter.
            // Để đơn giản, tạm thời trả về NotImplemented nếu không có phương thức trực tiếp.
            return StatusCode(StatusCodes.Status501NotImplemented, 
                new LibApiErrorResponse(new LibApiError(501, "Not Implemented", "Get CoverArt by ID is not implemented yet.")));
        }


        // DELETE: api/CoverArts/{coverId}
        [HttpDelete("{coverId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(LibApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
                return NotFound(new LibApiErrorResponse(new LibApiError(404, "Not Found", $"CoverArt with ID {coverId} not found.")));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting CoverArt {CoverId}. Status: {StatusCode}", coverId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new LibApiErrorResponse(new LibApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting CoverArt {CoverId}", coverId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new LibApiErrorResponse(new LibApiError(500, "Server Error", "An error occurred while deleting the cover art.")));
            }
        }
    }
} 