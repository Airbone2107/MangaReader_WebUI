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
    FormControl,
    InputLabel,
    Select,
    OutlinedInput,
    Checkbox,
    ListItemText,
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
 * @typedef {import('../../../types/manga').Author} AuthorForFilter
 * @typedef {import('../../../types/manga').Tag} TagForFilter
 * @typedef {import('../../../types/manga').PublicationDemographicType} PublicationDemographicType
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
    setFilter,
  } = useMangaStore()

  const isLoading = useUiStore(state => state.isLoading);

  /** @type {[AuthorForFilter[], React.Dispatch<React.SetStateAction<AuthorForFilter[]>>]} */
  const [availableAuthors, setAvailableAuthors] = useState([])
  /** @type {[TagForFilter[], React.Dispatch<React.SetStateAction<TagForFilter[]>>]} */
  const [availableTags, setAvailableTags] = useState([])
  
  // Local state cho các filter phức tạp nếu cần, hoặc lấy trực tiếp từ store.filters
  const [localFilters, setLocalFilters] = useState(filters);

  useEffect(() => {
    setLocalFilters(filters); // Đồng bộ local state khi filter store thay đổi
  }, [filters]);

  useEffect(() => {
    fetchMangas(true); 
  }, [fetchMangas]);

  useEffect(() => {
    const fetchFilterOptions = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 });
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name, type: 'author' })))

        const tagsResponse = await tagApi.getTags({ limit: 1000 });
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name, type: 'tag' })));
      } catch (error) {
        handleApiError(error, 'Không thể tải tùy chọn lọc.');
      }
    };
    fetchFilterOptions();
  }, []);

  const handleLocalFilterChange = (filterName, value) => {
    setLocalFilters(prev => ({ ...prev, [filterName]: value }));
  };

  const handleApplyLocalFilters = () => {
    applyFilters(localFilters); // Gửi toàn bộ localFilters cho store action
    fetchMangas(true); // Fetch lại dữ liệu với filter mới
  }

  const handleResetLocalFilters = () => {
    resetFilters(); // Action này đã tự fetch lại
    setLocalFilters(useMangaStore.getState().filters); // Cập nhật local state
  }

  const ITEM_HEIGHT = 48;
  const ITEM_PADDING_TOP = 8;
  const MenuProps = {
    PaperProps: {
      style: {
        maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
        width: 250,
      },
    },
  };

  return (
    <Box className="manga-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Manga
      </Typography>

      {/* Filter Section */}
      <Box className="filter-section">
        <Grid container spacing={2} alignItems="flex-start">
          {/* Title Filter */}
          <Grid item xs={12} sm={6} md={4} lg={3}>
            <TextField
              label="Lọc theo Tiêu đề"
              variant="outlined"
              fullWidth
              value={localFilters.titleFilter || ''}
              onChange={(e) => handleLocalFilterChange('titleFilter', e.target.value)}
            />
          </Grid>
          {/* Status Filter */}
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Trạng thái"
              variant="outlined"
              fullWidth
              value={localFilters.statusFilter || ''}
              onChange={(e) => handleLocalFilterChange('statusFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {MANGA_STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          {/* Content Rating Filter */}
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Đánh giá"
              variant="outlined"
              fullWidth
              value={localFilters.contentRatingFilter || ''}
              onChange={(e) => handleLocalFilterChange('contentRatingFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {CONTENT_RATING_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          {/* Publication Demographics Filter (NEW) - MultiSelect */}
          <Grid item xs={12} sm={6} md={4} lg={3}>
            <FormControl fullWidth variant="outlined">
              <InputLabel id="publication-demographics-filter-label">Đối tượng</InputLabel>
              <Select
                labelId="publication-demographics-filter-label"
                multiple
                value={localFilters.publicationDemographicsFilter || []}
                onChange={(e) => handleLocalFilterChange('publicationDemographicsFilter', e.target.value)}
                input={<OutlinedInput label="Đối tượng" />}
                renderValue={(selected) => (
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {(selected).map((value) => {
                      const label = PUBLICATION_DEMOGRAPHIC_OPTIONS.find(opt => opt.value === value)?.label || value;
                      return <Chip key={value} label={label} size="small" />;
                    })}
                  </Box>
                )}
                MenuProps={MenuProps}
              >
                {PUBLICATION_DEMOGRAPHIC_OPTIONS.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    <Checkbox checked={(localFilters.publicationDemographicsFilter || []).indexOf(option.value) > -1} />
                    <ListItemText primary={option.label} />
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          {/* Original Language Filter */}
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Ngôn ngữ gốc"
              variant="outlined"
              fullWidth
              value={localFilters.originalLanguageFilter || ''}
              onChange={(e) => handleLocalFilterChange('originalLanguageFilter', e.target.value)}
            >
              <MenuItem value="">Tất cả</MenuItem>
              {ORIGINAL_LANGUAGE_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          {/* Year Filter */}
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              label="Năm"
              variant="outlined"
              fullWidth
              type="number"
              value={localFilters.yearFilter || ''}
              onChange={(e) => handleLocalFilterChange('yearFilter', e.target.value === '' ? null : parseInt(e.target.value, 10))}
              inputProps={{ min: 1000, max: new Date().getFullYear() + 5, step: 1 }}
            />
          </Grid>
          {/* AuthorIds Filter */}
          <Grid item xs={12} sm={6} md={4} lg={3}>
            <Autocomplete
              multiple
              options={availableAuthors}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                (localFilters.authorIdsFilter && availableAuthors.length > 0)
                  ? availableAuthors.filter(a => localFilters.authorIdsFilter.includes(a.id))
                  : []
              }
              onChange={(event, newValue) => {
                handleLocalFilterChange('authorIdsFilter', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Lọc theo Tác giả" variant="outlined" />}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip label={option.name} {...getTagProps({ index })} />
                ))
              }
              fullWidth
            />
          </Grid>
          {/* Included Tags Filter (NEW) */}
          <Grid item xs={12} sm={6} md={4} lg={3}>
            <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                (localFilters.includedTags && availableTags.length > 0)
                  ? availableTags.filter(t => localFilters.includedTags.includes(t.id))
                  : []
              }
              onChange={(event, newValue) => {
                handleLocalFilterChange('includedTags', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Tags Phải Có" variant="outlined" />}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Chế độ Tags Phải Có"
              variant="outlined"
              fullWidth
              value={localFilters.includedTagsMode || 'AND'}
              onChange={(e) => handleLocalFilterChange('includedTagsMode', e.target.value)}
              disabled={!localFilters.includedTags || localFilters.includedTags.length === 0}
            >
              <MenuItem value="AND">VÀ (Tất cả)</MenuItem>
              <MenuItem value="OR">HOẶC (Bất kỳ)</MenuItem>
            </TextField>
          </Grid>
           {/* Excluded Tags Filter (NEW) */}
           <Grid item xs={12} sm={6} md={4} lg={3}>
            <Autocomplete
              multiple
              options={availableTags}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                (localFilters.excludedTags && availableTags.length > 0)
                  ? availableTags.filter(t => localFilters.excludedTags.includes(t.id))
                  : []
              }
              onChange={(event, newValue) => {
                handleLocalFilterChange('excludedTags', newValue.map(item => item.id));
              }}
              renderInput={(params) => <TextField {...params} label="Tags Không Có" variant="outlined" />}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={4} lg={2}>
            <TextField
              select
              label="Chế độ Tags Không Có"
              variant="outlined"
              fullWidth
              value={localFilters.excludedTagsMode || 'OR'}
              onChange={(e) => handleLocalFilterChange('excludedTagsMode', e.target.value)}
              disabled={!localFilters.excludedTags || localFilters.excludedTags.length === 0}
            >
              <MenuItem value="OR">HOẶC (Bất kỳ)</MenuItem>
              <MenuItem value="AND">VÀ (Tất cả)</MenuItem>
            </TextField>
          </Grid>

          {/* Action Buttons */}
          <Grid item xs={12} sm={6} md={2} lg={2} sx={{ display: 'flex', alignItems: 'center' }}>
            <Button
              variant="contained"
              color="primary"
              startIcon={<SearchIcon />}
              onClick={handleApplyLocalFilters}
              fullWidth
              sx={{ height: '56px' }} 
            >
              Áp dụng
            </Button>
          </Grid>
          <Grid item xs={12} sm={6} md={2} lg={2} sx={{ display: 'flex', alignItems: 'center' }}>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ClearIcon />}
              onClick={handleResetLocalFilters}
              fullWidth
              sx={{ height: '56px' }}
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
        onPageChange={(event, newPageVal) => setPage(event, newPageVal)}
        onRowsPerPageChange={(event) => setRowsPerPage(event)}
        onSort={(orderBy, orderDir) => setSort(orderBy, orderDir)}
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