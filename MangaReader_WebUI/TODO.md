# TODO.md: Refactor JavaScript với Alpine.js (Phương pháp Bottom-Up)

## Mục tiêu

Tái cấu trúc mã JavaScript hiện có (`wwwroot/js`) bằng cách chọn từng chức năng JavaScript cụ thể, tìm HTML tương ứng, chuyển đổi sang Alpine.js, và loại bỏ mã JavaScript gốc. Mục tiêu cuối cùng là làm mã nguồn gọn gàng hơn, dễ bảo trì và giảm sự phụ thuộc vào việc khởi tạo lại thủ công sau các thao tác HTMX.

## Phương pháp "Từ trong ra ngoài" (Bottom-Up)

Chúng ta sẽ thực hiện các bước sau cho **từng chức năng JavaScript** muốn refactor:

1.  **Chọn chức năng:** Chọn một hàm JavaScript cụ thể trong thư mục `wwwroot/js/modules` (ví dụ: `initSidebarToggle` trong `read-page.js`). Ưu tiên các hàm xử lý sự kiện UI đơn giản (click, toggle class, show/hide).
2.  **Xác định HTML liên quan:** Tìm các file `.cshtml` (trong `Views/`) chứa các phần tử HTML (thường có ID hoặc class cụ thể) mà hàm JavaScript đã chọn tương tác.
3.  **Thiết kế với Alpine.js:** Xác định cách triển khai lại chức năng đó bằng Alpine.js:
    *   Cần trạng thái gì? (`x-data`)
    *   Sự kiện nào kích hoạt thay đổi? (`@click`, `@change`, etc.)
    *   Phần tử nào cần thay đổi hiển thị/class? (`x-show`, `:class`)
    *   Có cần đóng khi click ra ngoài không? (`@click.outside`)
4.  **Chỉnh sửa HTML:** Thêm các directives (`x-data`, `x-init`, `x-show`, `@click`, `:class`, etc.) vào các phần tử HTML đã xác định ở Bước 2.
5.  **Kiểm thử (Quan trọng):**
    *   Tạm thời **bình luận (comment out)** lệnh gọi hàm JavaScript gốc trong `main.js` hoặc `htmx-handlers.js`.
    *   Kiểm tra xem chức năng trên HTML đã hoạt động đúng với Alpine.js chưa.
    *   Kiểm tra sau các thao tác HTMX (swap, load) để đảm bảo Alpine tự khởi tạo đúng cách.
6.  **Xóa mã JavaScript cũ:**
    *   Nếu Bước 5 thành công, **xóa bỏ** hàm JavaScript gốc khỏi file module của nó.
    *   **Xóa bỏ** lệnh gọi hàm đó khỏi `main.js` và/hoặc `htmx-handlers.js`.
7.  **Đơn giản hóa `htmx-handlers.js`:** Xóa bỏ các lệnh gọi `init...()` không còn cần thiết trong `reinitializeAfterHtmxSwap` và `reinitializeAfterHtmxLoad` vì Alpine.js sẽ tự quản lý.
8.  **Lặp lại:** Chọn một chức năng JavaScript khác và lặp lại quy trình.

Đây là danh sách các hàm JavaScript có tiềm năng cao để được thay thế hoặc đơn giản hóa đáng kể bằng Alpine.js:

1. custom-dropdown.js (Toàn bộ module)

initCustomDropdowns(): Alpine sẽ tự động khởi tạo các component x-data.

toggleDropdown(): Thay thế bằng @click="open = !open" và :class hoặc x-show.

closeAllDropdowns(): Alpine xử lý scope component và @click.outside sẽ đóng dropdown.

closeDropdownsOnClickOutside(): Thay thế bằng @click.outside="open = false" trên phần tử dropdown.

Lý do: Quản lý trạng thái mở/đóng và xử lý click là thế mạnh cốt lõi của Alpine.js.

2. theme.js (Phần lớn module)

initCustomThemeSwitcher(): Thay thế bằng định nghĩa component x-data="themeSwitcher()" trong HTML.

saveTheme(): Thay thế bằng plugin $persist của Alpine hoặc cập nhật localStorage trong method của Alpine.

getSavedTheme(): Dùng để lấy giá trị khởi tạo cho state Alpine (có thể kết hợp $persist).

updateThemeSwitcherUI(): Thay thế bằng các binding của Alpine (:class, x-text) dựa trên state isDark.

applyTheme(): Hàm này vẫn cần thiết dưới dạng JS helper để áp dụng class/attribute lên document.documentElement và cập nhật meta tag, nhưng nó sẽ được gọi từ Alpine (ví dụ: trong init() hoặc $watch).

Lý do: Quản lý trạng thái theme (sáng/tối), lưu trữ và cập nhật UI của nút switch rất phù hợp với mô hình của Alpine.

3. search.js (Nhiều phần)

initAdvancedFilter(): Việc toggle hiển thị #filterContainer có thể thay bằng x-data="{ filtersOpen: false }", @click="filtersOpen = !filtersOpen", x-show="filtersOpen". Logic checkForActiveFilters có thể chạy trong x-init để xác định trạng thái ban đầu.

initFilterDropdowns(): Toàn bộ logic quản lý trạng thái mở/đóng, cập nhật text hiển thị, xử lý chọn checkbox/radio trong các dropdown bộ lọc có thể chuyển sang các component Alpine riêng cho mỗi dropdown.

updateDropdownText(): Logic này sẽ được tích hợp vào các component Alpine của dropdown (ví dụ: dùng computed property hoặc method).

