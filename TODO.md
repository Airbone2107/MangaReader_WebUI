# TODO: Cập Nhật Client API MangaReader

Tài liệu này mô tả các bước cần thực hiện để cập nhật `MangaReaderLib`, `MangaReader_ManagerUI`, và `MangaReader_WebUI` theo những thay đổi mới nhất của Backend API được nêu trong `ClientAPI_Update.md`.

## Thứ tự ưu tiên:

1.  **`MangaReaderLib`**: Cập nhật thư viện client để làm việc với các endpoint mới và cấu trúc dữ liệu đã thay đổi.
2.  **`MangaReader_ManagerUI` và `MangaReader_WebUI`**: Cập nhật logic xây dựng đường dẫn ảnh mới.

---

## I. Cập nhật `MangaReaderLib`

Mục tiêu: Thêm các DTOs mới và cập nhật/thêm các phương thức trong client services để hỗ trợ các API quản lý trang chương mới được mô tả trong `ClientAPI_Update.md`.

### Bước 1: Định nghĩa/Cập nhật Data Transfer Objects (DTOs)

Các DTO này cần được thêm hoặc cập nhật trong thư mục `MangaReaderLib\DTOs`.

1.  **Cập nhật `ChapterPageAttributesDto.cs`**
    *   Đảm bảo trường `PublicId` phản ánh đúng định dạng mới: `chapters/{ChapterId}/pages/{PageId}`.

    ```csharp
    // MangaReaderLib\DTOs\Chapters\ChapterPageAttributesDto.cs
    namespace MangaReaderLib.DTOs.Chapters
    {
        public class ChapterPageAttributesDto
        {
            public int PageNumber { get; set; }
            public string PublicId { get; set; } = string.Empty;
            // Giữ lại các thuộc tính khác nếu có.
        }
    }
    ```

2.  **Tạo `PageOperationDto.cs`** (cho API Sync Pages)
    *   DTO này dùng để định nghĩa các thao tác trên từng trang khi đồng bộ.

    ```csharp
    // MangaReaderLib\DTOs\Chapters\PageOperationDto.cs
    namespace MangaReaderLib.DTOs.Chapters
    {
        public class PageOperationDto
        {
            public Guid? PageId { get; set; } 
            public int PageNumber { get; set; } 
            public string? FileIdentifier { get; set; } 
        }
    }
    ```

### Bước 2: Cập nhật Interface `IChapterClient.cs`

Thêm các phương thức mới để hỗ trợ batch upload và sync pages.

```csharp
// MangaReaderLib\Services\Interfaces\IChapterClient.cs
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System.IO; 
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    public interface IChapterClient
    {
        // ... (các phương thức hiện có: CreateChapterAsync, GetChaptersByTranslatedMangaAsync, GetChapterByIdAsync, UpdateChapterAsync, DeleteChapterAsync)

        Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> CreateChapterAsync(
            CreateChapterRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(
            Guid translatedMangaId,
            int? offset = null,
            int? limit = null,
            string? orderBy = null,
            bool? ascending = null,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> GetChapterByIdAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task UpdateChapterAsync(
            Guid chapterId,
            UpdateChapterRequestDto request,
            CancellationToken cancellationToken = default);

        Task DeleteChapterAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        // Phương thức mới cho Batch Upload
        Task<ApiResponse<List<ChapterPageAttributesDto>>?> BatchUploadChapterPagesAsync(
            Guid chapterId,
            IEnumerable<(Stream stream, string fileName, string contentType)> files,
            IEnumerable<int> pageNumbers,
            CancellationToken cancellationToken = default);

        // Phương thức mới cho Sync Pages
        Task<ApiResponse<List<ChapterPageAttributesDto>>?> SyncChapterPagesAsync(
            Guid chapterId,
            string pageOperationsJson,
            IDictionary<string, (Stream stream, string fileName, string contentType)>? files,
            CancellationToken cancellationToken = default);
    }
}
```

### Bước 3: Cập nhật Implementation `ChapterClient.cs`

Triển khai các phương thức mới trong `ChapterClient.cs`.

