# TODO: Refactor Trang Đọc Chapter (Read.cshtml)

**Mục tiêu:** Tích hợp trang đọc chapter vào layout chính, sử dụng HTMX để tải, thêm Reading Sidebar mới, và cải thiện trải nghiệm tải ảnh.

## Phase 1: Backend & Controller Setup

-   [ ] **Cập nhật `ChapterController.cs`:**
    -   [ ] Sửa action `Read(string id)`:
        -   Loại bỏ việc set `Layout = "_ChapterLayout";`. Trang này giờ sẽ sử dụng `_Layout.cshtml` mặc định.
        -   **Quan trọng:** Cập nhật `ChapterReadViewModel` để chứa danh sách *tất cả* các chapter **cùng ngôn ngữ** với chapter hiện tại (ví dụ: `List<ChapterViewModel> SiblingChapters`). Dữ liệu này cần để populate dropdown trong Reading Sidebar.
        -   Sửa đổi logic lấy `prevChapterId` và `nextChapterId` để hoạt động chính xác với danh sách `SiblingChapters` mới.
        -   Sử dụng `ViewRenderService` để trả về `PartialView("Read", viewModel)` nếu request là từ HTMX (`Request.Headers.ContainsKey("HX-Request")`), ngược lại trả về `View(viewModel)`.
-   [ ] **Cập nhật `ChapterReadViewModel.cs`:**
    -   [ ] Thêm thuộc tính `public List<ChapterViewModel> SiblingChapters { get; set; } = new List<ChapterViewModel>();`.
-   [ ] **Cập nhật `ChapterReadingServices.cs`:**
    -   [ ] Trong `GetChapterReadViewModel`, sau khi lấy được `chaptersList` (danh sách chapter cùng ngôn ngữ), gán nó vào thuộc tính `SiblingChapters` của `viewModel`.

## Phase 2: Frontend View Refactoring

-   [ ] **Refactor `Read.cshtml`:**
    -   [ ] Xóa bỏ hoàn toàn cấu trúc layout cũ (không còn `_ChapterLayout`). Trang này giờ sẽ render bên trong `#main-content` của `_Layout.cshtml`.
    -   [ ] Thêm cấu trúc HTML mới cho phần nội dung chính:
        -   Hàng tiêu đề truyện (`<h1>@Model.MangaTitle</h1>`).
        -   Hàng thông tin chapter:
            -   Cột 1: Số chapter (`<span>Chương @Model.ChapterNumber</span>`).
            -   Cột 2: Tên chapter (`<h2>@Model.ChapterTitle</h2>`).
            -   Cột 3: Nút mở Reading Sidebar (`<button id="readingSidebarToggle"><i class="bi bi-layout-sidebar-inset"></i></button>`).
        -   Container để chứa ảnh chapter (sẽ được render bởi partial view): `<div id="chapterImagesContainer" hx-trigger="load" hx-get="@Url.Action("GetChapterImagesPartial", "Chapter", new { id = Model.ChapterId })"> <!-- Loading indicator --> </div>`. *Lưu ý: Cần tạo action `GetChapterImagesPartial` mới.*
        -   Hàng điều hướng cuối trang (nếu có chương tiếp theo): `<a hx-get="@Url.Action("Read", "Chapter", new { id = Model.NextChapterId })" hx-target="#main-content" hx-push-url="true">Chương tiếp theo <i class="bi bi-arrow-right"></i></a>`.
-   [ ] **Tạo Partial View `_ChapterImagesPartial.cshtml`:**
    -   [ ] File này nhận `List<string>` (danh sách URL ảnh) làm Model.
    -   [ ] Loop qua danh sách URL:
        -   Với mỗi URL, tạo một `div` container (`<div class="page-image-container">`).
        -   Bên trong `div`, thêm:
            -   Một loading indicator (`<div class="loading-indicator"><div class="spinner-border"></div></div>`).
            -   Thẻ `img` với `src` ban đầu để trống hoặc là ảnh placeholder nhỏ, và `data-src="@imgPage"`. Thêm class `chapter-page-image lazy-load`.
            -   Một khu vực hiển thị lỗi (`<div class="error-overlay" style="display: none;"><i class="bi bi-exclamation-triangle"></i> Lỗi tải ảnh <button class="retry-button">Thử lại</button></div>`).
