Được rồi, đây là file `TODO.md` hướng dẫn chi tiết cách sửa lỗi bạn đang gặp phải.

```markdown
<!-- TODO.md -->
# TODO: Sửa lỗi `TypeError` trong `MangaTable.jsx`

Hướng dẫn này mô tả các bước cần thực hiện để sửa lỗi `MangaTable.jsx:172 Uncaught TypeError: Cannot read properties of undefined (reading 'tags')`.

## 1. Mô tả vấn đề

Sau khi cập nhật theo file `TODO2.md`, ứng dụng gặp lỗi sau khi hiển thị bảng danh sách manga:

```
MangaTable.jsx:172 Uncaught TypeError: Cannot read properties of undefined (reading 'tags')
    at Object.format (MangaTable.jsx:172:45)
    at DataTableMUI.jsx:91:56
    at Array.map (<anonymous>)
    at DataTableMUI.jsx:90:26
    at Array.map (<anonymous>)
    at DataTableMUI (DataTableMUI.jsx:83:18)
```

Lỗi xảy ra tại dòng 172 của file `MangaTable.jsx`, cụ thể là trong hàm `format` của cột `tags`.

## 2. Phân tích nguyên nhân

Lỗi này xảy ra do cách truy cập thuộc tính `tags` không chính xác bên trong hàm `format` của cột `tags`.

Trong hàm `formatMangaDataForTable` của file `MangaTable.jsx`, các thuộc tính của `manga.attributes` (bao gồm cả `tags`) đã được trải phẳng (spread) trực tiếp vào đối tượng `row` được truyền cho component `DataTableMUI`.

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx
// ...
  const formatMangaDataForTable = (mangasData) => {
    if (!mangasData) return [];
    return mangasData.map(manga => {
      return {
        ...manga.attributes, // << title, originalLanguage, ..., tags đều nằm trực tiếp ở đây
        id: manga.id, 
        relationships: manga.relationships, 
        coverArtPublicId: manga.coverArtPublicId,
      };
    });
  };
// ...
```

Khi đó, trong hàm `format` của cột `tags`, đối tượng `row` sẽ có cấu trúc là `{ id: '...', title: '...', tags: [...], ... }`.
Tuy nhiên, code hiện tại đang cố gắng truy cập `row.attributes.tags`:

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx
// ...
    {
      id: 'tags', // `id` này khiến cho `value` trong `format(value, row)` chính là `row.tags`
      label: 'Tags',
      minWidth: 170,
      sortable: false,
      format: (value, row) => { // `value` ở đây chính là `row.tags` (do id cột là 'tags')
        /** @type {ResourceObject<TagInMangaAttributesDto>[]} */
        const tagResources = row.attributes.tags || []; // LỖI: row.attributes không tồn tại ở đây, tags nằm trực tiếp trong `row` (hoặc `value`)
        const tagNames = tagResources.map(tagResource => tagResource.attributes.name);

        if (tagNames.length === 0) return <Typography variant="caption" color="textSecondary">N/A</Typography>;

        return (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {tagNames.slice(0, 3).map((tagName) => (
              <Chip key={tagName} label={tagName} size="small" variant="outlined" />
            ))}
            {tagNames.length > 3 && (
              <Chip label={`+${tagNames.length - 3}`} size="small" />
            )}
          </Box>
        )
      },
    },
