import { Box, Button, Grid, Paper } from '@mui/material'
import React, { useEffect } from 'react'
import FormInput from '../../../components/common/FormInput'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createAuthorSchema, updateAuthorSchema } from '../../../schemas/authorSchema'

/**
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').CreateAuthorRequest} CreateAuthorRequest
 * @typedef {import('../../../types/manga').UpdateAuthorRequest} UpdateAuthorRequest
 */

/**
 * AuthorForm component for creating or editing authors.
 * @param {object} props
 * @param {Author} [props.initialData] - Initial data for editing.
 * @param {function(CreateAuthorRequest | UpdateAuthorRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
function AuthorForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    reset,
  } = useFormWithZod({
    schema: isEditMode ? updateAuthorSchema : createAuthorSchema,
    defaultValues: initialData
      ? {
          name: initialData.attributes.name || '',
          biography: initialData.attributes.biography || '',
        }
      : {
          name: '',
          biography: '',
        },
  })

  // Reset form when initialData or isEditMode changes
  useEffect(() => {
    if (isEditMode && initialData) {
      reset({
        name: initialData.attributes.name || '',
        biography: initialData.attributes.biography || '',
      })
    } else if (!isEditMode) {
      reset({
        name: '',
        biography: '',
      })
    }
  }, [initialData, isEditMode, reset])

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Grid container spacing={2} columns={12}>
          <Grid size={12}>
            <FormInput control={control} name="name" label="Tên tác giả" />
          </Grid>
          <Grid size={12}>
            <FormInput
              control={control}
              name="biography"
              label="Tiểu sử (Tùy chọn)"
              multiline
              rows={4}
            />
          </Grid>
          <Grid size={12}>
            <Button type="submit" variant="contained" color="primary" sx={{ mt: 2 }}>
              {isEditMode ? 'Cập nhật Tác giả' : 'Tạo Tác giả'}
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  );
}

export default AuthorForm 