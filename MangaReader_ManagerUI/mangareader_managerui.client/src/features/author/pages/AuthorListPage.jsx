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
    filters = {},
    sort = {},
    fetchAuthors,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteAuthor,
    setFilter,
  } = useAuthorStore()

  const isLoading = useUiStore((state) => state.isLoading)

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {Author | null} */
  const [editingAuthor, setEditingAuthor] = useState(null)

  useEffect(() => {
    fetchAuthors(true) // Reset pagination on initial load
  }, [fetchAuthors])

  const handleApplyFilters = () => {
    // Gọi applyFilters với các filter hiện tại trong store.
    // Hành động applyFilters trong store đã được cấu hình để reset page và fetch.
    applyFilters({ nameFilter: filters.nameFilter }); 
    fetchAuthors(true);
  }

  const handleResetFilters = () => {
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
        <Grid container spacing={2} alignItems="flex-start">
          <Grid item xs={12} sm={6} md={4} lg={6}>
            <TextField
              label="Lọc theo Tên tác giả"
              variant="outlined"
              fullWidth
              value={filters.nameFilter || ''}
              onChange={(e) => setFilter('nameFilter', e.target.value)}
              sx={{ minWidth: '200px' }}
            />
          </Grid>
          <Grid item xs={12} sm={3} md={2} lg={3} sx={{ display: 'flex', alignItems: 'center' }}>
            <Button
              variant="contained"
              color="primary"
              startIcon={<SearchIcon />}
              onClick={handleApplyFilters}
              fullWidth
              sx={{ height: '56px' }}
            >
              Áp dụng
            </Button>
          </Grid>
          <Grid item xs={12} sm={3} md={2} lg={3} sx={{ display: 'flex', alignItems: 'center' }}>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ClearIcon />}
              onClick={handleResetFilters}
              fullWidth
              sx={{ height: '56px' }}
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