import React, { useState, useEffect, useCallback } from 'react';
import {
    Box, Button, Typography, Paper, IconButton, CircularProgress, Tooltip, CardMedia, Chip
} from '@mui/material';
import { 
    AddPhotoAlternate as AddPhotoAlternateIcon, 
    DeleteOutline as DeleteOutlineIcon, 
    Save as SaveIcon, 
    CloudUpload as CloudUploadIcon,
    DragIndicator as DragIndicatorIcon
} from '@mui/icons-material';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import { v4 as uuidv4 } from 'uuid';
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants';
import { showSuccessToast, showErrorToast } from '../../../components/common/Notification';
import chapterPageApi from '../../../api/chapterPageApi';
import useUiStore from '../../../stores/uiStore'; // Để quản lý loading global (nếu cần)
import useChapterStore from '../../../stores/chapterStore'; // Để fetch lại chapter list (pagesCount)


/**
 * @typedef {object} PageItem
 * @property {string} id - ID duy nhất cho mục trong danh sách (client-side, draggableId).
 * @property {string|null} pageId - ID của trang từ server (nếu đã tồn tại).
 * @property {string|null} publicId - publicId từ server (nếu đã tồn tại).
 * @property {File|null} file - Đối tượng File nếu là ảnh mới từ client.
 * @property {string} previewUrl - URL để hiển thị preview ảnh.
 * @property {boolean} isNew - Đánh dấu là ảnh mới hay đã có trên server.
 * @property {string|null} fileIdentifier - Dùng cho API sync, liên kết với file trong FormData.
 * @property {number} pageNumber - Số trang, sẽ được cập nhật sau khi kéo thả và trước khi lưu.
 * @property {string} name - Tên file hoặc tên hiển thị.
 */

