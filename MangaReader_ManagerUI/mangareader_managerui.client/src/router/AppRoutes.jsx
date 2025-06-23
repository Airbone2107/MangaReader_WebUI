import React from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import AdminLayout from '../components/layout/AdminLayout'
import LoginPage from '../features/auth/LoginPage'
import AuthorCreatePage from '../features/author/pages/AuthorCreatePage'
import AuthorEditPage from '../features/author/pages/AuthorEditPage'
import AuthorListPage from '../features/author/pages/AuthorListPage'
import ChapterEditPage from '../features/chapter/pages/ChapterEditPage'
import ChapterListPage from '../features/chapter/pages/ChapterListPage'
// import DashboardPage from '../features/dashboard/DashboardPage'
import MangaCreatePage from '../features/manga/pages/MangaCreatePage'
import MangaEditPage from '../features/manga/pages/MangaEditPage'
import MangaListPage from '../features/manga/pages/MangaListPage'
import TagCreatePage from '../features/tag/pages/TagCreatePage'
import TagEditPage from '../features/tag/pages/TagEditPage'
import TagListPage from '../features/tag/pages/TagListPage'
import TagGroupCreatePage from '../features/tagGroup/pages/TagGroupCreatePage'
import TagGroupEditPage from '../features/tagGroup/pages/TagGroupEditPage'
import TagGroupListPage from '../features/tagGroup/pages/TagGroupListPage'
import ProtectedRoute from './ProtectedRoute'
import RoleListPage from '../features/role/pages/RoleListPage'
import UserListPage from '../features/user/pages/UserListPage'

function AppRoutes() {
  // For now, all routes are public. We'll implement ProtectedRoute in Step 5.
  const isAuthenticated = true // Placeholder for now

  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<LoginPage />} />

      {/* Admin Protected Routes */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/mangas" replace />} />
        {/* <Route path="dashboard" element={<DashboardPage />} /> */}
        <Route path="mangas" element={<MangaListPage />} />
        <Route path="mangas/create" element={<MangaCreatePage />} />
        <Route path="mangas/edit/:id" element={<MangaEditPage />} />
        <Route path="mangas/:id/covers" element={<MangaEditPage />} /> {/* Route to open MangaEditPage on Cover Art tab */}
        <Route path="mangas/:id/translations" element={<MangaEditPage />} /> {/* Route to open MangaEditPage on Translations tab */}
        
        {/* Routes for Chapters and Chapter Pages */}
        <Route path="translatedmangas/:translatedMangaId/chapters" element={<ChapterListPage />} />
        <Route path="chapters/edit/:id" element={<ChapterEditPage />} />
        <Route path="chapters/:id/pages" element={<ChapterEditPage />} /> {/* Route to open ChapterEditPage on Pages tab */}

        {/* Authors Routes (NEW) */}
        <Route path="authors" element={<AuthorListPage />} />
        <Route path="authors/create" element={<AuthorCreatePage />} />
        <Route path="authors/edit/:id" element={<AuthorEditPage />} />

        {/* Tags Routes (NEW) */}
        <Route path="tags" element={<TagListPage />} />
        <Route path="tags/create" element={<TagCreatePage />} />
        <Route path="tags/edit/:id" element={<TagEditPage />} />

        {/* Tag Groups Routes (NEW) */}
        <Route path="taggroups" element={<TagGroupListPage />} />
        <Route path="taggroups/create" element={<TagGroupCreatePage />} />
        <Route path="taggroups/edit/:id" element={<TagGroupEditPage />} />

        {/* User and Role Management (NEW) */}
        <Route path="users" element={<UserListPage />} />
        <Route path="roles" element={<RoleListPage />} />

        {/* Catch-all for undefined routes */}
        <Route path="*" element={<Navigate to="/mangas" replace />} />
      </Route>
    </Routes>
  )
}

export default AppRoutes 