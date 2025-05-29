# TODO.md - Bước 3: Thiết Kế và Triển Khai Giao Diện Người Dùng (UI)

Mục tiêu của bước này là xây dựng các thành phần UI cơ bản và cấu trúc layout chính của ứng dụng bằng Material UI và SCSS.

## 1. Cấu hình SCSS và Tạo File Styles Cơ Bản

Đảm bảo rằng SCSS đã được cài đặt và tích hợp vào dự án. Chúng ta sẽ thêm nội dung vào các file SCSS hiện có để định nghĩa phong cách chung cho ứng dụng.

**a. Cấu trúc SCSS:**
Kiểm tra cấu trúc thư mục SCSS của bạn trong `src/assets/scss/`. Đảm bảo các file sau tồn tại:
- `src/assets/scss/main.scss`
- `src/assets/scss/base/_reset.scss`
- `src/assets/scss/base/_typography.scss`
- `src/assets/scss/components/_buttons.scss`
- `src/assets/scss/components/_tables.scss`
- `src/assets/scss/layout/_navbar.scss`
- `src/assets/scss/layout/_sidebar.scss`
- `src/assets/scss/pages/_login.scss`
- `src/assets/scss/pages/_mangaList.scss` (sẽ được sử dụng ở Bước 4)
- `src/assets/scss/themes/_default.scss`
- `src/assets/scss/utils/_functions.scss`
- `src/assets/scss/utils/_mixins.scss`
- `src/assets/scss/utils/_variables.scss`

**b. Cập nhật `src/assets/scss/utils/_variables.scss`**
Tạo các biến SCSS để định nghĩa màu sắc, font, kích thước, v.v.

```scss
// src/assets/scss/utils/_variables.scss
// Colors
$primary-color: #1976d2; // Blue
$secondary-color: #dc004e; // Pink
$success-color: #2e7d32; // Green
$error-color: #d32f2f; // Red
$warning-color: #ed6c02; // Orange
$info-color: #0288d1; // Light blue

$text-color: #333;
$text-color-light: #fff;
$background-color: #f4f6f8;
$surface-color: #fff;
$border-color: #e0e0e0;
$dark-gray: #616161;
$light-gray: #f5f5f5;

// Spacing
$spacing-xxs: 4px;
$spacing-xs: 8px;
$spacing-sm: 12px;
$spacing-md: 16px;
$spacing-lg: 24px;
$spacing-xl: 32px;
$spacing-xxl: 48px;

// Font Sizes
$font-size-sm: 0.875rem; // 14px
$font-size-md: 1rem;     // 16px
$font-size-lg: 1.125rem; // 18px
$font-size-xl: 1.5rem;   // 24px

// Border Radius
$border-radius-sm: 4px;
$border-radius-md: 8px;

// Z-index
$zindex-tooltip: 1100;
$zindex-modal: 1300;
$zindex-snackbar: 1400;

// Sidebar Width
$sidebar-width: 240px;

// Transitions
$transition-speed: 0.3s;
$transition-easing: ease-in-out;

// Box Shadow
$shadow-sm: 0px 2px 4px rgba(0, 0, 0, 0.05);
$shadow-md: 0px 4px 8px rgba(0, 0, 0, 0.1);
$shadow-lg: 0px 8px 16px rgba(0, 0, 0, 0.15);
```

**c. Cập nhật `src/assets/scss/utils/_mixins.scss`**
Tạo các mixin SCSS để tái sử dụng mã CSS.

```scss
// src/assets/scss/utils/_mixins.scss
@mixin flex-center {
  display: flex;
  justify-content: center;
  align-items: center;
}

@mixin flex-between {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

@mixin button-styles($bg-color, $text-color) {
  background-color: $bg-color;
  color: $text-color;
  border: none;
  padding: $spacing-sm $spacing-md;
  border-radius: $border-radius-sm;
  cursor: pointer;
  transition: background-color $transition-speed $transition-easing;

  &:hover {
    background-color: darken($bg-color, 10%);
  }

  &:disabled {
    background-color: $light-gray;
    color: $dark-gray;
    cursor: not-allowed;
  }
}

// Responsive breakpoints
@mixin for-phone-only {
  @media (max-width: 599px) { @content; }
}

@mixin for-tablet-portrait-up {
  @media (min-width: 600px) { @content; }
}

@mixin for-tablet-landscape-up {
  @media (min-width: 900px) { @content; }
}

@mixin for-desktop-up {
  @media (min-width: 1200px) { @content; }
}
```