```csharp
// MangaReaderLib\Services\Implementations\ChapterClient.cs
using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net.Http; 
using System.Text.Json;
using System.Collections.Generic; 
using System.IO; 
using System.Linq; 

namespace MangaReaderLib.Services.Implementations
{
    public class ChapterClient : IChapterClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ChapterClient> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ChapterClient(IApiClient apiClient, ILogger<ChapterClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }
        
        // ... (các phương thức hiện có: CreateChapterAsync, GetChaptersByTranslatedMangaAsync, GetChapterByIdAsync, UpdateChapterAsync, DeleteChapterAsync)
        
        public async Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> CreateChapterAsync(
            CreateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new chapter for translated manga ID: {TranslatedMangaId}, Chapter: {ChapterNumber}", 
                request.TranslatedMangaId, request.ChapterNumber);
            return await _apiClient.PostAsync<CreateChapterRequestDto, ApiResponse<ResourceObject<ChapterAttributesDto>>>("Chapters", request, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>?> GetChaptersByTranslatedMangaAsync(
            Guid translatedMangaId, int? offset = null, int? limit = null, 
            string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting chapters for translated manga ID: {TranslatedMangaId}", translatedMangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            
            string requestUri = BuildQueryString($"translatedmangas/{translatedMangaId}/chapters", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<ChapterAttributesDto>>?> GetChapterByIdAsync(
            Guid chapterId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting chapter by ID: {ChapterId}", chapterId);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<ChapterAttributesDto>>>($"Chapters/{chapterId}", cancellationToken);
        }

        public async Task UpdateChapterAsync(
            Guid chapterId, UpdateChapterRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating chapter with ID: {ChapterId}", chapterId);
            await _apiClient.PutAsync($"Chapters/{chapterId}", request, cancellationToken);
        }

        public async Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting chapter with ID: {ChapterId}", chapterId);
            await _apiClient.DeleteAsync($"Chapters/{chapterId}", cancellationToken);
        }

        public async Task<ApiResponse<List<ChapterPageAttributesDto>>?> BatchUploadChapterPagesAsync(
            Guid chapterId,
            IEnumerable<(Stream stream, string fileName, string contentType)> files,
            IEnumerable<int> pageNumbers,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Batch uploading pages for Chapter ID: {ChapterId}", chapterId);
            using var formData = new MultipartFormDataContent();

            if (files == null || !files.Any())
            {
                throw new ArgumentException("Files collection cannot be null or empty.", nameof(files));
            }
            if (pageNumbers == null || !pageNumbers.Any())
            {
                throw new ArgumentException("PageNumbers collection cannot be null or empty.", nameof(pageNumbers));
            }
            if (files.Count() != pageNumbers.Count())
            {
                throw new ArgumentException("The number of files must match the number of page numbers.");
            }

            foreach (var fileTuple in files)
            {
                var streamContent = new StreamContent(fileTuple.stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileTuple.contentType);
                formData.Add(streamContent, "files", fileTuple.fileName);
            }

            foreach (var pageNumber in pageNumbers)
            {
                formData.Add(new StringContent(pageNumber.ToString()), "pageNumbers");
            }

            return await _apiClient.PostAsync<ApiResponse<List<ChapterPageAttributesDto>>>(
                $"Chapters/{chapterId}/pages/batch", formData, cancellationToken);
        }

        public async Task<ApiResponse<List<ChapterPageAttributesDto>>?> SyncChapterPagesAsync(
            Guid chapterId,
            string pageOperationsJson, 
            IDictionary<string, (Stream stream, string fileName, string contentType)>? files,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Syncing pages for Chapter ID: {ChapterId}", chapterId);
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(pageOperationsJson, Encoding.UTF8, "application/json"), "pageOperationsJson");

            if (files != null)
            {
                foreach (var fileEntry in files)
                {
                    var fileIdentifier = fileEntry.Key;
                    var (stream, fileName, contentType) = fileEntry.Value;

                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    formData.Add(streamContent, fileIdentifier, fileName);
                }
            }
            
            return await _apiClient.PutAsync<ApiResponse<List<ChapterPageAttributesDto>>>(
                $"Chapters/{chapterId}/pages", formData, cancellationToken);
        }

        private string BuildQueryString(string baseUri, Dictionary<string, List<string>> queryParams)
        {
            var queryString = new StringBuilder();
            if (queryParams != null && queryParams.Any())
            {
                bool firstParam = true;
                foreach (var param in queryParams)
                {
                    if (param.Value != null && param.Value.Any())
                    {
                        foreach (var value in param.Value)
                        {
                             if (string.IsNullOrEmpty(value)) continue;

                            if (firstParam)
                            {
                                queryString.Append("?");
                                firstParam = false;
                            }
                            else
                            {
                                queryString.Append("&");
                            }
                            queryString.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
                        }
                    }
                }
            }
            return $"{baseUri}{queryString}";
        }

        private void AddQueryParam(Dictionary<string, List<string>> queryParams, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].Add(value);
            }
        }
    }
}
```

