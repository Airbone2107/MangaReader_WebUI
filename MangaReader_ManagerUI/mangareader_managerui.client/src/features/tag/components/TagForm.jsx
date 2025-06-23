import { Box, Button, CircularProgress, Grid, Paper } from '@mui/material'
import { useEffect, useState } from 'react';
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createTagSchema, updateTagSchema } from '../../../schemas/tagSchema'
import useTagGroupStore from '../../../stores/tagGroupStore'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Tag} Tag
 * @typedef {import('../../../types/manga').TagGroup} TagGroup
 * @typedef {import('../../../types/manga').CreateTagRequest} CreateTagRequest
 * @typedef {import('../../../types/manga').UpdateTagRequest} UpdateTagRequest
 */

/**
 * TagForm component for creating or editing tags.
 * @param {object} props
 * @param {Tag} [props.initialData] - Initial data for editing.
 * @param {function(CreateTagRequest | UpdateTagRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
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
        <Grid container spacing={2} columns={12}>
          <Grid size={12}>
            <FormInput control={control} name="name" label="Tên tag" />
          </Grid>
          <Grid size={12}>
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
          <Grid size={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Tag' : 'Tạo Tag'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  );
}

export default TagForm 