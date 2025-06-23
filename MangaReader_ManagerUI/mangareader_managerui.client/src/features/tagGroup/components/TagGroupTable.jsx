import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { Box, IconButton, Tooltip } from '@mui/material'
import { useState } from 'react';
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { formatDate } from '../../../utils/dateUtils'

/**
 * @typedef {import('../../../types/manga').TagGroup} TagGroup
 */

/**
 * TagGroupTable component to display a list of tag groups.
 * @param {object} props
 * @param {TagGroup[]} props.tagGroups - Array of tag group data.
 * @param {number} props.totalTagGroups - Total number of tag groups.
 * @param {number} props.page - Current page number (0-indexed).
 * @param {number} props.rowsPerPage - Number of rows per page.
 * @param {function(React.MouseEvent<HTMLButtonElement> | null, number): void} props.onPageChange - Callback for page change.
 * @param {function(React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void} props.onRowsPerPageChange - Callback for rows per page change.
 * @param {function(string, 'asc' | 'desc'): void} props.onSort - Callback for sorting.
 * @param {string} props.orderBy - Current sort by field.
 * @param {'asc' | 'desc'} props.order - Current sort order.
 * @param {function(string): void} props.onDelete - Callback for deleting a tag group.
 * @param {function(string): void} props.onEdit - Callback for editing a tag group.
 * @param {boolean} props.isLoading - Loading state.
 */
function TagGroupTable({
  tagGroups,
  totalTagGroups,
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
  const [tagGroupToDeleteId, setTagGroupToDeleteId] = useState(null)

  const handleDeleteClick = (id) => {
    setTagGroupToDeleteId(id)
    setOpenConfirm(true)
  }

  const handleConfirmDelete = () => {
    onDelete(tagGroupToDeleteId)
    setOpenConfirm(false)
    setTagGroupToDeleteId(null)
  }

  const handleCloseConfirm = () => {
    setOpenConfirm(false)
    setTagGroupToDeleteId(null)
  }

  const columns = [
    { id: 'name', label: 'Tên nhóm tag', minWidth: 170, sortable: true },
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
  const formatTagGroupDataForTable = (tagGroupsData) => {
    if (!tagGroupsData) return [];
    return tagGroupsData.map(tagGroup => ({
      ...tagGroup.attributes,
      id: tagGroup.id,
    }));
  };

  return (
    <>
      <DataTableMUI
        columns={columns}
        data={formatTagGroupDataForTable(tagGroups)}
        totalItems={totalTagGroups}
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
        title="Xác nhận xóa Nhóm tag"
        message={`Bạn có chắc chắn muốn xóa nhóm tag "${tagGroupToDeleteId ? tagGroups.find(tg => tg.id === tagGroupToDeleteId)?.attributes?.name : ''}" này? Nếu nhóm tag còn chứa các tag, bạn sẽ không thể xóa nó.`}
      />
    </>
  )
}

export default TagGroupTable 