### Bước 4: Cập nhật Interface `IApiClient.cs`

Đảm bảo `IApiClient` có phương thức `PutAsync` chấp nhận `HttpContent`.

```csharp
// MangaReaderLib\Services\Interfaces\IApiClient.cs
using System.Net.Http; 
// ... (các using khác)

namespace MangaReaderLib.Services.Interfaces
{
    public interface IApiClient
    {
        // ... (các phương thức GetAsync, PostAsync đã có)
        
        Task<TResponse?> PostAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default) where TResponse : class;
        
        Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class 
            where TResponse : class;

        Task PutAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) where TRequest : class;
        
        // Đảm bảo phương thức này tồn tại
        Task<TResponse?> PutAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default) where TResponse : class;

        Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
    }
}
```

### Bước 5: Cập nhật Implementation `ApiClient.cs`

Triển khai phương thức `PutAsync` với `HttpContent` nếu chưa có.

```csharp
// MangaReaderLib\Services\Implementations\ApiClient.cs
using System.Net.Http; 
// ... (các using khác)

namespace MangaReaderLib.Services.Implementations
{
    public class ApiClient : IApiClient
    {
        // ... (constructor và các phương thức hiện có)

        public async Task<TResponse?> PostAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default) 
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing POST request with HttpContent to {RequestUri}", requestUri);
                var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
                
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during POST request with HttpContent to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class 
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing PUT request to {RequestUri}", requestUri);
                var jsonContent = CreateJsonContent(content);
                var response = await _httpClient.PutAsync(requestUri, jsonContent, cancellationToken);
                
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during PUT request to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task PutAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class
        {
            try
            {
                _logger.LogInformation("Executing PUT request to {RequestUri}", requestUri);
                var jsonContent = CreateJsonContent(content);
                var response = await _httpClient.PutAsync(requestUri, jsonContent, cancellationToken);
                
                await EnsureSuccessStatusCodeAsync(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during PUT request to {RequestUri}", requestUri);
                throw;
            }
        }
        
        // Triển khai phương thức mới (nếu chưa có)
        public async Task<TResponse?> PutAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing PUT request with HttpContent to {RequestUri}", requestUri);
                var response = await _httpClient.PutAsync(requestUri, content, cancellationToken);
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during PUT request with HttpContent to {RequestUri}", requestUri);
                throw;
            }
        }

        // ... (DeleteAsync và các helper methods như CreateJsonContent, HandleResponseAsync, EnsureSuccessStatusCodeAsync, HandleErrorResponseAsync)
        private StringContent CreateJsonContent<T>(T content) where T : class
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            return stringContent;
        }

        private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                try
                {
                    return await JsonSerializer.DeserializeAsync<T>(content, _jsonOptions, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing response from {RequestUri}", response.RequestMessage?.RequestUri);
                    throw new InvalidOperationException("Invalid response format received from server.", ex);
                }
            }
            else
            {
                await HandleErrorResponseAsync(response, cancellationToken);
                return null; 
            }
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, cancellationToken);
            }
        }

        private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            ApiErrorResponse? errorResponse = null;
            try
            {
                errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse error response as ApiErrorResponse. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, content);
            }

            if (errorResponse?.Errors?.Count > 0)
            {
                throw new ApiException(
                    errorResponse.Errors[0].Detail ?? errorResponse.Errors[0].Title, 
                    errorResponse,                                                  
                    response.StatusCode                                             
                );
            }
            else
            {
                throw new HttpRequestException($"API request failed with status code: {(int)response.StatusCode} - {response.ReasonPhrase}", 
                    null, response.StatusCode);
            }
        }
    }
}
```

---

## II. Cập nhật `MangaReader_ManagerUI`

Mục tiêu: Cập nhật logic xây dựng URL ảnh chương để sử dụng `publicId` từ `ChapterPageAttributesDto`.

### Bước 1: Cập nhật logic hiển thị ảnh `ChapterPage`

*   **File bị ảnh hưởng**: `src/features/chapter/components/ChapterPageManager.jsx`
*   **Thay đổi**: Tìm đến đoạn code render ảnh trang chương. Sử dụng `publicId` từ `pageItem.attributes.publicId` và hằng số `CLOUDINARY_BASE_URL` để tạo URL ảnh.

