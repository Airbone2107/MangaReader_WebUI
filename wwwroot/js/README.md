# JavaScript Structure - Manga Reader Web

## Cấu trúc

```
js/
├── modules/ # Các module JavaScript theo chức năng
│ ├── htmx-handlers.js # Quản lý sự kiện HTMX và khởi tạo lại JS
│ ├── sidebar.js # Quản lý sidebar (đóng/mở, active links)
│ ├── ui.js # Chức năng UI chung (tooltips, lazy load, etc)
│ ├── theme.js # Quản lý chế độ tối/sáng
│ ├── toast.js # Hiển thị thông báo toast
│ ├── search.js # Logic cho trang tìm kiếm (filters, pagination)
│ ├── manga-details.js # Logic cho trang chi tiết manga
│ ├── manga-tags.js # Quản lý chọn/lọc tags manga
│ ├── reading-state.js # Lưu trữ trạng thái đọc
│ ├── error-handling.js # Xử lý lỗi
│ └── custom-dropdown.js # Quản lý custom dropdown thay thế Bootstrap
├── main.js # File chính, import và khởi tạo các module
└── auth.js # Module xử lý xác thực
```

## Luồng Hoạt Động
### 1. Khởi Tạo Ban Đầu (`main.js`)

-   `main.js` là điểm vào chính, được tải trong layout (`_Layout.cshtml`).
-   Nó import các module cần thiết và gọi các hàm `init...()` tương ứng trong sự kiện `DOMContentLoaded`.
-   Việc khởi tạo có thể là **toàn cục** (luôn chạy, ví dụ: `initSidebar`, `initThemeSwitcher`, `initCustomDropdowns`) hoặc **có điều kiện** (chỉ chạy nếu selector tương ứng tồn tại trên trang, ví dụ: `initMangaDetailsPage` chỉ chạy nếu có `.details-manga-header-background`).

### 2. Xử Lý HTMX (`htmx-handlers.js`)

Đây là module **quan trọng nhất** để đảm bảo JavaScript hoạt động với nội dung động do HTMX tải.

-   **`htmx:afterSwap`**: Sự kiện này được kích hoạt **sau khi** HTMX thay thế (swap) một phần tử trên trang (ví dụ: tải kết quả tìm kiếm mới, phân trang).
    -   Hàm `reinitializeAfterHtmxSwap(targetElement)` được gọi.
    -   Hàm này nhận `targetElement` (phần tử vừa được swap) làm tham số.
    -   Nó chỉ khởi tạo lại các chức năng JavaScript **cần thiết cho `targetElement` đó** (ví dụ: tooltips, dropdowns, pagination *bên trong* `targetElement`).
    -   **Quan trọng:** Hàm này *không* khởi tạo lại các chức năng toàn cục như sidebar hay theme switcher vì chúng không nằm trong `targetElement` bị swap.

-   **`htmx:load`**: Sự kiện này được kích hoạt **sau khi** nội dung được khôi phục từ lịch sử trình duyệt (bfcache) khi người dùng nhấn nút **Back/Forward**.
    -   Hàm `reinitializeAfterHtmxLoad(targetElement)` được gọi.
    -   `targetElement` ở đây thường là `body` hoặc container chính (`#main-content`).
    -   Hàm này cần khởi tạo lại **tất cả** các chức năng cần thiết cho **toàn bộ trang được khôi phục**, bao gồm:
        -   Các chức năng **toàn cục** (sidebar, theme, auth UI, back-to-top, etc.).
        -   Các chức năng **trang cụ thể** (dựa trên kiểm tra selector trong `targetElement`, ví dụ: khởi tạo lại trang tìm kiếm nếu thấy `#searchForm`).
        -   Các **component UI/Bootstrap** trên toàn trang.

