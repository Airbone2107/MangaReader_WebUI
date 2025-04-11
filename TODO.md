# TODO List - Cải thiện Trang Đọc Truyện (Read Page)

Danh sách này mô tả các công việc cần thực hiện để cải thiện chức năng và giao diện của trang đọc truyện.

## 1. Tải Trang Read Bằng HTMX Khi Điều Hướng Từ Details

**Mục tiêu:** Đảm bảo khi người dùng click vào một chapter trên trang `Details.cshtml`, trang `Read.cshtml` được tải động vào `#main-content` bằng HTMX thay vì tải lại toàn bộ trang.

**Các bước thực hiện:**

1.  **Kiểm tra Links Chapter trên `Details.cshtml`:**
    *   Mở file `Views/Manga/Details.cshtml`.
    *   Tìm đến phần hiển thị danh sách chapter (có thể là trong vòng lặp `@foreach (var chapter in ...)` bên trong `.custom-chapter-item`).
    *   **Xác nhận** rằng các thẻ `<a>` hoặc phần tử kích hoạt việc đọc chapter (ví dụ: `.custom-chapter-item.chapter-link`) **đã có** các thuộc tính HTMX sau:
        *   `hx-get="@Url.Action("Read", "Chapter", new { id = chapter.Id })"` (Hoặc URL tương tự)
        *   `hx-target="#main-content"`
        *   `hx-push-url="true"`
    *   Nếu chưa có, hãy **thêm** các thuộc tính này.

2.  **Kiểm tra `ChapterController.cs`:**
    *   Mở file `Controllers/ChapterController.cs`.
    *   Xem lại action `Read(string id)`.
    *   **Xác nhận** rằng action này đang sử dụng `_viewRenderService.RenderViewBasedOnRequest(this, viewModel);`. Điều này đảm bảo nó sẽ trả về `PartialView` cho request HTMX. (Hiện tại code đã đúng).

3.  **Cập nhật `htmx-handlers.js` để Khởi tạo `read-page.js`:**
    *   Mở file `wwwroot/js/modules/htmx-handlers.js`.
    *   Tìm đến hàm `reinitializeAfterHtmxSwap(targetElement)`.
    *   **Thêm** logic kiểm tra xem nội dung vừa được swap có phải là trang Read hay không. Bạn có thể kiểm tra sự tồn tại của một element đặc trưng như `.chapter-reader-container` hoặc `#readingSidebar`.
    *   Nếu đúng là trang Read, **gọi hàm `initReadPage()`** từ `read-page.js`.
    *   **Ví dụ:**
        ```javascript
        // Bên trong reinitializeAfterHtmxSwap(targetElement)

        // ... các kiểm tra khác ...

        // Kiểm tra trang đọc chapter
        if (targetElement.querySelector('.chapter-reader-container') || targetElement.querySelector('#readingSidebar')) {
            console.log('[HTMX Swap] Chapter Read page detected, initializing read-page modules');
            // Đảm bảo đã import initReadPage ở đầu file htmx-handlers.js
            initReadPage(); 
        }

        // ... các khởi tạo khác ...
        ```
    *   **Quan trọng:** Đảm bảo bạn đã import `initReadPage` ở đầu file `htmx-handlers.js`:
        ```javascript
        // Import các hàm từ các module khác (sẽ được sử dụng trong HTMX)
        // ... các import khác ...
        import { initReadPage } from './read-page.js'; // <--- Thêm dòng này
        ```

## 2. Ngăn Header Tự Động Ẩn Trên Trang Read

**Mục tiêu:** Giữ cho `.site-header` luôn hiển thị khi người dùng cuộn trang trên `Read.cshtml`, nhưng vẫn giữ nguyên hành vi tự ẩn trên các trang khác.

**Các bước thực hiện:**

