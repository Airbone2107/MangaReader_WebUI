import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import authorApi from '../../../api/authorApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useAuthorStore from '../../../stores/authorStore'
import useUiStore from '../../../stores/uiStore'
import { handleApiError } from '../../../utils/errorUtils'
import AuthorForm from '../components/AuthorForm'
import AuthorTable from '../components/AuthorTable'

/**
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').CreateAuthorRequest} CreateAuthorRequest
 * @typedef {import('../../../types/manga').UpdateAuthorRequest} UpdateAuthorRequest
 */

function AuthorListPage() {
  const {
    authors,
    totalAuthors,
    page,
    rowsPerPage,
    filters,
    sort,
    fetchAuthors,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteAuthor,
  } = useAuthorStore()

  const isLoading = useUiStore((state) => state.isLoading)

  // State for filter inputs (controlled components)
  const [localNameFilter, setLocalNameFilter] = useState(filters.nameFilter || '')

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {Author | null} */
  const [editingAuthor, setEditingAuthor] = useState(null)

  useEffect(() => {
    fetchAuthors(true) // Reset pagination on initial load
  }, [fetchAuthors])

  // Sync local filter states with global store filters when global filters change (e.g., after reset)
  useEffect(() => {
    setLocalNameFilter(filters.nameFilter || '')
  }, [filters])

  const handleApplyFilters = () => {
    applyFilters({
      nameFilter: localNameFilter,
    })
  }

  const handleResetFilters = () => {
    setLocalNameFilter('')
    resetFilters()
  }

  const handleCreateNew = () => {
    setEditingAuthor(null)
    setOpenFormDialog(true)
  }

  const handleEdit = async (id) => {
    try {
      const response = await authorApi.getAuthorById(id)
      setEditingAuthor(response.data)
      setOpenFormDialog(true)
    } catch (error) {
      console.error('Failed to fetch author for editing:', error)
      handleApiError(error, `Không thể tải tác giả có ID: ${id}.`)
    }
  }

  /**
   * @param {CreateAuthorRequest | UpdateAuthorRequest} data
   */
  const handleSubmitForm = async (data) => {
    try {
      if (editingAuthor) {
        await authorApi.updateAuthor(editingAuthor.id, /** @type {UpdateAuthorRequest} */ (data))
        showSuccessToast('Cập nhật tác giả thành công!')
      } else {
        await authorApi.createAuthor(/** @type {CreateAuthorRequest} */ (data))
        showSuccessToast('Tạo tác giả thành công!')
      }
      fetchAuthors(true) // Refresh list and reset pagination
      setOpenFormDialog(false)
    } catch (error) {
      console.error('Failed to save author:', error)
      handleApiError(error, 'Không thể lưu tác giả.')
    }
  }

  const handleDelete = (id) => {
    deleteAuthor(id)
  }

  return (
    <Box className="author-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Tác giả
      </Typography>

      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-end">
          <Grid xs={12} sm={6} md={4}>
            <TextField
              label="Lọc theo Tên tác giả"
              variant="outlined"
              fullWidth
              value={localNameFilter}
              onChange={(e) => setLocalNameFilter(e.target.value)}
            />
          </Grid>
          <Grid xs={12} sm={6} md={2}>
            <Button
              variant="contained"
              color="primary"
              startIcon={<SearchIcon />}
              onClick={handleApplyFilters}
              fullWidth
            >
              Áp dụng
            </Button>
          </Grid>
          <Grid xs={12} sm={6} md={2}>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ClearIcon />}
              onClick={handleResetFilters}
              fullWidth
            >
              Đặt lại
            </Button>
          </Grid>
        </Grid>
      </Box>

      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2, mt: 3 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={handleCreateNew}
        >
          Thêm Tác giả mới
        </Button>
      </Box>

      <AuthorTable
        authors={authors}
        totalAuthors={totalAuthors}
        page={page}
        rowsPerPage={rowsPerPage}
        onPageChange={setPage}
        onRowsPerPageChange={setRowsPerPage}
        onSort={setSort}
        orderBy={sort.orderBy}
        order={sort.ascending ? 'asc' : 'desc'}
        onDelete={handleDelete}
        onEdit={handleEdit}
        isLoading={isLoading}
      />

      <Dialog open={openFormDialog} onClose={() => setOpenFormDialog(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editingAuthor ? 'Chỉnh sửa Tác giả' : 'Tạo Tác giả mới'}</DialogTitle>
        <DialogContent>
          <AuthorForm
            initialData={editingAuthor}
            onSubmit={handleSubmitForm}
            isEditMode={!!editingAuthor}
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

export default AuthorListPage 