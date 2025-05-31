# TODO List: Sửa lỗi Frontend MangaReader Manager UI

Tài liệu này mô tả các bước cần thực hiện để khắc phục các lỗi `TypeError` và cảnh báo liên quan đến `MUI Grid` trong ứng dụng Frontend MangaReader Manager UI.

## 1. Khắc phục lỗi `TypeError: Cannot read properties of undefined (reading 'nameFilter')` (và các lỗi tương tự)

**Mô tả lỗi:**
Lỗi này xảy ra khi các trang danh sách (ví dụ: `TagListPage`, `AuthorListPage`, `MangaListPage`, `TagGroupListPage`) cố gắng truy cập một thuộc tính của đối tượng `filters` từ Zustand store, nhưng đối tượng `filters` hoặc thuộc tính cụ thể đó lại là `undefined` trong lần render đầu tiên hoặc khi state thay đổi. Điều này thường xảy ra khi `useState` được khởi tạo trực tiếp bằng một giá trị có thể là `undefined`.

**Giải pháp:**
Đảm bảo rằng các biến trạng thái cục bộ (`useState`) dùng để lưu trữ giá trị bộ lọc được khởi tạo với một giá trị mặc định an toàn (ví dụ: chuỗi rỗng `''`) bằng cách sử dụng toán tử nullish coalescing (`??`) hoặc toán tử OR (`||`). Điều này sẽ ngăn chặn việc truy cập thuộc tính trên `undefined` khi `filters` chưa kịp đồng bộ hoặc một thuộc tính cụ thể là `undefined` theo thiết kế (như `tagGroupId` trong `TagStore`).

**Các file cần sửa:**

### `src\features\author\pages\AuthorListPage.jsx`

```javascript
// src\features\author\pages\AuthorListPage.jsx
import React, { useEffect, useState } from 'react'
import { Box, Typography, Button, TextField, MenuItem, Grid, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import ClearIcon from '@mui/icons-material/Clear'
import AuthorTable from '../components/AuthorTable'
import AuthorForm from '../components/AuthorForm'
import useAuthorStore from '../../../stores/authorStore'
import useUiStore from '../../../stores/uiStore'
import authorApi from '../../../api/authorApi'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').CreateAuthorRequest} CreateAuthorRequest
 * @typedef {import('../../../types/manga').UpdateAuthorRequest} UpdateAuthorRequest
 */

function AuthorListPage() {
  const {
    authors,
    totalAuthors,
    page,
    rowsPerPage,
    filters,
    sort,
    fetchAuthors,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteAuthor,
  } = useAuthorStore()

  const isLoading = useUiStore((state) => state.isLoading)

  // State for filter inputs (controlled components)
  // FIX: Provide a default empty string in case filters.nameFilter is undefined initially
  const [localNameFilter, setLocalNameFilter] = useState(filters.nameFilter || '')

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {Author | null} */
  const [editingAuthor, setEditingAuthor] = useState(null)

  useEffect(() => {
    fetchAuthors(true) // Reset pagination on initial load
  }, [fetchAuthors])

  // Sync local filter states with global store filters when global filters change (e.g., after reset)
  useEffect(() => {
    // FIX: Use optional chaining or fallback for safety here as well
    setLocalNameFilter(filters.nameFilter || '')
  }, [filters])

  const handleApplyFilters = () => {
    applyFilters({
      nameFilter: localNameFilter,
    })
  }

  const handleResetFilters = () => {
    setLocalNameFilter('')
    resetFilters()
  }

  const handleCreateNew = () => {
    setEditingAuthor(null)
    setOpenFormDialog(true)
  }

  const handleEdit = async (id) => {
    try {
      const response = await authorApi.getAuthorById(id)
      setEditingAuthor(response.data)
      setOpenFormDialog(true)
    } catch (error) {
      console.error('Failed to fetch author for editing:', error)
      handleApiError(error, `Không thể tải tác giả có ID: ${id}.`)
    }
  }

  /**
   * @param {CreateAuthorRequest | UpdateAuthorRequest} data
   */
  const handleSubmitForm = async (data) => {
    try {
      if (editingAuthor) {
        await authorApi.updateAuthor(editingAuthor.id, /** @type {UpdateAuthorRequest} */ (data))
        showSuccessToast('Cập nhật tác giả thành công!')
      } else {
        await authorApi.createAuthor(/** @type {CreateAuthorRequest} */ (data))
        showSuccessToast('Tạo tác giả thành công!')
      }
      fetchAuthors(true) // Refresh list and reset pagination
      setOpenFormDialog(false)
    } catch (error) {
      console.error('Failed to save author:', error)
      handleApiError(error, 'Không thể lưu tác giả.')
    }
  }

  const handleDelete = (id) => {
    deleteAuthor(id)
  }

  return (
    <Box className="author-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Tác giả
      </Typography>

      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-end">
          <Grid xs={12} sm={6} md={4}> {/* FIX: Removed 'item' prop */}
            <TextField
              label="Lọc theo Tên tác giả"
              variant="outlined"
              fullWidth
              value={localNameFilter}
              onChange={(e) => setLocalNameFilter(e.target.value)}
            />
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
          Thêm Tác giả mới
        </Button>
      </Box>

      <AuthorTable
        authors={authors}
        totalAuthors={totalAuthors}
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
        <DialogTitle>{editingAuthor ? 'Chỉnh sửa Tác giả' : 'Tạo Tác giả mới'}</DialogTitle>
        <DialogContent>
          <AuthorForm
            initialData={editingAuthor}
            onSubmit={handleSubmitForm}
            isEditMode={!!editingAuthor}
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

export default AuthorListPage
```

### `src\features\manga\pages\MangaListPage.jsx`