function ChapterPageManager({ chapterId, onPagesUpdated }) {
  /** @type {[PageItem[], React.Dispatch<React.SetStateAction<PageItem[]>>]} */
  const [pages, setPages] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const setLoadingGlobal = useUiStore((state) => state.setLoading);
  const fetchChaptersByTranslatedMangaIdStore = useChapterStore((state) => state.fetchChaptersByTranslatedMangaId);
  const currentChapterDetails = useChapterStore((state) => state.chapters.find(c => c.id === chapterId));


  const loadChapterPages = useCallback(async () => {
    if (!chapterId) return;
    setIsLoading(true);
    setLoadingGlobal(true);
    try {
      const response = await chapterPageApi.getChapterPages(chapterId, { limit: 1000 }); // Lấy tối đa 1000 trang
      if (response && response.data) {
        const fetchedPages = response.data
          .sort((a, b) => a.attributes.pageNumber - b.attributes.pageNumber)
          .map((p) => ({
            id: p.id, 
            pageId: p.id,
            publicId: p.attributes.publicId,
            file: null,
            previewUrl: `${CLOUDINARY_BASE_URL}${p.attributes.publicId}`,
            isNew: false,
            fileIdentifier: null,
            pageNumber: p.attributes.pageNumber,
            name: `Trang ${p.attributes.pageNumber} (Server)`,
          }));
        setPages(fetchedPages);
      } else {
        setPages([]);
      }
    } catch (error) {
      showErrorToast('Không thể tải danh sách trang của chương.');
      console.error("Failed to load chapter pages:", error);
    } finally {
      setIsLoading(false);
      setLoadingGlobal(false);
    }
  }, [chapterId, setLoadingGlobal]);

  useEffect(() => {
    loadChapterPages();
  }, [loadChapterPages]);

  const onDragEnd = (result) => {
    if (!result.destination) return;
    const items = Array.from(pages);
    const [reorderedItem] = items.splice(result.source.index, 1);
    items.splice(result.destination.index, 0, reorderedItem);

    const updatedPages = items.map((page, index) => ({
      ...page,
      pageNumber: index + 1,
    }));
    setPages(updatedPages);
  };

  const handleFileChange = (event) => {
    const files = Array.from(event.target.files);
    if (files.length === 0) return;

    const newImageItems = files.map(file => {
      const tempId = uuidv4();
      return {
        id: tempId,
        pageId: null, // Sẽ được gán PageId mới (UUID) trước khi gửi đi
        publicId: null,
        file: file,
        previewUrl: URL.createObjectURL(file),
        isNew: true,
        fileIdentifier: `new_image_${tempId}`,
        pageNumber: 0, // Sẽ được cập nhật
        name: file.name,
      };
    });

    setPages(prevPages => {
      const updated = [...prevPages, ...newImageItems];
      return updated.map((page, index) => ({ ...page, pageNumber: index + 1 }));
    });
    // Reset input file để có thể chọn lại file giống tên
    event.target.value = null; 
  };

  const handleDeletePage = (idToDelete) => {
    setPages(prevPages => {
      const pageToDelete = prevPages.find(p => p.id === idToDelete);
      if (pageToDelete && pageToDelete.previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(pageToDelete.previewUrl); // Giải phóng Object URL cho ảnh mới
      }
      const updated = prevPages.filter(page => page.id !== idToDelete);
      return updated.map((page, index) => ({ ...page, pageNumber: index + 1 }));
    });
  };

  const handleSaveChanges = async () => {
    if (!chapterId) {
      showErrorToast("Chapter ID không hợp lệ.");
      return;
    }
    setIsSaving(true);
    setLoadingGlobal(true);

    const pageOperations = [];
    const filesToUpload = new Map();

    pages.forEach((page, index) => {
      const pageIdForOperation = page.isNew ? uuidv4() : page.pageId;
      
      const operation = {
        pageId: pageIdForOperation, 
        pageNumber: index + 1,
        fileIdentifier: null,
      };

      if (page.isNew && page.file) {
        // Đảm bảo fileIdentifier là duy nhất và được dùng để liên kết file trong FormData
        operation.fileIdentifier = page.fileIdentifier || `new_image_${page.id}`;
        filesToUpload.set(operation.fileIdentifier, page.file);
      }
      pageOperations.push(operation);
    });

    const formData = new FormData();
    formData.append('pageOperationsJson', JSON.stringify(pageOperations));

    filesToUpload.forEach((file, identifier) => {
      formData.append(identifier, file, file.name);
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
    // Dọn dẹp Object URLs khi component unmount hoặc pages thay đổi
    return () => {
      pages.forEach(page => {
        if (page.isNew && page.previewUrl && page.previewUrl.startsWith('blob:')) {
          URL.revokeObjectURL(page.previewUrl);
        }
      });
    };
  }, [pages]);


  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>;
  }

  return (
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
                flexWrap: 'nowrap', // Không cho xuống dòng, để scroll ngang
                gap: 2,
                p: 2,
                overflowX: 'auto', // Cho phép scroll ngang
                overflowY: 'hidden',
                border: pages.length > 0 ? (snapshotDroppable.isDraggingOver ? '2px dashed primary.main' : '1px dashed grey') : 'none',
                borderRadius: 1,
                minHeight: pages.length > 0 ? 250 : 'auto',
                backgroundColor: snapshotDroppable.isDraggingOver ? 'rgba(0,0,255,0.05)' :'action.disabledBackground',
                alignItems: 'flex-start', // Căn các item lên trên
              }}
            >
              {pages.map((page, index) => (
                <Draggable key={page.id} draggableId={page.id} index={index}>
                  {(providedDraggable, snapshotDraggable) => (
                    <Paper
                      ref={providedDraggable.innerRef}
                      {...providedDraggable.draggableProps}
                      // {...providedDraggable.dragHandleProps} // Không dùng dragHandleProps ở đây để toàn bộ card là tay cầm
                      elevation={snapshotDraggable.isDragging ? 8 : 2}
                      sx={{
                        width: 160,
                        minWidth: 160, // Quan trọng cho scroll ngang
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
                        {...providedDraggable.dragHandleProps} // Tay cầm kéo thả ở đây
                        sx={{ 
                            width: '100%', 
                            display: 'flex', 
                            justifyContent: 'center', 
                            color: 'text.disabled',
                            cursor: 'grab',
                            pb: 0.5,
                            touchAction: 'none', // Important for touch devices
                         }}
                        onMouseDown={(e) => e.stopPropagation()} // Ngăn không cho các event khác bị trigger
                      >
                        <DragIndicatorIcon fontSize="small"/>
                      </Box>
                      <CardMedia
                        component="img"
                        image={page.previewUrl}
                        alt={`Trang ${index + 1}`}
                        sx={{
                          width: 'calc(100% - 8px)', // Để có padding nhỏ
                          height: 140, // Giảm chiều cao để có chỗ cho text và các elements khác
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