// Plan.md
# Kế Hoạch Triển Khai Project MangaReader_ManagerUI

## 1. Mục Tiêu Dự Án

Xây dựng một ứng dụng Frontend (React App) tên là `MangaReader_ManagerUI` để quản lý danh sách manga, bao gồm các thông tin liên quan như tác giả, thể loại, chương truyện, ảnh bìa, và bản dịch. Project này sẽ tương tác với `MangaReaderAPI` thông qua project `.NET Class Library` tên là `MangaReaderLib`.

## 2. Các Bước Thực Hiện Tổng Quát

### Bước 1: Thiết Lập Môi Trường và Project Frontend (MangaReader_ManagerUI.Client)

1.  **Tạo Project React App:**
    *   Trong Visual Studio, tạo một project mới sử dụng template "ASP.NET Core with React.js".
    *   Đặt tên project là `MangaReader_ManagerUI`. Điều này sẽ tạo ra `MangaReader_ManagerUI.Server` (ASP.NET Core) và `mangareader_managerui.client` (React).
    *   Đặt project này song song với `MangaReader_WebUI` trong solution `MangaReaderFrontEnd`.

2.  **Cài đặt thư viện cần thiết cho `mangareader_managerui.client`:**
    *   **Axios:** Để thực hiện các request HTTP đến `MangaReader_ManagerUI.Server`.
        ```bash
        npm install axios
        ```
    *   **React Router DOM:** Cho việc điều hướng trang.
        ```bash
        npm install react-router-dom
        ```
    *   **Zustand:** State management library.
        ```bash
        npm install zustand
        ```
    *   **Material UI (MUI):** Thư viện UI.
        ```bash
        npm install @mui/material @emotion/react @emotion/styled @mui/icons-material
        ```
    *   **React Hook Form & Zod:** Cho quản lý form và validation.
        ```bash
        npm install react-hook-form zod @hookform/resolvers
        ```
    *   **React Toastify:** Để hiển thị thông báo (toast).
        ```bash
        npm install react-toastify
        ```
    *   **Date-fns:** Thư viện tiện ích cho ngày tháng.
        ```bash
        npm install date-fns
        ```
    *   **Sass:** Để sử dụng SCSS cho styling.
        ```bash
        npm install sass --save-dev
        ```

3.  **Cấu trúc thư mục ban đầu cho `mangareader_managerui.client` (sử dụng SCSS):**
    *   `public/`: Các file tĩnh public.
    *   `src/`:
        *   `api/`: Chứa các module gọi API (sử dụng Axios).
            *   `apiClient.js`: Cấu hình instance Axios.
            *   Các file service cho từng resource (ví dụ: `mangaApi.js`, `authorApi.js`).
        *   `assets/`: Hình ảnh, fonts, etc.
            *   `scss/`: Thư mục chứa các file SCSS.
                *   `base/`: SCSS cho các element HTML cơ bản, typography, resets.
                    *   `_reset.scss`
                    *   `_typography.scss`
                *   `components/`: SCSS cho các UI components tái sử dụng (Button, Modal, Table).
                    *   `_buttons.scss`
                    *   `_tables.scss`
                *   `layout/`: SCSS cho các phần layout (Sidebar, Navbar, Grid).
                    *   `_sidebar.scss`
                    *   `_navbar.scss`
                *   `pages/`: SCSS đặc thù cho từng trang hoặc feature.
                    *   `_login.scss`
                    *   `_mangaList.scss`
                *   `themes/`: (Tùy chọn) SCSS cho theme (ví dụ: dark mode, light mode).
                    *   `_default.scss`
                *   `utils/`: SCSS chứa mixins, functions, variables.
                    *   `_variables.scss`
                    *   `_mixins.scss`
                    *   `_functions.scss`
                *   `main.scss`: File SCSS chính, import tất cả các partials khác.
        *   `components/`: UI components tái sử dụng.
            *   `common/`: Components chung (Button, Modal, TableWrapper - có thể dùng MUI).
            *   `layout/`: Components layout (Sidebar, Navbar - sử dụng MUI).
        *   `constants/`: Hằng số ứng dụng.
        *   `features/`: Nhóm code theo chức năng lớn (ví dụ: auth, manga, chapter).
            *   Mỗi feature có thể có `components/`, `pages/`, `storeNameStore.js` (Zustand store).
            *   Form trong features sẽ dùng React Hook Form và Zod.
        *   `hooks/`: Custom React Hooks.
        *   `router/`: Cấu hình React Router DOM.
            *   `AppRoutes.jsx`
            *   `ProtectedRoute.jsx` (nếu cần).
        *   `schemas/`: Chứa các Zod schema cho validation.
        *   `stores/`: Chứa các Zustand stores.
            *   `authStore.js`
            *   `mangaStore.js`
            *   `uiStore.js` (cho UI state như loading, modals).
        *   `types/` (Nếu dùng TypeScript): Định nghĩa types/interfaces.
        *   `utils/`: Các hàm tiện ích (ví dụ: `dateUtils.js` sử dụng `date-fns`).
        *   `App.jsx`: Component gốc, chứa RouterProvider, ThemeProvider (MUI), ToastContainer (React Toastify). Import `main.scss` ở đây.
        *   `main.jsx`: Điểm vào của React app.

