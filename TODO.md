Chào bạn,

Tôi đã xem xét lại vấn đề và bạn đã phân tích rất chính xác! Lỗi nằm ở chỗ `MuiDrawer-root` vẫn chiếm không gian layout ngay cả khi thuộc tính `open` của nó là `false`. Tôi xin lỗi vì đã cung cấp giải pháp chưa triệt để ở lần trước.

Nguyên nhân chính là do cách chúng ta áp dụng style cho `Drawer` và `Box` nội dung chính chưa đúng với cách hoạt động của `variant="persistent"` trong Material-UI. Component `Drawer` khi đóng không bị loại khỏi cây DOM, mà nó được dịch chuyển ra khỏi màn hình, và phần nội dung chính cần phải được điều chỉnh để lấp vào khoảng trống đó.

Dưới đây là phiên bản `TODO.md` đã được sửa lại hoàn toàn để khắc phục triệt để vấn đề này. Bạn chỉ cần cập nhật 2 file: `AdminLayout.jsx` và `Sidebar.jsx`.

---

```markdown
# TODO - Sửa Lỗi Giao Diện Co Giãn Khi Đóng/Mở Sidebar (Phiên bản cuối)

Tài liệu này hướng dẫn cách sửa lỗi layout khiến nội dung chính không co giãn đúng cách khi đóng/mở sidebar. Lỗi này xảy ra do `MuiDrawer-root` vẫn chiếm không gian layout khi bị ẩn.

### Bước 1: Cập nhật `Sidebar.jsx`

Đây là bước quan trọng nhất để sửa lỗi. Chúng ta sẽ **loại bỏ** việc set `width` cho thẻ `Drawer` gốc và chỉ áp dụng `width` cho phần "giấy" (`MuiDrawer-paper`) bên trong nó. Điều này cho phép MUI tự quản lý việc ẩn/hiện của component gốc.

Đồng thời, chúng ta sẽ thêm logic để trên màn hình di động, sidebar sẽ có `variant="temporary"` (trượt ra và che phủ), còn trên máy tính sẽ là `variant="persistent"` (đẩy nội dung).

```jsx
// MangaReader_ManagerUI\mangareader_managerui.client\src\components\layout\Sidebar.jsx
import CategoryIcon from '@mui/icons-material/Category';
import LocalOfferIcon from '@mui/icons-material/LocalOffer';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import PersonIcon from '@mui/icons-material/Person';
import { Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, useMediaQuery, useTheme } from '@mui/material';
import React from 'react';
import { NavLink } from 'react-router-dom';
import useUiStore from '../../stores/uiStore';