1.  **Đánh Dấu Trang Read:**
    *   Mở file `Views/Chapter/Read.cshtml`.
    *   Thêm một class hoặc data attribute vào thẻ `<body>` hoặc container chính (`#main-content`) để đánh dấu đây là trang Read. Sử dụng `ViewData["PageType"]` đã có là một cách tốt.
    *   **Ví dụ (thêm class vào body):** Sửa `_ChapterLayout.cshtml` (nếu `Read.cshtml` dùng layout này) hoặc layout chính (`_Layout.cshtml`) để thêm class dựa trên `ViewData`. Cách đơn giản hơn là thêm class vào `#main-content` trong `Read.cshtml` nếu nó được swap vào đó.
        *   Trong `Read.cshtml`, sửa thẻ div `#main-content` (nếu có) hoặc container cha gần nhất:
            ```html
            <div id="main-content" class="page-type-chapter-read"> 
                @* Nội dung của Read.cshtml *@
            </div> 
            ```
        *   Hoặc nếu `Read.cshtml` dùng layout riêng (`_ChapterLayout.cshtml`), thêm vào thẻ `<body>`:
            ```html
             <body class="manga-reader-app page-type-chapter-read"> 
                @* ... *@
             </body>
            ```
        *   Hoặc nếu dùng layout chung, bạn có thể thêm class vào `body` bằng JavaScript trong `initReadPage()`:
            ```javascript
            // Trong initReadPage()
            document.body.classList.add('page-type-chapter-read'); 
            // Nhớ xóa class này khi rời trang Read (trong htmx:beforeRequest hoặc khi điều hướng)
            ```
            *Lưu ý: Dùng class trên body hoặc #main-content sẽ dễ quản lý hơn.*

2.  **Cập nhật Scroll Listener:**
    *   Mở file `wwwroot/js/modules/sidebar.js` (Nơi chứa logic ẩn/hiện header khi cuộn).
    *   Tìm đến `window.addEventListener('scroll', function() { ... });`.
    *   Bên trong hàm xử lý scroll, **thêm điều kiện kiểm tra** trước khi thêm/xóa class `header-hidden`.
    *   **Ví dụ:**
        ```javascript
        window.addEventListener('scroll', function() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            const isReadPage = document.body.classList.contains('page-type-chapter-read') || 
                               document.getElementById('main-content')?.classList.contains('page-type-chapter-read'); // Kiểm tra class đánh dấu

            // Chỉ ẩn header nếu KHÔNG phải trang Read
            if (!isReadPage) { 
                if (scrollTop > lastScrollTop && scrollTop > scrollThreshold) {
                    siteHeader?.classList.add('header-hidden'); // Thêm ?. để tránh lỗi nếu siteHeader không tồn tại
                } else if (scrollTop < lastScrollTop || scrollTop <= scrollThreshold) {
                    siteHeader?.classList.remove('header-hidden');
                }
            } else {
                 // Nếu là trang Read, luôn đảm bảo header không bị ẩn
                 siteHeader?.classList.remove('header-hidden');
            }

            lastScrollTop = scrollTop;
        });
        ```
    *   **Quan trọng:** Đảm bảo biến `siteHeader` được khai báo và lấy đúng phần tử ở đầu hàm `initSidebar()`.

## 3. Sửa Lỗi Click Để Ẩn/Hiện ReadingSidebar

**Mục tiêu:** Chức năng click vào vùng nội dung ảnh (`#chapterImagesContainer`) để mở/đóng `ReadingSidebar` cần hoạt động ổn định.

**Các bước thực hiện:**