```javascript
// src\features\manga\pages\MangaListPage.jsx
import React, { useEffect, useState } from 'react'
import {
  Box,
  Typography,
  Button,
  TextField,
  MenuItem,
  Grid,
  Chip,
  Autocomplete,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import ClearIcon from '@mui/icons-material/Clear'
import { useNavigate } from 'react-router-dom'
import MangaTable from '../components/MangaTable'
import useMangaStore from '../../../stores/mangaStore'
import {
  MANGA_STATUS_OPTIONS,
  PUBLICATION_DEMOGRAPHIC_OPTIONS,
  CONTENT_RATING_OPTIONS,
  ORIGINAL_LANGUAGE_OPTIONS,
} from '../../../constants/appConstants'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import { handleApiError } from '../../../utils/errorUtils'
import useUiStore from '../../../stores/uiStore'

/**
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').Tag} Tag
 * @typedef {import('../../../types/manga').SelectedRelationship} SelectedRelationship
 */

function MangaListPage() {
  const navigate = useNavigate()
  const {
    mangas,
    totalMangas,
    page,
    rowsPerPage,
    filters,
    sort,
    fetchMangas,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteManga,
  } = useMangaStore()

  const isLoading = useUiStore(state => state.isLoading);

  // State for filter inputs (controlled components)
  // FIX: Provide default empty string or null for initial state
  const [localTitleFilter, setLocalTitleFilter] = useState(filters.titleFilter || '')
  const [localStatusFilter, setLocalStatusFilter] = useState(filters.statusFilter || '')
  const [localContentRatingFilter, setLocalContentRatingFilter] = useState(filters.contentRatingFilter || '')
  const [localDemographicFilter, setLocalDemographicFilter] = useState(filters.demographicFilter || '')
  const [localOriginalLanguageFilter, setLocalOriginalLanguageFilter] = useState(filters.originalLanguageFilter || '')
  const [localYearFilter, setLocalYearFilter] = useState(filters.yearFilter || null)
  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [localSelectedAuthorFilters, setLocalSelectedAuthorFilters] = useState([])
  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [localSelectedTagFilters, setLocalSelectedTagFilters] = useState([])


  const [availableAuthors, setAvailableAuthors] = useState([])
  const [availableTags, setAvailableTags] = useState([])
  const [isFilterLoading, setIsFilterLoading] = useState(false); // Local loading for filter options

  useEffect(() => {
    // Initial fetch of mangas when component mounts
    fetchMangas(true); // Reset pagination on initial load
  }, [fetchMangas]);

  // Fetch available authors and tags for filters
  useEffect(() => {
    const fetchFilterOptions = async () => {
      setIsFilterLoading(true);
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 });
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name })));

        const tagsResponse = await tagApi.getTags({ limit: 1000 });
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name })));
      } catch (error) {
        handleApiError(error, 'Không thể tải tùy chọn lọc.');
      } finally {
        setIsFilterLoading(false);
      }
    };
    fetchFilterOptions();
  }, []);

  // Sync local filter states with global store filters when global filters change (e.g., after reset)
  useEffect(() => {
    // FIX: Provide default values in case filters properties are undefined
    setLocalTitleFilter(filters.titleFilter || '');
    setLocalStatusFilter(filters.statusFilter || '');
    setLocalContentRatingFilter(filters.contentRatingFilter || '');
    setLocalDemographicFilter(filters.demographicFilter || '');
    setLocalOriginalLanguageFilter(filters.originalLanguageFilter || '');
    setLocalYearFilter(filters.yearFilter || null);
    // When filters change globally, update local selected relationships for Autocomplete
    // This requires mapping filter IDs back to full objects using availableAuthors/Tags
    if (availableAuthors.length > 0 && filters.authorIdsFilter) {
      setLocalSelectedAuthorFilters(availableAuthors.filter(a => filters.authorIdsFilter?.includes(a.id))); // FIX: Use optional chaining
    } else {
      setLocalSelectedAuthorFilters([]); // FIX: Clear if filters.authorIdsFilter is undefined or empty
    }
    if (availableTags.length > 0 && filters.tagIdsFilter) {
      setLocalSelectedTagFilters(availableTags.filter(t => filters.tagIdsFilter?.includes(t.id))); // FIX: Use optional chaining
    } else {
      setLocalSelectedTagFilters([]); // FIX: Clear if filters.tagIdsFilter is undefined or empty
    }
  }, [filters, availableAuthors, availableTags]);


  const handleApplyFilters = () => {
    applyFilters({
      titleFilter: localTitleFilter,
      statusFilter: localStatusFilter,
      contentRatingFilter: localContentRatingFilter,
      demographicFilter: localDemographicFilter,
      originalLanguageFilter: localOriginalLanguageFilter,
      yearFilter: localYearFilter === '' ? null : localYearFilter, // Convert empty string to null for year
      tagIdsFilter: localSelectedTagFilters.map(t => t.id),
      authorIdsFilter: localSelectedAuthorFilters.map(a => a.id),
    })
  }

  const handleResetFilters = () => {
    setLocalTitleFilter('');
    setLocalStatusFilter('');
    setLocalContentRatingFilter('');
    setLocalDemographicFilter('');
    setLocalOriginalLanguageFilter('');
    setLocalYearFilter(null);
    setLocalSelectedAuthorFilters([]);
    setLocalSelectedTagFilters([]);
    resetFilters();
  };


  return (
    <Box className="manga-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Manga
      </Typography>

      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-end">
          <Grid xs={12} sm={6} md={3}> {/* FIX: Removed 'item' prop */}
            <TextField
              label="Lọc theo Tiêu đề"
              variant="outlined"
              fullWidth
              value={localTitleFilter}
              onChange={(e) => setLocalTitleFilter(e.target.value)}
            />
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
            <TextField
              select
              label="Trạng thái"
              variant="outlined"
              fullWidth
              value={localStatusFilter}
              onChange={(e) => setLocalStatusFilter(e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {MANGA_STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
            <TextField
              select
              label="Đánh giá"
              variant="outlined"
              fullWidth
              value={localContentRatingFilter}
              onChange={(e) => setLocalContentRatingFilter(e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {CONTENT_RATING_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
            <TextField
              select
              label="Đối tượng"
              variant="outlined"
              fullWidth
              value={localDemographicFilter}
              onChange={(e) => setLocalDemographicFilter(e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {PUBLICATION_DEMOGRAPHIC_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
            <TextField
              select
              label="Ngôn ngữ gốc"
              variant="outlined"
              fullWidth
              value={localOriginalLanguageFilter}
              onChange={(e) => setLocalOriginalLanguageFilter(e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {ORIGINAL_LANGUAGE_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid xs={12} sm={6} md={1}> {/* FIX: Removed 'item' prop */}
            <TextField
              label="Năm"
              variant="outlined"
              fullWidth
              type="number"
              value={localYearFilter || ''}
              onChange={(e) => setLocalYearFilter(e.target.value === '' ? null : parseInt(e.target.value, 10))}
              inputProps={{ min: 1000, max: new Date().getFullYear(), step: 1 }}
            />
          </Grid>
          <Grid xs={12} sm={6} md={3}> {/* FIX: Removed 'item' prop */}
            <Autocomplete
              multiple
              options={availableAuthors}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={localSelectedAuthorFilters}
              onChange={(event, newValue) => {
                setLocalSelectedAuthorFilters(newValue);
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Tác giả" />}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
            />
          </Grid>
          <Grid xs={12} sm={6} md={3}> {/* FIX: Removed 'item' prop */}
            <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={localSelectedTagFilters}
              onChange={(event, newValue) => {
                setLocalSelectedTagFilters(newValue);
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Tags" />}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
            />
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
          onClick={() => navigate('/mangas/create')}
        >
          Thêm Manga mới
        </Button>
      </Box>

      <MangaTable
        mangas={mangas}
        totalMangas={totalMangas}
        page={page}
        rowsPerPage={rowsPerPage}
        onPageChange={setPage}
        onRowsPerPageChange={setRowsPerPage}
        onSort={setSort}
        orderBy={sort.orderBy}
        order={sort.ascending ? 'asc' : 'desc'}
        onDelete={deleteManga}
        onEdit={(id) => navigate(`/mangas/edit/${id}`)}
        onViewCovers={(id) => navigate(`/mangas/${id}/covers`)}
        onViewTranslations={(id) => navigate(`/mangas/${id}/translations`)}
        isLoading={isLoading}
      />
    </Box>
  )
}

export default MangaListPage
```

