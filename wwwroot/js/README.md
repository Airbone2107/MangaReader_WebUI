# JavaScript Structure - Manga Reader Web

## Cấu trúc

```
js/
├── modules/                      # Các module JavaScript theo chức năng
│   ├── htmx-handlers.js          # Quản lý tất cả sự kiện HTMX và khởi tạo lại sau swap
│   ├── sidebar.js                # Quản lý sidebar (đóng/mở, active links)
│   ├── ui.js                     # Các chức năng UI chung (tooltips, lazy loading, etc)
│   ├── theme.js                  # Quản lý chế độ tối/sáng
│   ├── toast.js                  # Hiển thị thông báo toast
│   ├── search.js                 # Logic cho trang tìm kiếm
│   ├── manga-details.js          # Logic cho trang chi tiết manga
│   ├── manga-tags.js             # Quản lý chọn/lọc tags manga
│   ├── reading-state.js          # Lưu trữ trạng thái đọc
│   └── error-handling.js         # Xử lý lỗi
├── main.js                       # File chính, import và khởi tạo tất cả các module
└── auth.js                       # Module xử lý xác thực
```

## Luồng Hoạt Động

### 1. Khởi Tạo Ban Đầu

Tất cả việc khởi tạo bắt đầu từ `main.js` trong sự kiện `DOMContentLoaded`:

```javascript
document.addEventListener('DOMContentLoaded', function() {
    // Khởi tạo các chức năng chung (luôn cần thiết)
    cleanupActiveLinks();
    initTooltips();
    initLazyLoading();
    // ...

    // Khởi tạo có điều kiện (chỉ khi cần)
    if (document.querySelector('.details-manga-header-background')) {
        initMangaDetailsPage();
    }
    
    // Khởi tạo HTMX handlers (luôn cần để quản lý content swap)
    initHtmxHandlers();
});
```

### 2. Xử Lý HTMX

Khi nội dung được tải bằng HTMX (ví dụ: click vào link, submit form tìm kiếm), `htmx-handlers.js` 
đảm bảo các chức năng JavaScript vẫn hoạt động với nội dung mới:

```javascript
// Trong htmx-handlers.js
htmx.on('htmx:afterSwap', function(event) {
    // Truyền phần tử đã được swap
    reinitializeAfterHtmxSwap(event.detail.target);
});
```

Hàm `reinitializeAfterHtmxSwap` nhận phần tử đã swap và khởi tạo lại các chức năng JS cần thiết:

1. Luôn khởi tạo lại một số chức năng cơ bản (active sidebar links)
2. Khởi tạo có điều kiện các chức năng khác dựa trên nội dung của phần tử đã swap:
   - Nếu phần tử chứa `.details-manga-header-background` → Khởi tạo lại trang chi tiết manga
   - Nếu phần tử chứa `#searchForm` → Khởi tạo lại trang tìm kiếm
   - Nếu phần tử chứa `.pagination` → Khởi tạo lại phân trang

### 3. Quản Lý Event Listeners

Để tránh duplicate event listeners, chúng ta sử dụng hai kỹ thuật:

1. **Event Delegation**: Gắn event listener vào container ổn định thay vì các phần tử có thể thay đổi:

```javascript
// Trong manga-details.js
function initFollowButton() {
    const followBtnContainer = document.querySelector('.details-manga-info-meta');
    
    // Gỡ listener cũ trước khi thêm mới
    followBtnContainer.removeEventListener('click', handleFollowClick);
    
    // Thêm listener mới (delegation)
    followBtnContainer.addEventListener('click', function(event) {
        const button = event.target.closest('#followBtn');
        if (button) {
            toggleFollow(button);
        }
    });
}
```

2. **Clone và Replace**: Khi cần gắn lại event listener cho các phần tử cụ thể sau HTMX swap:

```javascript
// Trong htmx-handlers.js
const themeSwitch = targetElement.querySelector('#themeSwitch');
if (themeSwitch) {
    // Clone node để xóa tất cả event listener hiện tại
    const newThemeSwitch = themeSwitch.cloneNode(true);
    themeSwitch.parentNode.replaceChild(newThemeSwitch, themeSwitch);
    
    // Thêm event listener mới
    newThemeSwitch.addEventListener('change', function() { /*...*/ });
}
```

## Global Scope 

Dự án này sử dụng module ES6 để tránh ô nhiễm global scope. Chỉ có một số ít hàm được cố tình đưa vào global scope (`window`):

- `window.showToast`: Hàm tiện ích để hiển thị thông báo, được xác định trong `toast.js` và sử dụng ở nhiều nơi

## Kỹ Thuật Quan Trọng

### 1. Khởi Tạo Có Điều Kiện

```javascript
// Chỉ khởi tạo khi cần
if (document.querySelector('.selector-specific-to-feature')) {
    initFeature();
}
```

### 2. Dọn Dẹp Trước Khi Khởi Tạo Lại

```javascript
// Đối với Bootstrap components
const instance = bootstrap.Dropdown.getInstance(element);
if (instance) {
    instance.dispose();
}
new bootstrap.Dropdown(element);
```

### 3. Module Pattern

```javascript
// Trong modules/feature.js
export function initFeature() { /*...*/ }

// Trong main.js
import { initFeature } from './modules/feature.js';
```

## HTMX Integration Best Practices 

1. **Luôn truyền `targetElement`**: Luôn truyền phần tử đã swap vào hàm khởi tạo lại để giới hạn phạm vi:
   ```javascript
   reinitializeAfterHtmxSwap(event.detail.target);
   ```

2. **Kiểm tra điều kiện cụ thể**: Luôn kiểm tra xem phần tử đã swap có chứa các thành phần cần khởi tạo không:
   ```javascript
   if (targetElement.querySelector('.specific-selector')) { /*...*/ }
   ```

3. **Tránh khởi tạo toàn cục**: Tránh khởi tạo lại các chức năng không liên quan đến phần tử đã swap.

---

Tài liệu này được cập nhật cuối cùng sau quá trình refactor JavaScript để tích hợp với HTMX một cách hiệu quả. 