```javascript
// src/features/chapter/components/ChapterPageManager.jsx
import React, { useEffect, useState } from 'react'; // Thêm các import cần thiết
import {
    Box, Button, Card, CardActions, CardContent, CardMedia, CircularProgress,
    Dialog, DialogActions, DialogContent, DialogTitle, Grid, IconButton, TextField, Tooltip, Typography
} from '@mui/material';
import { Add as AddIcon, Delete as DeleteIcon, UploadFile as UploadFileIcon } from '@mui/icons-material';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants';
import { createChapterPageEntrySchema, uploadChapterPageImageSchema } from '../../../schemas/chapterSchema';
import useChapterPageStore from '../../../stores/chapterPageStore';
import ConfirmDialog from '../../../components/common/ConfirmDialog';
import { handleApiError } from '../../../utils/errorUtils';


function ChapterPageManager({ chapterId, onPagesUpdated }) {
  const {
    chapterPages,
    fetchChapterPagesByChapterId,
    createPageEntry,
    uploadPageImage,
    deleteChapterPage,
  } = useChapterPageStore();

  const [loadingPages, setLoadingPages] = useState(true);
  const [openCreatePageDialog, setOpenCreatePageDialog] = useState(false);
  const [openUploadImageDialog, setOpenUploadImageDialog] = useState(false);
  const [pageEntryToUploadImage, setPageEntryToUploadImage] = useState(null);
  const [openConfirmDelete, setOpenConfirmDelete] = useState(false);
  const [pageToDelete, setPageToDelete] = useState(null);

  const {
    register: registerCreate,
    handleSubmit: handleSubmitCreate,
    formState: { errors: errorsCreate },
    reset: resetCreate,
  } = useForm({
    resolver: zodResolver(createChapterPageEntrySchema),
  });

  const {
    register: registerUpload,
    handleSubmit: handleSubmitUpload,
    formState: { errors: errorsUpload },
    reset: resetUpload,
  } = useForm({
    resolver: zodResolver(uploadChapterPageImageSchema),
  });

  useEffect(() => {
    if (chapterId) {
      setLoadingPages(true);
      fetchChapterPagesByChapterId(chapterId, true)
        .finally(() => setLoadingPages(false));
    }
  }, [chapterId, fetchChapterPagesByChapterId]);

  const handleCreatePageEntry = async (data) => {
    try {
      const pageId = await createPageEntry(chapterId, data);
      if (pageId) {
        setPageEntryToUploadImage({ id: pageId, pageNumber: data.pageNumber });
        setOpenUploadImageDialog(true);
      }
      setOpenCreatePageDialog(false);
      resetCreate();
      if (onPagesUpdated) onPagesUpdated();
    } catch (error) {
      console.error('Failed to create page entry:', error);
    }
  };

  const handleUploadImageRequest = (pageId, pageNumber) => {
    setPageEntryToUploadImage({ id: pageId, pageNumber: pageNumber });
    setOpenUploadImageDialog(true);
  };

  const handleUploadImage = async (data) => {
    if (pageEntryToUploadImage && data.file && data.file[0]) {
      try {
        await uploadPageImage(pageEntryToUploadImage.id, data.file[0], chapterId);
        setOpenUploadImageDialog(false);
        resetUpload();
      } catch (error) {
        console.error('Failed to upload page image:', error);
      }
    }
  };

  const handleDeleteRequest = (page) => {
    setPageToDelete(page);
    setOpenConfirmDelete(true);
  };

  const handleConfirmDelete = async () => {
    if (pageToDelete) {
      try {
        await deleteChapterPage(pageToDelete.id, chapterId);
        if (onPagesUpdated) onPagesUpdated();
      } catch (error) {
        console.error('Failed to delete chapter page:', error);
        handleApiError(error, 'Không thể xóa trang chương.');
      } finally {
        setOpenConfirmDelete(false);
        setPageToDelete(null);
      }
    }
  };

  const handleCloseConfirmDelete = () => {
    setOpenConfirmDelete(false);
    setPageToDelete(null);
  };

  return (
    <Box className="chapter-page-manager" sx={{ mt: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={() => setOpenCreatePageDialog(true)}
        >
          Thêm Trang mới (Entry)
        </Button>
      </Box>

      {loadingPages ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '200px' }}>
          <CircularProgress />
        </Box>
      ) : chapterPages.length === 0 ? (
        <Typography variant="h6" className="no-pages-message" sx={{ textAlign: 'center', py: 5 }}>
          Chưa có trang nào cho chương này.
        </Typography>
      ) : (
        <Grid container spacing={2} className="chapter-page-grid" columns={{ xs: 4, sm: 6, md: 12, lg: 12 }}>
          {chapterPages
            .sort((a, b) => a.attributes.pageNumber - b.attributes.pageNumber)
            .map((pageItem) => (
              <Grid item key={pageItem.id} sx={{ gridColumn: { xs: 'span 4', sm: 'span 3', md: 'span 3', lg: 'span 3' } }}>
                <Card className="chapter-page-card">
                  <CardMedia
                    component="img"
                    image={
                      pageItem.attributes.publicId
                        ? `${CLOUDINARY_BASE_URL}${pageItem.attributes.publicId}` // Sử dụng publicId
                        : 'https://via.placeholder.com/150x200?text=No+Image'
                    }
                    alt={`Page ${pageItem.attributes.pageNumber}`}
                    sx={{ width: '100%', height: 250, objectFit: 'contain', backgroundColor: '#eee', borderBottom: '1px solid #ddd' }}
                  />
                  <CardContent>
                    <Typography variant="subtitle1" gutterBottom>
                      Trang số: {pageItem.attributes.pageNumber}
                    </Typography>
                  </CardContent>
                  <CardActions className="card-actions">
                    <Tooltip title="Tải ảnh lên">
                      <IconButton
                        color="primary"
                        onClick={() => handleUploadImageRequest(pageItem.id, pageItem.attributes.pageNumber)}
                      >
                        <UploadFileIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Xóa trang">
                      <IconButton
                        color="secondary"
                        onClick={() => handleDeleteRequest(pageItem)}
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Tooltip>
                  </CardActions>
                </Card>
              </Grid>
            ))}
        </Grid>
      )}

      <Dialog open={openCreatePageDialog} onClose={() => setOpenCreatePageDialog(false)}>
        <DialogTitle>Thêm Trang mới (Entry)</DialogTitle>
        <Box component="form" onSubmit={handleSubmitCreate(handleCreatePageEntry)} noValidate>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Số trang"
              type="number"
              fullWidth
              variant="outlined"
              {...registerCreate('pageNumber', { valueAsNumber: true })}
              error={!!errorsCreate.pageNumber}
              helperText={errorsCreate.pageNumber?.message}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenCreatePageDialog(false)} variant="outlined">
              Hủy
            </Button>
            <Button type="submit" variant="contained" color="primary">
              Tạo Entry
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={openUploadImageDialog} onClose={() => setOpenUploadImageDialog(false)}>
        <DialogTitle>Tải ảnh cho Trang {pageEntryToUploadImage?.pageNumber}</DialogTitle>
        <Box component="form" onSubmit={handleSubmitUpload(handleUploadImage)} noValidate>
          <DialogContent>
            <TextField
              margin="dense"
              label="Chọn File ảnh"
              type="file"
              fullWidth
              variant="outlined"
              {...registerUpload('file')}
              error={!!errorsUpload.file}
              helperText={errorsUpload.file?.message}
              inputProps={{ accept: 'image/jpeg,image/png,image/webp' }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenUploadImageDialog(false)} variant="outlined">
              Hủy
            </Button>
            <Button type="submit" variant="contained" color="primary">
              Tải lên
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <ConfirmDialog
        open={openConfirmDelete}
        onClose={handleCloseConfirmDelete}
        onConfirm={handleConfirmDelete}
        title="Xác nhận xóa Trang chương"
        message={`Bạn có chắc chắn muốn xóa trang ${pageToDelete?.attributes?.pageNumber} này? Thao tác này không thể hoàn tác và sẽ xóa ảnh liên quan.`}
      />
    </Box>
  );
}

export default ChapterPageManager;
```