1.  **Kiểm tra và Debug `initContentAreaClickToOpenSidebar()`:**
    *   Mở file `wwwroot/js/modules/read-page.js`.
    *   Tìm hàm `initContentAreaClickToOpenSidebar()`.
    *   **Thêm `console.log`** để debug:
        ```javascript
        imageContainer.addEventListener('click', (event) => {
            console.log('[Read Page] Click detected on image container area.');
            console.log('[Read Page] Event target:', event.target);
            console.log('[Read Page] Closest .page-image-container:', event.target.closest('.page-image-container'));
            console.log('[Read Page] Is target the container itself?', event.target === imageContainer);

            // Đảm bảo click là trực tiếp vào container hoặc vào một ảnh/vùng chứa ảnh
            const clickedOnImageArea = event.target === imageContainer || event.target.closest('.page-image-container');
            
            if (clickedOnImageArea) {
                console.log('[Read Page] Click target is valid for toggling sidebar.');
                if (sidebar.classList.contains('open')) {
                    // Kiểm tra xem có đang ghim không (sẽ thêm ở bước sau)
                    const isPinned = document.body.classList.contains('sidebar-pinned'); // Giả sử dùng class này
                    if (!isPinned) {
                        sidebar.classList.remove('open');
                        console.log('[Read Page] Sidebar closed by image container click (not pinned).');
                    } else {
                         console.log('[Read Page] Sidebar is pinned, click ignored.');
                    }
                } else {
                    sidebar.classList.add('open');
                    console.log('[Read Page] Sidebar opened by image container click.');
                }
            } else {
                 console.log('[Read Page] Click target is NOT valid for toggling sidebar.');
            }
        });
        ```
    *   **Phân tích log:** Chạy trang Read, click vào vùng ảnh và xem console log.
        *   Listener có được kích hoạt không?
        *   `event.target` là gì? Có phải là ảnh, container ảnh, hay container chính?
        *   Điều kiện `clickedOnImageArea` có trả về `true` không?
    *   **Điều chỉnh điều kiện:** Nếu điều kiện `clickedOnImageArea` không đúng, hãy sửa lại selector `event.target.closest(...)` cho phù hợp với cấu trúc HTML thực tế của bạn trong `_ChapterImagesPartial.cshtml`. Có thể bạn chỉ cần `event.target.closest('#chapterImagesContainer')`.
    *   **Kiểm tra xung đột:** Xem có event listener nào khác trên ảnh hoặc container (ví dụ: zoom ảnh) đang gọi `event.stopPropagation()` và ngăn không cho sự kiện click lan đến `#chapterImagesContainer` không.

## 4. Restyle Chapter Info Row và Nút Mở Sidebar

**Mục tiêu:**
*   Các phần tử trong `.chapter-info-row` (số chương, tiêu đề, nút controls) nằm trong các `div` riêng biệt, có nền và kiểu chữ riêng.
*   Nút mở sidebar (`#readingSidebarToggle`) chỉ hiển thị chữ "Menu", không có icon.

**Các bước thực hiện:**

1.  **Cập nhật HTML (`Read.cshtml`):**
    *   Mở file `Views/Chapter/Read.cshtml`.
    *   Tìm đến div `.chapter-info-row`.
    *   **Thay đổi cấu trúc** bên trong nó:
        ```html
         <div class="chapter-info-row">
             <div class="chapter-info-item chapter-info-number"> @* Div cho số chương *@
                 <span>Chương @Model.ChapterNumber</span>
             </div>
             <div class="chapter-info-item chapter-info-title-wrapper"> @* Div cho tiêu đề *@
                 <h2>@Model.ChapterTitle</h2>
             </div>
             <div class="chapter-info-item chapter-info-controls"> @* Div cho nút controls *@
                 <button id="readingSidebarToggle" class="btn btn-theme-outline">
                     Menu @* Chỉ còn text "Menu" *@
                 </button>
             </div>
         </div>
        ```

