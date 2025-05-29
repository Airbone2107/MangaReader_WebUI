import { Box, CircularProgress, Backdrop } from '@mui/material'
import useUiStore from '../../stores/uiStore'

function LoadingSpinner() {
  const isLoading = useUiStore((state) => state.isLoading)

  return (
    <Backdrop
      sx={{ color: '#fff', zIndex: (theme) => theme.zIndex.drawer + 100 }} // Ensure it's on top
      open={isLoading}
    >
      <CircularProgress color="inherit" />
    </Backdrop>
  )
}

export default LoadingSpinner 