---

## III. Cập nhật `MangaReader_WebUI` (Chỉ Logic Đường Dẫn Ảnh)

Mục tiêu: Đảm bảo logic hiển thị ảnh trang chương sử dụng `publicId` đúng cách khi nguồn dữ liệu là `MangaReaderLib`.

### Bước 1: Cập nhật `MangaReaderLibToAtHomeServerResponseMapper.cs`

*   **File bị ảnh hưởng**: `Services/MangaServices/DataProcessing/Services/MangaReaderLibMappers/MangaReaderLibToAtHomeServerResponseMapper.cs`
*   **Thay đổi**: Khi map `ChapterPageAttributesDto` từ `MangaReaderLib` sang `AtHomeServerResponse` (được dùng bởi `ChapterReadingServices`), đảm bảo rằng `Data` và `DataSaver` trong `AtHomeChapterData` chứa các URL Cloudinary đầy đủ được xây dựng từ `pageDto.Attributes.PublicId`.

    ```csharp
    // MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers\MangaReaderLibToAtHomeServerResponseMapper.cs
    using MangaReader.WebUI.Models.Mangadex;
    using MangaReaderLib.DTOs.Common;
    using MangaReaderLib.DTOs.Chapters;
    using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
    using Microsoft.Extensions.Logging;
    using System.Diagnostics;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;

    namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
    {
        public class MangaReaderLibToAtHomeServerResponseMapper : IMangaReaderLibToAtHomeServerResponseMapper
        {
            private readonly ILogger<MangaReaderLibToAtHomeServerResponseMapper> _logger;
            private readonly string _cloudinaryBaseUrl;

            public MangaReaderLibToAtHomeServerResponseMapper(
                ILogger<MangaReaderLibToAtHomeServerResponseMapper> logger,
                IConfiguration configuration)
            {
                _logger = logger;
                _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                    ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured for AtHomeServerResponseMapper.");
            }

            public AtHomeServerResponse MapToAtHomeServerResponse(
                ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
                string chapterId,
                string mangaReaderLibBaseUrlIgnored) 
            {
                Debug.Assert(chapterPagesData != null, "chapterPagesData không được null khi mapping.");
                Debug.Assert(!string.IsNullOrEmpty(chapterId), "chapterId không được rỗng.");

                var pages = new List<string>();
                if (chapterPagesData.Data != null && chapterPagesData.Data.Any())
                {
                    var sortedPagesDto = chapterPagesData.Data.OrderBy(p => p.Attributes.PageNumber);

                    foreach (var pageDto in sortedPagesDto)
                    {
                        if (pageDto?.Attributes?.PublicId != null)
                        {
                            // PublicId từ API MangaReaderLib đã là "chapters/{ChapterId}/pages/{PageId}"
                            // Chỉ cần ghép với Cloudinary base URL.
                            var imageUrl = $"{_cloudinaryBaseUrl}/{pageDto.Attributes.PublicId}";
                            pages.Add(imageUrl);
                            _logger.LogDebug("Mapped MangaReaderLib page: ChapterId={ChapterId}, PageNumber={PageNumber}, PublicId={PublicId} to Cloudinary URL: {ImageUrl}",
                                chapterId, pageDto.Attributes.PageNumber, pageDto.Attributes.PublicId, imageUrl);
                        }
                        else
                        {
                            _logger.LogWarning("Skipping page due to missing PublicId. ChapterId={ChapterId}, PageDtoId={PageDtoId}", chapterId, pageDto?.Id);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No page data found in chapterPagesData for ChapterId={ChapterId}", chapterId);
                }
                
                return new AtHomeServerResponse
                {
                    Result = "ok",
                    BaseUrl = "", // Không dùng BaseUrl của MangaDex@Home cho MangaReaderLib
                    Chapter = new AtHomeChapterData
                    {
                        Hash = chapterId, 
                        Data = pages,      
                        DataSaver = pages  
                    }
                };
            }
        }
    }
    ```

