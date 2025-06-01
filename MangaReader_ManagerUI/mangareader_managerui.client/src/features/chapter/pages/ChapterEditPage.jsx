import React, { useEffect, useState } from 'react'
import { Box, Typography, CircularProgress, Tabs, Tab } from '@mui/material'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import ChapterForm from '../components/ChapterForm'
import ChapterPageManager from '../components/ChapterPageManager'
import { showSuccessToast } from '../../../components/common/Notification'
import chapterApi from '../../../api/chapterApi'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Chapter} Chapter
 * @typedef {import('../../../types/manga').UpdateChapterRequest} UpdateChapterRequest
 */

function ChapterEditPage() {
  const { id } = useParams() // chapterId
  const navigate = useNavigate()
  const location = useLocation()
  
  /** @type {[Chapter | null, React.Dispatch<React.SetStateAction<Chapter | null>>]} */
  const [chapter, setChapter] = useState(null)
  const [loading, setLoading] = useState(true)
  const [tabValue, setTabValue] = useState(0) // State for tabs: 0 for Details, 1 for Pages

  // Determine which tab to show based on URL
  useEffect(() => {
    if (location.pathname.includes('/pages')) {
      setTabValue(1);
    } else {
      setTabValue(0);
    }
  }, [location.pathname]);

  useEffect(() => {
    const loadChapter = async () => {
      setLoading(true)
      try {
        const response = await chapterApi.getChapterById(id)
        setChapter(response.data)
      } catch (error) {
        console.error('Failed to fetch chapter for editing:', error)
        handleApiError(error, `Không thể tải chương có ID: ${id}.`)
        // Assuming we navigate back to the translated manga's chapters list
        const prevTranslatedMangaId = location.state?.translatedMangaId || '';
        if (prevTranslatedMangaId) {
            navigate(`/translatedmangas/${prevTranslatedMangaId}/chapters`);
        } else {
            navigate('/mangas'); // Fallback to mangas list
        }
      } finally {
        setLoading(false)
      }
    }
    loadChapter()
  }, [id, navigate, location.state?.translatedMangaId])

  /**
   * @param {UpdateChapterRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await chapterApi.updateChapter(id, data)
      showSuccessToast('Cập nhật chương thành công!')
      // Re-fetch the parent list if needed (e.g., if sorting/filtering is based on these fields)
      // For now, we just update the specific chapter data in state
      setChapter((prev) => prev ? { ...prev, attributes: { ...prev.attributes, ...data } } : null);
      // Fetch the parent list to update pagesCount in the list table if needed.
      // Requires translatedMangaId, which is not directly available here after navigation.
      // This is a trade-off for simplicity. A more complex state might store it.
    } catch (error) {
      console.error('Failed to update chapter:', error)
      handleApiError(error, 'Không thể cập nhật chương.')
    }
  }

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue)
    if (newValue === 0) {
      navigate(`/chapters/edit/${id}`, { state: { translatedMangaId: location.state?.translatedMangaId } });
    } else if (newValue === 1) {
      navigate(`/chapters/${id}/pages`, { state: { translatedMangaId: location.state?.translatedMangaId } });
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

  if (!chapter) {
    return <Typography variant="h5">Không tìm thấy Chương.</Typography>
  }

  return (
    <Box className="chapter-edit-page" sx={{ p: 3 }}>
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Chương: {chapter.attributes.chapterNumber} - {chapter.attributes.title || 'Không có tiêu đề'}
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange} aria-label="Chapter tabs">
          <Tab label="Chi tiết Chương" />
          <Tab label="Trang Chương" />
        </Tabs>
      </Box>

      {tabValue === 0 && (
        <Box sx={{ mt: 2 }}>
          <ChapterForm initialData={chapter} onSubmit={handleSubmit} isEditMode={true} />
        </Box>
      )}
      {tabValue === 1 && (
        <Box sx={{ mt: 2 }}>
          <ChapterPageManager chapterId={id} />
        </Box>
      )}
    </Box>
  )
}

export default ChapterEditPage 