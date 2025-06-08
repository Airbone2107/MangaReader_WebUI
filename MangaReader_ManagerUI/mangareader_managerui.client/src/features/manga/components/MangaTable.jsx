import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import TranslateIcon from '@mui/icons-material/Translate'
import { Box, Chip, IconButton, Tooltip } from '@mui/material'
import React from 'react'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { CLOUDINARY_BASE_URL, MANGA_STATUS_OPTIONS, CONTENT_RATING_OPTIONS, PUBLICATION_DEMOGRAPHIC_OPTIONS } from '../../../constants/appConstants'
import { formatDate } from '../../../utils/dateUtils'
import { translateLanguageCode } from '../../../utils/translationUtils'

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

  // Hàm tiện ích để lấy nhãn từ giá trị Enum
  const getEnumLabel = (value, options) => {
    const option = options.find(opt => opt.value === value);
    return option ? option.label : value; // Trả về nhãn nếu tìm thấy, nếu không thì trả về giá trị gốc
  };

  const columns = [
    {
      id: 'title',
      label: 'Tiêu đề',
      minWidth: 180,
      sortable: true,
      format: (value, row) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {row.coverArtPublicId ? (
            <img
              src={`${CLOUDINARY_BASE_URL}w_40,h_60,c_fill/${row.coverArtPublicId}`}
              alt="Cover"
              style={{ width: 40, height: 60, objectFit: 'cover', borderRadius: 4 }}
            />
          ) : (
            <img
              src="https://via.placeholder.com/40x60?text=No+Cover"
              alt="No Cover"
              style={{ width: 40, height: 60, objectFit: 'cover', borderRadius: 4, border: '1px solid #ddd' }}
            />
          )}
          <span>{value}</span>
        </Box>
      )
    },
    { 
      id: 'originalLanguage', 
      label: 'Ngôn ngữ gốc', 
      minWidth: 100, 
      sortable: true,
      format: (value) => translateLanguageCode(value)
    },
    {
      id: 'status',
      label: 'Trạng thái',
      minWidth: 100,
      sortable: true,
      // Sử dụng hàm getEnumLabel để hiển thị nhãn thân thiện
      format: (value) => getEnumLabel(value, MANGA_STATUS_OPTIONS),
    },
    { id: 'year', label: 'Năm', minWidth: 70, sortable: true },
    {
      id: 'contentRating',
      label: 'Đánh giá',
      minWidth: 80,
      sortable: true,
      // Sử dụng hàm getEnumLabel để hiển thị nhãn thân thiện
      format: (value) => getEnumLabel(value, CONTENT_RATING_OPTIONS),
    },
    // Thêm cột đối tượng độc giả
    { 
        id: 'publicationDemographic', 
        label: 'Đối tượng', 
        minWidth: 100, 
        sortable: true,
        format: (value) => value ? getEnumLabel(value, PUBLICATION_DEMOGRAPHIC_OPTIONS) : 'N/A', // Xử lý trường hợp null/None
    },
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
    if (!mangasData) return [];
    return mangasData.map(manga => {
      return {
        ...manga.attributes,
        id: manga.id, // Ensure ID is present for keying and actions
        relationships: manga.relationships, // Pass relationships for tags or other info
        coverArtPublicId: manga.coverArtPublicId, // <-- ĐẢM BẢO TRƯỜNG NÀY ĐƯỢC CHUYỂN QUA TỪ STORE
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