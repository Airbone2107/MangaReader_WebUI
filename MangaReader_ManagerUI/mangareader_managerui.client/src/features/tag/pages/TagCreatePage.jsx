import { Box, Typography } from '@mui/material'
import React from 'react'
import { useNavigate } from 'react-router-dom'
import tagApi from '../../../api/tagApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTagStore from '../../../stores/tagStore'
import { handleApiError } from '../../../utils/errorUtils'
import TagForm from '../components/TagForm'

/**
 * @typedef {import('../../../types/manga').CreateTagRequest} CreateTagRequest
 */

function TagCreatePage() {
  const navigate = useNavigate()
  const fetchTags = useTagStore((state) => state.fetchTags)

  /**
   * @param {CreateTagRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await tagApi.createTag(data)
      showSuccessToast('Tạo tag thành công!')
      fetchTags(true) // Refresh tag list and reset pagination
      navigate('/tags') // Navigate back to list page
    } catch (error) {
      console.error('Failed to create tag:', error)
      handleApiError(error, 'Không thể tạo tag.')
    }
  }

  return (
    <Box className="tag-create-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Tạo Tag mới
      </Typography>
      <TagForm onSubmit={handleSubmit} isEditMode={false} />
    </Box>
  )
}

export default TagCreatePage 