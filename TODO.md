# TODO: Đơn giản hóa Hiệu ứng Tải HTMX cho Kết quả Tìm kiếm

**Mục tiêu:** Thay thế logic JavaScript hiện tại (sử dụng spinner hoặc thay đổi opacity/visibility) bằng một phương pháp CSS đơn giản để ẩn ngay lập tức nội dung cũ trước khi request HTMX và hiển thị nội dung mới ngay sau khi swap, áp dụng cho khu vực kết quả tìm kiếm (`#search-results-and-pagination`).

**Các bước thực hiện:**

## 1. Định nghĩa CSS Helper Class

*   [ ] **Thêm CSS Rule:** Mở file `manga_reader_web\wwwroot\css\core\layout.css` (hoặc một file CSS core khác như `components.css` nếu thấy phù hợp hơn). Thêm quy tắc CSS sau vào cuối file:
    ```css
    /* Helper class để ẩn phần tử ngay lập tức trong quá trình HTMX request */
    .   htmx-request-hide {
        display: none !important;
    }
    ```
    *(**Lưu ý:** Đổi tên class từ `htmx-hide` thành `htmx-request-hide` để rõ ràng hơn về mục đích của nó trong ngữ cảnh HTMX request).*

## 2. Cập nhật JavaScript (`htmx-handlers.js`)

*   [ ] **Mở file:** Mở file `manga_reader_web\wwwroot\js\modules\htmx-handlers.js`.
*   [ ] **Xóa Logic Ẩn/Hiện Cũ:**
    *   Tìm đến các hàm xử lý sự kiện HTMX như `htmx:beforeRequest`, `htmx:afterRequest`, `htmx:beforeSwap`.
    *   **Xóa bỏ** tất cả các dòng code JavaScript bên trong các hàm này mà đang thực hiện các hành động sau **chỉ đối với target là `#search-results-and-pagination` hoặc các phần tử con của nó (`#search-results-container`, `#search-results-loader`, `.pagination`)**:
        *   Thay đổi `element.style.display`.
        *   Thay đổi `element.style.opacity`.
        *   Thay đổi `element.style.visibility`.
        *   Thêm/xóa các class như `d-none`, `invisible`, `fade`, `show`, hoặc các class tùy chỉnh khác liên quan đến việc ẩn/hiện/mờ dần.
        *   **Ví dụ các đoạn code cần xóa (có thể khác tùy theo code hiện tại của bạn):**
            ```javascript
            // Ví dụ trong htmx:beforeRequest hoặc htmx:beforeSwap
            const loader = document.getElementById('search-results-loader');
            const content = document.getElementById('search-results-container');
            const pagination = document.querySelector('#search-results-and-pagination .pagination');
            if (loader) loader.style.display = 'block'; // XÓA
            if (content) content.style.opacity = '0'; // XÓA
            if (pagination) pagination.style.display = 'none'; // XÓA
            targetElement.classList.add('some-hiding-class'); // XÓA (nếu class này chỉ để ẩn)

            // Ví dụ trong htmx:afterRequest hoặc htmx:afterSwap
            if (loader) loader.style.display = 'none'; // XÓA
            if (content) content.style.opacity = '1'; // XÓA
            if (pagination) pagination.style.display = ''; // XÓA
            targetElement.classList.remove('some-hiding-class'); // XÓA (nếu class này chỉ để ẩn)
            ```
    *   **Quan trọng:** **KHÔNG XÓA** lệnh gọi `reinitializeAfterHtmxSwap(targetElement)` trong sự kiện `htmx:afterSwap`. Hàm này vẫn cần thiết để khởi tạo lại các chức năng JS khác (tooltips, dropdowns, etc.) cho nội dung mới.
