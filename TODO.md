# TODO: Sửa lỗi hiển thị nhiều thông báo Toast khi chuyển đổi Theme

**Vấn đề:** Nút chuyển đổi theme đang kích hoạt nhiều thông báo toast mỗi khi được bấm.

**Nguyên nhân:**
1.  **Event Bubbling:** Sự kiện click trên input checkbox (`#themeSwitch`) lan truyền lên container cha (`#themeSwitcher`), và cả hai đều có thể có listener riêng để thay đổi theme.
2.  **Gắn Listener nhiều lần:** Hàm `initThemeSwitcher` có thể được gọi lại nhiều lần (ví dụ: sau khi HTMX swap) mà không xóa bỏ các listener cũ, dẫn đến việc hàm `changeTheme` được gọi nhiều lần cho một lần click.

**Giải pháp:**
1.  **Đơn giản hóa Event Listener:** Chỉ gắn một listener duy nhất vào container `#themeSwitcher`.
2.  **Dọn dẹp Listener cũ:** Sử dụng kỹ thuật `cloneNode` và `replaceChild` để đảm bảo listener cũ bị xóa trước khi gắn listener mới, đặc biệt khi `initThemeSwitcher` được gọi lại.
3.  **Cập nhật logic xử lý click:** Trong listener duy nhất, xác định hành động dựa trên `event.target` và gọi `changeTheme` một lần duy nhất.

**Các bước thực hiện:**

1.  **Chỉnh sửa `manga_reader_web\wwwroot\js\modules\theme.js`:**
    *   **Trong hàm `initThemeSwitcher()`:**
        *   Lấy các phần tử DOM cần thiết: `themeSwitcherContainer = document.getElementById('themeSwitcher')`, `themeSwitchInput = document.getElementById('themeSwitch')`, `themeText = document.getElementById('themeText')`.
        *   **Quan trọng:** Trước khi thêm listener mới, hãy xóa listener cũ bằng cách clone và replace container:
            ```javascript
            if (themeSwitcherContainer) {
                const newSwitcherContainer = themeSwitcherContainer.cloneNode(true);
                themeSwitcherContainer.parentNode.replaceChild(newSwitcherContainer, themeSwitcherContainer);
                // Cập nhật lại tham chiếu đến container và input mới sau khi clone
                themeSwitcherContainer = newSwitcherContainer;
                themeSwitchInput = themeSwitcherContainer.querySelector('#themeSwitch');
                themeText = themeSwitcherContainer.querySelector('#themeText');

                // Kiểm tra lại xem các phần tử con có tồn tại trong container mới không
                if (!themeSwitchInput || !themeText) {
                     console.error("Không tìm thấy input hoặc text của theme switcher sau khi clone.");
                     return; // Dừng nếu không tìm thấy
                }
            } else {
                console.error("Không tìm thấy container theme switcher.");
                return; // Dừng nếu không tìm thấy container
            }
            ```
        *   **Xóa bỏ các listener cũ riêng lẻ:** Loại bỏ các dòng `addEventListener` cho `themeSwitchInput` và `sidebarThemeSwitch` (nếu có).
        *   **Gắn một listener duy nhất vào `themeSwitcherContainer`:**
            ```javascript
            themeSwitcherContainer.addEventListener('click', function(e) {
                e.preventDefault(); // Ngăn hành vi mặc định của thẻ <a>

                // Lấy trạng thái hiện tại của checkbox *trước khi* thay đổi
                const isCurrentlyDark = themeSwitchInput.checked;
                let newIsDark;

                // Xác định trạng thái mới dựa trên việc click vào đâu
                if (e.target === themeSwitchInput) {
                    // Nếu click trực tiếp vào checkbox, trạng thái mới là trạng thái *sau khi* click
                    // Tuy nhiên, sự kiện click trên container xảy ra trước khi trạng thái checked thay đổi
                    // nên ta cần đảo ngược trạng thái hiện tại để có trạng thái mới
                    newIsDark = !isCurrentlyDark;
                    // Cập nhật trạng thái checked của input một cách thủ công để đồng bộ
                    themeSwitchInput.checked = newIsDark;
                } else {
                    // Nếu click vào vùng khác của container (<a> hoặc <span>),
                    // thì đảo ngược trạng thái hiện tại và cập nhật checkbox
                    newIsDark = !isCurrentlyDark;
                    themeSwitchInput.checked = newIsDark;
                }

                // Gọi hàm thay đổi theme một lần duy nhất với trạng thái mới
                changeTheme(newIsDark);
            });
            ```
        *   **Trong hàm `changeTheme(isDark, showNotification = true)`:** Đảm bảo hàm này chỉ gọi `showToast` một lần nếu `showNotification` là `true`. Logic hiện tại có vẻ đã đúng.
        *   **Trong hàm `updateSwitches(isDark)`:** Đảm bảo hàm này chỉ cập nhật trạng thái `checked` và text, không gọi lại `changeTheme`. Logic hiện tại có vẻ đã đúng.

2.  **Kiểm tra `manga_reader_web\wwwroot\js\modules\htmx-handlers.js`:**
    *   Đảm bảo rằng việc gọi lại `initThemeSwitcher()` trong `reinitializeAfterHtmxSwap` và `reinitializeAfterHtmxLoad` là cần thiết (chỉ khi phần tử theme switcher thực sự bị thay thế bởi HTMX). Logic kiểm tra `targetElement.querySelector('#themeSwitch')` hiện tại có vẻ hợp lý. Kỹ thuật `cloneNode` trong `initThemeSwitcher` sẽ xử lý việc dọn dẹp listener.

3.  **Kiểm tra `manga_reader_web\Views\Shared\_Layout.cshtml`:**
    *   Đảm bảo cấu trúc HTML của theme switcher không có vấn đề gì đặc biệt (ví dụ: không có các listener inline `onclick`). Cấu trúc hiện tại có vẻ ổn.

4.  **Kiểm tra `manga_reader_web\wwwroot\js\modules\custom-dropdown.js`:**
    *   Đảm bảo không có logic nào trong module này can thiệp trực tiếp vào sự kiện của theme switcher. Việc sử dụng `e.stopPropagation()` trong listener của `custom-dropdown.js` có thể cần thiết nếu có sự chồng chéo không mong muốn, nhưng giải pháp đơn giản hóa listener trong `theme.js` nên được ưu tiên.

5.  **Kiểm tra Console:** Sau khi áp dụng các thay đổi, mở Developer Console của trình duyệt và kiểm tra xem có lỗi JavaScript nào xuất hiện không, và quan sát log để đảm bảo hàm `changeTheme` chỉ được gọi một lần mỗi khi click.

**Kết quả mong đợi:** Sau khi thực hiện các bước trên, mỗi lần click vào nút chuyển đổi theme (dù là click vào checkbox hay vùng xung quanh), chỉ có một thông báo toast được hiển thị.