import { zodResolver } from '@hookform/resolvers/zod'
import { Add as AddIcon, Delete as DeleteIcon, UploadFile as UploadFileIcon } from '@mui/icons-material'
import {
    Box,
    Button,
    Card,
    CardActions,
    CardContent,
    CardMedia,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Grid,
    IconButton,
    TextField,
    Tooltip,
    Typography,
} from '@mui/material'
import React, { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import { showSuccessToast } from '../../../components/common/Notification'
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants'
import { createChapterPageEntrySchema, uploadChapterPageImageSchema } from '../../../schemas/chapterSchema'
import useChapterPageStore from '../../../stores/chapterPageStore'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').ChapterPage} ChapterPage
 * @typedef {import('../../../types/manga').CreateChapterPageEntryRequest} CreateChapterPageEntryRequest
 */

/**
 * ChapterPageManager component for managing pages of a specific chapter.
 * @param {object} props
 * @param {string} props.chapterId - The ID of the chapter.
 */
function ChapterPageManager({ chapterId }) {
  const {
    chapterPages,
    fetchChapterPagesByChapterId,
    createPageEntry,
    uploadPageImage,
    deleteChapterPage,
  } = useChapterPageStore()

  const [loadingPages, setLoadingPages] = useState(true)
  const [openCreatePageDialog, setOpenCreatePageDialog] = useState(false)
  const [openUploadImageDialog, setOpenUploadImageDialog] = useState(false)
  const [pageEntryToUploadImage, setPageEntryToUploadImage] = useState(null) // Stores pageId and pageNumber
  const [openConfirmDelete, setOpenConfirmDelete] = useState(false)
  const [pageToDelete, setPageToDelete] = useState(null)

  const {
    register: registerCreate,
    handleSubmit: handleSubmitCreate,
    formState: { errors: errorsCreate },
    reset: resetCreate,
  } = useForm({
    resolver: zodResolver(createChapterPageEntrySchema),
  })

  const {
    register: registerUpload,
    handleSubmit: handleSubmitUpload,
    formState: { errors: errorsUpload },
    reset: resetUpload,
  } = useForm({
    resolver: zodResolver(uploadChapterPageImageSchema),
  })

  useEffect(() => {
    if (chapterId) {
      setLoadingPages(true)
      fetchChapterPagesByChapterId(chapterId, true)
        .finally(() => setLoadingPages(false))
    }
  }, [chapterId, fetchChapterPagesByChapterId])

  const handleCreatePageEntry = async (data) => {
    try {
      const pageId = await createPageEntry(chapterId, data)
      if (pageId) {
        showSuccessToast('Entry trang chương đã được tạo.')
        fetchChapterPagesByChapterId(chapterId) // Refresh pages after creating new entry
        // Optionally open upload dialog directly
        setPageEntryToUploadImage({ id: pageId, pageNumber: data.pageNumber })
        setOpenUploadImageDialog(true)
      }
      setOpenCreatePageDialog(false)
      resetCreate()
    } catch (error) {
      console.error('Failed to create page entry:', error)
      // Error handled by store
    }
  }

  const handleUploadImageRequest = (pageId, pageNumber) => {
    setPageEntryToUploadImage({ id: pageId, pageNumber: pageNumber })
    setOpenUploadImageDialog(true)
  }

  const handleUploadImage = async (data) => {
    if (pageEntryToUploadImage && data.file && data.file[0]) {
      try {
        await uploadPageImage(pageEntryToUploadImage.id, data.file[0])
        // Re-fetch pages to ensure UI updates with new publicId (image)
        fetchChapterPagesByChapterId(chapterId)
        setOpenUploadImageDialog(false)
        resetUpload()
      } catch (error) {
        console.error('Failed to upload page image:', error)
        // Error handled by store
      }
    }
  }

  const handleDeleteRequest = (page) => {
    setPageToDelete(page)
    setOpenConfirmDelete(true)
  }

  const handleConfirmDelete = async () => {
    if (pageToDelete) {
      try {
        await deleteChapterPage(pageToDelete.id, chapterId)
      } catch (error) {
        console.error('Failed to delete chapter page:', error)
        handleApiError(error, 'Không thể xóa trang chương.')
      } finally {
        setOpenConfirmDelete(false)
        setPageToDelete(null)
      }
    }
  }

  const handleCloseConfirmDelete = () => {
    setOpenConfirmDelete(false)
    setPageToDelete(null)
  }

  return (
    <Box className="chapter-page-manager" sx={{ mt: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={() => setOpenCreatePageDialog(true)}
        >
          Thêm Trang mới
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
        <Grid container spacing={2} className="chapter-page-grid">
          {chapterPages
            .sort((a, b) => a.attributes.pageNumber - b.attributes.pageNumber) // Sort by pageNumber
            .map((pageItem) => (
              <Grid item key={pageItem.id} xs={12} sm={6} md={4} lg={3}>
                <Card className="chapter-page-card">
                  <CardMedia
                    component="img"
                    image={pageItem.attributes.publicId ? `${CLOUDINARY_BASE_URL}${pageItem.attributes.publicId}` : 'https://via.placeholder.com/150x200?text=No+Image'}
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

      {/* Create Page Entry Dialog */}
      <Dialog open={openCreatePageDialog} onClose={() => setOpenCreatePageDialog(false)}>
        <DialogTitle>Thêm Trang mới</DialogTitle>
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
              Tạo
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Upload Image Dialog */}
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
  )
}

export default ChapterPageManager 