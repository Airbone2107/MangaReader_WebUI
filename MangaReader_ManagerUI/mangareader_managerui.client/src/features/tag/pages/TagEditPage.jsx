import { Box, CircularProgress, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import tagApi from '../../../api/tagApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTagStore from '../../../stores/tagStore'
import { handleApiError } from '../../../utils/errorUtils'
import TagForm from '../components/TagForm'

/**
 * @typedef {import('../../../types/manga').Tag} Tag
 * @typedef {import('../../../types/manga').UpdateTagRequest} UpdateTagRequest
 */

function TagEditPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  
  /** @type {[Tag | null, React.Dispatch<React.SetStateAction<Tag | null>>]} */
  const [tag, setTag] = useState(null)
  const [loading, setLoading] = useState(true)
  
  const fetchTags = useTagStore((state) => state.fetchTags)

  useEffect(() => {
    const loadTag = async () => {
      setLoading(true)
      try {
        const response = await tagApi.getTagById(id)
        setTag(response.data)
      } catch (error) {
        console.error('Failed to fetch tag for editing:', error)
        handleApiError(error, `Không thể tải tag có ID: ${id}.`)
        navigate('/tags') // Redirect if tag not found or error
      } finally {
        setLoading(false)
      }
    }
    loadTag()
  }, [id, navigate])

  /**
   * @param {UpdateTagRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await tagApi.updateTag(id, data)
      showSuccessToast('Cập nhật tag thành công!')
      fetchTags() // Refresh tag list
      // Optionally navigate back or stay on the page
      // navigate('/tags');
    } catch (error) {
      console.error('Failed to update tag:', error)
      handleApiError(error, 'Không thể cập nhật tag.')
    }
  }

  if (loading) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100%',
        }}
      >
        <CircularProgress />
      </Box>
    )
  }

  if (!tag) {
    return <Typography variant="h5">Không tìm thấy Tag.</Typography>
  }

  return (
    <Box className="tag-edit-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Tag: {tag.attributes.name}
      </Typography>
      <TagForm initialData={tag} onSubmit={handleSubmit} isEditMode={true} />
    </Box>
  )
}

export default TagEditPage 