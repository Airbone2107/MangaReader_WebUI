import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { Box, IconButton, Tooltip } from '@mui/material'
import { useState } from 'react';
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { formatDate } from '../../../utils/dateUtils'

/**
 * @typedef {import('../../../types/manga').Tag} Tag
 */

/**
 * TagTable component to display a list of tags.
 * @param {object} props
 * @param {Tag[]} props.tags - Array of tag data.
 * @param {number} props.totalTags - Total number of tags.
 * @param {number} props.page - Current page number (0-indexed).
 * @param {number} props.rowsPerPage - Number of rows per page.
 * @param {function(React.MouseEvent<HTMLButtonElement> | null, number): void} props.onPageChange - Callback for page change.
 * @param {function(React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void} props.onRowsPerPageChange - Callback for rows per page change.
 * @param {function(string, 'asc' | 'desc'): void} props.onSort - Callback for sorting.
 * @param {string} props.orderBy - Current sort by field.
 * @param {'asc' | 'desc'} props.order - Current sort order.
 * @param {function(string): void} props.onDelete - Callback for deleting a tag.
 * @param {function(string): void} props.onEdit - Callback for editing a tag.
 * @param {boolean} props.isLoading - Loading state.
 */
function TagTable({
  tags,
  totalTags,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onSort,
  orderBy,
  order,
  onDelete,
  onEdit,
  isLoading,
}) {
  const [openConfirm, setOpenConfirm] = useState(false)
  const [tagToDeleteId, setTagToDeleteId] = useState(null)

  const handleDeleteClick = (id) => {
    setTagToDeleteId(id)
    setOpenConfirm(true)
  }

  const handleConfirmDelete = () => {
    onDelete(tagToDeleteId)
    setOpenConfirm(false)
    setTagToDeleteId(null)
  }

  const handleCloseConfirm = () => {
    setOpenConfirm(false)
    setTagToDeleteId(null)
  }

  const columns = [
    { id: 'name', label: 'Tên tag', minWidth: 120, sortable: true },
    { id: 'tagGroupName', label: 'Nhóm tag', minWidth: 120, sortable: true },
    {
      id: 'createdAt',
      label: 'Ngày tạo',
      minWidth: 150,
      sortable: true,
      format: (value) => formatDate(value),
    },
    {
      id: 'updatedAt',
      label: 'Cập nhật cuối',
      minWidth: 150,
      sortable: true,
      format: (value) => formatDate(value),
    },
    {
      id: 'actions',
      label: 'Hành động',
      minWidth: 100,
      align: 'center',
      format: (value, row) => (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
          <Tooltip title="Chỉnh sửa">
            <IconButton color="primary" onClick={() => onEdit(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Xóa">
            <IconButton color="secondary" onClick={() => handleDeleteClick(row.id)}>
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ]

  // Format data for DataTableMUI
  const formatTagDataForTable = (tagsData) => {
    if (!tagsData) return [];
    return tagsData.map(tag => ({
      ...tag.attributes,
      id: tag.id,
      tagGroupName: tag.attributes.tagGroupName, // Ensure tagGroupName is directly accessible
    }));
  };

  return (
    <>
      <DataTableMUI
        columns={columns}
        data={formatTagDataForTable(tags)}
        totalItems={totalTags}
        page={page}
        rowsPerPage={rowsPerPage}
        onPageChange={onPageChange}
        onRowsPerPageChange={onRowsPerPageChange}
        onSort={onSort}
        orderBy={orderBy}
        order={order}
        isLoading={isLoading}
      />
      <ConfirmDialog
        open={openConfirm}
        onClose={handleCloseConfirm}
        onConfirm={handleConfirmDelete}
        title="Xác nhận xóa Tag"
        message={`Bạn có chắc chắn muốn xóa tag "${tagToDeleteId ? tags.find(t => t.id === tagToDeleteId)?.attributes?.name : ''}" này? Thao tác này sẽ xóa mọi liên kết với manga.`}
      />
    </>
  )
}

export default TagTable 