import { Box, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import authorApi from '../../../api/authorApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useAuthorStore from '../../../stores/authorStore'
import { handleApiError } from '../../../utils/errorUtils'
import AuthorForm from '../components/AuthorForm'

/**
 * @typedef {import('../../../types/manga').CreateAuthorRequest} CreateAuthorRequest
 */

function AuthorCreatePage() {
  const navigate = useNavigate()
  const fetchAuthors = useAuthorStore((state) => state.fetchAuthors)

  /**
   * @param {CreateAuthorRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await authorApi.createAuthor(data)
      showSuccessToast('Tạo tác giả thành công!')
      fetchAuthors(true) // Refresh author list and reset pagination
      navigate('/authors') // Navigate back to list page
    } catch (error) {
      console.error('Failed to create author:', error)
      handleApiError(error, 'Không thể tạo tác giả.')
    }
  }

  return (
    <Box className="author-create-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Tạo Tác giả mới
      </Typography>
      <AuthorForm onSubmit={handleSubmit} isEditMode={false} />
    </Box>
  )
}

export default AuthorCreatePage 