### `src\features\tag\pages\TagListPage.jsx`

```javascript
// src\features\tag\pages\TagListPage.jsx
import React, { useEffect, useState } from 'react'
import { Box, Typography, Button, TextField, MenuItem, Grid, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import ClearIcon from '@mui/icons-material/Clear'
import TagTable from '../components/TagTable'
import TagForm from '../components/TagForm'
import useTagStore from '../../../stores/tagStore'
import useTagGroupStore from '../../../stores/tagGroupStore' // To fetch tag groups for filtering
import useUiStore from '../../../stores/uiStore'
import tagApi from '../../../api/tagApi'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'

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
    filters,
    sort,
    fetchTags,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteTag,
  } = useTagStore()

  const { tagGroups, fetchTagGroups } = useTagGroupStore() // For tag group filter dropdown

  const isLoading = useUiStore((state) => state.isLoading)

  // State for filter inputs (controlled components)
  // FIX: Provide a default empty string for initial state
  const [localNameFilter, setLocalNameFilter] = useState(filters.nameFilter || '')
  // FIX: Use nullish coalescing operator (??) to correctly handle undefined vs. empty string
  const [localTagGroupIdFilter, setLocalTagGroupIdFilter] = useState(filters.tagGroupId ?? '') // For select input

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {Tag | null} */
  const [editingTag, setEditingTag] = useState(null)

  useEffect(() => {
    fetchTags(true) // Reset pagination on initial load
    fetchTagGroups(true) // Fetch all tag groups for filter dropdown
  }, [fetchTags, fetchTagGroups])

  // Sync local filter states with global store filters when global filters change (e.g., after reset)
  useEffect(() => {
    // FIX: Ensure filters properties are safely accessed with default values
    setLocalNameFilter(filters.nameFilter || '')
    setLocalTagGroupIdFilter(filters.tagGroupId ?? '')
  }, [filters])

  const handleApplyFilters = () => {
    applyFilters({
      nameFilter: localNameFilter,
      tagGroupId: localTagGroupIdFilter === '' ? undefined : localTagGroupIdFilter,
    })
  }

  const handleResetFilters = () => {
    setLocalNameFilter('')
    setLocalTagGroupIdFilter('')
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
        <Grid container spacing={2} alignItems="flex-end">
          <Grid xs={12} sm={6} md={4}> {/* FIX: Removed 'item' prop */}
            <TextField
              label="Lọc theo Tên tag"
              variant="outlined"
              fullWidth
              value={localNameFilter}
              onChange={(e) => setLocalNameFilter(e.target.value)}
            />
          </Grid>
          <Grid xs={12} sm={6} md={4}> {/* FIX: Removed 'item' prop */}
            <TextField
              select
              label="Nhóm tag"
              variant="outlined"
              fullWidth
              value={localTagGroupIdFilter}
              onChange={(e) => setLocalTagGroupIdFilter(e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {tagGroupOptions.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
```

### `src\features\tagGroup\pages\TagGroupListPage.jsx`

```javascript
// src\features\tagGroup\pages\TagGroupListPage.jsx
import React, { useEffect, useState } from 'react'
import { Box, Typography, Button, TextField, Grid, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import ClearIcon from '@mui/icons-material/Clear'
import TagGroupTable from '../components/TagGroupTable'
import TagGroupForm from '../components/TagGroupForm'
import useTagGroupStore from '../../../stores/tagGroupStore'
import useUiStore from '../../../stores/uiStore'
import tagGroupApi from '../../../api/tagGroupApi'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'

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
    filters,
    sort,
    fetchTagGroups,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteTagGroup,
  } = useTagGroupStore()

  const isLoading = useUiStore((state) => state.isLoading)

  // State for filter inputs (controlled components)
  // FIX: Provide a default empty string for initial state
  const [localNameFilter, setLocalNameFilter] = useState(filters.nameFilter || '')

  const [openFormDialog, setOpenFormDialog] = useState(false)
  /** @type {TagGroup | null} */
  const [editingTagGroup, setEditingTagGroup] = useState(null)

  useEffect(() => {
    fetchTagGroups(true) // Reset pagination on initial load
  }, [fetchTagGroups])

  // Sync local filter states with global store filters when global filters change (e.g., after reset)
  useEffect(() => {
    // FIX: Ensure filters.nameFilter is safely accessed
    setLocalNameFilter(filters.nameFilter || '')
  }, [filters])

  const handleApplyFilters = () => {
    applyFilters({
      nameFilter: localNameFilter,
    })
  }

  const handleResetFilters = () => {
    setLocalNameFilter('')
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
        <Grid container spacing={2} alignItems="flex-end">
          <Grid xs={12} sm={6} md={4}> {/* FIX: Removed 'item' prop */}
            <TextField
              label="Lọc theo Tên nhóm tag"
              variant="outlined"
              fullWidth
              value={localNameFilter}
              onChange={(e) => setLocalNameFilter(e.target.value)}
            />
          </Grid>
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
          <Grid xs={12} sm={6} md={2}> {/* FIX: Removed 'item' prop */}
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
  )
}

export default TagGroupListPage
```

## 2. Khắc phục cảnh báo MUI Grid (`item`, `xs`, `sm`, `md` props)

**Mô tả cảnh báo:**
MUI v5 đã giới thiệu hệ thống Grid mới, nơi prop `item` không còn cần thiết cho các `Grid` components con của một `Grid container`. Các prop responsive như `xs`, `sm`, `md` được áp dụng trực tiếp lên `Grid` component con. Việc sử dụng `item` cùng với các prop này sẽ tạo ra cảnh báo.

**Giải pháp:**
Loại bỏ prop `item` khỏi tất cả các `Grid` components đang là con trực tiếp của một `Grid container`. Giữ nguyên các prop responsive như `xs`, `sm`, `md` để chúng xác định kích thước của item trong grid.

**Các file cần sửa:**

### `src\features\author\components\AuthorForm.jsx`

```javascript
// src\features\author\components\AuthorForm.jsx
import React, { useEffect } from 'react'
import { Box, Button, Grid, Paper } from '@mui/material'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createAuthorSchema, updateAuthorSchema } from '../../../schemas/authorSchema'

// ... (typedefs and function signature remain unchanged)

function AuthorForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateAuthorSchema : createAuthorSchema,
    defaultValues: initialData
      ? {
          name: initialData.attributes.name || '',
          biography: initialData.attributes.biography || '',
        }
      : {
          name: '',
          biography: '',
        },
  })

  // Reset form when initialData or isEditMode changes
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        name: initialData.attributes.name || '',
        biography: initialData.attributes.biography || '',
      })
    } else if (!isEditMode) {
      reset({
        name: '',
        biography: '',
      })
    }
  }, [initialData, isEditMode, reset])

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2}>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput control={control} name="name" label="Tên tác giả" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput
              control={control}
              name="biography"
              label="Tiểu sử (Tùy chọn)"
              multiline
              rows={4}
            />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Tác giả' : 'Tạo Tác giả'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  )
}

export default AuthorForm
```

### `src\features\chapter\components\ChapterForm.jsx`