**d. Cập nhật `src/assets/scss/utils/_functions.scss`**
Tạo các hàm SCSS.

```scss
// src/assets/scss/utils/_functions.scss
@function rem($px) {
  @return calc($px / 16) + rem;
}
```

**e. Cập nhật `src/assets/scss/base/_reset.scss`**
Đặt lại một số kiểu CSS mặc định của trình duyệt.

```scss
// src/assets/scss/base/_reset.scss
*,
*::before,
*::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

html, body, #root {
  height: 100%;
  width: 100%;
}

body {
  font-family: 'Roboto', sans-serif; // Assuming Roboto is available via MUI or Google Fonts
  line-height: 1.6;
  background-color: $background-color;
  color: $text-color;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

a {
  text-decoration: none;
  color: $primary-color;
}

ul {
  list-style: none;
}

button {
  cursor: pointer;
  border: none;
  background: transparent;
  padding: 0;
}
```

**f. Cập nhật `src/assets/scss/base/_typography.scss`**
Định nghĩa kiểu chữ chung.

```scss
// src/assets/scss/base/_typography.scss
h1, h2, h3, h4, h5, h6 {
  margin-bottom: $spacing-md;
  color: $text-color;
}

p {
  margin-bottom: $spacing-sm;
}

small {
  font-size: $font-size-sm;
  color: $dark-gray;
}
```

**g. Cập nhật `src/assets/scss/components/_buttons.scss`**
Phong cách cho các nút (nếu muốn override MUI mặc định).

```scss
// src/assets/scss/components/_buttons.scss
.MuiButton-root {
  text-transform: none; // Keep text as is, not uppercase
  font-weight: 500;
  font-size: $font-size-md;
  box-shadow: $shadow-sm; // Add a subtle shadow

  &.MuiButton-containedPrimary {
    @include button-styles($primary-color, $text-color-light);
  }

  &.MuiButton-outlinedPrimary {
    border: 1px solid $primary-color;
    color: $primary-color;
    background-color: transparent;
    &:hover {
      background-color: rgba($primary-color, 0.04);
    }
  }

  &.MuiButton-textPrimary {
    color: $primary-color;
    &:hover {
      background-color: rgba($primary-color, 0.04);
    }
  }

  &.MuiButton-containedSecondary {
    @include button-styles($secondary-color, $text-color-light);
  }

  &.Mui-disabled {
    cursor: not-allowed;
    pointer-events: auto; /* Allow cursor to show 'not-allowed' */
  }
}
```

**h. Cập nhật `src/assets/scss/components/_tables.scss`**
Phong cách cho bảng dữ liệu.

```scss
// src/assets/scss/components/_tables.scss
.MuiTableContainer-root {
  box-shadow: $shadow-sm;
  border-radius: $border-radius-md;
  background-color: $surface-color;
  margin-bottom: $spacing-lg;
}

.MuiTableHead-root {
  .MuiTableCell-root {
    font-weight: 600;
    color: $dark-gray;
    background-color: $light-gray;
    padding: $spacing-sm $spacing-md;
    border-bottom: 1px solid $border-color;
  }
}

.MuiTableBody-root {
  .MuiTableRow-root {
    &:nth-of-type(odd) {
      background-color: rgba($primary-color, 0.02);
    }
    &:hover {
      background-color: rgba($primary-color, 0.05);
      cursor: pointer;
    }
  }
  .MuiTableCell-root {
    padding: $spacing-sm $spacing-md;
    border-bottom: 1px solid $border-color;
    color: $text-color;
  }
}

.MuiTablePagination-root {
  background-color: $surface-color;
  border-top: 1px solid $border-color;
  .MuiTablePagination-toolbar {
    padding: $spacing-sm $spacing-md;
  }
}
```

