# wwwroot

Thư mục `wwwroot` là thư mục gốc công khai (web root) cho ứng dụng ASP.NET Core. Nó chứa tất cả các tài nguyên tĩnh mà trình duyệt có thể truy cập trực tiếp.

## Cấu trúc

- **`css/`**: Chứa các file Cascading Style Sheets (CSS) định dạng giao diện người dùng.
  - **`core/`**: Các file CSS cốt lõi, định nghĩa các biến, style cơ bản, layout, components, và các tiện ích chung.
    - `variables.css`: Định nghĩa các biến CSS (màu sắc, z-index, ...) cho cả chế độ sáng và tối.
    - `base.css`: Style cơ bản cho `html`, `body`, animations, scrollbar.
    - `layout.css`: Style cho cấu trúc layout chính (container, header, content area).
    - `components.css`: Style cho các UI component tái sử dụng (cards, buttons, dropdowns, pagination).
    - `navbar.css`: Style riêng cho thanh điều hướng (header).
    - `sidebar.css`: Style riêng cho menu sidebar trượt.
    - `manga.css`: Style chung liên quan đến hiển thị manga/chapter (cover, lists).
    - `utilities.css`: Các lớp CSS tiện ích (text truncation, spacing).
    - `responsive.css`: Tập trung các media queries chính điều chỉnh layout tổng thể.
  - **`pages/`**: Các file CSS đặc thù cho từng trang hoặc nhóm trang.
    - `home.css`: Style riêng cho trang chủ.
    - `manga-details.css`: Style riêng cho trang chi tiết manga.
    - `chapter.css`: Style riêng cho trang đọc chapter.
    - `search/`: Style cho trang tìm kiếm.
      - `index.css`: File import chính cho trang tìm kiếm.
      - `search-card.css`, `manga-card.css`, `tag-base.css`, `dropdown.css`, `filter.css`, `manga-tags.css`, `responsive.css`: Các file CSS module cho trang tìm kiếm.
  - `main.css`: File CSS chính, import tất cả các file CSS khác theo thứ tự đúng. **Đây là file duy nhất được link vào layout.**
- **`js/`**: Chứa các file JavaScript xử lý logic phía client.
  - **`modules/`**: Các file JavaScript được tổ chức thành module theo chức năng.
    - `sidebar.js`: Xử lý đóng/mở sidebar, cập nhật link active.
    - `htmx-handlers.js`: Xử lý các sự kiện và khởi tạo lại JS sau khi HTMX tải nội dung. Đảm bảo các chức năng JS vẫn hoạt động với nội dung động được tải qua HTMX.
    - `ui.js`: Các chức năng UI chung (tooltips, lazy loading, back-to-top, responsive adjustments).
    - `theme.js`: Xử lý chuyển đổi chế độ sáng/tối.
    - `toast.js`: Hiển thị thông báo toast. Cung cấp hàm `showToast` toàn cục.
    - `search.js`: Logic cho trang tìm kiếm (filter dropdowns, quick search).
    - `manga-tags.js`: Logic xử lý việc chọn và hiển thị tags manga trong form tìm kiếm.
    - `reading-state.js`: Quản lý trạng thái đọc (lưu/lấy từ localStorage).
    - `error-handling.js`: Xử lý lỗi phía client (lỗi ảnh, lỗi API).
    - `manga-details.js`: Logic riêng cho trang chi tiết manga (điều chỉnh background, dropdown chapter, theo dõi/hủy theo dõi manga).
  - `main.js`: File JavaScript chính, import và khởi tạo các module cần thiết. Đây là điểm nhập duy nhất cho tất cả các script toàn cục, đảm bảo khởi tạo một cách có điều kiện dựa trên nội dung trang hiện tại.
  - `auth.js`: Module xử lý logic xác thực phía client (kiểm tra trạng thái đăng nhập, cập nhật UI).
- **`lib/`**: Chứa các thư viện JavaScript và CSS của bên thứ ba.
  - `bootstrap/`: Bootstrap 5 framework.
  - `jquery/`: Thư viện jQuery.
  - `htmx/`: Thư viện HTMX.
  - `jquery-validation/`, `jquery-validation-unobtrusive/`: Thư viện validation (có thể không cần thiết nếu không dùng form validation phía client nhiều).
- **`images/`**: Chứa các file hình ảnh tĩnh được sử dụng trong giao diện (ví dụ: ảnh placeholder).
- **`favicon.png`**: Biểu tượng hiển thị trên tab trình duyệt.

## Kiến trúc JavaScript

### Luồng Khởi tạo

1. **Khởi tạo ban đầu (DOMContentLoaded):**
   - `main.js` là điểm khởi đầu chính, được tải trong layout và import tất cả các module cần thiết.
   - Trong sự kiện `DOMContentLoaded`, các module được khởi tạo có điều kiện dựa trên nội dung trang hiện tại.
   - Một số module như `toast.js` cung cấp hàm global (`window.showToast`) cho tiện ích.

2. **Khởi tạo lại sau HTMX swap:**
   - Module `htmx-handlers.js` đăng ký sự kiện `htmx:afterSwap` để theo dõi khi HTMX cập nhật nội dung.
   - Khi phát hiện swap, hàm `reinitializeAfterHtmxSwap(targetElement)` được gọi, nhận phần tử đích đã được swap.
   - Hàm này khởi tạo lại các chức năng JS cần thiết *chỉ* cho phần tử đã swap, dựa trên các selector đặc biệt.
   - Đảm bảo các dropdown, tooltip, tab, accordion... được khởi tạo lại chính xác trên nội dung mới.

3. **Xử lý sự kiện:**
   - Ưu tiên sử dụng event delegation thay vì gắn event listener trực tiếp vào các phần tử có thể thay đổi.
   - Các event handler được gắn vào các container ổn định (không bị swap) hoặc được khởi tạo lại trong `reinitializeAfterHtmxSwap`.

### Các Kỹ thuật Quan trọng

- **Khởi tạo có điều kiện:** Chỉ khởi tạo các chức năng cần thiết dựa trên nội dung trang hiện tại.
- **Tránh global scope:** Hầu hết các hàm/biến được đóng gói trong module ES6; chỉ một vài hàm tiện ích như `showToast` được đưa vào global scope.
- **Event delegation:** Gắn event listener vào các container ổn định, không phải các phần tử có thể thay đổi.
- **Clean-up trước khi khởi tạo lại:** Luôn làm sạch (dispose) các instance cũ trước khi tạo mới để tránh memory leak.

## Lưu ý

- Các tài nguyên trong `wwwroot` được phục vụ trực tiếp bởi web server.
- Dự án hiện tại không sử dụng quy trình build/bundle phức tạp cho CSS/JS. File `main.css` và `main.js` đóng vai trò là điểm nhập chính.
- Thứ tự import trong `main.css` và `main.js` rất quan trọng để đảm bảo các style và script hoạt động đúng.