```javascript
// src\features\chapter\components\ChapterForm.jsx
import React, { useEffect } from 'react'
import { Box, Button, Grid, Paper, Typography } from '@mui/material'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createChapterSchema, updateChapterSchema } from '../../../schemas/chapterSchema'
import { format } from 'date-fns'

// ... (typedefs and function signature remain unchanged)

function ChapterForm({ translatedMangaId, initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateChapterSchema : createChapterSchema,
    defaultValues: initialData
      ? {
          volume: initialData.attributes.volume || '',
          chapterNumber: initialData.attributes.chapterNumber || '',
          title: initialData.attributes.title || '',
          publishAt: format(new Date(initialData.attributes.publishAt), 'yyyy-MM-dd\'T\'HH:mm'),
          readableAt: format(new Date(initialData.attributes.readableAt), 'yyyy-MM-dd\'T\'HH:mm'),
        }
      : {
          translatedMangaId: translatedMangaId, // Only for creation
          volume: '',
          chapterNumber: '',
          title: '',
          publishAt: format(new Date(), 'yyyy-MM-dd\'T\'HH:mm'), // Default to current datetime
          readableAt: format(new Date(), 'yyyy-MM-dd\'T\'HH:mm'), // Default to current datetime
        },
  })

  // Reset form when initialData or translatedMangaId changes
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        volume: initialData.attributes.volume || '',
        chapterNumber: initialData.attributes.chapterNumber || '',
        title: initialData.attributes.title || '',
        publishAt: format(new Date(initialData.attributes.publishAt), 'yyyy-MM-dd\'T\'HH:mm'),
        readableAt: format(new Date(initialData.attributes.readableAt), 'yyyy-MM-dd\'T\'HH:mm'),
      })
    } else if (!isEditMode && translatedMangaId) {
      reset({
        translatedMangaId: translatedMangaId,
        volume: '',
        chapterNumber: '',
        title: '',
        publishAt: format(new Date(), 'yyyy-MM-dd\'T\'HH:mm'),
        readableAt: format(new Date(), 'yyyy-MM-dd\'T\'HH:mm'),
      })
    }
  }, [initialData, isEditMode, translatedMangaId, reset])

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2}>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12} sm={6}>
            <FormInput control={control} name="volume" label="Volume (Tùy chọn)" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12} sm={6}>
            <FormInput control={control} name="chapterNumber" label="Số chương (Tùy chọn)" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput control={control} name="title" label="Tiêu đề chương (Tùy chọn)" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12} sm={6}>
            <FormInput
              control={control}
              name="publishAt"
              label="Thời gian xuất bản"
              type="datetime-local"
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12} sm={6}>
            <FormInput
              control={control}
              name="readableAt"
              label="Thời gian có thể đọc"
              type="datetime-local"
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Chương' : 'Tạo Chương'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  )
}

export default ChapterForm
```

### `src\features\chapter\components\ChapterPageManager.jsx`

```javascript
// src\features\chapter\components\ChapterPageManager.jsx
import React, { useEffect, useState } from 'react'
import {
  Box,
  Typography,
  Grid,
  Button,
  Card,
  CardMedia,
  CardContent,
  CardActions,
  IconButton,
  TextField,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
  Tooltip,
} from '@mui/material'
import { Add as AddIcon, Delete as DeleteIcon, UploadFile as UploadFileIcon } from '@mui/icons-material'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createChapterPageEntrySchema, uploadChapterPageImageSchema } from '../../../schemas/chapterSchema'
import useChapterPageStore from '../../../stores/chapterPageStore'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants'

// ... (typedefs and function signature remain unchanged)

function ChapterPageManager({ chapterId }) {
  const {
    chapterPages,
    fetchChapterPagesByChapterId,
    createPageEntry,
    uploadPageImage,
    deleteChapterPage,
  } = useChapterPageStore()

  const [loadingPages, setLoadingPages] = useState(true)
  const [openCreatePageDialog, setOpenCreatePageDialog] = useState(false)
  const [openUploadImageDialog, setOpenUploadImageDialog] = useState(false)
  const [pageEntryToUploadImage, setPageEntryToUploadImage] = useState(null) // Stores pageId and pageNumber
  const [openConfirmDelete, setOpenConfirmDelete] = useState(false)
  const [pageToDelete, setPageToDelete] = useState(null)

  const {
    register: registerCreate,
    handleSubmit: handleSubmitCreate,
    formState: { errors: errorsCreate },
    reset: resetCreate,
  } = useForm({
    resolver: zodResolver(createChapterPageEntrySchema),
  })

  const {
    register: registerUpload,
    handleSubmit: handleSubmitUpload,
    formState: { errors: errorsUpload },
    reset: resetUpload,
  } = useForm({
    resolver: zodResolver(uploadChapterPageImageSchema),
  })

  useEffect(() => {
    if (chapterId) {
      setLoadingPages(true)
      fetchChapterPagesByChapterId(chapterId, true)
        .finally(() => setLoadingPages(false))
    }
  }, [chapterId, fetchChapterPagesByChapterId])

  const handleCreatePageEntry = async (data) => {
    try {
      const pageId = await createPageEntry(chapterId, data)
      if (pageId) {
        showSuccessToast('Entry trang chương đã được tạo.')
        fetchChapterPagesByChapterId(chapterId) // Refresh pages after creating new entry
        // Optionally open upload dialog directly
        setPageEntryToUploadImage({ id: pageId, pageNumber: data.pageNumber })
        setOpenUploadImageDialog(true)
      }
      setOpenCreatePageDialog(false)
      resetCreate()
    } catch (error) {
      console.error('Failed to create page entry:', error)
      // Error handled by store
    }
  }

  const handleUploadImageRequest = (pageId, pageNumber) => {
    setPageEntryToUploadImage({ id: pageId, pageNumber: pageNumber })
    setOpenUploadImageDialog(true)
  }

  const handleUploadImage = async (data) => {
    if (pageEntryToUploadImage && data.file && data.file[0]) {
      try {
        await uploadPageImage(pageEntryToUploadImage.id, data.file[0])
        // Re-fetch pages to ensure UI updates with new publicId (image)
        fetchChapterPagesByChapterId(chapterId)
        setOpenUploadImageDialog(false)
        resetUpload()
      } catch (error) {
        console.error('Failed to upload page image:', error)
        // Error handled by store
      }
    }
  }

  const handleDeleteRequest = (page) => {
    setPageToDelete(page)
    setOpenConfirmDelete(true)
  }

  const handleConfirmDelete = async () => {
    if (pageToDelete) {
      try {
        await deleteChapterPage(pageToDelete.id, chapterId)
      } catch (error) {
        console.error('Failed to delete chapter page:', error)
        handleApiError(error, 'Không thể xóa trang chương.')
      } finally {
        setOpenConfirmDelete(false)
        setPageToDelete(null)
      }
    }
  }

  const handleCloseConfirmDelete = () => {
    setOpenConfirmDelete(false)
    setPageToDelete(null)
  }

  return (
    <Box className="chapter-page-manager" sx={{ mt: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={() => setOpenCreatePageDialog(true)}
        >
          Thêm Trang mới
        </Button>
      </Box>

      {loadingPages ? (
        <Box
          sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '200px' }}
        >
          <CircularProgress />
        </Box>
      ) : chapterPages.length === 0 ? (
        <Typography variant="h6" className="no-pages-message" sx={{ textAlign: 'center', py: 5 }}>
          Chưa có trang nào cho chương này.
        </Typography>
      ) : (
        <Grid container spacing={2} className="chapter-page-grid">
          {chapterPages
            .sort((a, b) => a.attributes.pageNumber - b.attributes.pageNumber) // Sort by pageNumber
            .map((pageItem) => (
              {/* FIX: Removed 'item' prop */}
              <Grid key={pageItem.id} xs={12} sm={6} md={4} lg={3}>
                <Card className="chapter-page-card">
                  <CardMedia
                    component="img"
                    image={pageItem.attributes.publicId ? `${CLOUDINARY_BASE_URL}${pageItem.attributes.publicId}` : 'https://via.placeholder.com/150x200?text=No+Image'}
                    alt={`Page ${pageItem.attributes.pageNumber}`}
                    sx={{ width: '100%', height: 250, objectFit: 'contain', backgroundColor: '#eee', borderBottom: '1px solid #ddd' }}
                  />
                  <CardContent>
                    <Typography variant="subtitle1" gutterBottom>
                      Trang số: {pageItem.attributes.pageNumber}
                    </Typography>
                  </CardContent>
                  <CardActions className="card-actions">
                    <Tooltip title="Tải ảnh lên">
                      <IconButton
                        color="primary"
                        onClick={() => handleUploadImageRequest(pageItem.id, pageItem.attributes.pageNumber)}
                      >
                        <UploadFileIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Xóa trang">
                      <IconButton
                        color="secondary"
                        onClick={() => handleDeleteRequest(pageItem)}
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Tooltip>
                  </CardActions>
                </Card>
              </Grid>
            ))}
        </Grid>
      )}

      {/* Create Page Entry Dialog */}
      <Dialog open={openCreatePageDialog} onClose={() => setOpenCreatePageDialog(false)}>
        <DialogTitle>Thêm Trang mới</DialogTitle>
        <Box component="form" onSubmit={handleSubmitCreate(handleCreatePageEntry)} noValidate>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Số trang"
              type="number"
              fullWidth
              variant="outlined"
              {...registerCreate('pageNumber', { valueAsNumber: true })}
              error={!!errorsCreate.pageNumber}
              helperText={errorsCreate.pageNumber?.message}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenCreatePageDialog(false)} variant="outlined">
              Hủy
            </Button>
            <Button type="submit" variant="contained" color="primary">
              Tạo
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Upload Image Dialog */}
      <Dialog open={openUploadImageDialog} onClose={() => setOpenUploadImageDialog(false)}>
        <DialogTitle>Tải ảnh cho Trang {pageEntryToUploadImage?.pageNumber}</DialogTitle>
        <Box component="form" onSubmit={handleSubmitUpload(handleUploadImage)} noValidate>
          <DialogContent>
            <TextField
              margin="dense"
              label="Chọn File ảnh"
              type="file"
              fullWidth
              variant="outlined"
              {...registerUpload('file')}
              error={!!errorsUpload.file}
              helperText={errorsUpload.file?.message}
              inputProps={{ accept: 'image/jpeg,image/png,image/webp' }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenUploadImageDialog(false)} variant="outlined">
              Hủy
            </Button>
            <Button type="submit" variant="contained" color="primary">
              Tải lên
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <ConfirmDialog
        open={openConfirmDelete}
        onClose={handleCloseConfirmDelete}
        onConfirm={handleConfirmDelete}
        title="Xác nhận xóa Trang chương"
        message={`Bạn có chắc chắn muốn xóa trang ${pageToDelete?.attributes?.pageNumber} này? Thao tác này không thể hoàn tác và sẽ xóa ảnh liên quan.`}
      />
    </Box>
  )
}

export default ChapterPageManager
```