**i. Cập nhật `src/assets/scss/layout/_navbar.scss`**
Phong cách cho thanh điều hướng trên cùng.

```scss
// src/assets/scss/layout/_navbar.scss
.MuiAppBar-root {
  background-color: $primary-color !important;
  color: $text-color-light !important;
  box-shadow: $shadow-md !important;
  z-index: 1200; // Higher than sidebar to overlap when sidebar is closed/mobile
}

.navbar-toolbar {
  @include flex-between();
  padding: $spacing-sm $spacing-md;
}

.navbar-logo {
  font-size: $font-size-xl;
  font-weight: 700;
  color: inherit;
  text-decoration: none;
}
```

**j. Cập nhật `src/assets/scss/layout/_sidebar.scss`**
Phong cách cho thanh điều hướng bên.

```scss
// src/assets/scss/layout/_sidebar.scss
.MuiDrawer-root {
  width: $sidebar-width;
  flex-shrink: 0;
  .MuiDrawer-paper {
    width: $sidebar-width;
    background-color: $surface-color;
    box-shadow: $shadow-lg;
    box-sizing: border-box;
    padding-top: $spacing-xl; // Space for app bar
  }
}

.sidebar-header {
  padding: $spacing-md;
  text-align: center;
  font-size: $font-size-lg;
  font-weight: bold;
  border-bottom: 1px solid $border-color;
  margin-bottom: $spacing-md;
}

.sidebar-list {
  padding: 0 $spacing-sm;
}

.sidebar-list-item {
  margin-bottom: $spacing-xxs;
  .MuiListItemIcon-root {
    min-width: 40px;
    color: $primary-color;
  }
  .MuiListItemText-primary {
    font-size: $font-size-md;
    font-weight: 500;
  }
  .Mui-selected {
    background-color: rgba($primary-color, 0.1) !important;
    color: $primary-color;
    border-radius: $border-radius-sm;
  }
  .MuiListItemButton-root {
    border-radius: $border-radius-sm;
    &:hover {
      background-color: rgba($primary-color, 0.05);
    }
  }
}
```

**k. Cập nhật `src/assets/scss/pages/_dashboard.scss`**
Phong cách cho trang dashboard.

```scss
// src/assets/scss/pages/_dashboard.scss
.dashboard-page {
  padding: $spacing-lg;
  background-color: $background-color;
  min-height: calc(100vh - 64px); /* Assuming app bar height */
}

.dashboard-header {
  margin-bottom: $spacing-xl;
  color: $primary-color;
}

.dashboard-stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: $spacing-lg;
  margin-bottom: $spacing-xxl;
}

.stat-card {
  background-color: $surface-color;
  padding: $spacing-lg;
  border-radius: $border-radius-md;
  box-shadow: $shadow-sm;
  text-align: center;

  h3 {
    margin-bottom: $spacing-xs;
    color: $dark-gray;
  }

  p {
    font-size: $font-size-xl;
    font-weight: bold;
    color: $primary-color;
  }
}
```

**l. Cập nhật `src/assets/scss/themes/_default.scss`**
Định nghĩa theme mặc định.

```scss
// src/assets/scss/themes/_default.scss
// This file can be used for general theme-related styles or overrides if needed.
// For now, it mostly relies on variables and MUI's default theming.
body {
  // Use defined variables
  background-color: $background-color;
  color: $text-color;
}
```

**m. Cập nhật `src/assets/scss/main.scss`**
Import tất cả các file SCSS partials.

