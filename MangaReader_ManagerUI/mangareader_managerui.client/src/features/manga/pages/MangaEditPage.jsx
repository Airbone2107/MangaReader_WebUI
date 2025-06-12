import { Box, CircularProgress, Tab, Tabs, Typography } from '@mui/material'
import React, { useEffect, useState, useCallback } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import mangaApi from '../../../api/mangaApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useMangaStore from '../../../stores/mangaStore'
import { handleApiError } from '../../../utils/errorUtils'
import TranslatedMangaListPage from '../../translatedManga/pages/TranslatedMangaListPage'
import CoverArtManager from '../components/CoverArtManager'
import MangaForm from '../components/MangaForm'
import useUiStore from '../../../stores/uiStore'

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
  const isLoadingPage = useUiStore((state) => state.isLoading)
  const setLoadingPage = useUiStore((state) => state.setLoading)
  const [tabValue, setTabValue] = useState(0)
  
  const fetchMangasStore = useMangaStore((state) => state.fetchMangas)

  useEffect(() => {
    if (location.pathname.includes('/covers')) {
      setTabValue(1);
    } else if (location.pathname.includes('/translations')) {
      setTabValue(2);
    } else {
      setTabValue(0);
    }
  }, [location.pathname]);

  const loadManga = useCallback(async (mangaId) => {
    setLoadingPage(true);
    try {
      const response = await mangaApi.getMangaById(mangaId, { includes: ['author'] })
      setManga(response.data)
    } catch (error) {
      console.error('Failed to fetch manga for editing:', error)
      handleApiError(error, `Không thể tải manga có ID: ${mangaId}.`)
      navigate('/mangas')
    } finally {
      setLoadingPage(false);
    }
  }, [navigate, setLoadingPage]);

  useEffect(() => {
    if (id) {
      loadManga(id)
    }
  }, [id, loadManga])

  /**
   * @param {UpdateMangaRequest} data
   */
  const handleSubmit = async (data) => {
    if (!id) return;
    setLoadingPage(true);
    try {
      await mangaApi.updateManga(id, data)
      showSuccessToast('Cập nhật manga thành công!')
      fetchMangasStore()
      const updatedMangaResponse = await mangaApi.getMangaById(id, { includes: ['author'] });
      if (updatedMangaResponse.data) {
        setManga(updatedMangaResponse.data);
      }
    } catch (error) {
      console.error('Failed to update manga:', error)
      handleApiError(error, 'Không thể cập nhật manga.')
    } finally {
      setLoadingPage(false);
    }
  }

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue)
    if (newValue === 0) {
      navigate(`/mangas/edit/${id}`);
    } else if (newValue === 1) {
      navigate(`/mangas/${id}/covers`);
    } else if (newValue === 2) {
      navigate(`/mangas/${id}/translations`);
    }
  }

  if (isLoadingPage && !manga) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: 'calc(100vh - 128px)',
          p:3
        }}
      >
        <CircularProgress />
      </Box>
    )
  }

  if (!manga && !isLoadingPage) {
    return (
        <Box sx={{p:3}}>
            <Typography variant="h5">Không tìm thấy Manga.</Typography>
        </Box>
    );
  }

  return (
    <Box className="manga-edit-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Manga: {manga?.attributes.title || 'Đang tải...'}
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange} aria-label="Manga tabs">
          <Tab label="Chi tiết Manga" />
          <Tab label="Ảnh bìa" />
          <Tab label="Bản dịch" />
        </Tabs>
      </Box>

      {tabValue === 0 && manga && (
        <Box sx={{ mt: 2 }}>
          <MangaForm initialData={manga} onSubmit={handleSubmit} isEditMode={true} />
        </Box>
      )}
      {tabValue === 1 && id && (
        <Box sx={{ mt: 2 }}>
          <CoverArtManager mangaId={id} />
        </Box>
      )}
      {tabValue === 2 && id && (
        <Box sx={{ mt: 2 }}>
          <TranslatedMangaListPage mangaId={id} />
        </Box>
      )}
    </Box>
  )
}

export default MangaEditPage