### Bước 2: Xây Dựng Project `MangaReaderLib` và Tích Hợp

1.  **Phân tích `FrontendAPI.md`:**
    *   Xác định DTOs cần thiết cho request và response của `MangaReaderAPI`.
    *   Nắm rõ cấu trúc `ResourceObject`, `RelationshipObject`, và các cấu trúc response chung.
2.  **Thiết kế Client Services/Repositories trong `MangaReaderLib`:**
    *   Tạo các interfaces (ví dụ: `IMangaClient`, `IAuthorClient`).
    *   Triển khai các client này, sử dụng `HttpClient` (quản lý qua `IHttpClientFactory` trong `MangaReader_ManagerUI.Server`) để gọi đến các endpoint của `MangaReaderAPI` được định nghĩa trong `FrontendAPI.md`.
    *   Mỗi client sẽ chứa các phương thức tương ứng với các API.
3.  **Xử lý dữ liệu trong `MangaReaderLib`:**
    *   Deserialize JSON response từ `MangaReaderAPI` thành các DTOs.
    *   Xử lý lỗi cơ bản từ API.
    *   Xây dựng request với các tham số phân trang, lọc, sắp xếp.
    *   Xử lý upload file (sẽ được `MangaReader_ManagerUI.Server` gọi).
4.  **Tích hợp `MangaReaderLib` vào `MangaReader_ManagerUI.Server`:**
    *   Project `MangaReader_ManagerUI.Server` (ASP.NET Core) sẽ tham chiếu đến `MangaReaderLib`.
    *   Các API Controllers trong `MangaReader_ManagerUI.Server` sẽ inject và sử dụng các client services từ `MangaReaderLib` để tương tác với `MangaReaderAPI`.
    *   `MangaReader_ManagerUI.Client` (React App) sẽ gọi các API do `MangaReader_ManagerUI.Server` cung cấp.

### Bước 3: Thiết Kế và Triển Khai Giao Diện Người Dùng (UI) với Material UI và SCSS

1.  **Layout chính:**
    *   Sử dụng các component layout của Material UI (ví dụ: `AppBar`, `Drawer`, `Container`, `Grid`) để xây dựng layout chung cho ứng dụng quản lý.
    *   Style cho layout sẽ được viết bằng SCSS, đặt trong thư mục `src/assets/scss/layout/`.
    *   Tích hợp React Router DOM cho điều hướng.
2.  **UI Components cơ bản:**
    *   Tạo các components tái sử dụng từ Material UI:
        *   Bảng dữ liệu: Sử dụng `<Table>`, `<TableHead>`, `<TableRow>`, `<TableCell>` của MUI, tích hợp phân trang, sắp xếp. Style tùy chỉnh bằng SCSS nếu cần (`src/assets/scss/components/_tables.scss`).
        *   Form nhập liệu: Sử dụng `<TextField>`, `<Select>`, `<DatePicker>` (từ `@mui/x-date-pickers` nếu cần) của MUI, tích hợp với React Hook Form và Zod.
        *   Modal/Dialog: Sử dụng `<Dialog>` của MUI.
        *   Nút bấm: Sử dụng `<Button>` của MUI. Style tùy chỉnh bằng SCSS (`src/assets/scss/components/_buttons.scss`).
3.  **Trang chủ quản lý:**
    *   Một dashboard cơ bản hoặc trang danh sách manga mặc định.
    *   SCSS cho trang này sẽ nằm trong `src/assets/scss/pages/_dashboard.scss` hoặc tương tự.

### Bước 4: Triển Khai Chức Năng Quản Lý Chính (Theo `FrontendAPI.md`)

Triển khai các chức năng CRUD cho từng resource, sử dụng các components đã tạo và gọi API thông qua `MangaReader_ManagerUI.Server` (và `MangaReaderLib`). SCSS cho từng trang/feature sẽ được đặt trong `src/assets/scss/pages/`.

