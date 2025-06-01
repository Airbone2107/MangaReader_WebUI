import { Box, Typography } from '@mui/material'
import React from 'react'
import { useNavigate } from 'react-router-dom'
import tagGroupApi from '../../../api/tagGroupApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTagGroupStore from '../../../stores/tagGroupStore'
import { handleApiError } from '../../../utils/errorUtils'
import TagGroupForm from '../components/TagGroupForm'

/**
 * @typedef {import('../../../types/manga').CreateTagGroupRequest} CreateTagGroupRequest
 */

function TagGroupCreatePage() {
  const navigate = useNavigate()
  const fetchTagGroups = useTagGroupStore((state) => state.fetchTagGroups)

  /**
   * @param {CreateTagGroupRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await tagGroupApi.createTagGroup(data)
      showSuccessToast('Tạo nhóm tag thành công!')
      fetchTagGroups(true) // Refresh tag group list and reset pagination
      navigate('/taggroups') // Navigate back to list page
    } catch (error) {
      console.error('Failed to create tag group:', error)
      handleApiError(error, 'Không thể tạo nhóm tag.')
    }
  }

  return (
    <Box className="tag-group-create-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Tạo Nhóm tag mới
      </Typography>
      <TagGroupForm onSubmit={handleSubmit} isEditMode={false} />
    </Box>
  )
}

export default TagGroupCreatePage 