function Sidebar() {
  const { isSidebarOpen, setSidebarOpen } = useUiStore();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const sidebarWidth = '240px';

  const menuItems = [
    { text: 'Manga', icon: <MenuBookIcon />, path: '/mangas' },
    { text: 'Authors', icon: <PersonIcon />, path: '/authors' },
    { text: 'Tags', icon: <LocalOfferIcon />, path: '/tags' },
    { text: 'Tag Groups', icon: <CategoryIcon />, path: '/taggroups' },
  ];

  const drawerContent = (
    <List className="sidebar-list" sx={{ pt: 2 }}>
      {menuItems.map((item) => (
        <ListItem key={item.text} disablePadding className="sidebar-list-item">
          <ListItemButton
            component={NavLink}
            to={item.path}
            className={({ isActive }) => (isActive ? 'Mui-selected' : '')}
            onClick={isMobile ? () => setSidebarOpen(false) : undefined} // Đóng sidebar khi click trên mobile
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
      variant={isMobile ? "temporary" : "persistent"}
      anchor="left"
      open={isSidebarOpen}
      onClose={() => setSidebarOpen(false)} // Cần cho variant="temporary"
      sx={{
        flexShrink: 0,
        // Chỉ định style cho phần .MuiDrawer-paper bên trong
        [`& .MuiDrawer-paper`]: {
          width: sidebarWidth,
          boxSizing: 'border-box',
          marginTop: { xs: '56px', sm: '64px' }, // Điều chỉnh theo chiều cao AppBar responsive
          height: { xs: 'calc(100% - 56px)', sm: 'calc(100% - 64px)' },
          borderRight: '1px solid rgba(0, 0, 0, 0.12)',
        },
      }}
      ModalProps={{
        keepMounted: true, // Tốt hơn cho SEO và hiệu năng trên mobile
      }}
    >
      {drawerContent}
    </Drawer>
  );
}

export default Sidebar;
```

### Bước 2: Cập nhật `AdminLayout.jsx`

Bây giờ `Sidebar` đã hoạt động đúng, chúng ta sẽ cập nhật `AdminLayout` để `Box` chứa nội dung chính phản ứng chính xác với trạng thái của sidebar. Logic sẽ là: chỉ khi **không phải mobile** VÀ **sidebar đang mở**, thì mới đẩy nội dung sang phải.

```jsx
// MangaReader_ManagerUI\mangareader_managerui.client\src\components\layout\AdminLayout.jsx
import { Box, useMediaQuery, useTheme } from '@mui/material';
import React from 'react';
import { Outlet } from 'react-router-dom';
import useUiStore from '../../stores/uiStore';
import LoadingSpinner from '../common/LoadingSpinner';
import Navbar from './Navbar';
import Sidebar from './Sidebar';

function AdminLayout() {
  const isSidebarOpen = useUiStore((state) => state.isSidebarOpen);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const sidebarWidth = '240px';

  return (
    <Box sx={{ display: 'flex', height: '100vh' }}>
      <Navbar />
      <Sidebar />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          transition: theme.transitions.create('margin', {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.leavingScreen,
          }),
          // Chỉ đẩy nội dung khi ở màn hình lớn VÀ sidebar đang mở
          marginLeft: !isMobile && isSidebarOpen ? sidebarWidth : 0,
          // Chiều cao AppBar khác nhau trên mobile và desktop
          marginTop: { xs: '56px', sm: '64px' }, 
          height: { xs: 'calc(100vh - 56px)', sm: 'calc(100vh - 64px)' },
          overflowY: 'auto',
        }}
      >
        <Outlet />
      </Box>
      <LoadingSpinner />
    </Box>
  );
}

export default AdminLayout;
```

### Giải thích thay đổi

1.  **`Sidebar.jsx`**:
    *   **Loại bỏ `width` khỏi `sx` gốc**: Thay vì `sx={{ width: sidebarWidth, ... }}`, chúng ta nhắm thẳng vào class nội bộ `& .MuiDrawer-paper` để set `width`. Điều này để `Drawer` gốc tự co lại khi `open={false}`.
    *   **Responsive `variant`**: Sử dụng `useMediaQuery` để chuyển `variant` của `Drawer` thành `"temporary"` trên màn hình nhỏ. Nó sẽ hoạt động như một menu trượt ra và che phủ nội dung, đây là hành vi người dùng mong đợi trên di động.
    *   **`onClose` và `onClick`**: Thêm `onClose` cho `Drawer` và `onClick` cho `ListItemButton` để tự động đóng sidebar khi người dùng chọn một mục trên di động.

2.  **`AdminLayout.jsx`**:
    *   **Logic `marginLeft` được đơn giản hóa**: `marginLeft` của nội dung chính giờ đây chỉ phụ thuộc vào 2 điều kiện: `!isMobile` và `isSidebarOpen`. Nếu cả hai đều đúng, nội dung bị đẩy sang phải. Trong mọi trường hợp khác (đang ở mobile hoặc sidebar đã đóng), `marginLeft` là `0`, cho phép nội dung chiếm toàn bộ không gian.
    *   **Chiều cao và `marginTop` Responsive**: Chiều cao `AppBar` trên mobile thường thấp hơn, nên `marginTop` và `height` của nội dung chính cũng được điều chỉnh tương ứng.

Sau khi áp dụng 2 thay đổi này, layout của bạn sẽ hoạt động chính xác như mong đợi. Tôi rất xin lỗi vì sự nhầm lẫn trước đó.