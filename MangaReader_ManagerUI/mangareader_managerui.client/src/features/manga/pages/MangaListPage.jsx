import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import {
    Autocomplete,
    Box,
    Button,
    Chip,
    Grid, // Giữ lại Grid, nhưng cách sử dụng sẽ thay đổi
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
  } = useMangaStore()

  const isLoading = useUiStore(state => state.isLoading);

  /** @type {[AuthorForFilter[], React.Dispatch<React.SetStateAction<AuthorForFilter[]>>]} */
  const [availableAuthors, setAvailableAuthors] = useState([])
  /** @type {[TagForFilter[], React.Dispatch<React.SetStateAction<TagForFilter[]>>]} */
  const [availableTags, setAvailableTags] = useState([])
  
  const [localFilters, setLocalFilters] = useState(filters);

  useEffect(() => {
    setLocalFilters(filters); 
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
    applyFilters(localFilters); 
    fetchMangas(true); 
  }

  const handleResetLocalFilters = () => {
    resetFilters(); 
    setLocalFilters(useMangaStore.getState().filters); 
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
  
  const renderMultiSelectDisplay = (selectedItems, getTagProps, maxItemsToShow = 2) => {
    const numItems = selectedItems.length;
    const itemsToRender = selectedItems.slice(0, maxItemsToShow);
    
    let displayChips = itemsToRender.map((item, index) => {
      const label = typeof item === 'object' ? (item.name || item.label) : item;
      const key = typeof item === 'object' ? (item.id || item.value || index) : item;

      return (
        <Chip 
          variant="outlined" 
          label={label}
          size="small" 
          {...(getTagProps ? getTagProps({ index }) : {})} 
          key={key} 
          sx={{ 
            maxWidth: '120px', 
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
            mr: 0.5, 
            '&:last-child': {
                mr: (numItems <= maxItemsToShow && index === itemsToRender.length -1) ? 0 : 0.5,
            }
          }}
        />
      );
    });

    if (numItems > maxItemsToShow) {
      displayChips.push(
        <Chip 
          variant="outlined" 
          label={`+${numItems - maxItemsToShow}`} 
          size="small" 
          key="more-items" 
        />
      );
    }
    return displayChips;
  };


  return (
    <Box className="manga-list-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Quản lý Manga
      </Typography>
      <Box className="filter-section">
        {/* 
          Grid v2: 
          - Bỏ `item` prop.
          - Sử dụng trực tiếp `xs`, `sm`, `md`, `lg` trên `Grid` (nếu nó là item).
          - `Grid container` sẽ có `display: grid` theo mặc định.
            Để đạt được 3 cột, chúng ta sẽ sử dụng `gridTemplateColumns` hoặc
            để các `Grid` items tự xác định kích thước dựa trên props breakpoint.
        */}
        <Grid 
          container 
          spacing={2} 
          alignItems="stretch"
          // Không cần columns prop với Grid v2, nó sẽ tự chia dựa trên tổng số cột (12)
        >
          {/* Dòng 1 */}
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}> {/* Thay vì Grid item, trực tiếp đặt props breakpoint */}
            <TextField
              label="Lọc theo Tiêu đề"
              variant="outlined"
              fullWidth
              value={localFilters.titleFilter || ''}
              onChange={(e) => handleLocalFilterChange('titleFilter', e.target.value)}
            />
          </Grid>
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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

          {/* Dòng 2 */}
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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
                    {renderMultiSelectDisplay(
                      selected.map(val => PUBLICATION_DEMOGRAPHIC_OPTIONS.find(opt => opt.value === val) || { value: val, label: val }),
                      null, 
                      1 
                    )}
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
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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

          {/* Dòng 3 */}
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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
              renderTags={(value, getTagProps) => renderMultiSelectDisplay(value, getTagProps, 1)}
              fullWidth
            />
          </Grid>
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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
              renderTags={(value, getTagProps) => renderMultiSelectDisplay(value, getTagProps, 1)}
              fullWidth
            />
          </Grid>
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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

          {/* Dòng 4 */}
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 4
            }}>
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
              renderTags={(value, getTagProps) => renderMultiSelectDisplay(value, getTagProps, 1)}
              fullWidth
            />
          </Grid>
           <Grid
             size={{
               xs: 12,
               sm: 6,
               md: 4
             }}>
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
            </TextField>
          </Grid>
          <Grid
            sx={{ display: 'flex', gap: 2, alignItems: 'center', justifyContent: { xs: 'stretch', md: 'flex-end' } }}
            size={{
              xs: 12,
              sm: 12,
              md: 4
            }}>
             <Button
              variant="contained"
              color="primary"
              startIcon={<SearchIcon />}
              onClick={handleApplyLocalFilters}
              sx={{ flexGrow: { xs: 1, md: 0 }, height: '56px' }}  
            >
              Áp dụng
            </Button>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ClearIcon />}
              onClick={handleResetLocalFilters}
              sx={{ flexGrow: { xs: 1, md: 0 }, height: '56px' }} 
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
  );
}

export default MangaListPage