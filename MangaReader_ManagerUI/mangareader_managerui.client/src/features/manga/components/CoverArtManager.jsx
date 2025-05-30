import React, { useEffect, useState } from 'react'
import {
  Box,
  Typography,
  Grid,
  Button,
  Card,
  CardMedia,
  CardContent,
  CardActions,
  IconButton,
  TextField,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
} from '@mui/material'
import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { uploadCoverArtSchema } from '../../../schemas/mangaSchema'
import mangaApi from '../../../api/mangaApi'
import coverArtApi from '../../../api/coverArtApi'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants'

/**
 * @typedef {import('../../../types/manga').CoverArt} CoverArt
 * @typedef {import('../../../types/manga').UploadCoverArtRequest} UploadCoverArtRequest
 */

/**
 * CoverArtManager component for managing cover arts of a specific manga.
 * @param {object} props
 * @param {string} props.mangaId - The ID of the manga.
 */
function CoverArtManager({ mangaId }) {
  /** @type {[CoverArt[], React.Dispatch<React.SetStateAction<CoverArt[]>>]} */
  const [covers, setCovers] = useState([])
  const [loadingCovers, setLoadingCovers] = useState(true)
  const [openUploadDialog, setOpenUploadDialog] = useState(false)
  const [openConfirmDelete, setOpenConfirmDelete] = useState(false)
  const [coverArtToDelete, setCoverArtToDelete] = useState(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm({
    resolver: zodResolver(uploadCoverArtSchema),
  })

  const fetchCovers = async () => {
    setLoadingCovers(true)
    try {
      const response = await mangaApi.getMangaCovers(mangaId, { limit: 100 }) // Fetch all covers for now
      setCovers(response.data)
    } catch (error) {
      console.error('Failed to fetch covers:', error)
      handleApiError(error, 'Không thể tải ảnh bìa.')
    } finally {
      setLoadingCovers(false)
    }
  }

  useEffect(() => {
    if (mangaId) {
      fetchCovers()
    }
  }, [mangaId])

  /**
   * Handles upload form submission.
   * @param {UploadCoverArtRequest} data
   */
  const handleUploadSubmit = async (data) => {
    try {
      await mangaApi.uploadMangaCover(mangaId, {
        file: data.file[0], // Access the File object from FileList
        volume: data.volume,
        description: data.description,
      })
      showSuccessToast('Tải ảnh bìa thành công!')
      fetchCovers()
      setOpenUploadDialog(false)
      reset()
    } catch (error) {
      console.error('Failed to upload cover art:', error)
      handleApiError(error, 'Không thể tải ảnh bìa.')
    }
  }

  const handleDeleteRequest = (coverArt) => {
    setCoverArtToDelete(coverArt)
    setOpenConfirmDelete(true)
  }

  const handleConfirmDelete = async () => {
    if (coverArtToDelete) {
      try {
        await coverArtApi.deleteCoverArt(coverArtToDelete.id)
        showSuccessToast('Xóa ảnh bìa thành công!')
        fetchCovers()
      } catch (error) {
        console.error('Failed to delete cover art:', error)
        handleApiError(error, 'Không thể xóa ảnh bìa.')
      } finally {
        setOpenConfirmDelete(false)
        setCoverArtToDelete(null)
      }
    }
  }

  const handleCloseConfirmDelete = () => {
    setOpenConfirmDelete(false)
    setCoverArtToDelete(null)
  }

  return (
    <Box className="cover-art-manager">
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={() => setOpenUploadDialog(true)}
        >
          Tải ảnh bìa mới
        </Button>
      </Box>

      {loadingCovers ? (
        <Box
          sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '200px' }}
        >
          <CircularProgress />
        </Box>
      ) : covers.length === 0 ? (
        <Typography variant="h6" className="no-cover-message">
          Chưa có ảnh bìa nào cho manga này.
        </Typography>
      ) : (
        <Grid container spacing={2} className="cover-art-grid">
          {covers.map((cover) => (
            <Grid item key={cover.id} xs={12} sm={6} md={4} lg={3}>
              <Card className="cover-art-card">
                <CardMedia
                  component="img"
                  image={`${CLOUDINARY_BASE_URL}${cover.attributes.publicId}`}
                  alt={cover.attributes.description || `Cover for volume ${cover.attributes.volume}`}
                />
                <CardContent>
                  <Typography variant="body2" color="text.secondary">
                    Tập: {cover.attributes.volume || 'N/A'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Mô tả: {cover.attributes.description || 'Không có'}
                  </Typography>
                </CardContent>
                <CardActions className="card-actions">
                  <IconButton
                    color="secondary"
                    onClick={() => handleDeleteRequest(cover)}
                    aria-label="delete"
                  >
                    <DeleteIcon />
                  </IconButton>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Upload Dialog */}
      <Dialog open={openUploadDialog} onClose={() => setOpenUploadDialog(false)}>
        <DialogTitle>Tải ảnh bìa mới</DialogTitle>
        <Box component="form" onSubmit={handleSubmit(handleUploadSubmit)} noValidate>
          <DialogContent>
            <TextField
              margin="dense"
              label="Chọn File ảnh"
              type="file"
              fullWidth
              variant="outlined"
              {...register('file')}
              error={!!errors.file}
              helperText={errors.file?.message}
              inputProps={{ accept: 'image/jpeg,image/png,image/webp' }}
            />
            <TextField
              margin="dense"
              label="Volume (Tùy chọn)"
              type="text"
              fullWidth
              variant="outlined"
              {...register('volume')}
              error={!!errors.volume}
              helperText={errors.volume?.message}
            />
            <TextField
              margin="dense"
              label="Mô tả (Tùy chọn)"
              type="text"
              fullWidth
              multiline
              rows={3}
              variant="outlined"
              {...register('description')}
              error={!!errors.description}
              helperText={errors.description?.message}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenUploadDialog(false)} variant="outlined">
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
        title="Xác nhận xóa ảnh bìa"
        message={`Bạn có chắc chắn muốn xóa ảnh bìa này (Volume: ${coverArtToDelete?.attributes?.volume || 'N/A'})?`}
      />
    </Box>
  )
}

export default CoverArtManager 