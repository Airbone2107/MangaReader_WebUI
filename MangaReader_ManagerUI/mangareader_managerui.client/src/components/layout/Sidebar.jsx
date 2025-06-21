import CategoryIcon from '@mui/icons-material/Category'
// import DashboardIcon from '@mui/icons-material/Dashboard'
import LocalOfferIcon from '@mui/icons-material/LocalOffer'
import MenuBookIcon from '@mui/icons-material/MenuBook'
import PersonIcon from '@mui/icons-material/Person'
import { Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Toolbar } from '@mui/material'
import React from 'react'
import { NavLink } from 'react-router-dom'
import useUiStore from '../../stores/uiStore'

function Sidebar() {
  const isSidebarOpen = useUiStore((state) => state.isSidebarOpen)

  const menuItems = [
    // { text: 'Dashboard', icon: <DashboardIcon />, path: '/dashboard' },
    { text: 'Manga', icon: <MenuBookIcon />, path: '/mangas' },
    { text: 'Authors', icon: <PersonIcon />, path: '/authors' },          // NEW
    { text: 'Tags', icon: <LocalOfferIcon />, path: '/tags' },            // NEW
    { text: 'Tag Groups', icon: <CategoryIcon />, path: '/taggroups' },  // NEW
    // { text: 'Translated Mangas', icon: <TranslateIcon />, path: '/translatedmangas' },
    // { text: 'Chapters', icon: <CollectionsBookmarkIcon />, path: '/chapters' },
  ]

  return (
    <Drawer
      variant="persistent"
      anchor="left"
      open={isSidebarOpen}
      sx={{
        '& .MuiDrawer-paper': {
          width: 'var(--sidebar-width)', // Use CSS variable from SCSS
          boxSizing: 'border-box',
          // Adjust top padding to clear AppBar
          marginTop: '64px', // Standard AppBar height
          '@media (min-width: 0px) and (orientation: landscape)': {
            marginTop: '48px', // Smaller AppBar height for landscape mobile
          },
          '@media (min-width: 600px)': {
            marginTop: '64px',
          },
        },
      }}
    >
      <Toolbar /> {/* Spacer to push content below the AppBar */}
      <List className="sidebar-list">
        {menuItems.map((item) => (
          <ListItem key={item.text} disablePadding className="sidebar-list-item">
            <ListItemButton component={NavLink} to={item.path}>
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.text} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </Drawer>
  )
}

export default Sidebar 