```scss
// src/assets/scss/main.scss
@import 'base/reset';
@import 'base/typography';
@import 'utils/variables';
@import 'utils/mixins';
@import 'utils/functions';
@import 'components/buttons';
@import 'components/tables';
@import 'layout/navbar';
@import 'layout/sidebar';
@import 'pages/dashboard';
@import 'pages/login'; // Will be filled in later steps
@import 'pages/mangaList'; // Will be filled in later steps
@import 'themes/default';

// Global styles (minimal, usually handled by MUI's CssBaseline)
body {
  margin: 0;
  padding: 0;
  font-family: 'Roboto', 'Helvetica', 'Arial', sans-serif; // Ensure font is loaded, MUI might do this
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

// Add global spacing utility if needed, e.g. for page content
.page-content-wrapper {
  padding: $spacing-lg; // Example of a global padding
  flex-grow: 1; // Allow content to take available space
}

// Custom styles for React Toastify container
.Toastify__toast-container {
  font-family: 'Roboto', 'Helvetica', 'Arial', sans-serif;
  .Toastify__toast {
    border-radius: $border-radius-md;
    box-shadow: $shadow-md;
  }
  .Toastify__toast--success {
    background-color: $success-color;
    color: $text-color-light;
  }
  .Toastify__toast--error {
    background-color: $error-color;
    color: $text-color-light;
  }
  .Toastify__toast--warning {
    background-color: $warning-color;
    color: $text-color-light;
  }
  .Toastify__toast--info {
    background-color: $info-color;
    color: $text-color-light;
  }
}
```

## 2. Tạo và Cấu hình các UI Components Cơ Bản

Chúng ta sẽ tạo các component UI dùng chung và tích hợp Material UI, React Hook Form, Zod.

**a. Cập nhật `src/main.jsx`**
Import `ToastContainer` và `main.scss` để sử dụng global.

```jsx
// src/main.jsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter as Router } from 'react-router-dom' // Import BrowserRouter
import { ThemeProvider, createTheme } from '@mui/material/styles' // Import MUI ThemeProvider
import { CssBaseline } from '@mui/material' // Import CssBaseline for consistent styling
import { ToastContainer } from 'react-toastify' // Import ToastContainer
import 'react-toastify/dist/ReactToastify.css' // Import Toastify CSS
import './assets/scss/main.scss' // Import main SCSS file
import App from './App.jsx'

// Define a basic MUI theme
const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2', // Blue
    },
    secondary: {
      main: '#dc004e', // Pink
    },
    success: {
      main: '#2e7d32',
    },
    error: {
      main: '#d32f2f',
    },
    warning: {
      main: '#ed6c02',
    },
    info: {
      main: '#0288d1',
    },
    background: {
      default: '#f4f6f8',
      paper: '#fff',
    },
    text: {
      primary: '#333',
      secondary: '#616161',
    },
  },
  typography: {
    fontFamily: 'Roboto, Arial, sans-serif',
    h1: { fontSize: '2.5rem' },
    h2: { fontSize: '2rem' },
    h3: { fontSize: '1.75rem' },
    h4: { fontSize: '1.5rem' },
    h5: { fontSize: '1.25rem' },
    h6: { fontSize: '1rem' },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none', // Prevent uppercase transformation
          fontWeight: 500,
        },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 500,
        },
      },
    },
  },
});

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline /> {/* Applies a consistent baseline to build upon. */}
      <Router>
        <App />
        <ToastContainer /> {/* Add ToastContainer here */}
      </Router>
    </ThemeProvider>
  </StrictMode>,
)
```

**b. Cập nhật `src/App.jsx`**
Thiết lập component gốc của ứng dụng, sử dụng `AppRoutes`.

```jsx
// src/App.jsx
import AppRoutes from './router/AppRoutes'

function App() {
  return <AppRoutes />
}

export default App
```

**c. Tạo `src/stores/uiStore.js`**
Store này sẽ quản lý các trạng thái UI như loading, trạng thái mở/đóng sidebar, v.v.

```javascript
// src/stores/uiStore.js
import { create } from 'zustand'

const useUiStore = create((set) => ({
  isLoading: false,
  isSidebarOpen: true, // Initial state for sidebar
  
  setLoading: (loading) => set({ isLoading: loading }),
  toggleSidebar: () => set((state) => ({ isSidebarOpen: !state.isSidebarOpen })),
  setSidebarOpen: (open) => set({ isSidebarOpen: open }),
}))

export default useUiStore
```