### `src\features\dashboard\DashboardPage.jsx`

```javascript
// src\features\dashboard\DashboardPage.jsx
import React from 'react'
import { Box, Typography, Grid, Paper } from '@mui/material'

function DashboardPage() {
  return (
    <Box className="dashboard-page">
      <Typography variant="h4" component="h1" className="dashboard-header">
        Chào mừng đến với Bảng điều khiển quản lý Manga
      </Typography>

      <Grid container spacing={4} className="dashboard-stats-grid">
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Manga</Typography>
            <Typography variant="h4" color="primary">
              123
            </Typography>
          </Paper>
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Tác giả</Typography>
            <Typography variant="h4" color="primary">
              45
            </Typography>
          </Paper>
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Tags</Typography>
            <Typography variant="h4" color="primary">
              67
            </Typography>
          </Paper>
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Chapter đã tải lên</Typography>
            <Typography variant="h4" color="primary">
              890
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      <Paper sx={{ p: 3, boxShadow: 3, borderRadius: 2 }}>
        <Typography variant="h5" component="h2" gutterBottom>
          Thống kê nhanh
        </Typography>
        <Typography variant="body1">
          Đây là nơi hiển thị các biểu đồ và số liệu thống kê quan trọng về dữ liệu manga của bạn.
          Trong tương lai, bạn có thể tích hợp các biểu đồ từ thư viện như Chart.js hoặc Recharts
          để hiển thị các xu hướng hoặc thông tin tổng quan.
        </Typography>
      </Paper>
    </Box>
  )
}

export default DashboardPage
```

### `src\features\manga\components\CoverArtManager.jsx`

