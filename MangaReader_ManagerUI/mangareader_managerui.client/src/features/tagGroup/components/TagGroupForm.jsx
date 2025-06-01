import { Box, Button, Grid, Paper } from '@mui/material'
import React, { useEffect } from 'react'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createTagGroupSchema, updateTagGroupSchema } from '../../../schemas/tagGroupSchema'

/**
 * @typedef {import('../../../types/manga').TagGroup} TagGroup
 * @typedef {import('../../../types/manga').CreateTagGroupRequest} CreateTagGroupRequest
 * @typedef {import('../../../types/manga').UpdateTagGroupRequest} UpdateTagGroupRequest
 */

/**
 * TagGroupForm component for creating or editing tag groups.
 * @param {object} props
 * @param {TagGroup} [props.initialData] - Initial data for editing.
 * @param {function(CreateTagGroupRequest | UpdateTagGroupRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
function TagGroupForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateTagGroupSchema : createTagGroupSchema,
    defaultValues: initialData
      ? {
          name: initialData.attributes.name || '',
        }
      : {
          name: '',
        },
  })

  // Reset form when initialData or isEditMode changes
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        name: initialData.attributes.name || '',
      })
    } else if (!isEditMode) {
      reset({
        name: '',
      })
    }
  }, [initialData, isEditMode, reset])

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2} columns={12}>
          <Grid item xs={12}>
            <FormInput control={control} name="name" label="Tên nhóm tag" />
          </Grid>
          <Grid item xs={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Nhóm tag' : 'Tạo Nhóm tag'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  )
}

export default TagGroupForm 