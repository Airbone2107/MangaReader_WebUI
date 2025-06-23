import CategoryIcon from '@mui/icons-material/Category'
// import DashboardIcon from '@mui/icons-material/Dashboard'
import LocalOfferIcon from '@mui/icons-material/LocalOffer'
import MenuBookIcon from '@mui/icons-material/MenuBook'
import PersonIcon from '@mui/icons-material/Person'
import SecurityIcon from '@mui/icons-material/Security'
import SupervisedUserCircleIcon from '@mui/icons-material/SupervisedUserCircle'
import { Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Toolbar, useMediaQuery, useTheme } from '@mui/material'
import React from 'react'
import { NavLink } from 'react-router-dom'
import useUiStore from '../../stores/uiStore'

function Sidebar() {
  const { isSidebarOpen, setSidebarOpen } = useUiStore()
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const sidebarWidth = '240px'; 

  const menuItems = [
    // { text: 'Dashboard', icon: <DashboardIcon />, path: '/dashboard' },
    { text: 'Manga', icon: <MenuBookIcon />, path: '/mangas' },
    { text: 'Authors', icon: <PersonIcon />, path: '/authors' },
    { text: 'Tags', icon: <LocalOfferIcon />, path: '/tags' },
    { text: 'Tag Groups', icon: <CategoryIcon />, path: '/taggroups' },
    { text: 'Users', icon: <SupervisedUserCircleIcon />, path: '/users' },
    { text: 'Roles', icon: <SecurityIcon />, path: '/roles' },
  ]

  const drawerContent = (
      <List className="sidebar-list" sx={{pt: 2}}>
        {menuItems.map((item) => (
          <ListItem key={item.text} disablePadding className="sidebar-list-item">
            <ListItemButton 
              component={NavLink} 
              to={item.path}
              // Thêm activeClassName để MUI tự động áp dụng style Mui-selected
              className={({ isActive }) => (isActive ? 'Mui-selected' : '')}
              onClick={isMobile ? () => setSidebarOpen(false) : undefined}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.text} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
  );

  return (
    <Drawer
      // Trên mobile, dùng variant "temporary" để nó che phủ nội dung
      // Trên desktop, dùng "persistent" để nó đẩy nội dung
      variant={isMobile ? "temporary" : "persistent"}
      anchor="left"
      open={isSidebarOpen}
      onClose={() => setSidebarOpen(false)} // Cần thiết cho "temporary" variant
      sx={{
        width: sidebarWidth,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: sidebarWidth,
          boxSizing: 'border-box',
          // Để sidebar nằm dưới AppBar
          marginTop: { xs: '56px', sm: '64px' }, // Điều chỉnh theo chiều cao AppBar responsive
          height: { xs: 'calc(100% - 56px)', sm: 'calc(100% - 64px)' },
          border: 'none',
        },
      }}
      // Giữ cho modal không bị ẩn đi khi ở chế độ temporary
      ModalProps={{
        keepMounted: true, 
      }}
    >
      {drawerContent}
    </Drawer>
  )
}

export default Sidebar 