*   [ ] **Thêm Logic Ẩn/Hiện Mới:**
    *   Tìm đến hàm `initHtmxHandlers()` hoặc nơi bạn đăng ký các listener sự kiện HTMX.
    *   Thêm đoạn code sau để xử lý việc thêm/xóa class `htmx-request-hide`:

    ```javascript
    function initHtmxHandlers() {
        const searchResultTargetSelector = "#search-results-and-pagination";

        // --- Xóa các listener cũ liên quan đến ẩn/hiện search results ---
        // (Đã thực hiện ở bước trên)

        // --- Thêm listener mới ---

        // Trước khi gửi request cho khu vực tìm kiếm
        htmx.on('htmx:beforeRequest', function (event) {
            // Kiểm tra xem target của request có phải là khu vực kết quả tìm kiếm không
            const requestTargetElement = event.detail.target;
            const targetAttribute = event.detail.requestConfig.target; // Lấy selector từ hx-target

            // Kiểm tra xem target của request có phải là searchResultTargetSelector không
            // Hoặc phần tử kích hoạt request nằm trong searchResultTargetSelector (ví dụ: nút pagination)
            const isSearchTarget = (targetAttribute && targetAttribute === searchResultTargetSelector) ||
                                   (requestTargetElement && requestTargetElement.closest(searchResultTargetSelector));

            if (isSearchTarget) {
                console.log('[HTMX BeforeRequest] Hiding search results area.');
                const el = document.querySelector(searchResultTargetSelector);
                if (el) {
                    el.classList.add('htmx-request-hide'); // Ẩn khu vực target
                }
            }

            // Xử lý spinner toàn cục (nếu vẫn muốn giữ lại)
            if (event.detail.target.id === 'main-content') {
                 const mainSpinner = document.getElementById('content-loading-spinner');
                 if (mainSpinner) mainSpinner.style.display = 'block';
            }
        });

        // Sau khi swap xong nội dung cho khu vực tìm kiếm
        htmx.on('htmx:afterSwap', function (event) {
            const swappedElement = event.detail.target; // Phần tử đã được swap

            // Kiểm tra xem phần tử được swap có phải là khu vực kết quả tìm kiếm không
            if (swappedElement && swappedElement.id === searchResultTargetSelector.substring(1)) { // Bỏ dấu #
                console.log('[HTMX AfterSwap] Showing search results area.');
                // Xóa class ẩn ngay lập tức để hiển thị nội dung mới
                swappedElement.classList.remove('htmx-request-hide');
            }

            // Khởi tạo lại JS cho nội dung mới (Quan trọng - Giữ lại dòng này)
            reinitializeAfterHtmxSwap(swappedElement);

            // --- Các listener khác (nếu có) ---
            // ... (Ẩn spinner toàn cục, xử lý lỗi, etc.)
        });

        // Listener để ẩn spinner toàn cục khi request hoàn tất (thành công hoặc lỗi)
        htmx.on('htmx:afterRequest', function(event) {
            const mainSpinner = document.getElementById('content-loading-spinner');
            if (mainSpinner) mainSpinner.style.display = 'none';

            // Nếu request thất bại, đảm bảo khu vực target không bị ẩn vĩnh viễn
            if (!event.detail.successful) {
                const searchResultTargetSelector = "#search-results-and-pagination";
                const targetAttribute = event.detail.requestConfig.target;
                const requestTargetElement = event.detail.elt; // Phần tử kích hoạt request

                const isSearchTarget = (targetAttribute && targetAttribute === searchResultTargetSelector) ||
                                       (requestTargetElement && requestTargetElement.closest(searchResultTargetSelector));

                if (isSearchTarget) {
                     console.warn('[HTMX AfterRequest - Error] Showing search results area after failed request.');
                     const el = document.querySelector(searchResultTargetSelector);
                     if (el) {
                         el.classList.remove('htmx-request-hide');
                     }
                }
            }
        });

        // Xử lý lỗi HTMX (đảm bảo target không bị ẩn)
        htmx.on('htmx:responseError', function(event) {
            console.error("HTMX response error:", event.detail.xhr);
            // (Phần xử lý spinner toàn cục đã có trong afterRequest)

            // Đảm bảo khu vực target không bị ẩn vĩnh viễn khi có lỗi
            const searchResultTargetSelector = "#search-results-and-pagination";
            const targetAttribute = event.detail.requestConfig.target;
            const requestTargetElement = event.detail.elt; // Phần tử kích hoạt request

            const isSearchTarget = (targetAttribute && targetAttribute === searchResultTargetSelector) ||
                                   (requestTargetElement && requestTargetElement.closest(searchResultTargetSelector));

            if (isSearchTarget) {
                console.warn('[HTMX ResponseError] Showing search results area after error.');
                const el = document.querySelector(searchResultTargetSelector);
                if (el) {
                    el.classList.remove('htmx-request-hide');
                }
            }

            // Hiển thị toast lỗi (nếu có)
            if (window.showToast) {
                window.showToast('Lỗi', 'Đã xảy ra lỗi khi tải nội dung. Vui lòng thử lại.', 'error');
            }
        });

        // Bắt sự kiện popstate (nếu cần cập nhật UI khác)
        window.addEventListener('popstate', function() {
            updateActiveSidebarLink();
        });

        console.log("HTMX Handlers Initialized with simple hide/show for search results.");
    }
    ```
    *   **Giải thích:**
        *   Chúng ta lắng nghe `htmx:beforeRequest`.
        *   Kiểm tra xem request này có nhắm đến `#search-results-and-pagination` hay không (dựa vào `hx-target` hoặc nếu phần tử kích hoạt nằm trong đó).
        *   Nếu đúng, thêm class `htmx-request-hide` vào `#search-results-and-pagination` để ẩn nó đi.
        *   Chúng ta lắng nghe `htmx:afterSwap`.
        *   Kiểm tra xem phần tử *vừa được swap* có phải là `#search-results-and-pagination` không.
        *   Nếu đúng, xóa class `htmx-request-hide` khỏi nó để nội dung mới được hiển thị.
        *   Thêm xử lý trong `htmx:afterRequest` và `htmx:responseError` để đảm bảo khu vực không bị ẩn vĩnh viễn nếu request thất bại.

