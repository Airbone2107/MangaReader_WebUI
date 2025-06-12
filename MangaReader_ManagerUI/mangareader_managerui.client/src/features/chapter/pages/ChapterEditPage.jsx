import React, { useEffect, useState, useCallback } from 'react'
import { Box, Typography, CircularProgress, Tabs, Tab } from '@mui/material'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import ChapterForm from '../components/ChapterForm'
import ChapterPageManager from '../components/ChapterPageManager'
import { showSuccessToast } from '../../../components/common/Notification'
import chapterApi from '../../../api/chapterApi'
import useChapterStore from '../../../stores/chapterStore'
import { RELATIONSHIP_TYPES } from '../../../constants/appConstants'
import { handleApiError } from '../../../utils/errorUtils'
import useUiStore from '../../../stores/uiStore'

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
  const isLoadingPage = useUiStore((state) => state.isLoading);
  const setLoadingPage = useUiStore((state) => state.setLoading);

  const [tabValue, setTabValue] = useState(0)

  const fetchChaptersByTranslatedMangaIdStore = useChapterStore((state) => state.fetchChaptersByTranslatedMangaId)

  useEffect(() => {
    if (location.pathname.includes('/pages')) {
      setTabValue(1);
    } else {
      setTabValue(0);
    }
  }, [location.pathname]);

  const loadChapter = useCallback(async (chapterIdParam) => {
    setLoadingPage(true);
    try {
      const response = await chapterApi.getChapterById(chapterIdParam)
      setChapter(response.data)
    } catch (error) {
      console.error('Failed to fetch chapter for editing:', error)
      handleApiError(error, `Không thể tải chương có ID: ${chapterIdParam}.`)
      const prevTranslatedMangaId = location.state?.translatedMangaId || '';
      if (prevTranslatedMangaId) {
          navigate(`/translatedmangas/${prevTranslatedMangaId}/chapters`);
      } else {
          navigate('/mangas'); 
      }
    } finally {
      setLoadingPage(false);
    }
  }, [navigate, location.state?.translatedMangaId, setLoadingPage]);

  useEffect(() => {
    if (id) { 
        loadChapter(id)
    } else {
        setLoadingPage(false);
        handleApiError(null, 'ID chương không hợp lệ.');
        navigate('/mangas');
    }
  }, [id, loadChapter, setLoadingPage, navigate])

  /**
   * @param {UpdateChapterRequest} data
   */
  const handleSubmitDetails = async (data) => {
    if (!id) return;
    setLoadingPage(true);
    try {
      await chapterApi.updateChapter(id, data)
      showSuccessToast('Cập nhật chương thành công!')
      setChapter((prev) => prev ? { ...prev, attributes: { ...prev.attributes, ...data } } : null);
      
      const translatedMangaRel = chapter?.relationships?.find(rel => rel.type === RELATIONSHIP_TYPES.TRANSLATED_MANGA);
      const parentTranslatedMangaId = translatedMangaRel?.id || location.state?.translatedMangaId;
      if (parentTranslatedMangaId) {
        fetchChaptersByTranslatedMangaIdStore(parentTranslatedMangaId, false);
      }
    } catch (error) {
      console.error('Failed to update chapter:', error)
      handleApiError(error, 'Không thể cập nhật chương.')
    } finally {
      setLoadingPage(false);
    }
  }

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue)
    const parentTranslatedMangaId = chapter?.relationships?.find(rel => rel.type === RELATIONSHIP_TYPES.TRANSLATED_MANGA)?.id || location.state?.translatedMangaId;
    if (newValue === 0) {
      navigate(`/chapters/edit/${id}`, { state: { translatedMangaId: parentTranslatedMangaId } });
    } else if (newValue === 1) {
      navigate(`/chapters/${id}/pages`, { state: { translatedMangaId: parentTranslatedMangaId } });
    }
  }

  const handlePagesUpdatedInManager = useCallback(async () => {
    if (!id) return;
    setLoadingPage(true);
    try {
      const response = await chapterApi.getChapterById(id);
      if (response && response.data) {
        setChapter(response.data); 
      }
    } catch (error) {
      console.error('Failed to reload chapter details after pages update:', error);
    } finally {
      setLoadingPage(false);
    }
  }, [id, setLoadingPage]);

  if (isLoadingPage && !chapter) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 'calc(100vh - 128px)', p:3 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (!chapter && !isLoadingPage) {
    return (
        <Box sx={{p:3}}>
            <Typography variant="h5">Không tìm thấy Chương hoặc có lỗi khi tải.</Typography>
        </Box>
    );
  }

  return (
    <Box className="chapter-edit-page" sx={{ p: { xs: 1, sm: 2, md: 3 } }}>
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Chương: {chapter?.attributes.chapterNumber || '?'} - {chapter?.attributes.title || 'Không có tiêu đề'} 
        (Trang: {chapter?.attributes.pagesCount || 0})
      </Typography>
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange} aria-label="Chapter tabs">
          <Tab label="Chi tiết Chương" />
          <Tab label="Quản lý Trang Ảnh" />
        </Tabs>
      </Box>
      {tabValue === 0 && chapter && (
        <Box sx={{ mt: 2 }}>
          <ChapterForm initialData={chapter} onSubmit={handleSubmitDetails} isEditMode={true} translatedMangaId={chapter.relationships?.find(r => r.type === 'translated_manga')?.id || ''} />
        </Box>
      )}
      {tabValue === 1 && id && chapter && ( 
        (<Box sx={{ mt: 2 }}>
          <ChapterPageManager chapterId={id} onPagesUpdated={handlePagesUpdatedInManager} />
        </Box>)
      )}
    </Box>
  );
}

export default ChapterEditPage 