import { Add as AddIcon, CheckBox as CheckBoxIcon, CheckBoxOutlineBlank as CheckBoxOutlineBlankIcon, Delete as DeleteIcon } from '@mui/icons-material'
import { Autocomplete, Box, Button, Checkbox, Chip, FormControlLabel, Grid, Paper, Switch, TextField, Typography } from '@mui/material'
import React, { useEffect, useState, useMemo, useRef } from 'react'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import FormInput from '../../../components/common/FormInput'
import {
    CONTENT_RATING_OPTIONS,
    MANGA_STAFF_ROLE_OPTIONS,
    MANGA_STATUS_OPTIONS,
    ORIGINAL_LANGUAGE_OPTIONS,
    PUBLICATION_DEMOGRAPHIC_OPTIONS,
    RELATIONSHIP_TYPES,
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
 * @typedef {import('../../../types/manga').TagInMangaAttributesDto} TagInMangaAttributesDto
 * @typedef {import('../../../types/api').ResourceObject} ResourceObject
 */

/**
 * MangaForm component for creating or editing manga.
 * @param {object} props
 * @param {Manga} [props.initialData] - Initial data for editing.
 * @param {function(import('../../../types/manga').CreateMangaRequest | import('../../../types/manga').UpdateMangaRequest): void} props.onSubmit - Function to handle form submission.
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
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateMangaSchema : createMangaSchema,
    defaultValues: {
        title: '',
        originalLanguage: 'ja',
        publicationDemographic: null,
        status: 'Ongoing',
        year: isEditMode ? null : new Date().getFullYear(),
        contentRating: 'Safe',
        isLocked: false,
        authors: [],
        tagIds: [],
        tempAuthor: null,
        tempAuthorRole: 'Author',
    }
  })

  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedAuthorsVisual, setSelectedAuthorsVisual] = useState([])
  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedTagsVisual, setSelectedTagsVisual] = useState([])

  /** @type {[Author[], React.Dispatch<React.SetStateAction<Author[]>>]} */
  const [availableAuthors, setAvailableAuthors] = useState([])
  /** @type {[Tag[], React.Dispatch<React.SetStateAction<Tag[]>>]} */
  const [availableTags, setAvailableTags] = useState([])

  const currentAuthorsFormValue = watch('authors') || []
  const currentTagIdsFormValue = watch('tagIds') || []
  const isLocked = watch('isLocked')

  const [tempSelectedAuthor, setTempSelectedAuthor] = useState(null);
  
  const tagsAutocompleteRef = useRef(null);

  useEffect(() => {
    const fetchDropdownData = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 })
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name, type: 'author' })))

        const tagsResponse = await tagApi.getTags({ limit: 1000 })
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name, tagGroupName: t.attributes.tagGroupName || 'N/A' })))
      } catch (error) {
        handleApiError(error, 'Không thể tải dữ liệu tác giả/tag.');
      }
    }
    fetchDropdownData()
  }, [])
  
  const sortedAvailableTags = useMemo(() => {
    return [...availableTags].sort((a, b) => 
      (a.tagGroupName || '').localeCompare(b.tagGroupName || '') || a.name.localeCompare(b.name)
    );
  }, [availableTags]);

  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        title: initialData.attributes.title ?? '',
        originalLanguage: initialData.attributes.originalLanguage ?? 'ja',
        publicationDemographic: initialData.attributes.publicationDemographic ?? null,
        status: initialData.attributes.status ?? 'Ongoing',
        year: initialData.attributes.year ?? null,
        contentRating: initialData.attributes.contentRating ?? 'Safe',
        isLocked: initialData.attributes.isLocked ?? false,
        authors: initialData.relationships
          ?.filter((rel) => rel.type === RELATIONSHIP_TYPES.AUTHOR || rel.type === RELATIONSHIP_TYPES.ARTIST)
          .map((rel) => ({ authorId: rel.id, role: rel.type === RELATIONSHIP_TYPES.AUTHOR ? 'Author' : 'Artist' })) || [],
        tagIds: initialData.attributes.tags?.map((tagResource) => tagResource.id) || [],
      });

      if (availableAuthors.length > 0) {
        const hydratedAuthors = (initialData.relationships
            ?.filter((rel) => rel.type === RELATIONSHIP_TYPES.AUTHOR || rel.type === RELATIONSHIP_TYPES.ARTIST)
            .map((rel) => {
                const authorDetails = availableAuthors.find(a => a.id === rel.id);
                return authorDetails ? { ...authorDetails, role: rel.type === RELATIONSHIP_TYPES.AUTHOR ? 'Author' : 'Artist' } : null;
            }) || []).filter(Boolean);
        setSelectedAuthorsVisual(hydratedAuthors);
      }

      if (availableTags.length > 0 && initialData.attributes.tags) {
          const hydratedTags = initialData.attributes.tags.map(tagResource => {
            const fullTagInfo = availableTags.find(at => at.id === tagResource.id);
            return {
                id: tagResource.id,
                name: tagResource.attributes.name,
                tagGroupName: tagResource.attributes.tagGroupName || fullTagInfo?.tagGroupName || 'N/A'
            };
          });
        setSelectedTagsVisual(hydratedTags);
      }

    } else if (!isEditMode) {
      reset({
        title: '',
        originalLanguage: 'ja',
        publicationDemographic: null,
        status: 'Ongoing',
        year: new Date().getFullYear(),
        contentRating: 'Safe',
        isLocked: false,
        authors: [],
        tagIds: [],
        tempAuthor: null,
        tempAuthorRole: 'Author',
      });
      setSelectedAuthorsVisual([]);
      setSelectedTagsVisual([]);
    }
  }, [initialData, isEditMode, availableAuthors, availableTags, reset]);

  useEffect(() => {
    if (availableAuthors.length > 0) {
      const hydratedAuthors = currentAuthorsFormValue
        .map((formAuthor) => {
          const authorDetails = availableAuthors.find(a => a.id === formAuthor.authorId);
          return authorDetails ? { ...authorDetails, role: formAuthor.role } : null;
        })
        .filter(Boolean);
      setSelectedAuthorsVisual(hydratedAuthors);
    }
  }, [currentAuthorsFormValue, availableAuthors]);

  useEffect(() => {
    if (availableTags.length > 0) {
      const hydratedTags = currentTagIdsFormValue
        .map((tagId) => {
          const tagDetails = availableTags.find(t => t.id === tagId);
          return tagDetails ? { id: tagDetails.id, name: tagDetails.name, tagGroupName: tagDetails.tagGroupName } : null;
        })
        .filter(Boolean);
      setSelectedTagsVisual(hydratedTags);
    }
  }, [currentTagIdsFormValue, availableTags]);

  const handleAddAuthorToList = () => {
    const role = getValues('tempAuthorRole') || 'Author';
    if (!tempSelectedAuthor || !role) return;

    const newAuthorEntry = { authorId: tempSelectedAuthor.id, role: role };
    if (!currentAuthorsFormValue.some(
      (a) => a.authorId === newAuthorEntry.authorId && a.role === newAuthorEntry.role
    )) {
      setValue('authors', [...currentAuthorsFormValue, newAuthorEntry], { shouldValidate: true });
      setTempSelectedAuthor(null);
    } else {
      handleApiError(null, `${tempSelectedAuthor.name} với vai trò ${role} đã được thêm.`);
    }
  };

  const handleRemoveAuthorVisual = (authorIdToRemove, roleToRemove) => {
    const updatedAuthorsFormValue = currentAuthorsFormValue.filter(
      (a) => !(a.authorId === authorIdToRemove && a.role === roleToRemove)
    );
    setValue('authors', updatedAuthorsFormValue, { shouldValidate: true });
  };

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ mt: 1 }}>
      <Grid container spacing={2} columns={{ xs: 4, sm: 6, md: 12 }}>
        <Grid
          size={{
            xs: 4,
            sm: 6,
            md: 12
          }}>
          <FormInput control={control} name="title" label="Tiêu đề Manga" />
        </Grid>
        <Grid
          size={{
            xs: 4,
            sm: 3,
            md: 6
          }}>
          <FormInput
            control={control}
            name="originalLanguage"
            label="Ngôn ngữ gốc"
            type="select"
            options={ORIGINAL_LANGUAGE_OPTIONS}
          />
        </Grid>
        <Grid
          size={{
            xs: 4,
            sm: 3,
            md: 6
          }}>
          <FormInput
            control={control}
            name="publicationDemographic"
            label="Đối tượng xuất bản"
            type="select"
            options={PUBLICATION_DEMOGRAPHIC_OPTIONS}
          />
        </Grid>
        <Grid
          size={{
            xs: 4,
            sm: 3,
            md: 6
          }}>
          <FormInput
            control={control}
            name="status"
            label="Trạng thái"
            type="select"
            options={MANGA_STATUS_OPTIONS}
          />
        </Grid>
        <Grid
          size={{
            xs: 4,
            sm: 3,
            md: 6
          }}>
          <FormInput
            control={control}
            name="year"
            label="Năm xuất bản"
            type="number"
            onChange={(e) => {
              const inputValue = e.target.value;
              if (inputValue === '') {
                setValue('year', null, { shouldValidate: true });
              } else {
                const numValue = parseInt(inputValue, 10);
                if (!isNaN(numValue)) {
                  setValue('year', numValue, { shouldValidate: true });
                }
              }
            }}
            onKeyDown={(e) => {
              if ([46, 8, 9, 27, 13].includes(e.keyCode) ||
                  (e.keyCode === 65 && (e.ctrlKey === true || e.metaKey === true)) ||
                  (e.keyCode >= 35 && e.keyCode <= 40)) {
                    return;
              }
              if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
                  e.preventDefault();
              }
              if (['e', 'E', '+', '-', '.'].includes(e.key)) {
                e.preventDefault();
              }
            }}
            inputProps={{ 
              min: 1000, 
              max: new Date().getFullYear() + 5, 
              step: 1 
            }}
          />
        </Grid>
        <Grid
          size={{
            xs: 4,
            sm: 6,
            md: 12
          }}>
          <FormInput
            control={control}
            name="contentRating"
            label="Đánh giá nội dung"
            type="select"
            options={CONTENT_RATING_OPTIONS}
          />
        </Grid>

        <Grid size={12}>
          <Grid container spacing={1} alignItems="flex-start" columns={{ xs: 12, sm: 12, md: 12 }}>
            <Grid
              size={{
                xs: 12,
                sm: 7,
                md: 7
              }}>
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
            <Grid
              size={{
                xs: 12,
                sm: 3,
                md: 3
              }}>
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
            <Grid
              sx={{ 
                mt: { xs: 0, sm: 2 },
                pt: { xs: 1, sm: 0 }
              }}
              size={{
                xs: 12,
                sm: 2,
                md: 2
              }}>
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
          <Box sx={{ mt: 1, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
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

        <Grid size={12}>
          <Autocomplete
            ref={tagsAutocompleteRef}
            multiple
            disableCloseOnSelect
            id="manga-tags-autocomplete"
            options={sortedAvailableTags}
            groupBy={(option) => option.tagGroupName || 'Không Nhóm'}
            getOptionLabel={(option) => option.name}
            value={selectedTagsVisual}
            onChange={(event, newValue) => {
              setSelectedTagsVisual(newValue);
              setValue('tagIds', newValue.map(tag => tag.id), { shouldValidate: true });
            }}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            
            PaperComponent={({ children, ...other }) => (
                <Paper 
                    {...other}
                    sx={{
                        ...other.sx,
                        maxHeight: 400,
                        overflow: 'auto',
                    }} 
                >
                    {children}
                </Paper>
            )}
            
            renderGroup={(params) => (
              <Box component="li" {...params} sx={{ p: 0, m: 0, width: '100%' }}>
                <Typography 
                  variant="overline"
                  color="text.secondary" 
                  sx={{ 
                    fontWeight: 'bold', 
                    p: '6px 12px', 
                    display: 'block', 
                    backgroundColor: (theme) => theme.palette.action.hover,
                    borderBottom: (theme) => `1px solid ${theme.palette.divider}`,
                    lineHeight: 1.4,
                    position: 'sticky',
                    top: 0,
                    zIndex: 1,
                  }}
                >
                  {params.group}
                </Typography>
                <Box component="ul" sx={{ display: 'flex', flexWrap: 'wrap', p: '8px', gap: '6px', m:0, listStyle:'none' }}>
                  {params.children}
                </Box>
              </Box>
            )}
            
            renderOption={(props, option, { selected }) => {
              return (
                <Box 
                  component="li" 
                  {...props}
                  sx={{ 
                    width: 'auto',
                    p: 0,
                    m: '2px',
                    listStyle: 'none', 
                    borderRadius: '16px', 
                    '&:hover': { 
                      backgroundColor: 'transparent',
                    },
                  }}
                >
                  <Chip
                    icon={
                      <Checkbox 
                        checked={selected} 
                        size="small" 
                        sx={{mr: -0.75, ml: 0.25, p:0.5, '& .MuiSvgIcon-root': { fontSize: '1rem' }}} 
                        icon={<CheckBoxOutlineBlankIcon fontSize="small" />} 
                        checkedIcon={<CheckBoxIcon fontSize="small" />} 
                      />
                    }
                    label={option.name}
                    size="small"
                    variant={selected ? "filled" : "outlined"}
                    color={selected ? "primary" : "default"}
                    clickable
                    sx={{ cursor: 'pointer', '& .MuiChip-label': { pr:1, pl:0.5 } }}
                  />
                </Box>
              );
            }}
            
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
          <Grid
            size={{
              xs: 4,
              sm: 6,
              md: 12
            }}>
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

        <Grid
          size={{
            xs: 4,
            sm: 6,
            md: 12
          }}>
          <Button type="submit" variant="contained" color="primary" sx={{ mt: 3, mb: 2 }}>
            {isEditMode ? 'Cập nhật Manga' : 'Tạo Manga'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  );
}

export default MangaForm 