/**
 * theme.js - Quản lý các chức năng liên quan đến chế độ sáng/tối
 */

/**
 * Lưu chủ đề hiện tại vào localStorage
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 */
function saveTheme(theme) {
    localStorage.setItem('theme', theme);
}

/**
 * Áp dụng chủ đề cho trang web
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 */
function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
    
    // Cập nhật meta tag theme-color để phù hợp với theme
    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
        metaThemeColor.setAttribute('content', theme === 'dark' ? '#121212' : '#0d6efd');
    }
    
    // Cập nhật trạng thái nút chuyển đổi
    const themeSwitch = document.getElementById('themeSwitch');
    const themeText = document.getElementById('themeText');
    
    if (themeSwitch && themeText) {
        if (theme === 'dark') {
            themeSwitch.checked = true;
            themeText.innerHTML = '<i class="bi bi-sun me-2"></i>Chế độ sáng';
        } else {
            themeSwitch.checked = false;
            themeText.innerHTML = '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
        }
    }
}

/**
 * Khởi tạo chức năng chuyển đổi chế độ tối/sáng
 */
function initThemeSwitcher() {
    // Tìm các phần tử DOM cần thiết
    let themeSwitcherContainer = document.getElementById('themeSwitcher');
    let themeSwitchInput = document.getElementById('themeSwitch');
    let themeText = document.getElementById('themeText');
    const htmlElement = document.documentElement;

    // Quan trọng: Xóa listener cũ bằng cách clone và replace container
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

    // Kiểm tra theme đã lưu
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
        htmlElement.setAttribute('data-bs-theme', savedTheme);
        updateSwitches(savedTheme === 'dark');
    }

    // Cập nhật trạng thái các switcher
    function updateSwitches(isDark) {
        if (themeSwitchInput) themeSwitchInput.checked = isDark;
        
        // Cập nhật text
        if (themeText) {
            themeText.innerHTML = isDark ? 
                '<i class="bi bi-sun me-2"></i>Chế độ sáng' : 
                '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
        }
        
        // Xóa bỏ toàn bộ inline style color trên các nav-link active
        if (typeof window.cleanupActiveLinks === 'function') {
            window.cleanupActiveLinks();
        }
    }

    // Hàm thay đổi theme
    function changeTheme(isDark, showNotification = true) {
        const theme = isDark ? 'dark' : 'light';
        
        // Thiết lập theme
        htmlElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('theme', theme);
        
        // Cập nhật meta tag theme-color
        const metaThemeColor = document.querySelector('meta[name="theme-color"]');
        if (metaThemeColor) {
            metaThemeColor.setAttribute('content', theme === 'dark' ? '#121212' : '#0d6efd');
        }
        
        // Cập nhật UI
        updateSwitches(isDark);
        
        // Hiển thị thông báo nếu cần
        if (showNotification && window.showToast) {
            window.showToast('Thông báo', `Đã chuyển sang chế độ ${theme === 'dark' ? 'tối' : 'sáng'}!`, 'info');
        }
    }

    // Gắn một listener duy nhất vào themeSwitcherContainer
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
    
    // Khởi chạy lần đầu tiên mà không hiển thị thông báo
    changeTheme(savedTheme === 'dark', false);
    
    // Thêm phương thức toàn cục để các phần khác có thể sử dụng
    window.changeTheme = changeTheme;
}

export { saveTheme, applyTheme, initThemeSwitcher }; 