import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'; // For Chapter Pages
import { Box, IconButton, Tooltip } from '@mui/material'
import React from 'react'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { formatDate } from '../../../utils/dateUtils'

/**
 * @typedef {import('../../../types/manga').Chapter} Chapter
 */

/**
 * ChapterTable component to display a list of chapters.
 * @param {object} props
 * @param {Chapter[]} props.chapters - Array of chapter data.
 * @param {number} props.totalChapters - Total number of chapters.
 * @param {number} props.page - Current page number (0-indexed).
 * @param {number} props.rowsPerPage - Number of rows per page.
 * @param {function(React.MouseEvent<HTMLButtonElement> | null, number): void} props.onPageChange - Callback for page change.
 * @param {function(React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void} props.onRowsPerPageChange - Callback for rows per page change.
 * @param {function(string, 'asc' | 'desc'): void} props.onSort - Callback for sorting.
 * @param {string} props.orderBy - Current sort by field.
 * @param {'asc' | 'desc'} props.order - Current sort order.
 * @param {function(string): void} props.onDelete - Callback for deleting a chapter.
 * @param {function(string): void} props.onEdit - Callback for editing a chapter.
 * @param {function(string): void} props.onViewPages - Callback for viewing pages of a chapter.
 * @param {boolean} props.isLoading - Loading state.
 */
function ChapterTable({
  chapters,
  totalChapters,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onSort,
  orderBy,
  order,
  onDelete,
  onEdit,
  onViewPages,
  isLoading,
}) {
  const [openConfirm, setOpenConfirm] = React.useState(false)
  const [chapterToDeleteId, setChapterToDeleteId] = React.useState(null)

  const handleDeleteClick = (id) => {
    setChapterToDeleteId(id)
    setOpenConfirm(true)
  }

  const handleConfirmDelete = () => {
    onDelete(chapterToDeleteId)
    setOpenConfirm(false)
    setChapterToDeleteId(null)
  }

  const handleCloseConfirm = () => {
    setOpenConfirm(false)
    setChapterToDeleteId(null)
  }

  const columns = [
    { id: 'volume', label: 'Volume', minWidth: 80, sortable: true },
    { id: 'chapterNumber', label: 'Chương', minWidth: 80, sortable: true },
    { id: 'title', label: 'Tiêu đề chương', minWidth: 150, sortable: true },
    { id: 'pagesCount', label: 'Số trang', minWidth: 80, sortable: false, align: 'center' },
    {
      id: 'publishAt',
      label: 'Xuất bản lúc',
      minWidth: 150,
      sortable: true,
      format: (value) => formatDate(value),
    },
    {
      id: 'readableAt',
      label: 'Đọc được lúc',
      minWidth: 150,
      sortable: true,
      format: (value) => formatDate(value),
    },
    {
      id: 'actions',
      label: 'Hành động',
      minWidth: 150,
      align: 'center',
      format: (value, row) => (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
          <Tooltip title="Chỉnh sửa Chương">
            <IconButton color="primary" onClick={() => onEdit(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Quản lý Trang">
            <IconButton color="default" onClick={() => onViewPages(row.id)}>
              <ImageOutlinedIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Xóa Chương">
            <IconButton color="secondary" onClick={() => handleDeleteClick(row.id)}>
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ]

  // Format data for DataTableMUI
  const formatChapterDataForTable = (chaptersData) => {
    return chaptersData.map(chapter => ({
      ...chapter.attributes,
      id: chapter.id,
    }));
  };

  return (
    <>
      <DataTableMUI
        columns={columns}
        data={formatChapterDataForTable(chapters)}
        totalItems={totalChapters}
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
        title="Xác nhận xóa Chương"
        message={`Bạn có chắc chắn muốn xóa chương ${chapterToDeleteId ? chapters.find(c => c.id === chapterToDeleteId)?.attributes?.chapterNumber : ''} này? Thao tác này không thể hoàn tác và sẽ xóa tất cả các trang liên quan.`}
      />
    </>
  )
}

export default ChapterTable 