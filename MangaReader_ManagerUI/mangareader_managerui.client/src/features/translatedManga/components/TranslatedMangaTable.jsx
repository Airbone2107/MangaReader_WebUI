import CollectionsBookmarkIcon from '@mui/icons-material/CollectionsBookmark';
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { Box, IconButton, Tooltip } from '@mui/material'
import { useState } from 'react';
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { formatDate } from '../../../utils/dateUtils'
import { translateLanguageCode } from '../../../utils/translationUtils'

/**
 * @typedef {import('../../../types/manga').TranslatedManga} TranslatedManga
 */

/**
 * TranslatedMangaTable component to display a list of translated mangas.
 * @param {object} props
 * @param {TranslatedManga[]} props.translatedMangas - Array of translated manga data.
 * @param {number} props.totalTranslatedMangas - Total number of translated mangas.
 * @param {number} props.page - Current page number (0-indexed).
 * @param {number} props.rowsPerPage - Number of rows per page.
 * @param {function(React.MouseEvent<HTMLButtonElement> | null, number): void} props.onPageChange - Callback for page change.
 * @param {function(React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void} props.onRowsPerPageChange - Callback for rows per page change.
 * @param {function(string, 'asc' | 'desc'): void} props.onSort - Callback for sorting.
 * @param {string} props.orderBy - Current sort by field.
 * @param {'asc' | 'desc'} props.order - Current sort order.
 * @param {function(string): void} props.onDelete - Callback for deleting a translated manga.
 * @param {function(string): void} props.onEdit - Callback for editing a translated manga.
 * @param {function(string): void} props.onViewChapters - Callback for viewing chapters of a translated manga.
 * @param {boolean} props.isLoading - Loading state.
 */
function TranslatedMangaTable({
  translatedMangas,
  totalTranslatedMangas,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onSort,
  orderBy,
  order,
  onDelete,
  onEdit,
  onViewChapters,
  isLoading,
}) {
  const [openConfirm, setOpenConfirm] = useState(false)
  const [translatedMangaToDeleteId, setTranslatedMangaToDeleteId] = useState(null)

  const handleDeleteClick = (id) => {
    setTranslatedMangaToDeleteId(id)
    setOpenConfirm(true)
  }

  const handleConfirmDelete = () => {
    onDelete(translatedMangaToDeleteId)
    setOpenConfirm(false)
    setTranslatedMangaToDeleteId(null)
  }

  const handleCloseConfirm = () => {
    setOpenConfirm(false)
    setTranslatedMangaToDeleteId(null)
  }

  const columns = [
    { id: 'title', label: 'Tiêu đề bản dịch', minWidth: 150, sortable: true },
    { 
      id: 'languageKey', 
      label: 'Ngôn ngữ', 
      minWidth: 80, 
      sortable: true, 
      format: (value) => translateLanguageCode(value)
    },
    { id: 'description', label: 'Mô tả', minWidth: 200, sortable: false },
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
      minWidth: 180,
      align: 'center',
      format: (value, row) => (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
          <Tooltip title="Chỉnh sửa Bản dịch">
            <IconButton color="primary" onClick={() => onEdit(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Quản lý Chương">
            <IconButton color="default" onClick={() => onViewChapters(row.id)}>
              <CollectionsBookmarkIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Xóa Bản dịch">
            <IconButton color="secondary" onClick={() => handleDeleteClick(row.id)}>
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ]

  // Format data for DataTableMUI
  const formatTranslatedMangaDataForTable = (translatedMangasData) => {
    if (!translatedMangasData) return [];
    return translatedMangasData.map(translatedManga => ({
      ...translatedManga.attributes,
      id: translatedManga.id,
    }));
  };

  return (
    <>
      <DataTableMUI
        columns={columns}
        data={formatTranslatedMangaDataForTable(translatedMangas)}
        totalItems={totalTranslatedMangas}
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
        title="Xác nhận xóa Bản dịch Manga"
        message={`Bạn có chắc chắn muốn xóa bản dịch "${translatedMangaToDeleteId ? translatedMangas.find(tm => tm.id === translatedMangaToDeleteId)?.attributes?.title : ''}" này? Thao tác này không thể hoàn tác và sẽ xóa tất cả các chương và trang liên quan.`}
      />
    </>
  )
}

export default TranslatedMangaTable 