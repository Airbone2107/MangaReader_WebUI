import React, { useEffect, useState } from 'react'
import { Box, Typography, CircularProgress, Tabs, Tab } from '@mui/material'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import MangaForm from '../components/MangaForm'
import CoverArtManager from '../components/CoverArtManager'
import useMangaStore from '../../../stores/mangaStore'
import { showSuccessToast } from '../../../components/common/Notification'
import mangaApi from '../../../api/mangaApi'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Manga} Manga
 * @typedef {import('../../../types/manga').UpdateMangaRequest} UpdateMangaRequest
 */

function MangaEditPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  
  /** @type {[Manga | null, React.Dispatch<React.SetStateAction<Manga | null>>]} */
  const [manga, setManga] = useState(null)
  const [loading, setLoading] = useState(true)
  const [tabValue, setTabValue] = useState(0) // State for tabs: 0 for Details, 1 for Cover Art
  
  const fetchMangas = useMangaStore((state) => state.fetchMangas)

  // Determine which tab to show based on URL
  useEffect(() => {
    if (location.pathname.includes('/covers')) {
      setTabValue(1);
    } else {
      setTabValue(0);
    }
  }, [location.pathname]);

  useEffect(() => {
    const loadManga = async () => {
      setLoading(true)
      try {
        const response = await mangaApi.getMangaById(id)
        setManga(response.data)
      } catch (error) {
        console.error('Failed to fetch manga for editing:', error)
        handleApiError(error, `Không thể tải manga có ID: ${id}.`)
        navigate('/mangas') // Redirect if manga not found or error
      } finally {
        setLoading(false)
      }
    }
    loadManga()
  }, [id, navigate])

  /**
   * @param {UpdateMangaRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await mangaApi.updateManga(id, data)
      showSuccessToast('Cập nhật manga thành công!')
      fetchMangas() // Refresh manga list (no need to reset pagination)
      // Optionally navigate back or stay on the page
      // navigate('/mangas');
    } catch (error) {
      console.error('Failed to update manga:', error)
      handleApiError(error, 'Không thể cập nhật manga.')
    }
  }

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue)
    if (newValue === 0) {
      navigate(`/mangas/edit/${id}`);
    } else if (newValue === 1) {
      navigate(`/mangas/${id}/covers`);
    }
  }

  if (loading) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100%',
        }}
      >
        <CircularProgress />
      </Box>
    )
  }

  if (!manga) {
    return <Typography variant="h5">Không tìm thấy Manga.</Typography>
  }

  return (
    <Box className="manga-edit-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Manga: {manga.attributes.title}
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange} aria-label="Manga tabs">
          <Tab label="Chi tiết Manga" />
          <Tab label="Ảnh bìa" />
          {/* <Tab label="Bản dịch" /> */}
          {/* <Tab label="Chương" /> */}
        </Tabs>
      </Box>

      {tabValue === 0 && (
        <Box sx={{ mt: 2 }}>
          <MangaForm initialData={manga} onSubmit={handleSubmit} isEditMode={true} />
        </Box>
      )}
      {tabValue === 1 && (
        <Box sx={{ mt: 2 }}>
          <CoverArtManager mangaId={id} />
        </Box>
      )}
      {/* Add other tabs content here later */}
    </Box>
  )
}

export default MangaEditPage 