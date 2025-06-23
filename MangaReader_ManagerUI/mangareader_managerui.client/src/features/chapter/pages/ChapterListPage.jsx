import { useEffect, useState } from 'react';
import { Box, Typography, Button, Paper, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { useParams, useNavigate } from 'react-router-dom'
import ChapterTable from '../components/ChapterTable'
import ChapterForm from '../components/ChapterForm'
import useChapterStore from '../../../stores/chapterStore'
import useUiStore from '../../../stores/uiStore'
import chapterApi from '../../../api/chapterApi'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Chapter} Chapter
 * @typedef {import('../../../types/manga').CreateChapterRequest} CreateChapterRequest
 * @typedef {import('../../../types/manga').UpdateChapterRequest} UpdateChapterRequest
 */

function ChapterListPage() {
  const { translatedMangaId } = useParams() // ID of the translated manga
  const navigate = useNavigate()
  const {
    chapters,
    totalChapters,
    page,
    rowsPerPage,
    sort,
    fetchChaptersByTranslatedMangaId,
    setPage,
    setRowsPerPage,
    setSort,
    deleteChapter,
  } = useChapterStore()

  const isLoading = useUiStore((state) => state.isLoading)

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {Chapter | null} */
  const [editingChapter, setEditingChapter] = useState(null)

  useEffect(() => {
    if (translatedMangaId) {
      fetchChaptersByTranslatedMangaId(translatedMangaId, true) // reset pagination when translatedMangaId changes
    }
  }, [translatedMangaId, fetchChaptersByTranslatedMangaId])

  const handleCreateNew = () => {
    setEditingChapter(null)
    setOpenFormDialog(true)
  }

  // Not used directly in this page, but useful for opening edit form if needed
  // const handleEdit = async (id) => {
  //   try {
  //     const response = await chapterApi.getChapterById(id)
  //     setEditingChapter(response.data)
  //     setOpenFormDialog(true)
  //   } catch (error) {
  //     console.error('Failed to fetch chapter for editing:', error)
  //     handleApiError(error, `Không thể tải chương có ID: ${id}.`)
  //   }
  // }

  /**
   * @param {CreateChapterRequest | UpdateChapterRequest} data
   */
  const handleSubmitForm = async (data) => {
    try {
      if (editingChapter) {
        // This path is currently not taken from this page, but from ChapterEditPage
        await chapterApi.updateChapter(editingChapter.id, /** @type {UpdateChapterRequest} */ (data))
        showSuccessToast('Cập nhật chương thành công!')
      } else {
        await chapterApi.createChapter(/** @type {CreateChapterRequest} */ ({ ...data, translatedMangaId }))
        showSuccessToast('Tạo chương thành công!')
      }
      fetchChaptersByTranslatedMangaId(translatedMangaId, true) // Refresh list
      setOpenFormDialog(false)
    } catch (error) {
      console.error('Failed to save chapter:', error)
      handleApiError(error, 'Không thể lưu chương.')
    }
  }

  const handleDelete = (id) => {
    deleteChapter(id, translatedMangaId)
  }

  const handleEditChapter = (id) => {
    // Navigate to ChapterEditPage, passing translatedMangaId via state for back navigation
    navigate(`/chapters/edit/${id}`, { state: { translatedMangaId: translatedMangaId } })
  }

  const handleViewPages = (id) => {
    // Navigate to ChapterEditPage, opening the Chapter Pages tab directly
    navigate(`/chapters/${id}/pages`, { state: { translatedMangaId: translatedMangaId } })
  }

  return (
    <Box className="chapter-list-page" sx={{ p: 3 }}>
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Chương cho Bản dịch: {translatedMangaId}
      </Typography>

      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={handleCreateNew}
        >
          Thêm Chương mới
        </Button>
      </Box>

      <ChapterTable
        chapters={chapters}
        totalChapters={totalChapters}
        page={page}
        rowsPerPage={rowsPerPage}
        onPageChange={(e, newPage) => setPage(e, newPage, translatedMangaId)}
        onRowsPerPageChange={(e) => setRowsPerPage(e, translatedMangaId)}
        onSort={(orderBy, order) => setSort(orderBy, order, translatedMangaId)}
        orderBy={sort.orderBy}
        order={sort.ascending ? 'asc' : 'desc'}
        onDelete={handleDelete}
        onEdit={handleEditChapter} // Pass a function that navigates to edit page
        onViewPages={handleViewPages}
        isLoading={isLoading}
      />

      <Dialog open={openFormDialog} onClose={() => setOpenFormDialog(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editingChapter ? 'Chỉnh sửa Chương' : 'Tạo Chương mới'}</DialogTitle>
        <DialogContent>
          <ChapterForm
            translatedMangaId={translatedMangaId}
            initialData={editingChapter}
            onSubmit={handleSubmitForm}
            isEditMode={!!editingChapter}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenFormDialog(false)} color="primary" variant="outlined">
            Đóng
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default ChapterListPage 