-   [ ] **Tạo Partial View `_ReadingSidebarPartial.cshtml`:**
    -   [ ] File này nhận `ChapterReadViewModel` làm Model.
    -   [ ] Tạo cấu trúc HTML cho sidebar (div cố định bên phải, ẩn ban đầu).
    -   [ ] Thêm nút đóng sidebar.
    -   [ ] Hàng 1: Navigation
        -   Nút "Chương trước" (`<a>` hoặc `<button>`) - vô hiệu hóa nếu `Model.PrevChapterId` null. Sử dụng `hx-get` để tải chapter trước.
        -   Dropdown (`<select id="chapterSelect">`):
            -   Loop qua `Model.SiblingChapters`.
            -   Tạo `<option value="@chapter.Id" @(chapter.Id == Model.ChapterId ? "selected" : "")>@chapter.Title</option>`.
        -   Nút "Chương sau" (`<a>` hoặc `<button>`) - vô hiệu hóa nếu `Model.NextChapterId` null. Sử dụng `hx-get` để tải chapter sau.
    -   [ ] Hàng 2: Nút "Chế độ đọc" (Placeholder: `<button id="readingModeBtn">Chế độ đọc</button>`).
    -   [ ] Hàng 3: Nút "Scale ảnh" (Placeholder: `<button id="imageScaleBtn">Tỷ lệ ảnh</button>`).

## Phase 3: CSS Styling

-   [ ] **Tạo file `wwwroot/css/pages/read.css` (hoặc tên tương tự):**
    -   [ ] Import file này vào `main.css`.
-   [ ] **Style cho `Read.cshtml`:**
    -   [ ] Định dạng layout mới (tiêu đề truyện, hàng thông tin chapter, container ảnh, link chương tiếp theo).
    -   [ ] Style cho nút mở Reading Sidebar.
-   [ ] **Style cho Reading Sidebar (`_ReadingSidebarPartial.cshtml`):**
    -   [ ] Định vị `position: fixed`, `right: -{width}`, `top: 0`, `height: 100vh`, `width`, `background-color` (sử dụng CSS variables).
    -   [ ] Style cho `z-index` để đảm bảo nó hiển thị trên nội dung chính nhưng dưới header/overlay (nếu có).
    -   [ ] Thêm `transition` cho thuộc tính `right` để tạo hiệu ứng slide.
    -   [ ] Style cho trạng thái mở (`.reading-sidebar.open { right: 0; }`).
    -   [ ] Style cho header sidebar (nút đóng).
    -   [ ] Style cho các hàng điều khiển (navigation, các nút placeholder).
    -   [ ] Style cho dropdown chapter (`#chapterSelect`).
    -   [ ] Style cho overlay (nếu cần để đóng khi click ra ngoài).
-   [ ] **Style cho Ảnh Chapter (`_ChapterImagesPartial.cshtml`):**
    -   [ ] Style cho `.page-image-container` (`position: relative`, `min-height` để giữ chỗ khi ảnh chưa tải).
    -   [ ] Style cho `.loading-indicator` (spinner, định vị tuyệt đối, căn giữa trong container).
    -   [ ] Style cho `.chapter-page-image` (hiển thị block, max-width 100%).
    -   [ ] Style cho `.error-overlay` (định vị tuyệt đối, căn giữa, nền mờ, icon lỗi, nút thử lại).
    -   [ ] Style cho trạng thái ẩn/hiện của indicator và error overlay.

## Phase 4: JavaScript Implementation

-   [ ] **Tạo file `wwwroot/js/modules/read-page.js` (hoặc tên tương tự):**
    -   [ ] Import file này vào `main.js` và gọi hàm `initReadPage()` *có điều kiện* (chỉ khi ở trang Read).
