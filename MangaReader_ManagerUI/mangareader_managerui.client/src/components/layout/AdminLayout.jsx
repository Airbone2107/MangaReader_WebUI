import React from 'react'
import { Box } from '@mui/material'
import Navbar from './Navbar'
import Sidebar from './Sidebar'
import LoadingSpinner from '../common/LoadingSpinner'
import useUiStore from '../../stores/uiStore'

function AdminLayout({ children }) {
  const isSidebarOpen = useUiStore((state) => state.isSidebarOpen)

  return (
    <Box sx={{ display: 'flex' }}>
      <Navbar />
      <Sidebar />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          mt: '64px', // AppBar height
          width: '100%',
          transition: (theme) =>
            theme.transitions.create('margin', {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.leavingScreen,
            }),
          marginLeft: isSidebarOpen ? 'var(--sidebar-width)' : '0', // Adjust margin when sidebar is open/closed
          '@media (max-width: 599px)': { // For mobile, sidebar should overlay
            marginLeft: 0,
            transition: 'none',
          }
        }}
      >
        {children}
      </Box>
      <LoadingSpinner />
    </Box>
  )
}

export default AdminLayout 