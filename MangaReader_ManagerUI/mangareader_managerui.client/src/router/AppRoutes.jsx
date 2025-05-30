import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { Typography } from '@mui/material'
import AdminLayout from '../components/layout/AdminLayout'
import DashboardPage from '../features/dashboard/DashboardPage'
import LoginPage from '../features/auth/LoginPage'
import MangaListPage from '../features/manga/pages/MangaListPage'
import MangaCreatePage from '../features/manga/pages/MangaCreatePage'
import MangaEditPage from '../features/manga/pages/MangaEditPage'

function AppRoutes() {
  // For now, all routes are public. We'll implement ProtectedRoute in Step 5.
  const isAuthenticated = true // Placeholder for now

  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<LoginPage />} />

      {/* Admin Protected Routes */}
      {/* For demo, always render AdminLayout. In a real app, wrap with <ProtectedRoute> */}
      <Route
        path="/"
        element={
          isAuthenticated ? <AdminLayout /> : <Navigate to="/login" replace />
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="mangas" element={<MangaListPage />} />
        <Route path="mangas/create" element={<MangaCreatePage />} />
        <Route path="mangas/edit/:id" element={<MangaEditPage />} />
        <Route path="mangas/:id/covers" element={<MangaEditPage />} /> {/* Route to open MangaEditPage on Cover Art tab */}
        <Route path="mangas/:id/translations" element={<MangaEditPage />} /> {/* Route to open MangaEditPage on Translations tab */}

        {/* Placeholder routes for other entities */}
        <Route path="authors" element={<Typography variant="h4" sx={{p:3}}>Authors Page (Coming Soon)</Typography>} />
        <Route path="tags" element={<Typography variant="h4" sx={{p:3}}>Tags Page (Coming Soon)</Typography>} />
        <Route path="taggroups" element={<Typography variant="h4" sx={{p:3}}>Tag Groups Page (Coming Soon)</Typography>} />
      </Route>

      {/* Catch-all for undefined routes */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}

export default AppRoutes 