-   [ ] **Trong `read-page.js`:**
    -   [ ] **`initReadPage()` function:**
        -   Gọi các hàm khởi tạo con bên dưới.
    -   [ ] **Reading Sidebar Toggle:**
        -   Lấy nút toggle (`#readingSidebarToggle`) và sidebar (`#readingSidebar`).
        -   Thêm event listener cho nút toggle để thêm/xóa class `open` cho sidebar.
        -   (Optional) Thêm xử lý đóng sidebar khi click vào overlay hoặc nhấn phím ESC.
    -   [ ] **Chapter Dropdown Navigation:**
        -   Lấy dropdown (`#chapterSelect`).
        -   Thêm event listener `change`.
        -   Khi giá trị thay đổi, lấy `chapterId` đã chọn.
        -   Tạo URL mới: `/Chapter/Read/{chapterId}`.
        -   Sử dụng `htmx.ajax('GET', newUrl, { target: '#main-content', pushUrl: true });` để tải chapter mới. *Hoặc cân nhắc dùng `window.location.href = newUrl;` nếu muốn tải lại toàn bộ trang.*
    -   [ ] **Image Loading Logic:**
        -   Viết hàm `initImageLoading(containerSelector)` nhận selector của container chứa ảnh (ví dụ: `#chapterImagesContainer`).
        -   Trong hàm này:
            -   Tìm tất cả `.page-image-container` bên trong `containerSelector`.
            -   Với mỗi container:
                -   Lấy thẻ `img`, `loading-indicator`, `error-overlay`, `retry-button`.
                -   Lấy `data-src` từ thẻ `img`.
                -   Hiển thị `loading-indicator`.
                -   Gán `src` cho thẻ `img` từ `data-src`.
                -   Thêm event listener `load` cho `img`:
                    -   Khi load thành công: Ẩn `loading-indicator`, ẩn `error-overlay`.
                -   Thêm event listener `error` cho `img`:
                    -   Khi load lỗi: Ẩn `loading-indicator`, hiển thị `error-overlay`.
                -   Thêm event listener `click` cho `retry-button`:
                    -   Khi click: Ẩn `error-overlay`, hiển thị `loading-indicator`, thử tải lại ảnh (có thể thêm timestamp vào URL: `img.src = dataSrc + '?t=' + Date.now();`).
    -   [ ] **Placeholder Buttons:**
        -   Thêm event listener cho các nút placeholder (`#readingModeBtn`, `#imageScaleBtn`) để log ra console khi được click (để dành cho triển khai sau).
-   [ ] **Cập nhật `htmx-handlers.js`:**
    -   [ ] Trong `reinitializeAfterHtmxSwap(targetElement)` và `reinitializeAfterHtmxLoad(targetElement)`:
        -   Kiểm tra nếu `targetElement` chứa các element đặc trưng của trang Read mới (ví dụ: `#readingSidebarToggle` hoặc `#chapterImagesContainer`).
        -   Nếu có, gọi `initReadPage()` (hoặc các hàm khởi tạo con cụ thể như `initImageLoading('#chapterImagesContainer')`, `initSidebarToggle()`, `initChapterDropdownNav()`). **Rất quan trọng** để đảm bảo JS hoạt động sau khi HTMX tải trang Read.

## Phase 5: Controller Action for Partial View

-   [ ] **Tạo Action `GetChapterImagesPartial` trong `ChapterController.cs`:**
    -   [ ] Action này nhận `string id` (chapterId).
    -   [ ] Gọi service để lấy danh sách URL ảnh cho chapterId đó (ví dụ: `_chapterReadingServices.GetChapterPagesAsync(id)` - cần tạo hàm này nếu chưa có).
    -   [ ] Trả về `PartialView("_ChapterImagesPartial", pageUrls)`.

## Phase 6: Testing & Refinement

-   [ ] **Kiểm tra tải trang:**
    -   Truy cập trực tiếp URL `/Chapter/Read/{id}`. Trang phải hiển thị đúng layout chính và nội dung chapter.
    -   Điều hướng đến trang Read từ trang Details bằng HTMX. Trang Read phải được tải vào `#main-content` mà không load lại toàn bộ trang.
-   [ ] **Kiểm tra Reading Sidebar:**
    -   Nút toggle hoạt động đúng (mở/đóng).
    -   Dropdown chapter hiển thị đúng danh sách chapter cùng ngôn ngữ.
    -   Chọn chapter từ dropdown điều hướng đúng trang (qua HTMX).
    -   Nút Previous/Next hoạt động đúng (qua HTMX).
    -   Các nút placeholder hoạt động (log ra console).
-   [ ] **Kiểm tra tải ảnh:**
    -   Loading indicator hiển thị khi ảnh đang tải.
    -   Ảnh hiển thị đúng sau khi tải xong, indicator biến mất.
    -   Mô phỏng lỗi tải ảnh (ví dụ: sửa URL ảnh trong DevTools): Error overlay hiển thị.
    -   Nút "Thử lại" hoạt động, tải lại ảnh thành công.
-   [ ] **Kiểm tra HTMX Re-initialization:**
    -   Sau khi điều hướng giữa các chapter bằng dropdown hoặc nút Prev/Next (qua HTMX), đảm bảo:
        -   Reading Sidebar vẫn hoạt động.
        -   Logic tải ảnh cho chapter mới hoạt động đúng.
-   [ ] **Kiểm tra Responsive:**
    -   Layout hiển thị đúng trên các kích thước màn hình khác nhau.
    -   Reading Sidebar hoạt động tốt trên mobile.
-   [ ] **Kiểm tra Console:** Không có lỗi JavaScript nào xuất hiện.