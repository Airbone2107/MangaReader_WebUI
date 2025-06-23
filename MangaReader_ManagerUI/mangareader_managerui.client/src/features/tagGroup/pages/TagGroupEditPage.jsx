import { Box, CircularProgress, Typography } from '@mui/material'
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom'
import tagGroupApi from '../../../api/tagGroupApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useTagGroupStore from '../../../stores/tagGroupStore'
import { handleApiError } from '../../../utils/errorUtils'
import TagGroupForm from '../components/TagGroupForm'

/**
 * @typedef {import('../../../types/manga').TagGroup} TagGroup
 * @typedef {import('../../../types/manga').UpdateTagGroupRequest} UpdateTagGroupRequest
 */

function TagGroupEditPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  
  /** @type {[TagGroup | null, React.Dispatch<React.SetStateAction<TagGroup | null>>]} */
  const [tagGroup, setTagGroup] = useState(null)
  const [loading, setLoading] = useState(true)
  
  const fetchTagGroups = useTagGroupStore((state) => state.fetchTagGroups)

  useEffect(() => {
    const loadTagGroup = async () => {
      setLoading(true)
      try {
        const response = await tagGroupApi.getTagGroupById(id)
        setTagGroup(response.data)
      } catch (error) {
        console.error('Failed to fetch tag group for editing:', error)
        handleApiError(error, `Không thể tải nhóm tag có ID: ${id}.`)
        navigate('/taggroups') // Redirect if tag group not found or error
      } finally {
        setLoading(false)
      }
    }
    loadTagGroup()
  }, [id, navigate])

  /**
   * @param {UpdateTagGroupRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await tagGroupApi.updateTagGroup(id, data)
      showSuccessToast('Cập nhật nhóm tag thành công!')
      fetchTagGroups() // Refresh tag group list
      // Optionally navigate back or stay on the page
      // navigate('/taggroups');
    } catch (error) {
      console.error('Failed to update tag group:', error)
      handleApiError(error, 'Không thể cập nhật nhóm tag.')
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

  if (!tagGroup) {
    return <Typography variant="h5">Không tìm thấy Nhóm tag.</Typography>
  }

  return (
    <Box className="tag-group-edit-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Nhóm tag: {tagGroup.attributes.name}
      </Typography>
      <TagGroupForm initialData={tagGroup} onSubmit={handleSubmit} isEditMode={true} />
    </Box>
  )
}

export default TagGroupEditPage 