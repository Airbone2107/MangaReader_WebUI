import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import tagGroupApi from '../../../api/tagGroupApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTagGroupStore from '../../../stores/tagGroupStore'
import useUiStore from '../../../stores/uiStore'
import { handleApiError } from '../../../utils/errorUtils'
import TagGroupForm from '../components/TagGroupForm'
import TagGroupTable from '../components/TagGroupTable'

/**
 * @typedef {import('../../../types/manga').TagGroup} TagGroup
 * @typedef {import('../../../types/manga').CreateTagGroupRequest} CreateTagGroupRequest
 * @typedef {import('../../../types/manga').UpdateTagGroupRequest} UpdateTagGroupRequest
 */

function TagGroupListPage() {
  const {
    tagGroups,
    totalTagGroups,
    page,
    rowsPerPage,
    filters = {},
    sort = {},
    fetchTagGroups,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteTagGroup,
    setFilter,
  } = useTagGroupStore()

  const isLoading = useUiStore((state) => state.isLoading)

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {TagGroup | null} */
  const [editingTagGroup, setEditingTagGroup] = useState(null)

  useEffect(() => {
    fetchTagGroups(true) // Reset pagination on initial load
  }, [fetchTagGroups])

  const handleApplyFilters = () => {
    // Gọi applyFilters với các filter hiện tại trong store.
    // Hành động applyFilters trong store đã được cấu hình để reset page và fetch.
    applyFilters({ nameFilter: filters.nameFilter });
    fetchTagGroups(true);
  }

  const handleResetFilters = () => {
    resetFilters()
  }

  const handleCreateNew = () => {
    setEditingTagGroup(null)
    setOpenFormDialog(true)
  }

  const handleEdit = async (id) => {
    try {
      const response = await tagGroupApi.getTagGroupById(id)
      setEditingTagGroup(response.data)
      setOpenFormDialog(true)
    } catch (error) {
      console.error('Failed to fetch tag group for editing:', error)
      handleApiError(error, `Không thể tải nhóm tag có ID: ${id}.`)
    }
  }

  /**
   * @param {CreateTagGroupRequest | UpdateTagGroupRequest} data
   */
  const handleSubmitForm = async (data) => {
    try {
      if (editingTagGroup) {
        await tagGroupApi.updateTagGroup(editingTagGroup.id, /** @type {UpdateTagGroupRequest} */ (data))
        showSuccessToast('Cập nhật nhóm tag thành công!')
      } else {
        await tagGroupApi.createTagGroup(/** @type {CreateTagGroupRequest} */ (data))
        showSuccessToast('Tạo nhóm tag thành công!')
      }
      fetchTagGroups(true) // Refresh list and reset pagination
      setOpenFormDialog(false)
    } catch (error) {
      console.error('Failed to save tag group:', error)
      handleApiError(error, 'Không thể lưu nhóm tag.')
    }
  }

  const handleDelete = (id) => {
    deleteTagGroup(id)
  }

  return (
    <Box className="tag-group-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Nhóm tag
      </Typography>
      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-start">
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4,
              lg: 6
            }}>
            <TextField
              label="Lọc theo Tên nhóm tag"
              variant="outlined"
              fullWidth
              value={filters.nameFilter || ''}
              onChange={(e) => setFilter('nameFilter', e.target.value)}
              sx={{ minWidth: '200px' }}
            />
          </Grid>
          <Grid
            sx={{ display: 'flex', alignItems: 'center' }}
            size={{
              xs: 12,
              sm: 3,
              md: 2,
              lg: 3
            }}>
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
          <Grid
            sx={{ display: 'flex', alignItems: 'center' }}
            size={{
              xs: 12,
              sm: 3,
              md: 2,
              lg: 3
            }}>
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
          Thêm Nhóm tag mới
        </Button>
      </Box>
      <TagGroupTable
        tagGroups={tagGroups}
        totalTagGroups={totalTagGroups}
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
        <DialogTitle>{editingTagGroup ? 'Chỉnh sửa Nhóm tag' : 'Tạo Nhóm tag mới'}</DialogTitle>
        <DialogContent>
          <TagGroupForm
            initialData={editingTagGroup}
            onSubmit={handleSubmitForm}
            isEditMode={!!editingTagGroup}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenFormDialog(false)} color="primary" variant="outlined">
            Đóng
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default TagGroupListPage 