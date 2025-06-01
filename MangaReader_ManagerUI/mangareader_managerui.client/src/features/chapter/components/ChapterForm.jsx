import { Box, Button, Grid, Paper } from '@mui/material'
import { format } from 'date-fns'
import React, { useEffect } from 'react'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createChapterSchema, updateChapterSchema } from '../../../schemas/chapterSchema'

/**
 * @typedef {import('../../../types/manga').Chapter} Chapter
 * @typedef {import('../../../types/manga').CreateChapterRequest} CreateChapterRequest
 * @typedef {import('../../../types/manga').UpdateChapterRequest} UpdateChapterRequest
 */

/**
 * ChapterForm component for creating or editing chapters.
 * @param {object} props
 * @param {string} props.translatedMangaId - ID of the translated manga (required for creation).
 * @param {Chapter} [props.initialData] - Initial data for editing.
 * @param {function(CreateChapterRequest | UpdateChapterRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
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
        <Grid container spacing={2} columns={{ xs: 4, sm: 6, md: 12 }}>
          <Grid item xs={4} sm={3} md={6}>
            <FormInput control={control} name="volume" label="Volume (Tùy chọn)" />
          </Grid>
          <Grid item xs={4} sm={3} md={6}>
            <FormInput control={control} name="chapterNumber" label="Số chương (Tùy chọn)" />
          </Grid>
          <Grid item xs={4} sm={6} md={12}>
            <FormInput control={control} name="title" label="Tiêu đề chương (Tùy chọn)" />
          </Grid>
          <Grid item xs={4} sm={3} md={6}>
            <FormInput
              control={control}
              name="publishAt"
              label="Thời gian xuất bản"
              type="datetime-local"
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          <Grid item xs={4} sm={3} md={6}>
            <FormInput
              control={control}
              name="readableAt"
              label="Thời gian có thể đọc"
              type="datetime-local"
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          <Grid item xs={4} sm={6} md={12}>
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