```javascript
// src\features\manga\components\CoverArtManager.jsx
import React, { useEffect, useState } from 'react'
import {
  Box,
  Typography,
  Grid,
  Button,
  Card,
  CardMedia,
  CardContent,
  CardActions,
  IconButton,
  TextField,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
} from '@mui/material'
import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { uploadCoverArtSchema } from '../../../schemas/mangaSchema'
import mangaApi from '../../../api/mangaApi'
import coverArtApi from '../../../api/coverArtApi'
import ConfirmDialog from '../../../components/common/ConfirmDialog'
import { showSuccessToast } from '../../../components/common/Notification'
import { handleApiError } from '../../../utils/errorUtils'
import { CLOUDINARY_BASE_URL } from '../../../constants/appConstants'

// ... (typedefs and function signature remain unchanged)

function CoverArtManager({ mangaId }) {
  /** @type {[CoverArt[], React.Dispatch<React.SetStateAction<CoverArt[]>>]} */
  const [covers, setCovers] = useState([])
  const [loadingCovers, setLoadingCovers] = useState(true)
  const [openUploadDialog, setOpenUploadDialog] = useState(false)
  const [openConfirmDelete, setOpenConfirmDelete] = useState(false)
  const [coverArtToDelete, setCoverArtToDelete] = useState(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm({
    resolver: zodResolver(uploadCoverArtSchema),
  })

  const fetchCovers = async () => {
    setLoadingCovers(true)
    try {
      const response = await mangaApi.getMangaCovers(mangaId, { limit: 100 }) // Fetch all covers for now
      setCovers(response.data)
    } catch (error) {
      console.error('Failed to fetch covers:', error)
      handleApiError(error, 'Không thể tải ảnh bìa.')
    } finally {
      setLoadingCovers(false)
    }
  }

  useEffect(() => {
    if (mangaId) {
      fetchCovers()
    }
  }, [mangaId])

  /**
   * Handles upload form submission.
   * @param {UploadCoverArtRequest} data
   */
  const handleUploadSubmit = async (data) => {
    try {
      await mangaApi.uploadMangaCover(mangaId, {
        file: data.file[0], // Access the File object from FileList
        volume: data.volume,
        description: data.description,
      })
      showSuccessToast('Tải ảnh bìa thành công!')
      fetchCovers()
      setOpenUploadDialog(false)
      reset()
    } catch (error) {
      console.error('Failed to upload cover art:', error)
      handleApiError(error, 'Không thể tải ảnh bìa.')
    }
  }

  const handleDeleteRequest = (coverArt) => {
    setCoverArtToDelete(coverArt)
    setOpenConfirmDelete(true)
  }

  const handleConfirmDelete = async () => {
    if (coverArtToDelete) {
      try {
        await coverArtApi.deleteCoverArt(coverArtToDelete.id)
        showSuccessToast('Xóa ảnh bìa thành công!')
        fetchCovers()
      } catch (error) {
        console.error('Failed to delete cover art:', error)
        handleApiError(error, 'Không thể xóa ảnh bìa.')
      } finally {
        setOpenConfirmDelete(false)
        setCoverArtToDelete(null)
      }
    }
  }

  const handleCloseConfirmDelete = () => {
    setOpenConfirmDelete(false)
    setCoverArtToDelete(null)
  }

  return (
    <Box className="cover-art-manager">
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={() => setOpenUploadDialog(true)}
        >
          Tải ảnh bìa mới
        </Button>
      </Box>

      {loadingCovers ? (
        <Box
          sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '200px' }}
        >
          <CircularProgress />
        </Box>
      ) : covers.length === 0 ? (
        <Typography variant="h6" className="no-cover-message">
          Chưa có ảnh bìa nào cho manga này.
        </Typography>
      ) : (
        <Grid container spacing={2} className="cover-art-grid">
          {covers.map((cover) => (
            {/* FIX: Removed 'item' prop */}
            <Grid key={cover.id} xs={12} sm={6} md={4} lg={3}>
              <Card className="cover-art-card">
                <CardMedia
                  component="img"
                  image={`${CLOUDINARY_BASE_URL}${cover.attributes.publicId}`}
                  alt={cover.attributes.description || `Cover for volume ${cover.attributes.volume}`}
                />
                <CardContent>
                  <Typography variant="body2" color="text.secondary">
                    Tập: {cover.attributes.volume || 'N/A'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Mô tả: {cover.attributes.description || 'Không có'}
                  </Typography>
                </CardContent>
                <CardActions className="card-actions">
                  <IconButton
                    color="secondary"
                    onClick={() => handleDeleteRequest(cover)}
                    aria-label="delete"
                  >
                    <DeleteIcon />
                  </IconButton>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Upload Dialog */}
      <Dialog open={openUploadDialog} onClose={() => setOpenUploadDialog(false)}>
        <DialogTitle>Tải ảnh bìa mới</DialogTitle>
        <Box component="form" onSubmit={handleSubmit(handleUploadSubmit)} noValidate>
          <DialogContent>
            <TextField
              margin="dense"
              label="Chọn File ảnh"
              type="file"
              fullWidth
              variant="outlined"
              {...register('file')}
              error={!!errors.file}
              helperText={errors.file?.message}
              inputProps={{ accept: 'image/jpeg,image/png,image/webp' }}
            />
            <TextField
              margin="dense"
              label="Volume (Tùy chọn)"
              type="text"
              fullWidth
              variant="outlined"
              {...register('volume')}
              error={!!errors.volume}
              helperText={errors.volume?.message}
            />
            <TextField
              margin="dense"
              label="Mô tả (Tùy chọn)"
              type="text"
              fullWidth
              multiline
              rows={3}
              variant="outlined"
              {...register('description')}
              error={!!errors.description}
              helperText={errors.description?.message}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenUploadDialog(false)} variant="outlined">
              Hủy
            </Button>
            <Button type="submit" variant="contained" color="primary">
              Tải lên
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <ConfirmDialog
        open={openConfirmDelete}
        onClose={handleCloseConfirmDelete}
        onConfirm={handleConfirmDelete}
        title="Xác nhận xóa ảnh bìa"
        message={`Bạn có chắc chắn muốn xóa ảnh bìa này (Volume: ${coverArtToDelete?.attributes?.volume || 'N/A'})?`}
      />
    </Box>
  )
}

export default CoverArtManager
```

### `src\features\manga\components\MangaForm.jsx`

