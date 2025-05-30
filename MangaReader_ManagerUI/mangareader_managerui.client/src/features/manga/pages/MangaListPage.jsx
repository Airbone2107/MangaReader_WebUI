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

  const isLoading = useUiStore(state => state.loading);

  // State for filter inputs (controlled components)
  const [localTitleFilter, setLocalTitleFilter] = useState(filters.titleFilter)
  const [localStatusFilter, setLocalStatusFilter] = useState(filters.statusFilter)
  const [localContentRatingFilter, setLocalContentRatingFilter] = useState(filters.contentRatingFilter)
  const [localDemographicFilter, setLocalDemographicFilter] = useState(filters.demographicFilter)
  const [localOriginalLanguageFilter, setLocalOriginalLanguageFilter] = useState(filters.originalLanguageFilter)
  const [localYearFilter, setLocalYearFilter] = useState(filters.yearFilter)
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
    setLocalTitleFilter(filters.titleFilter);
    setLocalStatusFilter(filters.statusFilter);
    setLocalContentRatingFilter(filters.contentRatingFilter);
    setLocalDemographicFilter(filters.demographicFilter);
    setLocalOriginalLanguageFilter(filters.originalLanguageFilter);
    setLocalYearFilter(filters.yearFilter);
    // When filters change globally, update local selected relationships for Autocomplete
    // This requires mapping filter IDs back to full objects using availableAuthors/Tags
    if (availableAuthors.length > 0 && filters.authorIdsFilter) {
      setLocalSelectedAuthorFilters(availableAuthors.filter(a => filters.authorIdsFilter.includes(a.id)));
    }
    if (availableTags.length > 0 && filters.tagIdsFilter) {
      setLocalSelectedTagFilters(availableTags.filter(t => filters.tagIdsFilter.includes(t.id)));
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
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              label="Lọc theo Tiêu đề"
              variant="outlined"
              fullWidth
              value={localTitleFilter}
              onChange={(e) => setLocalTitleFilter(e.target.value)}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
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
          <Grid item xs={12} sm={6} md={2}>
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
          <Grid item xs={12} sm={6} md={2}>
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
          <Grid item xs={12} sm={6} md={2}>
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
          <Grid item xs={12} sm={6} md={1}>
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
          <Grid item xs={12} sm={6} md={3}>
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
          <Grid item xs={12} sm={6} md={3}>
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
          <Grid item xs={12} sm={6} md={2}>
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
          <Grid item xs={12} sm={6} md={2}>
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