-   **`pageshow` (Fallback cho điều hướng từ non-HTMX sang HTMX)**: 
    -   Sự kiện `pageshow` được gắn vào `window` trong `main.js` để xử lý trường hợp đặc biệt khi điều hướng từ trang **non-HTMX** (như `Read.cshtml`) về trang **HTMX** (như `Details.cshtml`).
    -   Khi người dùng nhấn nút **Back** để quay lại trang HTMX từ trang non-HTMX, đôi khi sự kiện `htmx:load` không được kích hoạt đúng cách.
    -   `event.persisted === true` cho biết trang được khôi phục từ bfcache (Back/Forward navigation).
    -   Nếu là trang HTMX (kiểm tra qua sự tồn tại của `#main-content` hoặc `hx-boost`), chúng ta gọi thủ công `reinitializeAfterHtmxLoad(document.body)`.
    -   Cơ chế này đóng vai trò như một "lưới an toàn" để đảm bảo JavaScript được khởi tạo lại ngay cả khi `htmx:load` không kích hoạt.

### 3. Quản Lý Event Listeners

Để tránh lỗi và duplicate listeners khi nội dung thay đổi:

1.  **Event Delegation:** Gắn listener vào các **container cha ổn định** (không bị HTMX swap) và kiểm tra `event.target` để xác định phần tử con được click. (Ví dụ: nút follow trong `manga-details.js`).
2.  **Clone và Replace:** Khi *bắt buộc* phải gắn listener trực tiếp vào phần tử có thể bị swap, hãy sử dụng kỹ thuật clone node trong hàm khởi tạo lại (`reinitializeAfterHtmxSwap` hoặc `reinitializeAfterHtmxLoad`) để xóa listener cũ trước khi thêm listener mới. (Ví dụ: theme switcher trong `htmx-handlers.js`).
3.  **Dispose Bootstrap Instances:** Luôn gọi `dispose()` trên các instance Bootstrap cũ (Dropdown, Collapse, Tooltip, Tab) trước khi tạo instance mới trong các hàm khởi tạo lại.

### 4. Xử lý Loading State

Quản lý class `htmx-loading-target` là rất quan trọng để tránh UI bị "kẹt" ở trạng thái loading:

1.  **Thêm Loading State:** Trong `htmx:beforeRequest`, thêm class `htmx-loading-target` vào target element.
2.  **Xóa Loading State:** 
    -   Trong `htmx:afterSwap`: Xóa class sau khi nội dung mới được swap thành công.
    -   Trong `htmx:afterRequest`: Dọn dẹp class nếu request kết thúc mà không có swap.
    -   Trong `htmx:load`: Kiểm tra và xóa class khỏi phần tử được khôi phục từ bfcache.
    -   Trong `reinitializeAfterHtmxLoad`: Ưu tiên xóa loading state ngay lập tức.
3.  **Fallback Cleanup:** Luôn đảm bảo dọn dẹp `loadingTargetElement` để tránh memory leak.

## Global Scope

Dự án sử dụng module ES6. Chỉ có một số ít hàm tiện ích như `window.showToast` được đưa vào global scope một cách có chủ đích.

## **QUAN TRỌNG: Bảo trì Code**

Khi bạn thêm một chức năng JavaScript mới yêu cầu khởi tạo (ví dụ: một thư viện slider mới, một component tương tác mới):

1.  **Xác định phạm vi:** Chức năng này là toàn cục hay chỉ dành cho trang/phần tử cụ thể?
2.  **Tạo hàm `init...()`:** Viết hàm khởi tạo cho chức năng đó trong module tương ứng.
3.  **Cập nhật `main.js`:** Gọi hàm `init...()` trong `DOMContentLoaded` (có thể có điều kiện).
4.  **Cập nhật `htmx-handlers.js`:**
    *   **`reinitializeAfterHtmxSwap(targetElement)`:** Thêm lệnh gọi `init...()` *nếu* chức năng đó cần chạy lại khi một phần tử *cụ thể* bị swap (kiểm tra `targetElement.querySelector(...)`).
    *   **`reinitializeAfterHtmxLoad(targetElement)`:** Thêm lệnh gọi `init...()` *nếu* chức năng đó cần chạy lại khi toàn bộ trang được khôi phục từ lịch sử (có thể cần kiểm tra điều kiện trong `targetElement` nếu là chức năng trang cụ thể, hoặc gọi trực tiếp nếu là chức năng toàn cục).

**Việc cập nhật `htmx-handlers.js` là bắt buộc để đảm bảo chức năng mới hoạt động đúng với HTMX và điều hướng lịch sử.**