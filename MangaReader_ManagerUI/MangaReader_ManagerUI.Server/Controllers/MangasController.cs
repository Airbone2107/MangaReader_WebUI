using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader_ManagerUI.Server.Controllers
{
    public class MangasController : BaseApiController
    {
        private readonly IMangaClient _mangaClient;
        private readonly ILogger<MangasController> _logger;

        public MangasController(IMangaClient mangaClient, ILogger<MangasController> logger)
        {
            _mangaClient = mangaClient;
            _logger = logger;
        }

        // GET: api/Mangas
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangas(
            [FromQuery] int? offset, 
            [FromQuery] int? limit, 
            [FromQuery] string? titleFilter,
            [FromQuery] string? statusFilter, 
            [FromQuery] string? contentRatingFilter, 
            [FromQuery] string? demographicFilter, 
            [FromQuery] string? originalLanguageFilter,
            [FromQuery] int? yearFilter,
            [FromQuery(Name = "tagIdsFilter[]")] List<Guid>? tagIdsFilter, 
            [FromQuery(Name = "authorIdsFilter[]")] List<Guid>? authorIdsFilter,
            [FromQuery] string? orderBy, 
            [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting list of mangas.");
            try
            {
                var result = await _mangaClient.GetMangasAsync(offset, limit, titleFilter, 
                    statusFilter, contentRatingFilter, demographicFilter, originalLanguageFilter,
                    yearFilter, tagIdsFilter, authorIdsFilter,
                    orderBy, ascending, HttpContext.RequestAborted);
                
                if (result == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new ApiErrorResponse(new ApiError(500, "API Error", "Failed to fetch mangas from the backend API.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching mangas. Status: {StatusCode}", ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching mangas.");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Mangas/{mangaId}
        [HttpGet("{mangaId}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangaById(Guid mangaId)
        {
            _logger.LogInformation("API: Requesting manga by ID: {MangaId}", mangaId);
            try
            {
                var result = await _mangaClient.GetMangaByIdAsync(mangaId, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                 _logger.LogWarning("API: Manga with ID {MangaId} not found in backend. Status: {StatusCode}", mangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // POST: api/Mangas
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateManga([FromBody] CreateMangaRequestDto createDto)
        {
            _logger.LogInformation("API: Request to create manga: {Title}", createDto.Title);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                var result = await _mangaClient.CreateMangaAsync(createDto, HttpContext.RequestAborted);
                if (result == null || result.Data == null) 
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Creation Failed", "Could not create manga via backend API.")));
                }
                return CreatedAtAction(nameof(GetMangaById), new { mangaId = Guid.Parse(result.Data.Id) }, result);
            }
             catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error creating manga. Status: {StatusCode}", ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while creating manga.");
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // PUT: api/Mangas/{mangaId}
        [HttpPut("{mangaId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateManga(Guid mangaId, [FromBody] UpdateMangaRequestDto updateDto)
        {
            _logger.LogInformation("API: Request to update manga: {MangaId}", mangaId);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => new ApiError(400, "Validation Error", e.ErrorMessage))
                                     .ToList();
                return BadRequest(new ApiErrorResponse(errors));
            }
            try
            {
                await _mangaClient.UpdateMangaAsync(mangaId, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                 _logger.LogWarning("API: Manga with ID {MangaId} not found for update. Status: {StatusCode}", mangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error updating manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while updating manga {MangaId}.", mangaId);
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/Mangas/{mangaId}
        [HttpDelete("{mangaId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteManga(Guid mangaId)
        {
            _logger.LogInformation("API: Request to delete manga: {MangaId}", mangaId);
            try
            {
                await _mangaClient.DeleteMangaAsync(mangaId, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                 _logger.LogWarning("API: Manga with ID {MangaId} not found for deletion. Status: {StatusCode}", mangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error deleting manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while deleting manga {MangaId}.", mangaId);
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // POST: api/Mangas/{mangaId}/covers
        [HttpPost("{mangaId}/covers")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadMangaCover(Guid mangaId, [FromForm] IFormFile file, [FromForm] string? volume, [FromForm] string? description)
        {
            _logger.LogInformation("API: Request to upload cover for Manga ID: {MangaId}", mangaId);
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", "No file uploaded.")));
            }
            try
            {
                using var stream = file.OpenReadStream();
                var result = await _mangaClient.UploadMangaCoverAsync(mangaId, stream, file.FileName, volume, description, HttpContext.RequestAborted);
                
                if (result == null || result.Data == null)
                {
                    return BadRequest(new ApiErrorResponse(new ApiError(400, "Upload Failed", "Could not upload cover via backend API.")));
                }
                // FrontendAPI.md trả về 201 Created cho POST /mangas/{mangaId}/covers
                return CreatedAtAction(nameof(CoverArtsController.GetCoverArtById), "CoverArts", new { id = Guid.Parse(result.Data.Id) }, result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found for cover upload. Status: {StatusCode}", mangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found.")));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error uploading cover for manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while uploading cover for manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Mangas/{mangaId}/covers
        [HttpGet("{mangaId}/covers")]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangaCovers(Guid mangaId, [FromQuery] int? offset, [FromQuery] int? limit)
        {
            _logger.LogInformation("API: Requesting covers for manga ID: {MangaId}", mangaId);
            try
            {
                var result = await _mangaClient.GetMangaCoversAsync(mangaId, offset, limit, HttpContext.RequestAborted);
                if (result == null)
                {
                    // This case implies an issue with the ApiClient or a non-HTTP error before the request
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found or has no covers.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found when fetching covers. Status: {StatusCode}", mangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching covers for manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching covers for manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/Mangas/{mangaId}/translations
        [HttpGet("{mangaId}/translations")]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMangaTranslations(Guid mangaId, [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API: Requesting translations for manga ID: {MangaId}", mangaId);
            try
            {
                var result = await _mangaClient.GetMangaTranslationsAsync(mangaId, offset, limit, orderBy, ascending, HttpContext.RequestAborted);
                if (result == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Manga with ID {mangaId} not found or has no translations.")));
                }
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API: Manga with ID {MangaId} not found when fetching translations. Status: {StatusCode}", mangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API Error fetching translations for manga {MangaId}. Status: {StatusCode}", mangaId, ex.StatusCode);
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                                  new ApiErrorResponse(new ApiError((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), "API Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching translations for manga {MangaId}.", mangaId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
    }
} 