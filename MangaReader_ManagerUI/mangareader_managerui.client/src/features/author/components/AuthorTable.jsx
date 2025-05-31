import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { Box, IconButton, Tooltip } from '@mui/material'
import React from 'react'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { formatDate } from '../../../utils/dateUtils'

/**
 * @typedef {import('../../../types/manga').Author} Author
 */

/**
 * AuthorTable component to display a list of authors.
 * @param {object} props
 * @param {Author[]} props.authors - Array of author data.
 * @param {number} props.totalAuthors - Total number of authors.
 * @param {number} props.page - Current page number (0-indexed).
 * @param {number} props.rowsPerPage - Number of rows per page.
 * @param {function(React.MouseEvent<HTMLButtonElement> | null, number): void} props.onPageChange - Callback for page change.
 * @param {function(React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void} props.onRowsPerPageChange - Callback for rows per page change.
 * @param {function(string, 'asc' | 'desc'): void} props.onSort - Callback for sorting.
 * @param {string} props.orderBy - Current sort by field.
 * @param {'asc' | 'desc'} props.order - Current sort order.
 * @param {function(string): void} props.onDelete - Callback for deleting an author.
 * @param {function(string): void} props.onEdit - Callback for editing an author.
 * @param {boolean} props.isLoading - Loading state.
 */
function AuthorTable({
  authors,
  totalAuthors,
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
  const [openConfirm, setOpenConfirm] = React.useState(false)
  const [authorToDeleteId, setAuthorToDeleteId] = React.useState(null)

  const handleDeleteClick = (id) => {
    setAuthorToDeleteId(id)
    setOpenConfirm(true)
  }

  const handleConfirmDelete = () => {
    onDelete(authorToDeleteId)
    setOpenConfirm(false)
    setAuthorToDeleteId(null)
  }

  const handleCloseConfirm = () => {
    setOpenConfirm(false)
    setAuthorToDeleteId(null)
  }

  const columns = [
    { id: 'name', label: 'Tên tác giả', minWidth: 170, sortable: true },
    { id: 'biography', label: 'Tiểu sử', minWidth: 200, sortable: false },
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
  const formatAuthorDataForTable = (authorsData) => {
    return authorsData.map(author => ({
      ...author.attributes,
      id: author.id,
    }));
  };

  return (
    <>
      <DataTableMUI
        columns={columns}
        data={formatAuthorDataForTable(authors)}
        totalItems={totalAuthors}
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
        title="Xác nhận xóa Tác giả"
        message={`Bạn có chắc chắn muốn xóa tác giả "${authorToDeleteId ? authors.find(a => a.id === authorToDeleteId)?.attributes?.name : ''}" này? Thao tác này không thể hoàn tác.`}
      />
    </>
  )
}

export default AuthorTable 