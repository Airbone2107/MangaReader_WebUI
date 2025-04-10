Okay, hiểu rồi. Log cho thấy các hàm JavaScript đang được gọi đúng thứ tự và *đang cố gắng* thay đổi `style.display`. Tuy nhiên, việc bạn không thấy hiệu ứng trực quan (ẩn/hiện, loader) thường xảy ra vì một vài lý do:

1.  **Thời gian quá nhanh:** Request HTMX (đặc biệt khi phân trang hoặc đổi view mode mà không gọi API mới) có thể hoàn thành quá nhanh. Trình duyệt không kịp render trạng thái trung gian (ẩn nội dung cũ, hiện loader) trước khi nội dung mới được swap vào.
2.  **HTMX Swap:** Cơ chế swap mặc định của HTMX có thể thay thế nội dung *trước khi* các thay đổi `style.display` trong `beforeSwap` kịp có hiệu lực trên màn hình.
3.  **CSS Conflicts:** Có thể có CSS khác đang ghi đè `display: none` hoặc `display: block`.
4.  **Vấn đề với Loader:** Bản thân `#search-results-loader` có thể đang bị ẩn bởi CSS khác hoặc không có kích thước/nội dung để hiển thị.

**Giải pháp đề xuất: Sử dụng CSS và các lớp trạng thái của HTMX**

Đây là cách tiếp cận thường được khuyến nghị và đáng tin cậy hơn để xử lý trạng thái tải với HTMX, vì nó dựa vào các lớp CSS được HTMX tự động thêm/xóa trong quá trình request.

**Bước 1: Cập nhật CSS**

Thêm các quy tắc CSS sau vào một file CSS phù hợp (ví dụ: `wwwroot/css/pages/search/search-card.css` hoặc `wwwroot/css/core/components.css`):

```css
/* --- HTMX Loading States for Search Results --- */

/* Mặc định ẩn loader */
#search-results-loader {
    display: none;
    opacity: 0;
    visibility: hidden;
    transition: opacity 0.2s ease-out, visibility 0s linear 0.2s; /* Transition cho fade out */
}

/* Ẩn nội dung cũ và phân trang KHI request đang chạy */
/* Target là wrapper để áp dụng cho cả kết quả và phân trang */
#search-results-and-pagination.htmx-request #search-results-container,
#search-results-and-pagination.htmx-request .pagination,
#search-results-and-pagination.htmx-request .text-center.mt-2.text-muted /* Info phân trang */
{
    opacity: 0;
    visibility: hidden;
    /* Thêm transition để tạo hiệu ứng fade out mượt hơn */
    transition: opacity 0.2s ease-out, visibility 0s linear 0.2s; /* Delay visibility change */
}

/* Hiển thị loader KHI request đang chạy */
#search-results-and-pagination.htmx-request #search-results-loader {
    display: block !important; /* Quan trọng để ghi đè display: none ban đầu */
    opacity: 1;
    visibility: visible;
    transition: opacity 0.2s ease-in; /* Transition cho fade in */
}

/* Đảm bảo nội dung mới fade in sau khi swap */
#search-results-container,
#search-results-and-pagination .pagination,
#search-results-and-pagination .text-center.mt-2.text-muted {
    opacity: 1;
    visibility: visible;
    transition: opacity 0.3s ease-in 0.1s; /* Fade in hơi trễ một chút */
}

/* --- End HTMX Loading States --- */

/* Đảm bảo loader có style cơ bản nếu chưa có */
#search-results-loader {
    /* Thêm các style cần thiết khác nếu chưa có, ví dụ: */
     text-align: center;
     padding: 2rem 0;
     /* background-color: rgba(var(--body-bg-rgb), 0.5); /* Optional: semi-transparent background */
     /* backdrop-filter: blur(2px); /* Optional: blur effect */
}
```

**Bước 2: Cập nhật JavaScript (`htmx-handlers.js`)**

Xóa bỏ các dòng code thay đổi `style.display` trong `htmx:beforeSwap` và `htmx:afterSwap` cho các phần tử liên quan đến kết quả tìm kiếm. CSS sẽ tự động xử lý việc này dựa trên lớp `.htmx-request`.

