import AccountCircle from '@mui/icons-material/AccountCircle'
import MenuIcon from '@mui/icons-material/Menu'
import { AppBar, IconButton, Toolbar, Typography } from '@mui/material'
import React from 'react'
import { Link } from 'react-router-dom'
import useUiStore from '../../stores/uiStore'

function Navbar() {
  const toggleSidebar = useUiStore((state) => state.toggleSidebar)

  return (
    <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
      <Toolbar className="navbar-toolbar">
        <IconButton
          color="inherit"
          aria-label="open drawer"
          edge="start"
          onClick={toggleSidebar}
          sx={{ mr: 2 }}
        >
          <MenuIcon />
        </IconButton>
        <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
          <Link to="/" className="navbar-logo">
            MangaReader Manager
          </Link>
        </Typography>
        <IconButton color="inherit">
          <AccountCircle />
        </IconButton>
        {/* Potentially add user menu/logout here */}
      </Toolbar>
    </AppBar>
  )
}

export default Navbar 