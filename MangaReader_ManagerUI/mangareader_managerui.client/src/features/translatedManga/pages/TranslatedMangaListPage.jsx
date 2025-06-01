import AddIcon from '@mui/icons-material/Add'
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import translatedMangaApi from '../../../api/translatedMangaApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTranslatedMangaStore from '../../../stores/translatedMangaStore'
import useUiStore from '../../../stores/uiStore'
import { handleApiError } from '../../../utils/errorUtils'
import TranslatedMangaForm from '../components/TranslatedMangaForm'
import TranslatedMangaTable from '../components/TranslatedMangaTable'

/**
 * @typedef {import('../../../types/manga').TranslatedManga} TranslatedManga
 * @typedef {import('../../../types/manga').CreateTranslatedMangaRequest} CreateTranslatedMangaRequest
 * @typedef {import('../../../types/manga').UpdateTranslatedMangaRequest} UpdateTranslatedMangaRequest
 */

/**
 * Page to list and manage translated mangas for a specific original manga.
 * @param {object} props
 * @param {string} props.mangaId - The ID of the original manga.
 */
function TranslatedMangaListPage({ mangaId }) {
  const navigate = useNavigate()
  const {
    translatedMangas,
    totalTranslatedMangas,
    page,
    rowsPerPage,
    sort,
    fetchTranslatedMangasByMangaId,
    setPage,
    setRowsPerPage,
    setSort,
    deleteTranslatedManga,
  } = useTranslatedMangaStore()

  const isLoading = useUiStore((state) => state.isLoading)

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {[TranslatedManga | null, React.Dispatch<React.SetStateAction<TranslatedManga | null>>]} */
  const [editingTranslatedManga, setEditingTranslatedManga] = useState(null)

  useEffect(() => {
    if (mangaId) {
      fetchTranslatedMangasByMangaId(mangaId, true) // reset pagination when mangaId changes
    }
  }, [mangaId, fetchTranslatedMangasByMangaId])

  const handleCreateNew = () => {
    setEditingTranslatedManga(null)
    setOpenFormDialog(true)
  }

  const handleEdit = async (id) => {
    try {
      const response = await translatedMangaApi.getTranslatedMangaById(id)
      setEditingTranslatedManga(response.data)
      setOpenFormDialog(true)
    } catch (error) {
      console.error('Failed to fetch translated manga for editing:', error)
      handleApiError(error, `Không thể tải bản dịch manga có ID: ${id}.`)
    }
  }

  /**
   * @param {CreateTranslatedMangaRequest | UpdateTranslatedMangaRequest} data
   */
  const handleSubmitForm = async (data) => {
    try {
      if (editingTranslatedManga) {
        await translatedMangaApi.updateTranslatedManga(editingTranslatedManga.id, /** @type {UpdateTranslatedMangaRequest} */ (data))
        showSuccessToast('Cập nhật bản dịch manga thành công!')
      } else {
        await translatedMangaApi.createTranslatedManga(/** @type {CreateTranslatedMangaRequest} */ ({ ...data, mangaId }))
        showSuccessToast('Tạo bản dịch manga thành công!')
      }
      fetchTranslatedMangasByMangaId(mangaId, true) // Refresh list
      setOpenFormDialog(false)
    } catch (error) {
      console.error('Failed to save translated manga:', error)
      handleApiError(error, 'Không thể lưu bản dịch manga.')
    }
  }

  const handleDelete = (id) => {
    deleteTranslatedManga(id, mangaId)
  }

  const handleViewChapters = (translatedMangaId) => {
    navigate(`/translatedmangas/${translatedMangaId}/chapters`)
  }

  return (
    <Box sx={{ mt: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Danh sách Bản dịch</Typography>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={handleCreateNew}
        >
          Thêm Bản dịch mới
        </Button>
      </Box>

      <TranslatedMangaTable
        translatedMangas={translatedMangas}
        totalTranslatedMangas={totalTranslatedMangas}
        page={page}
        rowsPerPage={rowsPerPage}
        onPageChange={(e, newPage) => setPage(e, newPage, mangaId)}
        onRowsPerPageChange={(e) => setRowsPerPage(e, mangaId)}
        onSort={(orderBy, order) => setSort(orderBy, order, mangaId)}
        orderBy={sort.orderBy}
        order={sort.ascending ? 'asc' : 'desc'}
        onDelete={handleDelete}
        onEdit={handleEdit}
        onViewChapters={handleViewChapters}
        isLoading={isLoading}
      />

      <Dialog open={openFormDialog} onClose={() => setOpenFormDialog(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editingTranslatedManga ? 'Chỉnh sửa Bản dịch' : 'Tạo Bản dịch mới'}</DialogTitle>
        <DialogContent>
          <TranslatedMangaForm
            mangaId={mangaId}
            initialData={editingTranslatedManga}
            onSubmit={handleSubmitForm}
            isEditMode={!!editingTranslatedManga}
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

export default TranslatedMangaListPage 