import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, MenuItem, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import tagApi from '../../../api/tagApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTagGroupStore from '../../../stores/tagGroupStore';
import useTagStore from '../../../stores/tagStore'
import useUiStore from '../../../stores/uiStore'
import { handleApiError } from '../../../utils/errorUtils'
import TagForm from '../components/TagForm'
import TagTable from '../components/TagTable'

/**
 * @typedef {import('../../../types/manga').Tag} Tag
 * @typedef {import('../../../types/manga').TagGroup} TagGroup
 * @typedef {import('../../../types/manga').CreateTagRequest} CreateTagRequest
 * @typedef {import('../../../types/manga').UpdateTagRequest} UpdateTagRequest
 */

function TagListPage() {
  const {
    tags,
    totalTags,
    page,
    rowsPerPage,
    filters = {},
    sort = {},
    fetchTags,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteTag,
    setFilter,
  } = useTagStore()

  const { tagGroups, fetchTagGroups } = useTagGroupStore() // For tag group filter dropdown

  const isLoading = useUiStore((state) => state.isLoading)

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {Tag | null} */
  const [editingTag, setEditingTag] = useState(null)

  useEffect(() => {
    fetchTags(true) // Reset pagination on initial load
    fetchTagGroups(true) // Fetch all tag groups for filter dropdown
  }, [fetchTags, fetchTagGroups])

  const handleApplyFilters = () => {
    // Gọi applyFilters với các filter hiện tại trong store.
    // Hành động applyFilters trong store đã được cấu hình để reset page và fetch.
    applyFilters({
      nameFilter: filters.nameFilter,
      tagGroupId: filters.tagGroupId,
    });
    fetchTags(true);
  }

  const handleResetFilters = () => {
    resetFilters()
  }

  const handleCreateNew = () => {
    setEditingTag(null)
    setOpenFormDialog(true)
  }

  const handleEdit = async (id) => {
    try {
      const response = await tagApi.getTagById(id)
      setEditingTag(response.data)
      setOpenFormDialog(true)
    } catch (error) {
      console.error('Failed to fetch tag for editing:', error)
      handleApiError(error, `Không thể tải tag có ID: ${id}.`)
    }
  }

  /**
   * @param {CreateTagRequest | UpdateTagRequest} data
   */
  const handleSubmitForm = async (data) => {
    try {
      if (editingTag) {
        await tagApi.updateTag(editingTag.id, /** @type {UpdateTagRequest} */ (data))
        showSuccessToast('Cập nhật tag thành công!')
      } else {
        await tagApi.createTag(/** @type {CreateTagRequest} */ (data))
        showSuccessToast('Tạo tag thành công!')
      }
      fetchTags(true) // Refresh list and reset pagination
      setOpenFormDialog(false)
    } catch (error) {
      console.error('Failed to save tag:', error)
      handleApiError(error, 'Không thể lưu tag.')
    }
  }

  const handleDelete = (id) => {
    deleteTag(id)
  }

  const tagGroupOptions = tagGroups.map((group) => ({
    value: group.id,
    label: group.attributes.name,
  }))

  return (
    <Box className="tag-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Tags
      </Typography>

      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-end" columns={{ xs: 4, sm: 6, md: 12 }}>
          <Grid item xs={4} sm={3} md={4}>
            <TextField
              label="Lọc theo Tên tag"
              variant="outlined"
              fullWidth
              value={filters.nameFilter || ''}
              onChange={(e) => setFilter('nameFilter', e.target.value)}
            />
          </Grid>
          <Grid item xs={4} sm={3} md={4}>
            <TextField
              select
              label="Nhóm tag"
              variant="outlined"
              fullWidth
              value={filters.tagGroupId || ''}
              onChange={(e) => setFilter('tagGroupId', e.target.value === '' ? undefined : e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {tagGroupOptions.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={4} sm={3} md={2}>
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
          <Grid item xs={4} sm={3} md={2}>
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
          Thêm Tag mới
        </Button>
      </Box>

      <TagTable
        tags={tags}
        totalTags={totalTags}
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
        <DialogTitle>{editingTag ? 'Chỉnh sửa Tag' : 'Tạo Tag mới'}</DialogTitle>
        <DialogContent>
          <TagForm
            initialData={editingTag}
            onSubmit={handleSubmitForm}
            isEditMode={!!editingTag}
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

export default TagListPage 