initViewModeToggle(): Việc chuyển đổi trạng thái (grid/list), lưu localStorage, cập nhật class active cho nút có thể chuyển sang Alpine (x-data, $persist, @click, :class).

handleViewModeToggleClick(): Thay thế bằng method trong component Alpine.

updateViewModeButtons(): Thay thế bằng :class binding trong Alpine.

applySavedViewMode(): Logic này được xử lý bởi $persist hoặc x-init của Alpine.

initPageGoTo() (Một phần): Việc hiển thị/ẩn input khi click "..." có thể dùng state Alpine (x-data="{ editingPage: false }"), nhưng logic tạo input và xử lý Enter/Blur có thể vẫn cần JS helper.

setupResetFilters() (Một phần): Trigger reset bằng @click của Alpine, nhưng logic reset các trường form phức tạp nên giữ trong hàm JS được gọi từ Alpine.

Lý do: Alpine rất mạnh trong việc quản lý trạng thái và hiển thị có điều kiện của các form filter và các nút toggle đơn giản.

4. manga-details.js (Một phần)

initDropdowns() (Chapter/Volume Accordions): Logic toggle class active cho header ngôn ngữ và volume hoàn toàn có thể thay bằng Alpine (x-data, @click, :class).

initLanguageFilter(): Tương tự, việc quản lý nút filter ngôn ngữ nào đang active và ẩn/hiện các section tương ứng rất phù hợp với Alpine (x-data, @click, :class/x-show).

initFollowButton() / handleFollowClick() / toggleFollow() (Phần UI): Trạng thái isFollowing, loading và việc cập nhật text/icon/disabled của nút follow là ứng viên tốt cho x-data, :class, x-text, :disabled. Logic fetch có thể là một method async trong x-data.

Lý do: Các tương tác toggle, quản lý trạng thái đơn giản (active/inactive, following/not following, loading/not loading) là những gì Alpine làm tốt nhất.

5. read-page.js (Một phần)

initSidebarToggle(): Quản lý trạng thái mở/đóng sidebar đọc truyện (x-data, @click, :class, @click.outside, @keydown.escape).

initContentAreaClickToOpenSidebar(): Xử lý toggle sidebar khi click vùng ảnh (@click trên container ảnh gọi đến state của sidebar).

initChapterDropdownNav(): Có thể dùng x-model và @change (hoặc $watch) trên thẻ <select> để trigger navigation bằng JS (gọi htmx.ajax hoặc window.location).

initImageScaling(): Quản lý trạng thái fitWidth, lưu localStorage (dùng $persist), và cập nhật class cho container ảnh (x-data, $persist, @click, :class).

Lý do: Các chức năng toggle UI, quản lý trạng thái đơn giản và lưu trữ local rất phù hợp với Alpine.

6. manga-tags.js (Một phần)

initTagDropdownToggle(): Quản lý trạng thái mở/đóng dropdown (x-data, @click, x-show, @click.outside).

Listeners trong initTagsInSearchForm (Search, Mode Boxes): Logic lọc UI khi search và toggle trạng thái/class cho các mode box (AND/OR) có thể chuyển sang Alpine (x-model, x-data, @click, :class).

Logic chọn/bỏ chọn/loại trừ tag (trong renderTags): Thay vì addEventListener, dùng @click="cycleTagState(tag.id, tag.name)" trên tag item và xử lý logic thay đổi state trong Alpine.

Logic xóa tag badge (trong updateSelectedTagsDisplay): Dùng @click="removeTag(tag.id, isExcluded)" trên nút xóa của badge.

updateTagsInput(): Có thể thay bằng x-bind:value trên các input ẩn, liên kết với state mảng ID tag đã chọn/loại trừ.

updateTagItemStates(): Thay thế bằng :class binding trên các tag item trong dropdown, dựa trên state tag đã chọn/loại trừ.

Lý do: Alpine giúp quản lý trạng thái phức tạp của việc chọn/loại trừ tag và cập nhật UI tương ứng một cách reactive.

Các hàm/module ít có khả năng hoặc không nên thay thế bằng Alpine.js:

htmx-handlers.js: Module này điều phối việc gọi lại các hàm init khác, nó sẽ được đơn giản hóa khi các hàm init bị loại bỏ, chứ không bị thay thế.

main.js: Điểm vào chính, logic pageshow xử lý bfcache.

toast.js: Cung cấp tiện ích global, không quản lý state UI cụ thể.

ui.js (Hầu hết): Chứa các tiện ích DOM manipulation phức tạp (adjust titles, footer), tích hợp Bootstrap JS (tooltips, accordion fixes), hoặc dùng API trình duyệt (IntersectionObserver, scroll).

error-handling.js: Chủ yếu là xử lý lỗi và fallback, ít liên quan đến state UI phức tạp.

auth.js (checkAuthState): Phần fetch lấy dữ liệu.

manga-details.js (adjustHeaderBackgroundHeight): Tính toán layout phức tạp.

manga-tags.js (loadTags, renderTags, createTagBadge): Logic fetch và tạo DOM phức tạp (nhưng các event listener bên trong chúng có thể thay thế).

read-page.js (initImageLoading): Quản lý state phức tạp cho từng ảnh và dùng API trình duyệt.

search.js (initQuickSearch, checkForActiveFilters, getTotalPages, navigateToPage, setViewModeCookie, setupResetFilters (phần logic reset)): Các hàm tiện ích, đọc DOM, hoặc logic nghiệp vụ không trực tiếp quản lý state UI đơn giản.