2.  **Cập nhật CSS (`read.css`):**
    *   Mở file `wwwroot/css/pages/read.css`.
    *   **Thêm CSS** để style các `div` mới:
        ```css
        .chapter-info-row {
            display: flex;
            justify-content: space-between; /* Giữ nguyên hoặc điều chỉnh nếu cần */
            align-items: center; /* Giữ nguyên hoặc điều chỉnh */
            background-color: var(--card-bg); /* Nền chung cho cả hàng */
            padding: 0.75rem 1rem;
            border-radius: 0.5rem;
            margin-bottom: 1.5rem;
            box-shadow: var(--card-shadow);
        }

        .chapter-info-item {
            /* Có thể thêm padding/margin nếu cần khoảng cách giữa các item */
             padding: 0.25rem 0.5rem; 
        }

        .chapter-info-number {
            /* Style cho số chương */
            font-weight: bold;
            color: var(--text-muted);
            font-size: 1.1rem;
            text-align: left;
            flex-basis: 20%; /* Phân chia không gian */
        }

        .chapter-info-title-wrapper {
            /* Style cho tiêu đề */
            text-align: center;
            flex-grow: 1; /* Cho phép tiêu đề chiếm không gian còn lại */
        }
        
        .chapter-info-title-wrapper h2 {
             margin-bottom: 0; /* Reset margin mặc định của h2 */
             font-size: 1.2rem; /* Giữ nguyên hoặc điều chỉnh */
             color: var(--body-color);
        }

        .chapter-info-controls {
            /* Style cho nút controls */
            text-align: right;
            flex-basis: 20%; /* Phân chia không gian */
        }
        
        /* Style riêng cho nút Menu nếu cần */
        #readingSidebarToggle {
            /* Ví dụ: font-weight: bold; */
        }

        /* Responsive cho chapter-info-row */
        @media (max-width: 768px) {
            .chapter-info-row {
                flex-direction: column;
                gap: 0.5rem; /* Thêm khoảng cách giữa các item khi xếp dọc */
                 padding: 0.5rem;
            }
            .chapter-info-number,
            .chapter-info-title-wrapper,
            .chapter-info-controls {
                 text-align: center; /* Căn giữa trên mobile */
                 flex-basis: auto; /* Reset flex-basis */
                 width: 100%;
            }
             .chapter-info-number {
                 font-size: 1rem;
             }
             .chapter-info-title-wrapper h2 {
                 font-size: 1.1rem;
             }
        }
        ```
    *   Điều chỉnh các giá trị `padding`, `font-size`, `flex-basis`, `background-color` (có thể dùng `var(--bs-secondary-bg)` hoặc màu khác) cho phù hợp với thiết kế mong muốn.

## 5. Tăng Kích Thước Font Tiêu Đề Manga

**Mục tiêu:** Làm cho tiêu đề manga (`h1.manga-title`) trên trang Read lớn hơn.

**Các bước thực hiện:**

1.  **Cập nhật CSS (`read.css`):**
    *   Mở file `wwwroot/css/pages/read.css`.
    *   **Thêm hoặc sửa** CSS rule cho `h1.manga-title` (có thể cần thêm selector cha `.chapter-reader-container` để đảm bảo chỉ ảnh hưởng trang Read):
        ```css
        .chapter-reader-container h1.manga-title {
            font-size: 2rem; /* Tăng kích thước font (điều chỉnh giá trị nếu cần) */
            font-weight: bold; /* Có thể thêm độ đậm nếu muốn */
            margin-bottom: 0.75rem; /* Điều chỉnh khoảng cách dưới */
        }
        ```

## 6. Đồng Bộ Style Cho Reading Sidebar

**Mục tiêu:** Làm cho nền và viền của `#readingSidebar` giống với `#sidebarMenu`.

**Các bước thực hiện:**

1.  **Kiểm tra Style Sidebar Chính:**
    *   Mở file `wwwroot/css/core/sidebar.css`.
    *   Tìm đến rule `#sidebarMenu`.
    *   Ghi lại các giá trị của `background-color` (ví dụ: `var(--body-bg)`) và `border-right` (ví dụ: `1px solid var(--border-color)`).