```javascript
// Import các hàm từ các module khác (sẽ được sử dụng trong HTMX)
import { updateActiveSidebarLink } from './sidebar.js';
import { initTooltips, adjustMangaTitles } from './ui.js';
import { adjustHeaderBackgroundHeight, initMangaDetailsPage } from './manga-details.js';
import { initTagsInSearchForm } from './manga-tags.js';
import SearchModule from './search.js';

// ... (Các hàm khác giữ nguyên) ...

/**
 * Khởi tạo các sự kiện xử lý HTMX
 */
function initHtmxHandlers() {
    // Hiển thị loading spinner khi bắt đầu request HTMX
    htmx.on('htmx:beforeRequest', function(event) {
        if (event.detail.target.id === 'main-content') {
            const mainSpinner = document.getElementById('content-loading-spinner');
            if (mainSpinner) mainSpinner.style.display = 'block';
        }
        // Không cần xử lý loader của search ở đây nữa, CSS sẽ làm
    });

    // Xử lý trước khi swap nội dung
    htmx.on('htmx:beforeSwap', function(event) {
        const targetId = event.detail.target.id;
        const isSearchResultSwap = (targetId === 'search-results-container' || targetId === 'search-results-and-pagination');

        if (isSearchResultSwap) {
            // Chỉ cần log, không cần thay đổi display nữa
            console.log(`[HTMX BeforeSwap] Target is ${targetId}. CSS will handle hiding/showing.`);
            console.log(`[HTMX BeforeSwap] Request path: ${event.detail.pathInfo.requestPath}`);
        }
    });

    // Ẩn loading spinner khi request hoàn thành (bao gồm cả lỗi)
    htmx.on('htmx:afterRequest', function(event) {
        const mainSpinner = document.getElementById('content-loading-spinner');
        if (mainSpinner) mainSpinner.style.display = 'none';
        // Loader của search results sẽ tự ẩn khi class htmx-request bị xóa
    });

    // Cập nhật và khởi tạo lại các chức năng sau khi nội dung được tải bằng HTMX
    htmx.on('htmx:afterSwap', function(event) {
        const targetElement = event.detail.target;
        const targetId = targetElement.id;
        console.log('[HTMX_HANDLERS] HTMX afterSwap triggered for target:', targetId);

        const isSearchResultSwap = (targetId === 'search-results-container' || targetId === 'search-results-and-pagination');

        if (isSearchResultSwap) {
            console.log(`[HTMX AfterSwap] Target is ${targetId}. CSS handled visibility. Re-initializing components.`);

            // *** KHÔNG cần thay đổi display ở đây nữa ***

            // Khởi tạo lại chức năng nhảy trang cho pagination nếu wrapper được swap
            if (targetId === 'search-results-and-pagination') {
                if (typeof SearchModule !== 'undefined' && typeof SearchModule.initPageGoTo === 'function') {
                    console.log('[HTMX AfterSwap] Re-initializing pagination goto function.');
                    SearchModule.initPageGoTo();
                } else {
                    console.warn('[HTMX AfterSwap] SearchModule or initPageGoTo not available for re-initialization.');
                }
            }
        }

        // Khởi tạo lại tất cả các chức năng cần thiết với targetElement
        reinitializeAfterHtmxSwap(targetElement); // Hàm này vẫn cần để khởi tạo lại JS cho nội dung mới

        // Kiểm tra sự tồn tại của manga-header-background và gọi hàm điều chỉnh
        if (targetElement.querySelector('.details-manga-header-background')) {
            setTimeout(adjustHeaderBackgroundHeight, 100);
        }
    });

    // Xử lý lỗi HTMX
    htmx.on('htmx:responseError', function(event) {
        const mainSpinner = document.getElementById('content-loading-spinner');
        if (mainSpinner) mainSpinner.style.display = 'none';

        // Loader của search results sẽ tự ẩn khi class htmx-request bị xóa

        console.error("HTMX response error:", event.detail.xhr);
        window.showToast('Lỗi', 'Đã xảy ra lỗi khi tải nội dung. Vui lòng thử lại.', 'error');

        // Không cần hiển thị lại nội dung cũ bằng JS, vì CSS sẽ tự làm khi htmx-request bị xóa
    });

    // Bắt sự kiện popstate (khi người dùng nhấn nút back/forward trình duyệt)
    window.addEventListener('popstate', function() {
        updateActiveSidebarLink();
    });
}

// Export các hàm cần thiết
export { reinitializeAfterHtmxSwap, initHtmxHandlers };
```

**Giải thích:**

1.  **CSS:**
    *   Chúng ta định nghĩa trạng thái mặc định của loader là ẩn (`display: none`).
    *   Khi HTMX thêm class `.htmx-request` vào wrapper `#search-results-and-pagination` trong quá trình request:
        *   CSS sẽ ẩn container kết quả (`#search-results-container`) và phân trang (`.pagination`, `.text-center.mt-2.text-muted`) bằng cách set `opacity: 0` và `visibility: hidden`. Có `transition` để tạo hiệu ứng fade out.
        *   CSS sẽ hiển thị loader (`#search-results-loader`) bằng cách set `display: block !important` và `opacity: 1`, `visibility: visible`. Có `transition` để tạo hiệu ứng fade in.
    *   Khi HTMX hoàn thành request và gỡ bỏ class `.htmx-request`:
        *   Các quy tắc CSS ẩn nội dung cũ/hiện loader sẽ không còn áp dụng.
        *   Loader sẽ quay về trạng thái mặc định (`display: none`).
        *   Nội dung mới (đã được swap vào) sẽ có `opacity: 1` và `visibility: visible` (theo style mặc định hoặc style fade-in của chúng ta).

2.  **JavaScript (`htmx-handlers.js`):**
    *   Đã **xóa bỏ hoàn toàn** các dòng code dùng `element.style.display = 'none'` hoặc `element.style.display = 'block'` cho các phần tử `#search-results-container`, `.pagination`, `.text-center.mt-2.text-muted`, và `#search-results-loader` trong cả `beforeSwap` và `afterSwap`.
    *   Việc ẩn/hiện giờ hoàn toàn do CSS điều khiển thông qua lớp `.htmx-request`.
    *   Hàm `reinitializeAfterHtmxSwap` vẫn rất quan trọng để khởi tạo lại các event listener và chức năng JS khác cho nội dung *mới* sau khi nó được swap vào.

**Lợi ích của phương pháp này:**

*   **Đáng tin cậy hơn:** Trạng thái trực quan được gắn trực tiếp với vòng đời request của HTMX.
*   **Mượt mà hơn:** Sử dụng `opacity` và `transition` của CSS tạo hiệu ứng fade in/out mượt mà hơn là thay đổi `display` đột ngột.
*   **Tách biệt logic:** Logic hiển thị trạng thái tải nằm trong CSS, logic khởi tạo lại chức năng nằm trong JS.

Hãy thử áp dụng các thay đổi này và kiểm tra lại tất cả các kịch bản HTMX trên trang tìm kiếm.