## 3. Kiểm tra HTML (`_SearchFormPartial.cshtml`, `_SearchPaginationPartial.cshtml`)

*   [ ] **Xác minh `hx-target`:** Đảm bảo rằng các thuộc tính `hx-get` hoặc `hx-post` trên form tìm kiếm (`#searchForm`) và các link phân trang (`.page-link` trong `_SearchPaginationPartial.cshtml`) đều có `hx-target="#search-results-and-pagination"`. Điều này đảm bảo rằng các sự kiện HTMX sẽ nhắm đúng vào container mà chúng ta đang xử lý trong JavaScript.

## 4. Dọn dẹp các file JavaScript khác (Nếu cần)

*   [ ] **Kiểm tra `search.js`:** Mở file `manga_reader_web\wwwroot\js\modules\search.js`. Tìm và xóa bất kỳ đoạn code nào trong file này (nếu có) đang cố gắng điều khiển việc ẩn/hiện hoặc hiệu ứng loading cho `#search-results-and-pagination` hoặc các phần tử con của nó. Logic này giờ đã được tập trung trong `htmx-handlers.js`.

## 5. Kiểm tra và Tinh chỉnh

*   [ ] **Chạy ứng dụng:** Build và chạy lại project.
*   [ ] **Kiểm tra Trang Tìm Kiếm:**
    *   Truy cập `/Manga/Search`.
    *   Thực hiện tìm kiếm, thay đổi filter, sắp xếp, chuyển trang.
    *   **Quan sát:** Nội dung cũ (`#search-results-and-pagination`) phải biến mất *ngay lập tức* khi bạn thực hiện hành động (click nút tìm kiếm, link phân trang), và nội dung mới phải xuất hiện *ngay lập tức* sau khi tải xong. Sẽ **không** có spinner hay hiệu ứng mờ dần nào trong khu vực này.
    *   Kiểm tra console trình duyệt xem có lỗi JavaScript nào không, đặc biệt là các log từ `[HTMX BeforeRequest]` và `[HTMX AfterSwap]`.
    *   Đảm bảo các chức năng khác (tooltips, dropdown filter, nút "..." phân trang, nút chuyển đổi view mode) vẫn hoạt động bình thường sau khi nội dung được swap.
    *   Kiểm tra trường hợp request lỗi (ví dụ: ngắt mạng tạm thời) để đảm bảo khu vực kết quả không bị ẩn vĩnh viễn.
*   [ ] **(Tùy chọn) Tinh chỉnh:** Nếu có vấn đề về layout shift (nội dung nhảy lung tung) khi ẩn/hiện, bạn có thể cần xem xét lại cấu trúc HTML hoặc thêm `min-height` cho container `#search-results-and-pagination` để giữ chỗ trong khi nội dung bị ẩn. Tuy nhiên, với `display: none`, layout shift thường ít xảy ra hơn so với dùng `opacity` hoặc `visibility`.

---

Sau khi hoàn thành các bước này, hiệu ứng tải trang trong khu vực kết quả tìm kiếm sẽ được đơn giản hóa thành việc ẩn/hiện tức thời bằng CSS, loại bỏ các xử lý phức tạp bằng JavaScript trước đó.