# TODO: Sửa lỗi Upload và Đồng bộ hóa Ảnh Trang Chapter

Tài liệu này mô tả các bước cần thực hiện để sửa lỗi liên quan đến việc upload ảnh mới và đồng bộ hóa các trang của một chapter trong `MangaReader_ManagerUI`.

## Vấn đề Hiện tại

Khi người dùng thêm ảnh mới vào một chapter và thực hiện lưu (đồng bộ hóa), API backend (`MangaReaderAPI`) báo lỗi không tìm thấy file được chỉ định trong `pageOperationsJson` ("File with identifier '...' was specified but not found in the uploaded files."). Log từ server proxy (`MangaReader_ManagerUI.Server`) cũng cho thấy `Files count: 0` tại `ChaptersController`, ngụ ý rằng server proxy không nhận được hoặc không xử lý đúng các file ảnh từ client.

## Các bước thực hiện

### Bước 1: Cập nhật `ChapterPageManager.jsx` (Phía Client)

Mục tiêu: Đảm bảo rằng khi client gửi `FormData` đến server proxy, các file ảnh mới được `append` với key chính là `fileIdentifier` đã được tạo và gửi trong `pageOperationsJson`.

**File**: `MangaReader_ManagerUI/mangareader_managerui.client/src/features/chapter/components/ChapterPageManager.jsx`

**Thay đổi**:
Trong hàm `handleSaveChanges`, đảm bảo rằng `formData.append()` sử dụng đúng `page.fileIdentifier` làm key cho file.

