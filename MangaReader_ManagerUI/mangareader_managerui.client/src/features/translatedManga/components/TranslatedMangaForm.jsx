import { Box, Button, Grid, Paper } from '@mui/material'
import { useEffect } from 'react';
import FormInput from '../../../components/common/FormInput'
import { ORIGINAL_LANGUAGE_OPTIONS } from '../../../constants/appConstants'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createTranslatedMangaSchema, updateTranslatedMangaSchema } from '../../../schemas/translatedMangaSchema'

/**
 * @typedef {import('../../../types/manga').TranslatedManga} TranslatedManga
 * @typedef {import('../../../types/manga').CreateTranslatedMangaRequest} CreateTranslatedMangaRequest
 * @typedef {import('../../../types/manga').UpdateTranslatedMangaRequest} UpdateTranslatedMangaRequest
 */

/**
 * TranslatedMangaForm component for creating or editing translated manga.
 * @param {object} props
 * @param {string} props.mangaId - ID of the original manga (required for creation).
 * @param {TranslatedManga} [props.initialData] - Initial data for editing.
 * @param {function(CreateTranslatedMangaRequest | UpdateTranslatedMangaRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
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
        <Grid container spacing={2} columns={{ xs: 4, sm: 6, md: 12 }}>
          <Grid
            size={{
              xs: 4,
              sm: 3,
              md: 6
            }}>
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
          <Grid
            size={{
              xs: 4,
              sm: 3,
              md: 6
            }}>
            <FormInput control={control} name="title" label="Tiêu đề bản dịch" />
          </Grid>
          <Grid
            size={{
              xs: 4,
              sm: 6,
              md: 12
            }}>
            <FormInput
              control={control}
              name="description"
              label="Mô tả bản dịch (Tùy chọn)"
              multiline
              rows={4}
            />
          </Grid>
          <Grid
            size={{
              xs: 4,
              sm: 6,
              md: 12
            }}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Bản dịch' : 'Tạo Bản dịch'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  );
}

export default TranslatedMangaForm 