using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MangaReaderLib.Services.Exceptions;
using System.Net;
using System.Text.Json; // For JsonSerializer

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
             _logger.LogInformation("API Proxy: Request to create chapter for TranslatedMangaId: {TranslatedMangaId}", createDto.TranslatedMangaId);
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
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during CreateChapter. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API Proxy: TranslatedManga with ID {TranslatedMangaId} not found for creating chapter. Status: {StatusCode}", createDto.TranslatedMangaId, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during CreateChapter. Status: {StatusCode}", ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during CreateChapter");
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // GET: api/translatedmangas/{translatedMangaId}/chapters
        [HttpGet("/api/translatedmangas/{translatedMangaId}/chapters")] 
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChaptersByTranslatedManga(Guid translatedMangaId, [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? orderBy, [FromQuery] bool? ascending)
        {
            _logger.LogInformation("API Proxy: Requesting chapters for TranslatedMangaId: {TranslatedMangaId}", translatedMangaId);
            try
            {
                var result = await _chapterClient.GetChaptersByTranslatedMangaAsync(translatedMangaId, offset, limit, orderBy, ascending, HttpContext.RequestAborted);
                if (result == null)
                {
                     return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"TranslatedManga with ID {translatedMangaId} not found or has no chapters.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during GetChaptersByTranslatedManga for TranslatedMangaId {TranslatedMangaId}. Status: {StatusCode}", translatedMangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during GetChaptersByTranslatedManga for TranslatedMangaId {TranslatedMangaId}. Status: {StatusCode}", translatedMangaId, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during GetChaptersByTranslatedManga for translated manga {id}", translatedMangaId);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }
        
        // GET: api/Chapters/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChapterById(Guid id)
        {
            _logger.LogInformation("API Proxy: Requesting chapter by ID: {ChapterId}", id);
            try
            {
                var result = await _chapterClient.GetChapterByIdAsync(id, HttpContext.RequestAborted);
                if (result == null || result.Data == null)
                {
                    return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Chapter with ID {id} not found.")));
                }
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during GetChapterById for ChapterId {ChapterId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
             catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during GetChapterById for ChapterId {ChapterId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during GetChapterById for chapter {id}", id);
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
            _logger.LogInformation("API Proxy: Request to update chapter: {ChapterId}", id);
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
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during UpdateChapter for ChapterId {ChapterId}. Status: {StatusCode}", id, ex.StatusCode);
                 return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
             catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API Proxy: Chapter with ID {ChapterId} not found for update. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during UpdateChapter for ChapterId {ChapterId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during UpdateChapter for chapter {id}", id);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // DELETE: api/Chapters/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            _logger.LogInformation("API Proxy: Request to delete chapter: {ChapterId}", id);
            try
            {
                await _chapterClient.DeleteChapterAsync(id, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during DeleteChapter for ChapterId {ChapterId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("API Proxy: Chapter with ID {ChapterId} not found for deletion. Status: {StatusCode}", id, ex.StatusCode);
                return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", ex.Message)));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during DeleteChapter for ChapterId {ChapterId}. Status: {StatusCode}", id, ex.StatusCode);
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, 
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Internal server error during DeleteChapter for chapter {id}", id);
                return StatusCode(500, new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
        }

        // ACTION MỚI CHO BATCH UPLOAD
        [HttpPost("{chapterId}/pages/batch")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<ChapterPageAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BatchUploadChapterPages(
            Guid chapterId,
            [FromForm] List<IFormFile> files, 
            [FromForm] List<int> pageNumbers)
        {
            _logger.LogInformation("API Proxy: Batch uploading pages for Chapter ID: {ChapterId}. Files count: {FilesCount}, PageNumbers count: {PageNumbersCount}", chapterId, files?.Count, pageNumbers?.Count);
            if (files == null || !files.Any() || pageNumbers == null || files.Count != pageNumbers.Count)
            {
                _logger.LogWarning("API Proxy: BatchUploadChapterPages - Invalid input. Files: {FilesCount}, PageNumbers: {PageNumbersCount}", files?.Count, pageNumbers?.Count);
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", "Invalid files or pageNumbers input. Ensure files are provided and counts match.")));
            }

            var fileStreams = new List<(Stream stream, string fileName, string contentType)>();
            try
            {
                foreach (var file in files)
                {
                    if (file.Length == 0)
                    {
                        _logger.LogWarning("API Proxy: BatchUploadChapterPages - Empty file detected: {FileName}", file.FileName);
                        return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", $"File '{file.FileName}' is empty.")));
                    }
                    fileStreams.Add((file.OpenReadStream(), file.FileName, file.ContentType));
                }

                var result = await _chapterClient.BatchUploadChapterPagesAsync(chapterId, fileStreams, pageNumbers, HttpContext.RequestAborted);

                if (result == null)
                {
                    _logger.LogWarning("API Proxy: BatchUploadChapterPagesAsync for ChapterId {ChapterId} returned null from the MangaReaderLib client.", chapterId);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ApiErrorResponse(new ApiError(500, "API Client Error", "Failed to batch upload pages; backend client returned no response.")));
                }
                return CreatedAtRoute("GetChapterPages", new { chapterId = chapterId }, result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during BatchUploadChapterPages for ChapterId {ChapterId}. Status: {StatusCode}", chapterId, ex.StatusCode);
                 return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
            catch (JsonException jsonEx) // Bắt lỗi parse JSON
            {
                _logger.LogError(jsonEx, "API Proxy: BatchUploadChapterPages - JSON parsing error. This might indicate an issue with the API response format or a network problem corrupting the JSON.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse(new ApiError(500, "Response Parse Error", "Error parsing response from backend API.")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error during BatchUploadChapterPages for ChapterId {ChapterId}.", chapterId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
            finally
            {
                // Dọn dẹp stream sau khi sử dụng
                foreach (var entry in fileStreams)
                {
                    entry.stream.Dispose();
                }
            }
        }

        // ACTION MỚI CHO SYNC PAGES
        [HttpPut("{chapterId}/pages")]
        [Consumes("multipart/form-data")] 
        [ProducesResponseType(typeof(ApiResponse<List<ChapterPageAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncChapterPages(
            Guid chapterId,
            [FromForm] string pageOperationsJson
        )
        {
            // Truy cập files trực tiếp từ HttpContext.Request.Form.Files
            var formFiles = HttpContext.Request.Form.Files;

            _logger.LogInformation("API Proxy: Syncing pages for Chapter ID: {ChapterId}. PageOperationsJson length: {JsonLength}, Files count: {FilesCount}", 
                chapterId, pageOperationsJson?.Length, formFiles?.Count ?? 0);

            if (string.IsNullOrEmpty(pageOperationsJson))
            {
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", "pageOperationsJson is required.")));
            }
            
            List<PageOperationDto> pageOperations;
            try
            {
                pageOperations = JsonSerializer.Deserialize<List<PageOperationDto>>(pageOperationsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                                 ?? new List<PageOperationDto>();
            }
            catch(JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "API Proxy: SyncChapterPages - Invalid pageOperationsJson format for ChapterId {ChapterId}.", chapterId);
                return BadRequest(new ApiErrorResponse(new ApiError(400, "Validation Error", $"pageOperationsJson is not a valid JSON array of PageOperationDto: {jsonEx.Message}")));
            }

            var fileDictionary = new Dictionary<string, (Stream stream, string fileName, string contentType)>();
            var streamsToDispose = new List<Stream>(); // Để theo dõi các stream cần dispose

            try
            {
                if (formFiles != null)
                {
                    foreach (var file in formFiles)
                    {
                        // file.Name ở đây chính là fileIdentifier được gửi từ client
                        // file.FileName là tên file gốc mà người dùng chọn
                        if (file.Length == 0)
                        {
                            _logger.LogWarning("API Proxy: SyncChapterPages - Empty file detected: {FileName} for fileIdentifier {FileIdentifier}", file.FileName, file.Name);
                            // Có thể bỏ qua file rỗng hoặc trả lỗi tùy theo yêu cầu
                        }
                        var stream = file.OpenReadStream();
                        streamsToDispose.Add(stream); // Thêm vào danh sách để dispose sau
                        fileDictionary[file.Name] = (stream, file.FileName, file.ContentType);
                    }
                }
                
                // Kiểm tra xem tất cả fileIdentifier trong pageOperationsJson có file tương ứng không (nếu fileIdentifier không null)
                foreach (var operation in pageOperations)
                {
                    if (!string.IsNullOrEmpty(operation.FileIdentifier) && !fileDictionary.ContainsKey(operation.FileIdentifier))
                    {
                        _logger.LogWarning("API Proxy: SyncChapterPages - File with identifier '{FileIdentifier}' was specified in pageOperationsJson but not found in uploaded files for ChapterId {ChapterId}.", operation.FileIdentifier, chapterId);
                        // Trả về lỗi sớm nếu file được yêu cầu không có trong form data
                        // Điều này sẽ giống với lỗi mà API backend đang trả về
                        return BadRequest(new ApiErrorResponse(new ApiError(400, "File Missing", $"File with identifier '{operation.FileIdentifier}' was specified but not found in the uploaded files.")));
                    }
                }


                var result = await _chapterClient.SyncChapterPagesAsync(chapterId, pageOperationsJson, fileDictionary, HttpContext.RequestAborted);
                
                if (result == null)
                {
                    _logger.LogWarning("API Proxy: SyncChapterPagesAsync for ChapterId {ChapterId} returned null from the MangaReaderLib client.", chapterId);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ApiErrorResponse(new ApiError(500, "API Client Error", "Failed to sync pages; backend client returned no response.")));
                }
                return Ok(result); 
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error from MangaReaderLib during SyncChapterPages for ChapterId {ChapterId}. Status: {StatusCode}", chapterId, ex.StatusCode);
                 return StatusCode(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, ex.ApiErrorResponse ?? new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? StatusCodes.Status500InternalServerError, "API Error", ex.Message)));
            }
            catch (JsonException jsonEx) 
            {
                _logger.LogError(jsonEx, "API Proxy: SyncChapterPages - JSON parsing error. This might indicate an issue with the API response format or a network problem corrupting the JSON.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse(new ApiError(500, "Response Parse Error", "Error parsing response from backend API.")));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error during SyncChapterPages for ChapterId {ChapterId}. Status: {StatusCode}", chapterId, ex.StatusCode);
                 if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                     return NotFound(new ApiErrorResponse(new ApiError(404, "Not Found", $"Chapter with ID {chapterId} not found on backend.")));
                }
                return StatusCode(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError,
                                  new ApiErrorResponse(new ApiError(((int?)ex.StatusCode) ?? (int)HttpStatusCode.InternalServerError, "HTTP Error", ex.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error during SyncChapterPages for ChapterId {ChapterId}.", chapterId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(new ApiError(500, "Server Error", ex.Message)));
            }
            finally
            {
                // Quan trọng: Dispose tất cả các stream đã mở
                foreach(var stream in streamsToDispose)
                {
                    stream.Dispose();
                }
            }
        }
    }
}