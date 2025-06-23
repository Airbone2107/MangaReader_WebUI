import { Box, CircularProgress, Typography } from '@mui/material'
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom'
import authorApi from '../../../api/authorApi'
import { showSuccessToast } from '../../../components/common/Notification'
import useAuthorStore from '../../../stores/authorStore'
import { handleApiError } from '../../../utils/errorUtils'
import AuthorForm from '../components/AuthorForm'

/**
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').UpdateAuthorRequest} UpdateAuthorRequest
 */

function AuthorEditPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  
  /** @type {[Author | null, React.Dispatch<React.SetStateAction<Author | null>>]} */
  const [author, setAuthor] = useState(null)
  const [loading, setLoading] = useState(true)
  
  const fetchAuthors = useAuthorStore((state) => state.fetchAuthors)

  useEffect(() => {
    const loadAuthor = async () => {
      setLoading(true)
      try {
        const response = await authorApi.getAuthorById(id)
        setAuthor(response.data)
      } catch (error) {
        console.error('Failed to fetch author for editing:', error)
        handleApiError(error, `Không thể tải tác giả có ID: ${id}.`)
        navigate('/authors') // Redirect if author not found or error
      } finally {
        setLoading(false)
      }
    }
    loadAuthor()
  }, [id, navigate])

  /**
   * @param {UpdateAuthorRequest} data
   */
  const handleSubmit = async (data) => {
    try {
      await authorApi.updateAuthor(id, data)
      showSuccessToast('Cập nhật tác giả thành công!')
      fetchAuthors() // Refresh author list
      // Optionally navigate back or stay on the page
      // navigate('/authors');
    } catch (error) {
      console.error('Failed to update author:', error)
      handleApiError(error, 'Không thể cập nhật tác giả.')
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

  if (!author) {
    return <Typography variant="h5">Không tìm thấy Tác giả.</Typography>
  }

  return (
    <Box className="author-edit-page">
      <Typography variant="h4" component="h1" gutterBottom className="page-header">
        Chỉnh sửa Tác giả: {author.attributes.name}
      </Typography>
      <AuthorForm initialData={author} onSubmit={handleSubmit} isEditMode={true} />
    </Box>
  )
}

export default AuthorEditPage 