import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material'
import { Autocomplete, Box, Button, Chip, FormControlLabel, Grid, Switch, TextField, Typography } from '@mui/material'
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
            inputProps={{ min: 1000, max: new Date().getFullYear(), step: 1 }}
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

        {/* Authors Section */}
        <Grid item xs={4} sm={6} md={12}>
          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
            Tác giả / Họa sĩ
          </Typography>
          <Grid container spacing={1} alignItems="center" columns={{ xs: 4, sm: 6, md: 12 }}>
            <Grid item xs={4} sm={3} md={6}>
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
            <Grid item xs={4} sm={1.5} md={3}>
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
            <Grid item xs={4} sm={1.5} md={3}>
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
        <Grid item xs={4} sm={6} md={12}>
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