**d. Tạo `src/components/common/LoadingSpinner.jsx`**
Component hiển thị biểu tượng loading.

```jsx
// src/components/common/LoadingSpinner.jsx
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
```

**e. Tạo `src/components/common/Notification.jsx`**
Component hiển thị thông báo toast. Logic này đã được tích hợp vào `main.jsx` thông qua `ToastContainer`, nhưng bạn có thể tạo một hàm tiện ích để gọi toast dễ dàng hơn.

```javascript
// src/components/common/Notification.jsx
// This file is not a React Component but rather a utility for showing toasts.
// No specific React component to render, as ToastContainer is handled in main.jsx.

import { toast } from 'react-toastify';

export const showSuccessToast = (message) => {
  toast.success(message, {
    position: "top-right",
    autoClose: 3000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

export const showErrorToast = (message) => {
  toast.error(message, {
    position: "top-right",
    autoClose: 5000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

export const showWarningToast = (message) => {
  toast.warning(message, {
    position: "top-right",
    autoClose: 4000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

export const showInfoToast = (message) => {
  toast.info(message, {
    position: "top-right",
    autoClose: 3000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

// You can also create a generic one for API errors
export const handleApiError = (error, defaultMessage = "Đã có lỗi xảy ra!") => {
  if (error && error.response && error.response.data && error.response.data.errors) {
    const errorMessages = error.response.data.errors.map(err => err.detail || err.title).join('\n');
    showErrorToast(errorMessages || defaultMessage);
  } else if (error && error.message) {
    showErrorToast(error.message);
  } else {
    showErrorToast(defaultMessage);
  }
};
```

**f. Tạo `src/components/common/ConfirmDialog.jsx`**
Component hộp thoại xác nhận chung.

```jsx
// src/components/common/ConfirmDialog.jsx
import React from 'react'
import {
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Button,
} from '@mui/material'

function ConfirmDialog({ open, onClose, onConfirm, title, message }) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-description"
    >
      <DialogTitle id="confirm-dialog-title">{title}</DialogTitle>
      <DialogContent>
        <DialogContentText id="confirm-dialog-description">
          {message}
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="primary" variant="outlined">
          Hủy
        </Button>
        <Button onClick={onConfirm} color="secondary" variant="contained" autoFocus>
          Xác nhận
        </Button>
      </DialogActions>
    </Dialog>
  )
}

export default ConfirmDialog
```

**g. Tạo `src/components/common/FormInput.jsx`**
Component input form tái sử dụng, tích hợp với React Hook Form và MUI.

```jsx
// src/components/common/FormInput.jsx
import React from 'react'
import { TextField, FormControl, InputLabel, Select, MenuItem, FormHelperText } from '@mui/material'
import { useController } from 'react-hook-form'

function FormInput({ name, control, label, type = 'text', options, ...props }) {
  const {
    field,
    fieldState: { error },
  } = useController({
    name,
    control,
  })

  if (type === 'select' && options) {
    return (
      <FormControl fullWidth margin="normal" error={!!error}>
        <InputLabel id={`${name}-label`}>{label}</InputLabel>
        <Select
          labelId={`${name}-label`}
          id={name}
          {...field}
          label={label}
          {...props}
        >
          {options.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </Select>
        {error && <FormHelperText>{error.message}</FormHelperText>}
      </FormControl>
    )
  }

  return (
    <TextField
      {...field}
      label={label}
      type={type}
      fullWidth
      margin="normal"
      error={!!error}
      helperText={error ? error.message : null}
      {...props}
    />
  )
}

export default FormInput
```

**h. Tạo `src/components/common/DataTableMUI.jsx`**
Component bảng dữ liệu chung với phân trang và sắp xếp cơ bản.