### Bước 2: Kiểm tra `Views/ChapterRead/_ChapterImagesPartial.cshtml`

*   Đảm bảo view này render trực tiếp các URL ảnh từ `Model` (là `List<string>`), vì các URL này đã được chuẩn bị sẵn bởi mapper.

    ```html
    <!-- Views\ChapterRead\_ChapterImagesPartial.cshtml -->
    @model List<string>

    @if (Model != null && Model.Any())
    {
        <div class="chapter-images">
            @foreach (var imgPageUrl in Model) <!-- imgPageUrl bây giờ là URL đầy đủ -->
            {
                <div class="page-image-container">
                    <div class="loading-indicator">
                        <div class="spinner-border text-primary" role="status"></div>
                    </div>
                    <img class="chapter-page-image lazy-load" src="" data-src="@imgPageUrl" alt="Trang truyện @(Model.IndexOf(imgPageUrl) + 1)" />
                    <div class="error-overlay">
                        <i class="bi bi-exclamation-triangle-fill"></i>
                        <span>Lỗi tải ảnh</span>
                        <button class="btn btn-sm btn-light retry-button mt-2">Thử lại</button>
                    </div>
                </div>
            }
        </div>
    }
    else
    {
        <div class="alert alert-warning text-center">
            <i class="bi bi-exclamation-circle"></i> Không có trang nào cho chương này
        </div>
    }
    ```
    Logic này không cần thay đổi vì nó đã render URL trực tiếp.

---