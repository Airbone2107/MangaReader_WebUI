import { Add as AddIcon, CheckBox as CheckBoxIcon, CheckBoxOutlineBlank as CheckBoxOutlineBlankIcon, Delete as DeleteIcon } from '@mui/icons-material'
import { Autocomplete, Box, Button, Checkbox, Chip, FormControlLabel, Grid, Paper, Switch, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import FormInput from '../../../components/common/FormInput'
import {
    CONTENT_RATING_OPTIONS,
    MANGA_STAFF_ROLE_OPTIONS,
    MANGA_STATUS_OPTIONS,
    ORIGINAL_LANGUAGE_OPTIONS,
    PUBLICATION_DEMOGRAPHIC_OPTIONS,
} from '../../../constants/appConstants'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createMangaSchema, updateMangaSchema } from '../../../schemas/mangaSchema'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Manga} Manga
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').Tag} Tag
 * @typedef {import('../../../types/manga').MangaAuthorInput} MangaAuthorInput
 * @typedef {import('../../../types/manga').SelectedRelationship} SelectedRelationship
 */

/**
 * MangaForm component for creating or editing manga.
 * @param {object} props
 * @param {Manga} [props.initialData] - Initial data for editing.
 * @param {function(CreateMangaRequest | UpdateMangaRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
function MangaForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
    getValues,
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
              role: rel.type === 'author' ? 'Author' : 'Artist',
            })) || [],
          tempAuthor: null,
          tempAuthorRole: 'Author',
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
          tempAuthor: null,
          tempAuthorRole: 'Author',
        },
  })

  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedAuthorsVisual, setSelectedAuthorsVisual] = useState([])
  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedTagsVisual, setSelectedTagsVisual] = useState([])

  const [availableAuthors, setAvailableAuthors] = useState([])
  const [availableTags, setAvailableTags] = useState([])

  const currentAuthorsFormValue = watch('authors') || []
  const currentTagIdsFormValue = watch('tagIds') || []
  const isLocked = watch('isLocked')

  // State cho Autocomplete chọn tác giả (không phải là một phần của form data chính thức)
  const [tempSelectedAuthor, setTempSelectedAuthor] = useState(null);

  // Fetch available authors and tags
  useEffect(() => {
    const fetchDropdownData = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 })
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name })))

        const tagsResponse = await tagApi.getTags({ limit: 1000 })
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name, tagGroupId: t.attributes.tagGroupId, tagGroupName: t.attributes.tagGroupName })))
      } catch (error) {
        handleApiError(error, 'Không thể tải dữ liệu tác giả/tag.');
      }
    }
    fetchDropdownData()
  }, [])

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

      setSelectedAuthorsVisual(hydratedAuthors)

      const initialTagIds = initialData.relationships
        ?.filter((rel) => rel.type === 'tag')
        .map((rel) => rel.id) || [];

      const hydratedTags = initialTagIds
        .map((tagId) => {
          const tag = availableTags.find((t) => t.id === tagId)
          return tag ? { id: tag.id, name: tag.name, tagGroupName: tag.tagGroupName || 'N/A' } : null
        })
        .filter(Boolean)

      setSelectedTagsVisual(hydratedTags)
    }
  }, [initialData, availableAuthors, availableTags, setValue])

  // Cập nhật selectedAuthorsVisual khi currentAuthorsFormValue thay đổi
  useEffect(() => {
    const hydratedAuthors = currentAuthorsFormValue
      .map((formAuthor) => {
        const authorDetails = availableAuthors.find(a => a.id === formAuthor.authorId);
        return authorDetails ? { ...authorDetails, role: formAuthor.role } : null;
      })
      .filter(Boolean);
    setSelectedAuthorsVisual(hydratedAuthors);
  }, [currentAuthorsFormValue, availableAuthors]);

  // Cập nhật selectedTagsVisual khi currentTagIdsFormValue thay đổi
  useEffect(() => {
    const hydratedTags = currentTagIdsFormValue
      .map((tagId) => {
        const tagDetails = availableTags.find(t => t.id === tagId);
        return tagDetails ? { id: tagDetails.id, name: tagDetails.name, tagGroupName: tagDetails.tagGroupName } : null;
      })
      .filter(Boolean);
    setSelectedTagsVisual(hydratedTags);
  }, [currentTagIdsFormValue, availableTags]);

  const handleAddAuthorToList = () => {
    const role = getValues('tempAuthorRole') || 'Author';
    if (!tempSelectedAuthor || !role) return;

    const newAuthorEntry = { authorId: tempSelectedAuthor.id, role: role };
    if (!currentAuthorsFormValue.some(
      (a) => a.authorId === newAuthorEntry.authorId && a.role === newAuthorEntry.role
    )) {
      setValue('authors', [...currentAuthorsFormValue, newAuthorEntry]);
      setTempSelectedAuthor(null);
    } else {
      handleApiError(null, `${tempSelectedAuthor.name} với vai trò ${role} đã được thêm.`);
    }
  };

  const handleRemoveAuthorVisual = (authorIdToRemove, roleToRemove) => {
    const updatedAuthorsFormValue = currentAuthorsFormValue.filter(
      (a) => !(a.authorId === authorIdToRemove && a.role === roleToRemove)
    );
    setValue('authors', updatedAuthorsFormValue);
  };

  // Custom Paper component cho Autocomplete Tags
  const HorizontalTagPaper = (props) => {
    return (
      <Paper {...props} sx={{ ...props.sx, maxHeight: 300, overflow: 'auto' }}>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', p: 1, gap: 0.5 }}>
          {props.children}
        </Box>
      </Paper>
    );
  };

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ mt: 1 }}>
      <Grid container spacing={2} columns={{ xs: 4, sm: 6, md: 12 }}>
        <Grid item xs={4} sm={6} md={12}>
          <FormInput control={control} name="title" label="Tiêu đề Manga" />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="originalLanguage"
            label="Ngôn ngữ gốc"
            type="select"
            options={ORIGINAL_LANGUAGE_OPTIONS}
          />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="publicationDemographic"
            label="Đối tượng xuất bản"
            type="select"
            options={PUBLICATION_DEMOGRAPHIC_OPTIONS}
          />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="status"
            label="Trạng thái"
            type="select"
            options={MANGA_STATUS_OPTIONS}
          />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="year"
            label="Năm xuất bản"
            type="number"
            inputProps={{ min: 1000, max: new Date().getFullYear() + 5, step: 1 }}
          />
        </Grid>
        <Grid item xs={4} sm={6} md={12}>
          <FormInput
            control={control}
            name="contentRating"
            label="Đánh giá nội dung"
            type="select"
            options={CONTENT_RATING_OPTIONS}
          />
        </Grid>

        {/* Authors Section - Thay đổi Typography thành label */}
        <Grid item xs={12}>
          <Grid container spacing={1} alignItems="flex-end" columns={{ xs: 12, sm: 12, md: 12 }}>
            <Grid item xs={12} sm={7} md={7}>
              <Autocomplete
                options={availableAuthors}
                getOptionLabel={(option) => option.name}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                value={tempSelectedAuthor}
                onChange={(event, newValue) => {
                  setTempSelectedAuthor(newValue);
                }}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Tác giả / Họa sĩ"
                    variant="outlined"
                    margin="normal"
                    error={!!errors.authors && currentAuthorsFormValue.length === 0}
                    helperText={errors.authors && currentAuthorsFormValue.length === 0 ? "Vui lòng thêm ít nhất một tác giả/họa sĩ" : null}
                  />
                )}
              />
            </Grid>
            <Grid item xs={12} sm={3} md={3}>
              <FormInput
                control={control}
                name="tempAuthorRole"
                label="Vai trò"
                type="select"
                options={MANGA_STAFF_ROLE_OPTIONS}
                defaultValue="Author"
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} sm={2} md={2} sx={{ alignSelf: 'center', mt: { xs: 1, sm: '24px' } }}>
              <Button
                variant="contained"
                color="primary"
                onClick={handleAddAuthorToList}
                startIcon={<AddIcon />}
                fullWidth
              >
                Thêm
              </Button>
            </Grid>
          </Grid>
          <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {selectedAuthorsVisual.map((author, index) => (
              <Chip
                key={`${author.id}-${author.role}-${index}`}
                label={`${author.name} (${author.role})`}
                onDelete={() => handleRemoveAuthorVisual(author.id, author.role)}
                deleteIcon={<DeleteIcon />}
                color="primary"
                variant="outlined"
              />
            ))}
          </Box>
        </Grid>

        {/* Tags Section - Thay đổi Typography thành label, cập nhật Autocomplete */}
        <Grid item xs={12}>
          <Autocomplete
            multiple
            disableCloseOnSelect
            id="manga-tags-autocomplete"
            options={availableTags.sort((a, b) => a.tagGroupName?.localeCompare(b.tagGroupName) || a.name.localeCompare(b.name))}
            groupBy={(option) => option.tagGroupName || 'Khác'}
            getOptionLabel={(option) => option.name}
            value={selectedTagsVisual}
            onChange={(event, newValue) => {
              setSelectedTagsVisual(newValue);
              setValue('tagIds', newValue.map(tag => tag.id));
            }}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            renderOption={(props, option, { selected }) => (
              <Box component="li" {...props} sx={{ width: '100%', justifyContent: 'flex-start', px: 1, py: 0.5 }}>
                <Checkbox
                  icon={<CheckBoxOutlineBlankIcon fontSize="small" />}
                  checkedIcon={<CheckBoxIcon fontSize="small" />}
                  style={{ marginRight: 8 }}
                  checked={selected}
                />
                <Chip 
                  label={option.name} 
                  size="small" 
                  variant="outlined" 
                  sx={{ cursor: 'pointer', flexGrow: 1, justifyContent: 'flex-start' }} 
                />
              </Box>
            )}
            PaperComponent={HorizontalTagPaper}
            renderInput={(params) => (
              <TextField
                {...params}
                variant="outlined"
                label="Tags"
                placeholder="Chọn tags"
                margin="normal"
                error={!!errors.tagIds}
                helperText={errors.tagIds ? errors.tagIds.message : null}
              />
            )}
            renderTags={(value, getTagProps) =>
              value.map((option, index) => (
                <Chip
                  key={option.id}
                  label={option.name}
                  {...getTagProps({ index })}
                  color="secondary"
                  variant="outlined"
                />
              ))
            }
            fullWidth
          />
        </Grid>
        
        {isEditMode && (
          <Grid item xs={4} sm={6} md={12}>
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

        <Grid item xs={4} sm={6} md={12}>
          <Button type="submit" variant="contained" color="primary" sx={{ mt: 3, mb: 2 }}>
            {isEditMode ? 'Cập nhật Manga' : 'Tạo Manga'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  )
}

export default MangaForm 