```jsx
// src/components/common/DataTableMUI.jsx
import React, { useState } from 'react'
import {
  TableContainer,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  TablePagination,
  Paper,
  TableSortLabel,
  Box,
} from '@mui/material'
import { visuallyHidden } from '@mui/utils'

function DataTableMUI({
  columns,
  data,
  totalItems,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onSort,
  orderBy,
  order, // 'asc' or 'desc'
  isLoading,
  onRowClick,
}) {
  const handleSort = (property) => {
    const isAsc = orderBy === property && order === 'asc'
    onSort(property, isAsc ? 'desc' : 'asc')
  }

  return (
    <TableContainer component={Paper}>
      <Table aria-label="data table">
        <TableHead>
          <TableRow>
            {columns.map((column) => (
              <TableCell
                key={column.id}
                align={column.align || 'left'}
                padding={column.disablePadding ? 'none' : 'normal'}
                sortDirection={orderBy === column.id ? order : false}
                style={{ minWidth: column.minWidth }}
              >
                {column.sortable ? (
                  <TableSortLabel
                    active={orderBy === column.id}
                    direction={orderBy === column.id ? order : 'asc'}
                    onClick={() => handleSort(column.id)}
                  >
                    {column.label}
                    {orderBy === column.id ? (
                      <Box component="span" sx={visuallyHidden}>
                        {order === 'desc' ? 'sorted descending' : 'sorted ascending'}
                      </Box>
                    ) : null}
                  </TableSortLabel>
                ) : (
                  column.label
                )}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {isLoading ? (
            <TableRow>
              <TableCell colSpan={columns.length} align="center">
                Đang tải dữ liệu...
              </TableCell>
            </TableRow>
          ) : data.length === 0 ? (
            <TableRow>
              <TableCell colSpan={columns.length} align="center">
                Không tìm thấy dữ liệu.
              </TableCell>
            </TableRow>
          ) : (
            data.map((row, index) => (
              <TableRow
                hover
                key={row.id || index}
                onClick={() => onRowClick && onRowClick(row)}
                sx={{ cursor: onRowClick ? 'pointer' : 'default' }}
              >
                {columns.map((column) => {
                  const value = column.format ? column.format(row[column.id], row) : row[column.id]
                  return (
                    <TableCell key={column.id + row.id} align={column.align || 'left'}>
                      {value}
                    </TableCell>
                  )
                })}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
      <TablePagination
        rowsPerPageOptions={[5, 10, 20]}
        component="div"
        count={totalItems}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={onPageChange}
        onRowsPerPageChange={onRowsPerPageChange}
        labelRowsPerPage="Số hàng mỗi trang:"
        labelDisplayedRows={({ from, to, count }) =>
          `${from}-${to} của ${count !== -1 ? count : `hơn ${to}`}`
        }
      />
    </TableContainer>
  )
}

export default DataTableMUI
```

## 3. Tạo và Cấu hình các Components Layout

Các components này sẽ tạo nên cấu trúc khung sườn của ứng dụng.

**a. Tạo `src/components/layout/Navbar.jsx`**
Thanh điều hướng trên cùng.

```jsx
// src/components/layout/Navbar.jsx
import React from 'react'
import { AppBar, Toolbar, Typography, IconButton } from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import AccountCircle from '@mui/icons-material/AccountCircle'
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
```

**b. Tạo `src/components/layout/Sidebar.jsx`**
Thanh điều hướng bên.

```jsx
// src/components/layout/Sidebar.jsx
import React from 'react'
import { Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Toolbar } from '@mui/material'
import DashboardIcon from '@mui/icons-material/Dashboard'
import MenuBookIcon from '@mui/icons-material/MenuBook'
import PersonIcon from '@mui/icons-material/Person'
import LocalOfferIcon from '@mui/icons-material/LocalOffer'
import CategoryIcon from '@mui/icons-material/Category'
import TranslateIcon from '@mui/icons-material/Translate'
import CollectionsBookmarkIcon from '@mui/icons-material/CollectionsBookmark'
import { NavLink } from 'react-router-dom'
import useUiStore from '../../stores/uiStore'

function Sidebar() {
  const isSidebarOpen = useUiStore((state) => state.isSidebarOpen)

  const menuItems = [
    { text: 'Dashboard', icon: <DashboardIcon />, path: '/dashboard' },
    { text: 'Manga', icon: <MenuBookIcon />, path: '/mangas' },
    { text: 'Authors', icon: <PersonIcon />, path: '/authors' },
    { text: 'Tags', icon: <LocalOfferIcon />, path: '/tags' },
    { text: 'Tag Groups', icon: <CategoryIcon />, path: '/taggroups' },
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
```