```javascript
// src\features\manga\components\MangaForm.jsx
import React, { useEffect, useState } from 'react'
import { Box, Button, Grid, Typography, Chip, Autocomplete, TextField, Switch, FormControlLabel } from '@mui/material'
import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createMangaSchema, updateMangaSchema } from '../../../schemas/mangaSchema'
import {
  MANGA_STATUS_OPTIONS,
  PUBLICATION_DEMOGRAPHIC_OPTIONS,
  CONTENT_RATING_OPTIONS,
  ORIGINAL_LANGUAGE_OPTIONS,
  MANGA_STAFF_ROLE_OPTIONS,
} from '../../../constants/appConstants'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import { handleApiError } from '../../../utils/errorUtils'

// ... (typedefs and function signature remain unchanged)

function MangaForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useFormWithZod({
    schema: isEditMode ? updateMangaSchema : createMangaSchema,
    defaultValues: initialData
      ? {
          title: initialData.attributes.title || '',
          originalLanguage: initialData.attributes.originalLanguage || '',
          publicationDemographic: initialData.attributes.publicationDemographic || null,
          status: initialData.attributes.status || 'Ongoing',
          year: initialData.attributes.year || null,
          contentRating: initialData.attributes.contentRating || 'Safe',
          isLocked: initialData.attributes.isLocked || false,
          tagIds: initialData.relationships
            ?.filter((rel) => rel.type === 'tag')
            .map((rel) => rel.id) || [],
          authors: initialData.relationships
            ?.filter((rel) => rel.type === 'author' || rel.type === 'artist')
            .map((rel) => ({
              authorId: rel.id,
              role: rel.type === 'author' ? 'Author' : 'Artist', // Map based on type
            })) || [],
        }
      : {
          title: '',
          originalLanguage: 'ja',
          publicationDemographic: null,
          status: 'Ongoing',
          year: new Date().getFullYear(),
          contentRating: 'Safe',
          isLocked: false,
          tagIds: [],
          authors: [],
        },
  })

  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedAuthors, setSelectedAuthors] = useState([])
  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedTags, setSelectedTags] = useState([])

  const [availableAuthors, setAvailableAuthors] = useState([])
  const [availableTags, setAvailableTags] = useState([])

  const currentAuthors = watch('authors') || []
  const currentTagIds = watch('tagIds') || []
  const isLocked = watch('isLocked')

  // Fetch available authors and tags
  useEffect(() => {
    const fetchDropdownData = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 }) // Fetch all or paginate
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name })))

        const tagsResponse = await tagApi.getTags({ limit: 1000 }) // Fetch all or paginate
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name })))
      } catch (error) {
        handleApiError(error, 'Không thể tải dữ liệu tác giả/tag.');
      }
    }
    fetchDropdownData()
  }, [])

  // Populate selected authors/tags when initialData is loaded
  useEffect(() => {
    if (initialData && availableAuthors.length > 0 && availableTags.length > 0) {
      const initialAuthorRelationships = initialData.relationships
        ?.filter((rel) => rel.type === 'author' || rel.type === 'artist')
        .map((rel) => ({
          authorId: rel.id,
          role: rel.type === 'author' ? 'Author' : 'Artist',
        })) || [];

      const hydratedAuthors = initialAuthorRelationships
        .map((rel) => {
          const author = availableAuthors.find((a) => a.id === rel.authorId)
          return author ? { ...author, role: rel.role } : null
        })
        .filter(Boolean)

      setSelectedAuthors(hydratedAuthors)
      setValue('authors', initialAuthorRelationships)

      const initialTagIds = initialData.relationships
        ?.filter((rel) => rel.type === 'tag')
        .map((rel) => rel.id) || [];

      const hydratedTags = initialTagIds
        .map((tagId) => {
          const tag = availableTags.find((t) => t.id === tagId)
          return tag ? { id: tag.id, name: tag.name } : null
        })
        .filter(Boolean)

      setSelectedTags(hydratedTags)
      setValue('tagIds', initialTagIds)
    }
  }, [initialData, availableAuthors, availableTags, setValue])

  /**
   * @param {SelectedRelationship} author
   * @param {'Author' | 'Artist'} role
   */
  const handleAddAuthor = (author, role) => {
    if (!author) return
    const newAuthorEntry = { authorId: author.id, role }
    // Check if author with this ID and role already exists
    if (!currentAuthors.some(
      (a) => a.authorId === newAuthorEntry.authorId && a.role === newAuthorEntry.role
    )) {
      const updatedAuthors = [...currentAuthors, newAuthorEntry]
      setValue('authors', updatedAuthors)
      setSelectedAuthors((prev) => [...prev, { ...author, role }])
    }
  }

  /**
   * @param {string} authorIdToRemove
   * @param {'Author' | 'Artist'} roleToRemove
   */
  const handleRemoveAuthor = (authorIdToRemove, roleToRemove) => {
    const updatedAuthors = currentAuthors.filter(
      (a) => !(a.authorId === authorIdToRemove && a.role === roleToRemove)
    )
    setValue('authors', updatedAuthors)
    setSelectedAuthors((prev) =>
      prev.filter(
        (a) => !(a.id === authorIdToRemove && a.role === roleToRemove)
      )
    )
  }

  /**
   * @param {SelectedRelationship} tag
   */
  const handleAddTag = (tag) => {
    if (!tag || currentTagIds.includes(tag.id)) return
    const updatedTags = [...currentTagIds, tag.id]
    setValue('tagIds', updatedTags)
    setSelectedTags((prev) => [...prev, tag])
  }

  /**
   * @param {string} tagIdToRemove
   */
  const handleRemoveTag = (tagIdToRemove) => {
    const updatedTags = currentTagIds.filter((id) => id !== tagIdToRemove)
    setValue('tagIds', updatedTags)
    setSelectedTags((prev) => prev.filter((t) => t.id !== tagIdToRemove))
  }

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ mt: 1 }}>
      <Grid container spacing={2}>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12}>
          <FormInput control={control} name="title" label="Tiêu đề Manga" />
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6}>
          <FormInput
            control={control}
            name="originalLanguage"
            label="Ngôn ngữ gốc"
            type="select"
            options={ORIGINAL_LANGUAGE_OPTIONS}
          />
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6}>
          <FormInput
            control={control}
            name="publicationDemographic"
            label="Đối tượng xuất bản"
            type="select"
            options={PUBLICATION_DEMOGRAPHIC_OPTIONS}
          />
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6}>
          <FormInput
            control={control}
            name="status"
            label="Trạng thái"
            type="select"
            options={MANGA_STATUS_OPTIONS}
          />
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12} sm={6}>
          <FormInput
            control={control}
            name="year"
            label="Năm xuất bản"
            type="number"
            inputProps={{ min: 1000, max: new Date().getFullYear(), step: 1 }}
          />
        </Grid>
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12}>
          <FormInput
            control={control}
            name="contentRating"
            label="Đánh giá nội dung"
            type="select"
            options={CONTENT_RATING_OPTIONS}
          />
        </Grid>

        {/* Authors Section */}
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12}>
          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
            Tác giả / Họa sĩ
          </Typography>
          <Grid container spacing={1} alignItems="center">
            {/* FIX: Removed 'item' prop */}
            <Grid xs={6}>
              <Autocomplete
                options={availableAuthors}
                getOptionLabel={(option) => option.name}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                onChange={(event, newValue) => {
                  if (newValue) {
                    // This is a temp variable, role is added in handleAddAuthor
                    // We need to keep this simple for Autocomplete, role added when adding
                  }
                }}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Chọn tác giả/họa sĩ"
                    error={!!errors.authors}
                    helperText={errors.authors?.message}
                  />
                )}
              />
            </Grid>
            {/* FIX: Removed 'item' prop */}
            <Grid xs={3}>
              <FormInput
                control={control}
                name="tempAuthorRole" // Temporary field for role selection
                label="Vai trò"
                type="select"
                options={MANGA_STAFF_ROLE_OPTIONS}
                defaultValue="Author"
                size="small"
              />
            </Grid>
            {/* FIX: Removed 'item' prop */}
            <Grid xs={3}>
              <Button
                variant="contained"
                color="primary"
                onClick={() => {
                  const selectedAuthor = control._formValues.tempAuthorRole // Access direct value
                    ? availableAuthors.find(a => a.id === control._formValues.tempAuthorId) // Assume tempAuthorId if direct input
                    : null;
                  const role = control._formValues.tempAuthorRole || 'Author'; // Default role
                  const authorInput = document.querySelector('input[aria-expanded][role="combobox"]'); // Get autocomplete input

                  if (authorInput && authorInput.value) {
                    const selectedOption = availableAuthors.find(opt => opt.name === authorInput.value);
                    if (selectedOption) {
                      handleAddAuthor(selectedOption, role);
                      authorInput.value = ''; // Clear autocomplete input
                    }
                  }
                }}
                startIcon={<AddIcon />}
                fullWidth
              >
                Thêm
              </Button>
            </Grid>
          </Grid>
          <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {selectedAuthors.map((author, index) => (
              <Chip
                key={`${author.id}-${author.role}-${index}`} // Use a unique key
                label={`${author.name} (${author.role})`}
                onDelete={() => handleRemoveAuthor(author.id, author.role)}
                deleteIcon={<DeleteIcon />}
                color="primary"
                variant="outlined"
              />
            ))}
          </Box>
        </Grid>

        {/* Tags Section */}
        {/* FIX: Removed 'item' prop */}
        <Grid xs={12}>
          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
            Tags
          </Typography>
          <Autocomplete
            multiple
            options={availableTags}
            getOptionLabel={(option) => option.name}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            value={selectedTags}
            onChange={(event, newValue) => {
              setSelectedTags(newValue);
              setValue('tagIds', newValue.map(tag => tag.id));
            }}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Chọn Tags"
                error={!!errors.tagIds}
                helperText={errors.tagIds?.message}
              />
            )}
            renderTags={(value, getTagProps) =>
              value.map((option, index) => (
                <Chip label={option.name} {...getTagProps({ index })} onDelete={() => handleRemoveTag(option.id)} />
              ))
            }
          />
        </Grid>
        
        {/* Is Locked Switch */}
        {isEditMode && (
          // FIX: Removed 'item' prop
          <Grid xs={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={isLocked}
                  onChange={(e) => setValue('isLocked', e.target.checked)}
                  name="isLocked"
                  color="primary"
                />
              }
              label="Khóa Manga (Không cho phép đọc)"
              sx={{ mt: 2 }}
            />
          </Grid>
        )}

        {/* FIX: Removed 'item' prop */}
        <Grid xs={12}>
          <Button type="submit" variant="contained" color="primary" sx={{ mt: 3, mb: 2 }}>
            {isEditMode ? 'Cập nhật Manga' : 'Tạo Manga'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  )
}

export default MangaForm
```

