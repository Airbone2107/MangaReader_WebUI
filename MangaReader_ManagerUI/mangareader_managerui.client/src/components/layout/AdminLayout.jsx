import { Box, useTheme } from '@mui/material'
import React from 'react'
import { Outlet } from 'react-router-dom';
import useUiStore from '../../stores/uiStore'
import LoadingSpinner from '../common/LoadingSpinner'
import Navbar from './Navbar'
import Sidebar from './Sidebar'

function AdminLayout() {
  const isSidebarOpen = useUiStore((state) => state.isSidebarOpen)
  const theme = useTheme()
  const sidebarWidth = 240; // Dùng số để tính toán

  return (
    <Box sx={{ display: 'flex', height: '100vh', flexDirection: 'column' }}>
      <Navbar />
      <Box sx={{ display: 'flex', flexGrow: 1, overflow: 'hidden', marginTop: { xs: '56px', sm: '64px' } }}>
        <Sidebar />
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            p: 3,
            height: '100%',
            overflowY: 'auto',
            transition: theme.transitions.create('margin', {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.leavingScreen,
            }),
            // Khi sidebar đóng, kéo main content sang trái để che đi khoảng trống 240px mà Drawer root vẫn chiếm giữ.
            marginLeft: `-${sidebarWidth}px`,

            // Khi sidebar mở, áp dụng style này để đẩy main content về vị trí ban đầu (marginLeft: 0)
            ...(isSidebarOpen && {
              transition: theme.transitions.create('margin', {
                easing: theme.transitions.easing.easeOut,
                duration: theme.transitions.duration.enteringScreen,
              }),
              marginLeft: 0,
            }),
             // Trên mobile, không áp dụng margin, để drawer trượt ra che phủ
            [theme.breakpoints.down('sm')]: {
                marginLeft: 0,
            },
          }}
        >
          <Outlet /> {/* Render nội dung của route con ở đây */}
        </Box>
      </Box>
      <LoadingSpinner />
    </Box>
  )
}

export default AdminLayout