```javascript
// MangaReader_ManagerUI/mangareader_managerui.client/src/features/chapter/components/ChapterPageManager.jsx
// ... (các import và code khác giữ nguyên)

function ChapterPageManager({ chapterId, onPagesUpdated }) {
  // ... (các state và useEffect giữ nguyên)

  const loadChapterPages = useCallback(async () => {
    // ... (giữ nguyên)
  }, [chapterId, setLoadingGlobal]);

  useEffect(() => {
    loadChapterPages();
  }, [loadChapterPages]);

  const onDragEnd = (result) => {
    // ... (giữ nguyên)
  };

  const handleFileChange = (event) => {
    // ... (giữ nguyên)
  };

  const handleDeletePage = (idToDelete) => {
    // ... (giữ nguyên)
  };

  const handleSaveChanges = async () => {
    if (!chapterId) {
      showErrorToast("Chapter ID không hợp lệ.");
      return;
    }
    setIsSaving(true);
    setLoadingGlobal(true);

    const pageOperations = [];
    const filesToUpload = new Map(); // Sử dụng Map để tránh key trùng lặp nếu có

    pages.forEach((page, index) => {
      const pageIdForOperation = page.isNew ? uuidv4() : page.pageId;
      
      const operation = {
        pageId: pageIdForOperation, 
        pageNumber: index + 1,
        fileIdentifier: null,
      };

      if (page.isNew && page.file) {
        // Đảm bảo fileIdentifier là duy nhất và được dùng để liên kết file trong FormData
        // Sửa ở đây: fileIdentifier phải là key mà server sẽ dùng để lấy file
        operation.fileIdentifier = page.fileIdentifier || `new_image_${page.id}`; // Giữ nguyên nếu đã có, nếu không thì tạo
        filesToUpload.set(operation.fileIdentifier, page.file);
      }
      pageOperations.push(operation);
    });

    const formData = new FormData();
    formData.append('pageOperationsJson', JSON.stringify(pageOperations));

    // Quan trọng: Key khi append file phải chính xác là fileIdentifier
    filesToUpload.forEach((file, identifier) => {
      formData.append(identifier, file, file.name); // Đảm bảo `identifier` là key của file
    });

    try {
      const response = await chapterPageApi.syncChapterPages(chapterId, formData);
      if (response && response.data) {
        showSuccessToast('Đã lưu thứ tự và cập nhật trang thành công!');
        
        const serverResponseData = response.data; 

        const syncedPages = serverResponseData
         .sort((a, b) => a.pageNumber - b.pageNumber) 
         .map(p_server_attr => { 
            const publicIdParts = p_server_attr.publicId.split('/');
            const extractedPageId = publicIdParts[publicIdParts.length - 1]; 
            const clientDraggableId = extractedPageId || uuidv4(); 

            return {
                id: clientDraggableId, 
                pageId: extractedPageId, 
                publicId: p_server_attr.publicId,
                file: null,
                previewUrl: `${CLOUDINARY_BASE_URL}${p_server_attr.publicId}`,
                isNew: false,
                fileIdentifier: null,
                pageNumber: p_server_attr.pageNumber,
                name: `Trang ${p_server_attr.pageNumber} (Server)`,
            };
         });

        setPages(syncedPages); 

        if (onPagesUpdated) onPagesUpdated();
        const parentTmId = currentChapterDetails?.relationships?.find(r => r.type === 'translated_manga')?.id;
        if (parentTmId) {
            fetchChaptersByTranslatedMangaIdStore(parentTmId, false);
        }

      } else {
        showErrorToast('Lưu không thành công. Phản hồi từ server không hợp lệ.');
      }
    } catch (error) {
      console.error("Failed to save chapter pages:", error);
      showErrorToast(error.message || 'Lỗi khi lưu trang. Vui lòng thử lại.');
    } finally {
      setIsSaving(false);
      setLoadingGlobal(false);
    }
  };
  
  useEffect(() => {
    // ... (giữ nguyên)
  }, [pages]);


  if (isLoading) {
    // ... (giữ nguyên)
  }

  return (
    // ... (JSX giữ nguyên)
    <Box sx={{ p: { xs: 1, sm: 2} }}>
      <Typography variant="h6" gutterBottom>
        Quản lý trang ảnh ({pages.length} trang)
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        Kéo thả để sắp xếp lại thứ tự các trang. Số trang sẽ được tự động cập nhật khi bạn lưu.
      </Typography>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, mt: 2, flexWrap: 'wrap', gap: 1 }}>
        <Button
          variant="outlined"
          component="label"
          startIcon={<AddPhotoAlternateIcon />}
          disabled={isSaving}
        >
          Thêm ảnh mới
          <input type="file" hidden multiple accept="image/jpeg,image/png,image/webp" onChange={handleFileChange} />
        </Button>
        <Button
          variant="contained"
          color="primary"
          startIcon={<SaveIcon />}
          onClick={handleSaveChanges}
          disabled={isSaving || pages.length === 0}
        >
          {isSaving ? <CircularProgress size={24} color="inherit" /> : 'Lưu & Đồng bộ hóa'}
        </Button>
      </Box>
      
      {pages.length === 0 && (
        <Paper elevation={0} sx={{ p: 3, textAlign: 'center', mt: 3, backgroundColor: 'action.hover' }}>
          <CloudUploadIcon sx={{ fontSize: 48, color: 'text.disabled', mb:1 }}/>
          <Typography variant="subtitle1" color="text.secondary">
            Chưa có ảnh nào cho chương này.
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb:2}}>
            Hãy nhấn "Thêm ảnh mới" để bắt đầu tải lên.
          </Typography>
        </Paper>
      )}

      <DragDropContext onDragEnd={onDragEnd}>
        <Droppable droppableId="pagesDroppable" direction="horizontal">
          {(provided, snapshotDroppable) => (
            <Box
              {...provided.droppableProps}
              ref={provided.innerRef}
              sx={{
                display: 'flex',
                flexWrap: 'nowrap', 
                gap: 2,
                p: 2,
                overflowX: 'auto', 
                overflowY: 'hidden',
                border: pages.length > 0 ? (snapshotDroppable.isDraggingOver ? '2px dashed primary.main' : '1px dashed grey') : 'none',
                borderRadius: 1,
                minHeight: pages.length > 0 ? 250 : 'auto',
                backgroundColor: snapshotDroppable.isDraggingOver ? 'rgba(0,0,255,0.05)' :'action.disabledBackground',
                alignItems: 'flex-start', 
              }}
            >
              {pages.map((page, index) => (
                <Draggable key={page.id} draggableId={page.id} index={index}>
                  {(providedDraggable, snapshotDraggable) => (
                    <Paper
                      ref={providedDraggable.innerRef}
                      {...providedDraggable.draggableProps}
                      elevation={snapshotDraggable.isDragging ? 8 : 2}
                      sx={{
                        width: 160,
                        minWidth: 160, 
                        height: 230,
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        p: 1,
                        position: 'relative',
                        backgroundColor: 'background.paper',
                        transition: 'box-shadow 0.2s ease, transform 0.2s ease',
                        boxShadow: snapshotDraggable.isDragging ? '0px 6px 18px rgba(0,0,0,0.3)' : '0px 2px 6px rgba(0,0,0,0.1)',
                        transform: snapshotDraggable.isDragging ? 'rotate(1deg) scale(1.03)' : 'rotate(0deg) scale(1)',
                        cursor: 'grab',
                        '&:active': {
                            cursor: 'grabbing',
                        }
                      }}
                    >
                      <Box 
                        {...providedDraggable.dragHandleProps} 
                        sx={{ 
                            width: '100%', 
                            display: 'flex', 
                            justifyContent: 'center', 
                            color: 'text.disabled',
                            cursor: 'grab',
                            pb: 0.5,
                            touchAction: 'none', 
                         }}
                        onMouseDown={(e) => e.stopPropagation()} 
                      >
                        <DragIndicatorIcon fontSize="small"/>
                      </Box>
                      <CardMedia
                        component="img"
                        image={page.previewUrl}
                        alt={`Trang ${index + 1}`}
                        sx={{
                          width: 'calc(100% - 8px)', 
                          height: 140, 
                          objectFit: 'contain',
                          mb: 1,
                          border: '1px solid #ddd',
                          borderRadius: '4px',
                          backgroundColor: '#f0f0f0'
                        }}
                      />
                      <Typography variant="caption" noWrap sx={{ width: '100%', textAlign: 'center', px:0.5, fontWeight: '500' }}>
                        Trang {page.pageNumber}
                      </Typography>
                      <Typography variant="caption" noWrap sx={{ width: '100%', textAlign: 'center', px:0.5, color:'text.secondary', fontSize: '0.7rem' }}>
                         ({page.isNew ? 'Mới' : 'Đã có'}) {page.name.length > 15 ? page.name.substring(0,12) + '...' : page.name}
                      </Typography>
                      
                      <Tooltip title="Xóa trang này">
                        <IconButton
                          size="small"
                          onClick={(e) => { e.stopPropagation(); handleDeletePage(page.id);}}
                          disabled={isSaving}
                          sx={{
                            position: 'absolute',
                            top: 4,
                            right: 4,
                            color: 'error.light',
                            backgroundColor: 'rgba(0,0,0,0.3)',
                            '&:hover': {
                              backgroundColor: 'rgba(0,0,0,0.5)',
                              color: 'error.main'
                            },
                            p: 0.3
                          }}
                        >
                          <DeleteOutlineIcon fontSize="inherit" />
                        </IconButton>
                      </Tooltip>
                    </Paper>
                  )}
                </Draggable>
              ))}
              {provided.placeholder}
            </Box>
          )}
        </Droppable>
      </DragDropContext>
    </Box>
  );
}

export default ChapterPageManager;
```

