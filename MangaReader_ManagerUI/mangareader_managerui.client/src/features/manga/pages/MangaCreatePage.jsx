import { Box, Typography } from '@mui/material'
import React from 'react'
import { useNavigate } from 'react-router-dom'
import mangaApi from '../../../api/mangaApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useMangaStore from '../../../stores/mangaStore'
import { handleApiError } from '../../../utils/errorUtils'
import MangaForm from '../components/MangaForm'

/**
 * @typedef {import('../../../types/manga').CreateMangaRequest} CreateMangaRequest
 */

function MangaCreatePage() {
  const navigate = useNavigate()
  const fetchMangas = useMangaStore((state) => state.fetchMangas)

  /**
   * @param {CreateMangaRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await mangaApi.createManga(data)
      showSuccessToast('Tạo manga thành công!')
      fetchMangas(true) // Refresh manga list and reset pagination
      navigate('/mangas') // Navigate back to list page
    } catch (error) {
      console.error('Failed to create manga:', error)
      handleApiError(error, 'Không thể tạo manga.')
    }
  }

  return (
    <Box className="manga-create-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Tạo Manga mới
      </Typography>
      <MangaForm onSubmit={handleSubmit} isEditMode={false} />
    </Box>
  )
}

export default MangaCreatePage 