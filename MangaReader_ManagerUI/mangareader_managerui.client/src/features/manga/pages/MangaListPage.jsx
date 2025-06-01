import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import {
    Autocomplete,
    Box,
    Button,
    Chip,
    Grid,
    MenuItem,
    TextField,
    Typography,
} from '@mui/material'
import React, { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import {
    CONTENT_RATING_OPTIONS,
    MANGA_STATUS_OPTIONS,
    ORIGINAL_LANGUAGE_OPTIONS,
    PUBLICATION_DEMOGRAPHIC_OPTIONS,
} from '../../../constants/appConstants'
import useMangaStore from '../../../stores/mangaStore'
import useUiStore from '../../../stores/uiStore'
import { handleApiError } from '../../../utils/errorUtils'
import MangaTable from '../components/MangaTable'

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
    filters = {},
    sort = {},
    fetchMangas,
    setPage,
    setRowsPerPage,
    setSort,
    applyFilters,
    resetFilters,
    deleteManga,
    setFilter,
  } = useMangaStore()

  const isLoading = useUiStore(state => state.isLoading);

  const [availableAuthors, setAvailableAuthors] = useState([])
  const [availableTags, setAvailableTags] = useState([])

  useEffect(() => {
    // Initial fetch of mangas when component mounts
    fetchMangas(true); // Reset pagination on initial load
  }, [fetchMangas]);

  // Fetch available authors and tags for filters
  useEffect(() => {
    const fetchFilterOptions = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 });
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name })));

        const tagsResponse = await tagApi.getTags({ limit: 1000 });
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name })));
      } catch (error) {
        handleApiError(error, 'Không thể tải tùy chọn lọc.');
      }
    };
    fetchFilterOptions();
  }, []);

  const handleApplyFilters = () => {
    // Gọi applyFilters với các filter hiện tại trong store.
    // Hành động applyFilters trong store đã được cấu hình để reset page và fetch.
    applyFilters({
      titleFilter: filters.titleFilter,
      statusFilter: filters.statusFilter,
      contentRatingFilter: filters.contentRatingFilter,
      demographicFilter: filters.demographicFilter,
      originalLanguageFilter: filters.originalLanguageFilter,
      yearFilter: filters.yearFilter,
      tagIdsFilter: filters.tagIdsFilter,
      authorIdsFilter: filters.authorIdsFilter,
    });
    fetchMangas(true);
  }

  const handleResetFilters = () => {
    resetFilters();
  }

  return (
    <Box className="manga-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Manga
      </Typography>

      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-end" columns={{ xs: 4, sm: 6, md: 12 }}>
          <Grid item xs={4} sm={3} md={3}>
            <TextField
              label="Lọc theo Tiêu đề"
              variant="outlined"
              fullWidth
              value={filters.titleFilter || ''}
              onChange={(e) => setFilter('titleFilter', e.target.value)}
            />
          </Grid>
          <Grid item xs={4} sm={3} md={2}>
            <TextField
              select
              label="Trạng thái"
              variant="outlined"
              fullWidth
              value={filters.statusFilter || ''}
              onChange={(e) => setFilter('statusFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {MANGA_STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={4} sm={3} md={2}>
            <TextField
              select
              label="Đánh giá"
              variant="outlined"
              fullWidth
              value={filters.contentRatingFilter || ''}
              onChange={(e) => setFilter('contentRatingFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {CONTENT_RATING_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={4} sm={3} md={2}>
            <TextField
              select
              label="Đối tượng"
              variant="outlined"
              fullWidth
              value={filters.demographicFilter || ''}
              onChange={(e) => setFilter('demographicFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {PUBLICATION_DEMOGRAPHIC_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={4} sm={3} md={2}>
            <TextField
              select
              label="Ngôn ngữ gốc"
              variant="outlined"
              fullWidth
              value={filters.originalLanguageFilter || ''}
              onChange={(e) => setFilter('originalLanguageFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {ORIGINAL_LANGUAGE_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={4} sm={3} md={1}>
            <TextField
              label="Năm"
              variant="outlined"
              fullWidth
              type="number"
              value={filters.yearFilter || ''}
              onChange={(e) => setFilter('yearFilter', e.target.value === '' ? null : parseInt(e.target.value, 10))}
              inputProps={{ min: 1000, max: new Date().getFullYear(), step: 1 }}
            />
          </Grid>
          <Grid item xs={4} sm={3} md={3}>
            <Autocomplete
              multiple
              options={availableAuthors}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                filters.authorIdsFilter && availableAuthors.length > 0
                  ? availableAuthors.filter(a => filters.authorIdsFilter.includes(a.id))
                  : []
              }
              onChange={(event, newValue) => {
                setFilter('authorIdsFilter', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Tác giả" />}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
            />
          </Grid>
          <Grid item xs={4} sm={3} md={3}>
            <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                filters.tagIdsFilter && availableTags.length > 0
                  ? availableTags.filter(t => filters.tagIdsFilter.includes(t.id))
                  : []
              }
              onChange={(event, newValue) => {
                setFilter('tagIdsFilter', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Tags" />}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
            />
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