2.  **Cập nhật CSS (`read.css`):**
    *   Mở file `wwwroot/css/pages/read.css`.
    *   Tìm đến rule `.reading-sidebar` hoặc `#readingSidebar`.
    *   **Áp dụng** các style tương tự, nhưng đổi `border-right` thành `border-left`:
        ```css
        .reading-sidebar {
            /* ... các style khác ... */
            background-color: var(--body-bg); /* Áp dụng background từ sidebar chính */
            border-left: 1px solid var(--border-color); /* Áp dụng border tương tự, nhưng ở bên trái */
            box-shadow: -5px 0px 15px rgba(0, 0, 0, 0.1); /* Điều chỉnh shadow cho phù hợp khi ở bên phải */
             color: var(--body-color); /* Đảm bảo màu chữ phù hợp */
        }

        /* Đảm bảo các thành phần con cũng có màu phù hợp */
         .reading-sidebar .sidebar-section h6 {
             color: var(--text-muted);
         }
          .reading-sidebar .btn-theme-outline {
             color: var(--body-color);
             border-color: var(--border-color);
         }
         .reading-sidebar .btn-theme-outline:hover {
             background-color: var(--hover-bg);
         }
         .reading-sidebar .form-select {
             background-color: var(--input-bg);
             color: var(--input-color);
             border-color: var(--input-border);
         }
          .reading-sidebar .btn-close {
              /* Đảm bảo nút close hiển thị đúng theme */
              filter: var(--bs-btn-close-filter); 
          }
        ```
    *   **Lưu ý:** Sử dụng các biến CSS (`var(...)`) để đảm bảo sidebar này cũng thay đổi theo theme sáng/tối.

## 7. Implement Chức Năng Ghim Sidebar (Pin Sidebar)

**Mục tiêu:**
*   Nút `#pinSidebarBtn` có thể ghim/bỏ ghim `ReadingSidebar`.
*   Khi ghim, sidebar luôn mở (trừ khi bấm nút close). Click ra ngoài không đóng sidebar.
*   Khi ghim, layout chính (`.chapter-reader-container`) tự động co lại để chừa chỗ cho sidebar.

**Các bước thực hiện:**

1.  **Cập nhật JavaScript (`read-page.js`):**
    *   Mở file `wwwroot/js/modules/read-page.js`.
    *   **Thêm biến trạng thái:**
        ```javascript
        let isSidebarPinned = localStorage.getItem('readingSidebarPinned') === 'true'; // Đọc trạng thái đã lưu
        ```
    *   **Cập nhật `initSidebarToggle()`:**
        *   Trong hàm `closeSidebar()`, thêm kiểm tra:
            ```javascript
            function closeSidebar(forceClose = false) { // Thêm tham số forceClose
                if (!isSidebarPinned || forceClose) { // Chỉ đóng nếu không ghim HOẶC bị buộc đóng
                    sidebar.classList.remove('open');
                    // Không cần lưu state ở đây nữa nếu dùng localStorage cho pin
                } else {
                    console.log('[Read Page] Sidebar is pinned, close action ignored.');
                }
            }
            ```
        *   Sửa listener của nút close (`#closeSidebarBtn`) để luôn đóng:
            ```javascript
            closeBtn.addEventListener('click', () => closeSidebar(true)); // Luôn đóng khi bấm nút X
            ```
        *   Sửa listener của phím ESC để luôn đóng:
            ```javascript
             document.addEventListener('keydown', (e) => {
                 if (e.key === 'Escape' && sidebar.classList.contains('open')) {
                     closeSidebar(true); // Luôn đóng khi bấm ESC
                 }
             });
            ```
    *   **Cập nhật `initContentAreaClickToOpenSidebar()`:**
        *   Trong listener `imageContainer.addEventListener('click', ...)`:
            *   Khi kiểm tra `if (sidebar.classList.contains('open'))`, thêm điều kiện `&& !isSidebarPinned`:
                ```javascript
                 if (sidebar.classList.contains('open') && !isSidebarPinned) { // Chỉ đóng nếu đang mở và KHÔNG ghim
                     sidebar.classList.remove('open');
                     console.log('[Read Page] Sidebar closed by image container click (not pinned).');
                 } else if (!sidebar.classList.contains('open')) { // Vẫn mở nếu đang đóng
                     sidebar.classList.add('open');
                     console.log('[Read Page] Sidebar opened by image container click.');
                 } else if (isSidebarPinned) {
                      console.log('[Read Page] Sidebar is pinned, click on content ignored.');
                 }
                ```
    *   **Thêm hàm xử lý nút Pin:**
        ```javascript
        function initPinButton() {
            const pinBtn = document.getElementById('pinSidebarBtn');
            const sidebar = document.getElementById('readingSidebar');
            const body = document.body; // Hoặc container chính nếu cần

            if (!pinBtn || !sidebar) return;

            // Cập nhật trạng thái nút pin ban đầu
            function updatePinButtonState() {
                if (isSidebarPinned) {
                    pinBtn.classList.add('active');
                    pinBtn.innerHTML = '<i class="bi bi-pin-fill"></i>'; // Icon đã ghim
                    body.classList.add('sidebar-pinned'); // Thêm class vào body
                    if (!sidebar.classList.contains('open')) {
                         sidebar.classList.add('open'); // Mở sidebar nếu đang ghim mà bị đóng
                    }
                } else {
                    pinBtn.classList.remove('active');
                    pinBtn.innerHTML = '<i class="bi bi-pin"></i>'; // Icon chưa ghim
                    body.classList.remove('sidebar-pinned'); // Xóa class khỏi body
                }
            }

            pinBtn.addEventListener('click', () => {
                isSidebarPinned = !isSidebarPinned; // Đảo trạng thái
                localStorage.setItem('readingSidebarPinned', isSidebarPinned); // Lưu trạng thái
                updatePinButtonState();
                console.log(`[Read Page] Sidebar pinned state: ${isSidebarPinned}`);
                 // Mở sidebar nếu vừa ghim
                 if (isSidebarPinned && !sidebar.classList.contains('open')) {
                     sidebar.classList.add('open');
                 }
            });

            // Áp dụng trạng thái ban đầu khi load
            updatePinButtonState();
        }
        ```
    *   **Gọi hàm `initPinButton()`** bên trong `initReadPage()`.