// ...
```
Vì `row.attributes` là `undefined`, nên việc truy cập `row.attributes.tags` sẽ gây ra lỗi "Cannot read properties of undefined (reading 'tags')".

## 3. Hướng dẫn sửa lỗi

Để khắc phục lỗi, chúng ta cần cập nhật lại cách truy cập mảng `tags` trong hàm `format` của cột `tags` tại file `MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx`.

**File cần sửa:**
`MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx`

**Nội dung file sau khi sửa:**

Thay thế toàn bộ nội dung file `MangaTable.jsx` bằng đoạn code sau:

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import TranslateIcon from '@mui/icons-material/Translate'
import { Box, Chip, IconButton, Tooltip, Typography } from '@mui/material'
import React from 'react'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import DataTableMUI from '../../../components/common/DataTableMUI'
import { CLOUDINARY_BASE_URL, MANGA_STATUS_OPTIONS, CONTENT_RATING_OPTIONS, PUBLICATION_DEMOGRAPHIC_OPTIONS, RELATIONSHIP_TYPES } from '../../../constants/appConstants'
import { formatDate } from '../../../utils/dateUtils'
import { translateLanguageCode } from '../../../utils/translationUtils'

/**
 * @typedef {import('../../../types/manga').Manga} Manga
 * @typedef {import('../../../types/api').AuthorInRelationshipAttributes} AuthorInRelationshipAttributes
 * @typedef {import('../../../types/manga').TagInMangaAttributesDto} TagInMangaAttributesDto
 * @typedef {import('../../../types/api').ResourceObject} ResourceObject
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
    if (mangaToDeleteId) { // Kiểm tra null trước khi gọi onDelete
        onDelete(mangaToDeleteId)
    }
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
      minWidth: 220,
      sortable: true,
      format: (value, row) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          {row.coverArtPublicId ? (
            <img
              src={`${CLOUDINARY_BASE_URL}w_50,h_75,c_fill/${row.coverArtPublicId}`}
              alt="Cover"
              style={{ width: 50, height: 75, objectFit: 'cover', borderRadius: 4, border: '1px solid #eee' }}
              loading="lazy"
            />
          ) : (
            <Box sx={{ width: 50, height: 75, backgroundColor: '#f0f0f0', borderRadius: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid #ddd' }}>
              <ImageOutlinedIcon sx={{ color: '#ccc' }} />
            </Box>
          )}
          <Typography variant="body2" component="span" sx={{ fontWeight: 500 }}>{value}</Typography>
        </Box>
      )
    },
    { 
      id: 'originalLanguage', 
      label: 'Ngôn ngữ', 
      minWidth: 90, 
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
    { id: 'year', label: 'Năm', minWidth: 60, sortable: true, align: 'center' },
    {
      id: 'contentRating',
      label: 'Rating',
      minWidth: 90,
      sortable: true,
      // Sử dụng hàm getEnumLabel để hiển thị nhãn thân thiện
      format: (value) => getEnumLabel(value, CONTENT_RATING_OPTIONS),
    },
    // Thêm cột đối tượng độc giả
    { 
        id: 'publicationDemographic', 
        label: 'Đối tượng', 
        minWidth: 90, 
        sortable: true,
        format: (value) => value ? getEnumLabel(value, PUBLICATION_DEMOGRAPHIC_OPTIONS) : 'N/A', // Xử lý trường hợp null/None
    },
    {
      id: 'authors', 
      label: 'Tác giả/Họa sĩ',
      minWidth: 150,
      sortable: false, 
      format: (value, row) => {
        const staff = row.relationships
          ?.filter(rel => (rel.type === RELATIONSHIP_TYPES.AUTHOR || rel.type === RELATIONSHIP_TYPES.ARTIST) && rel.attributes)
          .map(rel => {
            /** @type {AuthorInRelationshipAttributes | undefined} */
            const staffAttrs = rel.attributes;
            return `${staffAttrs?.name || 'N/A'} (${rel.type === RELATIONSHIP_TYPES.AUTHOR ? 'T.giả' : 'H.sĩ'})`;
          }) || [];
        
        if (staff.length === 0) return <Typography variant="caption" color="textSecondary">N/A</Typography>;
        
        return (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.2 }}>
            {staff.slice(0, 2).map((s, i) => (
              <Chip key={i} label={s} size="small" variant="outlined" sx={{ maxWidth: '100%' }}/>
            ))}
            {staff.length > 2 && (
              <Chip label={`+${staff.length - 2} khác`} size="small" />
            )}
          </Box>
        );
      },
    },
    {
      id: 'tags', 
      label: 'Tags',
      minWidth: 170,
      sortable: false,
      format: (value, row) => { // `value` ở đây chính là `row.tags`
        /** @type {ResourceObject<TagInMangaAttributesDto>[]} */
        // SỬA ĐỔI Ở ĐÂY:
        // Thay vì: const tagResources = row.attributes.tags || [];
        // Sử dụng:
        const tagResources = value || []; // `value` (tức là row.tags) đã là mảng các ResourceObject
        // HOẶC CÓ THỂ LÀ:
        // const tagResources = row.tags || [];

        const tagNames = tagResources.map(tagResource => tagResource.attributes.name);

        if (tagNames.length === 0) return <Typography variant="caption" color="textSecondary">N/A</Typography>;

        return (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {tagNames.slice(0, 3).map((tagName) => (
              <Chip key={tagName} label={tagName} size="small" variant="outlined" />
            ))}
            {tagNames.length > 3 && (
              <Chip label={`+${tagNames.length - 3}`} size="small" />
            )}
          </Box>
        )
      },
    },
    {
      id: 'updatedAt',
      label: 'Cập nhật',
      minWidth: 120,
      sortable: true,
      format: (value) => formatDate(value),
    },
    {
      id: 'actions',
      label: 'Hành động',
      minWidth: 150, 
      align: 'center',
      format: (value, row) => (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 0.5 }}> 
          <Tooltip title="Chỉnh sửa">
            <IconButton size="small" color="primary" onClick={() => onEdit(row.id)}>
              <EditIcon fontSize="small"/>
            </IconButton>
          </Tooltip>
          <Tooltip title="Quản lý Ảnh bìa">
            <IconButton size="small" color="default" onClick={() => onViewCovers(row.id)}>
              <ImageOutlinedIcon fontSize="small"/>
            </IconButton>
          </Tooltip>
          <Tooltip title="Quản lý Bản dịch">
            <IconButton size="small" color="default" onClick={() => onViewTranslations(row.id)}>
              <TranslateIcon fontSize="small"/>
            </IconButton>
          </Tooltip>
          <Tooltip title="Xóa">
            <IconButton size="small" color="secondary" onClick={() => handleDeleteClick(row.id)}>
              <DeleteIcon fontSize="small"/>
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
        ...manga.attributes, // Bao gồm title, originalLanguage, status, year, contentRating, publicationDemographic, tags
        id: manga.id, // Ensure ID is present for keying and actions
        relationships: manga.relationships, // Pass relationships for tags or other info
        coverArtPublicId: manga.coverArtPublicId, 
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
        message={`Bạn có chắc chắn muốn xóa manga "${mangaToDeleteId ? mangas.find(m => m.id === mangaToDeleteId)?.attributes?.title : ''}" này? Thao tác này không thể hoàn tác và sẽ xóa tất cả các bản dịch, chapter, và ảnh bìa liên quan.`}
      />
    </>
  )
}

export default MangaTable
```

**Giải thích thay đổi:**

Trong hàm `format` của cột `tags` (khoảng dòng 172):
-   **Code cũ (gây lỗi):**
    ```javascript
    const tagResources = row.attributes.tags || [];
    ```
-   **Code mới (đã sửa):**
    ```javascript
    const tagResources = value || [];
    ```
    Hoặc:
    ```javascript
    const tagResources = row.tags || [];
    ```
    Lý do: Vì `id` của cột này là `'tags'`, nên tham số `value` được truyền vào hàm `format` chính là `row.tags` (do `row` đã được trải phẳng `manga.attributes`). Do đó, chúng ta có thể sử dụng trực tiếp `value` hoặc `row.tags` để lấy mảng `tags`.

## 4. Kiểm tra lại

Sau khi áp dụng thay đổi trên, hãy chạy lại ứng dụng và kiểm tra xem lỗi đã được khắc phục hay chưa. Danh sách manga và các tag của chúng phải được hiển thị chính xác.

---

Kết thúc TODO.
```