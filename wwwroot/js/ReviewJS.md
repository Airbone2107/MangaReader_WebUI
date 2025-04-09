# Phân Tích Hệ Thống JavaScript - Manga Reader Web

## Giai Đoạn 1: Chuẩn Bị và Phân Tích

### 1. Backup Dự Án
- Cần tạo một bản sao lưu (commit Git) trước khi bắt đầu quá trình refactor

### 2. Review Code Hiện Tại

#### `main.js` - Entry point chính
- **Chức năng:** Là file chính để import và khởi tạo tất cả các module
- **Hoạt động:**
  - Import nhiều module từ các file trong thư mục `modules/`
  - Khởi tạo tất cả các module trong sự kiện `DOMContentLoaded`
  - Đăng ký xử lý các sự kiện `htmx:afterSwap` để khởi tạo lại các module khi HTMX thay đổi nội dung
- **Global scope:** Không trực tiếp đưa biến/hàm vào global scope
- **Vấn đề:** Có một số logic trùng lặp với `htmx-handlers.js` trong việc xử lý sau khi HTMX swap

#### `auth.js` - Xử lý xác thực
- **Chức năng:** Xử lý xác thực và quản lý thông tin người dùng
- **Hoạt động:**
  - Tự khởi tạo trong `DOMContentLoaded` (không thông qua `main.js`)
  - Gọi API để kiểm tra trạng thái đăng nhập
  - Cập nhật giao diện dựa trên trạng thái đăng nhập
- **Global scope:** Không trực tiếp đưa biến/hàm vào global scope
- **Vấn đề:** 
  - File này được load riêng lẻ, không thông qua `main.js`
  - Cần module hóa và tích hợp vào hệ thống khởi tạo chính

#### `manga-details.js` (File gốc)
- **Chức năng:** JavaScript cho trang chi tiết manga
- **Hoạt động:** 
  - Import các hàm từ module `modules/manga-details.js`
  - Đưa các hàm `toggleFollow` và `showToast` vào global scope
  - Khởi tạo trang chi tiết manga trong `DOMContentLoaded`
  - Thiết lập xử lý sự kiện HTMX cho trang chi tiết manga
- **Global scope:** 
  - `window.toggleFollow` - Dùng cho nút theo dõi/hủy theo dõi manga
  - `window.showToast` - Hiển thị thông báo
- **Vấn đề:**
  - File này load riêng lẻ trong view `Details.cshtml`
  - Đưa các hàm vào global scope thay vì sử dụng event delegation
  - Có logic xử lý HTMX trùng lặp với `htmx-handlers.js`

#### `modules/manga-details.js` - Module chi tiết manga
- **Chức năng:** Xử lý các chức năng JavaScript cho trang chi tiết manga
- **Hoạt động:** 
  - Cung cấp nhiều hàm cho trang chi tiết manga:
    - `adjustHeaderBackgroundHeight` - Điều chỉnh chiều cao header
    - `initDropdowns` - Khởi tạo các dropdown
    - `initLanguageFilter` - Khởi tạo bộ lọc ngôn ngữ
    - `initChapterItems` - Khởi tạo danh sách chapter
    - `initFollowButton` - Khởi tạo nút theo dõi
    - `toggleFollow` - Xử lý theo dõi/hủy theo dõi
    - `showToast` - Hiển thị thông báo
    - `initMangaDetailsPage` - Khởi tạo toàn bộ trang
    - `initAfterHtmxLoad` - Khởi tạo sau khi HTMX load nội dung
- **Global scope:** Không trực tiếp đưa biến/hàm vào global scope 
- **Vấn đề:** Function `toggleFollow` được sử dụng thông qua global scope từ file gốc `manga-details.js`

#### `modules/htmx-handlers.js` - Xử lý HTMX
- **Chức năng:** Quản lý tất cả chức năng liên quan đến HTMX
- **Hoạt động:**
  - Cung cấp hàm `reinitializeAfterHtmxSwap` để khởi tạo lại các chức năng sau khi HTMX swap
  - Cung cấp hàm `initHtmxHandlers` để khởi tạo các sự kiện HTMX
- **Global scope:** Không trực tiếp đưa biến/hàm vào global scope
- **Vấn đề:**
  - Có một số logic trùng lặp với file `manga-details.js` trong việc xử lý sau khi HTMX swap
  - Cần cải thiện việc phát hiện và khởi tạo lại các chức năng dựa trên phần tử cụ thể được HTMX swap

### 3. Phân Tích Global Scope
- **Hàm đưa vào global scope:**
  - `window.toggleFollow` - Từ `manga-details.js`
  - `window.showToast` - Từ `manga-details.js`

### 4. Phần Tử DOM Thường Xuyên Hoán Đổi Bởi HTMX
- **`#main-content`** - Phần tử chính chứa nội dung trang
- **`.details-manga-header-background`** - Phần tử header trong trang chi tiết manga
- **`#searchForm`** - Form tìm kiếm 
- **`.pagination`** - Phần tử phân trang

## Kết Luận và Đề Xuất

### Vấn Đề Chính Cần Giải Quyết
1. **Tổ chức không nhất quán:**
   - `auth.js` load riêng lẻ, không thông qua `main.js`
   - `manga-details.js` load riêng lẻ trong view Details.cshtml

2. **Global scope không cần thiết:**
   - `toggleFollow` và `showToast` được đưa vào global scope

3. **Logic trùng lặp:**
   - Logic xử lý HTMX trùng lặp giữa `manga-details.js` và `htmx-handlers.js`
   - Nhiều hàm khởi tạo được gọi ở nhiều nơi khác nhau

4. **Khởi tạo không hiệu quả sau HTMX swap:**
   - Một số hàm khởi tạo được gọi mà không kiểm tra phần tử cụ thể được HTMX swap

### Đề Xuất Refactor
1. **Module hóa `auth.js`:**
   - Chuyển đổi thành module ES6
   - Đưa logic khởi tạo vào `main.js`

2. **Loại bỏ file `manga-details.js` (gốc):**
   - Xử lý tất cả tính năng thông qua module `modules/manga-details.js`
   - Sử dụng event delegation thay vì đưa hàm vào global scope

3. **Cải thiện `htmx-handlers.js`:**
   - Thêm parameter `targetElement` cho hàm `reinitializeAfterHtmxSwap`
   - Khởi tạo có điều kiện dựa trên phần tử cụ thể được swap

4. **Tạo module riêng cho showToast:**
   - Chuyển hàm `showToast` sang một module riêng
   - Import và sử dụng hàm này ở những nơi cần thiết

5. **Khởi tạo tùy theo nội dung:**
   - Trong `main.js`, chỉ khởi tạo các module cần thiết dựa trên nội dung trang hiện tại
   - Trong `htmx-handlers.js`, chỉ khởi tạo lại các module cần thiết dựa trên nội dung đã swap