**c. Tạo `src/components/layout/AdminLayout.jsx`**
Layout tổng thể cho trang quản trị, bao gồm Navbar và Sidebar.

```jsx
// src/components/layout/AdminLayout.jsx
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
```

## 4. Cập nhật Trang chủ quản lý (Dashboard)

**a. Tạo `src/features/dashboard/DashboardPage.jsx`**
Trang Dashboard cơ bản.

```jsx
// src/features/dashboard/DashboardPage.jsx
import React from 'react'
import { Box, Typography, Grid, Paper } from '@mui/material'

function DashboardPage() {
  return (
    <Box className="dashboard-page">
      <Typography variant="h4" component="h1" className="dashboard-header">
        Chào mừng đến với Bảng điều khiển quản lý Manga
      </Typography>

      <Grid container spacing={4} className="dashboard-stats-grid">
        <Grid item xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Manga</Typography>
            <Typography variant="h4" color="primary">
              123
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Tác giả</Typography>
            <Typography variant="h4" color="primary">
              45
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Tags</Typography>
            <Typography variant="h4" color="primary">
              67
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Chapter đã tải lên</Typography>
            <Typography variant="h4" color="primary">
              890
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      <Paper sx={{ p: 3, boxShadow: 3, borderRadius: 2 }}>
        <Typography variant="h5" component="h2" gutterBottom>
          Thống kê nhanh
        </Typography>
        <Typography variant="body1">
          Đây là nơi hiển thị các biểu đồ và số liệu thống kê quan trọng về dữ liệu manga của bạn.
          Trong tương lai, bạn có thể tích hợp các biểu đồ từ thư viện như Chart.js hoặc Recharts
          để hiển thị các xu hướng hoặc thông tin tổng quan.
        </Typography>
      </Paper>
    </Box>
  )
}

export default DashboardPage
```

## 5. Cập nhật Cấu hình Router

**a. Cập nhật `src/router/AppRoutes.jsx`**
Định nghĩa các route chính của ứng dụng, bao gồm layout Admin và trang Dashboard.

```jsx
// src/router/AppRoutes.jsx
import React from 'react'
import { Routes, Route } from 'react-router-dom'
import AdminLayout from '../components/layout/AdminLayout'
import DashboardPage from '../features/dashboard/DashboardPage'
import LoginPage from '../features/auth/LoginPage' // Placeholder, will be implemented in Step 5

function AppRoutes() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<LoginPage />} />

      {/* Protected Routes (Admin Layout) */}
      <Route path="/" element={<AdminLayout />}>
        {/* Default route redirects to dashboard */}
        <Route index element={<DashboardPage />} />
        <Route path="dashboard" element={<DashboardPage />} />
        
        {/* Placeholder for Manga Management (Step 4) */}
        <Route path="mangas" element={<div>Manga List Page (TODO)</div>} />
        <Route path="mangas/create" element={<div>Manga Create Page (TODO)</div>} />
        <Route path="mangas/edit/:id" element={<div>Manga Edit Page (TODO)</div>} />

        {/* Placeholder for other features */}
        <Route path="authors" element={<div>Authors List Page (TODO)</div>} />
        <Route path="tags" element={<div>Tags List Page (TODO)</div>} />
        <Route path="taggroups" element={<div>Tag Groups List Page (TODO)</div>} />
      </Route>

      {/* Fallback for undefined routes */}
      <Route path="*" element={<div>404 Not Found</div>} />
    </Routes>
  )
}

export default AppRoutes
```