### `src\features\tag\components\TagForm.jsx`

```javascript
// src\features\tag\components\TagForm.jsx
import React, { useEffect, useState } from 'react'
import { Box, Button, Grid, Paper, CircularProgress } from '@mui/material'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createTagSchema, updateTagSchema } from '../../../schemas/tagSchema'
import useTagGroupStore from '../../../stores/tagGroupStore'
import { handleApiError } from '../../../utils/errorUtils'

// ... (typedefs and function signature remain unchanged)

function TagForm({ initialData, onSubmit, isEditMode }) {
  const { tagGroups, fetchTagGroups } = useTagGroupStore()
  const [loadingTagGroups, setLoadingTagGroups] = useState(true)

  useEffect(() => {
    const loadTagGroups = async () => {
      setLoadingTagGroups(true)
      try {
        await fetchTagGroups(true) // Fetch all tag groups, reset pagination
      } catch (error) {
        console.error('Failed to load tag groups for TagForm:', error)
        handleApiError(error, 'Không thể tải danh sách nhóm tag.')
      } finally {
        setLoadingTagGroups(false)
      }
    }
    loadTagGroups()
  }, [fetchTagGroups])

  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateTagSchema : createTagSchema,
    defaultValues: initialData
      ? {
          name: initialData.attributes.name || '',
          tagGroupId: initialData.attributes.tagGroupId || '',
        }
      : {
          name: '',
          tagGroupId: '', // Default to empty
        },
  })

  // Reset form when initialData or isEditMode changes
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        name: initialData.attributes.name || '',
        tagGroupId: initialData.attributes.tagGroupId || '',
      })
    } else if (!isEditMode) {
      reset({
        name: '',
        tagGroupId: '',
      })
    }
  }, [initialData, isEditMode, reset])

  const tagGroupOptions = tagGroups.map((group) => ({
    value: group.id,
    label: group.attributes.name,
  }))

  if (loadingTagGroups) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
        <CircularProgress />
      </Box>
    )
  }

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2}>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput control={control} name="name" label="Tên tag" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput
              control={control}
              name="tagGroupId"
              label="Nhóm tag"
              type="select"
              options={tagGroupOptions}
              // Disabled in edit mode if you don't want to allow changing tag group after creation
              // disabled={isEditMode}
            />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Tag' : 'Tạo Tag'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  )
}

export default TagForm
```

### `src\features\tagGroup\components\TagGroupForm.jsx`

```javascript
// src\features\tagGroup\components\TagGroupForm.jsx
import React, { useEffect } from 'react'
import { Box, Button, Grid, Paper } from '@mui/material'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createTagGroupSchema, updateTagGroupSchema } from '../../../schemas/tagGroupSchema'

// ... (typedefs and function signature remain unchanged)

function TagGroupForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateTagGroupSchema : createTagGroupSchema,
    defaultValues: initialData
      ? {
          name: initialData.attributes.name || '',
        }
      : {
          name: '',
        },
  })

  // Reset form when initialData or isEditMode changes
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        name: initialData.attributes.name || '',
      })
    } else if (!isEditMode) {
      reset({
        name: '',
      })
    }
  }, [initialData, isEditMode, reset])

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2}>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput control={control} name="name" label="Tên nhóm tag" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Nhóm tag' : 'Tạo Nhóm tag'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  )
}

export default TagGroupForm
```

### `src\features\translatedManga\components\TranslatedMangaForm.jsx`

```javascript
// src\features\translatedManga\components\TranslatedMangaForm.jsx
import React, { useEffect } from 'react'
import { Box, Button, Grid, Paper, Typography } from '@mui/material'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createTranslatedMangaSchema, updateTranslatedMangaSchema } from '../../../schemas/translatedMangaSchema'
import { ORIGINAL_LANGUAGE_OPTIONS } from '../../../constants/appConstants'

// ... (typedefs and function signature remain unchanged)

function TranslatedMangaForm({ mangaId, initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateTranslatedMangaSchema : createTranslatedMangaSchema,
    defaultValues: initialData
      ? {
          languageKey: initialData.attributes.languageKey || '',
          title: initialData.attributes.title || '',
          description: initialData.attributes.description || '',
        }
      : {
          mangaId: mangaId, // Only for creation
          languageKey: 'en', // Default to English
          title: '',
          description: '',
        },
  })

  // Reset form when initialData or mangaId changes (e.g., when switching between edit/create or different mangas)
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        languageKey: initialData.attributes.languageKey || '',
        title: initialData.attributes.title || '',
        description: initialData.attributes.description || '',
      })
    } else if (!isEditMode && mangaId) {
      reset({
        mangaId: mangaId,
        languageKey: 'en',
        title: '',
        description: '',
      })
    }
  }, [initialData, isEditMode, mangaId, reset])

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2}>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12} sm={6}>
            <FormInput
              control={control}
              name="languageKey"
              label="Ngôn ngữ bản dịch"
              type="select"
              options={ORIGINAL_LANGUAGE_OPTIONS}
              // Disabled if in edit mode to prevent changing languageKey after creation
              disabled={isEditMode}
            />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12} sm={6}>
            <FormInput control={control} name="title" label="Tiêu đề bản dịch" />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <FormInput
              control={control}
              name="description"
              label="Mô tả bản dịch (Tùy chọn)"
              multiline
              rows={4}
            />
          </Grid>
          {/* FIX: Removed 'item' prop */}
          <Grid xs={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Bản dịch' : 'Tạo Bản dịch'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  )
}

export default TranslatedMangaForm
```