### Bước 2: Cập nhật `ChaptersController.cs` (Phía Server Proxy)

Mục tiêu: Thay đổi cách server proxy nhận và xử lý file. Thay vì bind `IFormFileCollection files`, chúng ta sẽ đọc trực tiếp từ `HttpContext.Request.Form.Files`. Điều này linh hoạt hơn với các tên file động.

**File**: `MangaReader_ManagerUI/MangaReader_ManagerUI.Server/Controllers/ChaptersController.cs`

**Thay đổi**:
Trong action `SyncChapterPages`, sửa cách lấy `files` và tạo `fileDictionary`.

```csharp
// MangaReader_ManagerUI/MangaReader_ManagerUI.Server/Controllers/ChaptersController.cs
// ... (các using và khai báo class giữ nguyên)

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
            // Bỏ tham số [FromForm] IFormFileCollection? files ở đây
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
// ... (các actions khác giữ nguyên)
    }
}
```

### Bước 3: Kiểm tra `ChapterClient.cs` (Trong `MangaReaderLib`)

Mục tiêu: Đảm bảo `ChapterClient` gửi `MultipartFormDataContent` với các file có key là `fileIdentifier` đúng như mong đợi của API backend.

**File**: `MangaReaderLib/Services/Implementations/ChapterClient.cs`

**Xem xét**:
Logic hiện tại trong `SyncChapterPagesAsync` của `ChapterClient.cs` có vẻ đã đúng khi nó lặp qua `IDictionary<string, (Stream stream, string fileName, string contentType)>? files` và `formData.Add(streamContent, fileIdentifier, fileName);`. `fileIdentifier` ở đây chính là key.

```csharp
// MangaReaderLib/Services/Implementations/ChapterClient.cs
// ... (các using và khai báo class giữ nguyên)

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
                    var fileIdentifier = fileEntry.Key; // Đây là key được dùng để add vào formData
                    var (stream, fileName, contentType) = fileEntry.Value;

                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    // fileIdentifier sẽ là tên của part trong multipart request
                    formData.Add(streamContent, fileIdentifier, fileName); 
                }
            }
            
            return await _apiClient.PutAsync<ApiResponse<List<ChapterPageAttributesDto>>>(
                $"Chapters/{chapterId}/pages", formData, cancellationToken);
        }
    // ... (các phương thức khác giữ nguyên)
}
```
Logic này có vẻ ổn. Vấn đề chính rất có thể nằm ở việc `ChaptersController` của UI Server không chuyển đúng `files` cho `ChapterClient`. Việc thay đổi ở Bước 2 để đọc từ `HttpContext.Request.Form.Files` sẽ giải quyết điều này.