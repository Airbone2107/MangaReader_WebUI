import React from 'react'
import { Box, IconButton, Tooltip, Chip } from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import TranslateIcon from '@mui/icons-material/Translate'
import DataTableMUI from '../../../components/common/DataTableMUI'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import { formatDate } from '../../../utils/dateUtils'
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants'

/**
 * @typedef {import('../../../types/manga').Manga} Manga
 */

/**
 * MangaTable component to display a list of mangas.
 * @param {object} props
 * @param {Manga[]} props.mangas - Array of manga data.
 * @param {number} props.totalMangas - Total number of mangas.
 * @param {number} props.page - Current page number (0-indexed).
 * @param {number} props.rowsPerPage - Number of rows per page.
 * @param {function(React.MouseEvent<HTMLButtonElement> | null, number): void} props.onPageChange - Callback for page change.
 * @param {function(React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void} props.onRowsPerPageChange - Callback for rows per page change.
 * @param {function(string, 'asc' | 'desc'): void} props.onSort - Callback for sorting.
 * @param {string} props.orderBy - Current sort by field.
 * @param {'asc' | 'desc'} props.order - Current sort order.
 * @param {function(string): void} props.onDelete - Callback for deleting a manga.
 * @param {function(string): void} props.onEdit - Callback for editing a manga.
 * @param {function(string): void} props.onViewCovers - Callback for viewing covers of a manga.
 * @param {function(string): void} props.onViewTranslations - Callback for viewing translations of a manga.
 * @param {boolean} props.isLoading - Loading state.
 */
function MangaTable({
  mangas,
  totalMangas,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onSort,
  orderBy,
  order,
  onDelete,
  onEdit,
  onViewCovers,
  onViewTranslations,
  isLoading,
}) {
  const [openConfirm, setOpenConfirm] = React.useState(false)
  const [mangaToDeleteId, setMangaToDeleteId] = React.useState(null)

  const handleDeleteClick = (id) => {
    setMangaToDeleteId(id)
    setOpenConfirm(true)
  }

  const handleConfirmDelete = () => {
    onDelete(mangaToDeleteId)
    setOpenConfirm(false)
    setMangaToDeleteId(null)
  }

  const handleCloseConfirm = () => {
    setOpenConfirm(false)
    setMangaToDeleteId(null)
  }

  const columns = [
    {
      id: 'title',
      label: 'Tiêu đề',
      minWidth: 180,
      sortable: true,
      format: (value, row) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {row.relationships?.find(r => r.type === 'cover_art') && (
            <img
              src={`${CLOUDINARY_BASE_URL}w_40,h_60,c_fill/${row.relationships.find(r => r.type === 'cover_art').id}.jpg`}
              alt="Cover"
              style={{ width: 40, height: 60, objectFit: 'cover', borderRadius: 4 }}
            />
          )}
          <span>{value}</span>
        </Box>
      )
    },
    { id: 'originalLanguage', label: 'Ngôn ngữ gốc', minWidth: 100, sortable: true },
    { id: 'status', label: 'Trạng thái', minWidth: 100, sortable: true },
    { id: 'year', label: 'Năm', minWidth: 70, sortable: true },
    { id: 'contentRating', label: 'Đánh giá', minWidth: 80, sortable: true },
    {
      id: 'relationships',
      label: 'Tags',
      minWidth: 150,
      sortable: false,
      format: (value) => {
        const tags = value?.filter((rel) => rel.type === 'tag') || []
        return (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {tags.slice(0, 2).map((tag) => (
              <Chip key={tag.id} label={tag.name || 'Tag'} size="small" />
            ))}
            {tags.length > 2 && (
              <Chip label={`+${tags.length - 2}`} size="small" />
            )}
          </Box>
        )
      },
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
      minWidth: 150,
      align: 'center',
      format: (value, row) => (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
          <Tooltip title="Chỉnh sửa">
            <IconButton color="primary" onClick={() => onEdit(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Quản lý Ảnh bìa">
            <IconButton color="default" onClick={() => onViewCovers(row.id)}>
              <ImageOutlinedIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Quản lý Bản dịch">
            <IconButton color="default" onClick={() => onViewTranslations(row.id)}>
              <TranslateIcon />
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

  // Format manga data for display in the table
  const formatMangaDataForTable = (mangasData) => {
    return mangasData.map(manga => {
      // Find the main cover art relationship if available
      const coverArtRel = manga.relationships?.find(rel => rel.type === 'cover_art');
      
      return {
        ...manga.attributes,
        id: manga.id, // Ensure ID is present for keying and actions
        relationships: manga.relationships, // Pass relationships for tags or other info
        coverArtPublicId: coverArtRel ? coverArtRel.id : null, // Add cover art publicId if found
      };
    });
  };

  return (
    <>
      <DataTableMUI
        columns={columns}
        data={formatMangaDataForTable(mangas)}
        totalItems={totalMangas}
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
        title="Xác nhận xóa Manga"
        message="Bạn có chắc chắn muốn xóa manga này? Thao tác này không thể hoàn tác và sẽ xóa tất cả các bản dịch, chapter, và ảnh bìa liên quan."
      />
    </>
  )
}

export default MangaTable 