2.  **Cập nhật CSS (`read.css`):**
    *   Mở file `wwwroot/css/pages/read.css`.
    *   **Thêm CSS** để xử lý layout khi sidebar được ghim:
        ```css
        /* Container chính của trang đọc */
        .chapter-reader-container {
            transition: margin-right var(--transition-speed) ease-in-out, width var(--transition-speed) ease-in-out;
            /* Mặc định không có margin-right */
            margin-right: 0; 
             width: 100%; /* Hoặc giá trị max-width ban đầu */
        }

        /* Khi sidebar được ghim (thêm class vào body) */
        body.sidebar-pinned .chapter-reader-container {
            /* Đẩy nội dung sang trái để chừa chỗ cho sidebar */
            margin-right: 300px; /* Bằng chiều rộng của sidebar */
            /* Giảm chiều rộng của nội dung */
             width: calc(100% - 300px); /* Điều chỉnh nếu container có max-width */
             /* Hoặc nếu dùng max-width: 
                max-width: calc(1000px - 300px); /* Ví dụ max-width ban đầu là 1000px */
             */
        }

        /* Style cho nút pin khi active */
        #pinSidebarBtn.active {
            background-color: var(--primary-color);
            color: white;
             border-color: var(--primary-color);
        }
         #pinSidebarBtn.active:hover {
             background-color: var(--bs-primary-dark); /* Màu đậm hơn khi hover */
         }
        ```
    *   Điều chỉnh giá trị `300px` (chiều rộng sidebar) và selector `.chapter-reader-container` nếu cần cho phù hợp với cấu trúc HTML và CSS của bạn.

3.  **Khởi tạo Trạng thái Ghim:**
    *   Trong hàm `initReadPage()`, sau khi gọi `initPinButton()`, đảm bảo trạng thái ghim ban đầu được áp dụng đúng cách (hàm `updatePinButtonState` đã xử lý việc này khi đọc từ `localStorage`).

---

**Lưu ý:**
*   Kiểm tra kỹ các selector CSS và ID của element để đảm bảo chúng khớp với code HTML hiện tại.
*   Sử dụng `console.log` thường xuyên trong quá trình phát triển để theo dõi luồng thực thi và giá trị biến.
*   Sau mỗi thay đổi, kiểm tra lại trên cả desktop và mobile.
*   Đảm bảo các thay đổi trong JavaScript được gọi đúng thời điểm, đặc biệt là sau các thao tác HTMX.