1.  **Quản lý Manga (`/Mangas`):**
    *   UI: Trang danh sách manga (dùng `DataTableMUI`), trang tạo/chỉnh sửa manga (dùng `MangaForm` với MUI components, React Hook Form, Zod).
    *   Logic: Gọi các API tương ứng từ `mangaApi.js` (React).
2.  **Quản lý Ảnh bìa (`/mangas/{mangaId}/covers`, `/CoverArts/{coverId}`):**
    *   UI: Component upload ảnh (có thể dùng input file HTML hoặc component upload của MUI), danh sách ảnh bìa.
    *   Logic: Xử lý upload file ở client, gửi đến API của `MangaReader_ManagerUI.Server`.
3.  **Quản lý Bản dịch (`/TranslatedMangas`, `/mangas/{mangaId}/translations`):**
    *   Tương tự quản lý Manga.
4.  **Quản lý Chapters (cho từng `TranslatedManga`):**
    *   Tương tự quản lý Manga.
5.  **Quản lý Trang (ChapterPage) (cho từng `Chapter`):**
    *   Tương tự quản lý Ảnh bìa (có bước tạo entry trước).
6.  **Quản lý Tác giả (Authors) và Tags/TagGroups (phục vụ chọn lựa):**
    *   UI: Component chọn (ví dụ: `Autocomplete` của MUI) để chọn từ danh sách.
    *   Logic: Lấy danh sách từ API để hiển thị trong component chọn.

### Bước 5: Triển Khai Xác Thực

1.  **Trang Đăng Nhập:**
    *   UI: Form đăng nhập sử dụng Material UI, React Hook Form, Zod. SCSS trong `src/assets/scss/pages/_login.scss`.
2.  **Logic Đăng Nhập:**
    *   React App gọi API đăng nhập của `MangaReader_ManagerUI.Server`.
    *   `MangaReader_ManagerUI.Server` sẽ gọi API đăng nhập của `MangaReaderAPI` (cần backend cung cấp cơ chế này).
    *   Lưu trữ token (JWT) trong `localStorage` hoặc `sessionStorage` (hoặc dùng HttpOnly cookie từ server nếu có thể).
    *   Sử dụng Zustand (`authStore.js`) để quản lý trạng thái đăng nhập.
3.  **Xử lý Request xác thực:**
    *   `apiClient.js` (Axios instance) sẽ có interceptor để tự động đính kèm token vào header `Authorization` cho các request cần xác thực.
4.  **Bảo vệ Route:**
    *   Sử dụng `ProtectedRoute.jsx` kết hợp với `authStore` để bảo vệ các trang quản lý.
5.  **Đăng Xuất:**
    *   Xóa token, cập nhật `authStore`.

### Bước 6: Kiểm Thử và Hoàn Thiện

1.  **Kiểm thử chức năng:** Toàn diện các tính năng CRUD, xác thực, phân trang, lọc, sắp xếp.
2.  **Xử lý lỗi:**
    *   Hiển thị thông báo lỗi thân thiện (dùng `react-toastify`) khi có lỗi từ API hoặc client-side.
3.  **Tối ưu hóa:** Hiệu năng, lazy loading components.
4.  **Responsive Design:** Kiểm tra với các components của Material UI và các SCSS tùy chỉnh.
5.  **Review và Refactor Code.**

### Bước 7 (Tương lai): Phân Quyền

1.  Sau khi `MangaReaderAPI` có cơ chế phân quyền:
    *   `MangaReader_ManagerUI.Server` sẽ lấy thông tin quyền và truyền cho client.
    *   React App (sử dụng `authStore` hoặc context riêng) sẽ quản lý quyền.
    *   Ẩn/hiện UI elements hoặc hạn chế truy cập route dựa trên quyền.

## Lưu ý

*   **Cloudinary URL:** Việc xây dựng URL ảnh từ `publicId` sẽ thực hiện ở `MangaReader_ManagerUI.Client`.
*   **API Base URL:**
    *   `MangaReader_ManagerUI.Client` sẽ gọi đến base URL của `MangaReader_ManagerUI.Server`.
    *   `MangaReaderLib` (thông qua `MangaReader_ManagerUI.Server`) sẽ gọi đến base URL của `MangaReaderAPI`.
*   **Xử lý `relationships`:** Frontend Manager UI sẽ phân tích `relationships` từ API để hiển thị hoặc fetch thêm dữ liệu liên quan.

Kế hoạch này đã được cập nhật để phản ánh các thư viện bạn đã chọn và bổ sung việc sử dụng SCSS. Chúc bạn triển khai dự án thành công!