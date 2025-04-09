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
    const themeSwitch = document.getElementById('themeSwitch');
    const sidebarThemeSwitch = document.getElementById('sidebarThemeSwitch');
    const themeText = document.getElementById('themeText');
    const sidebarThemeText = document.getElementById('sidebarThemeText');
    const htmlElement = document.documentElement;

    // Kiểm tra theme đã lưu
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
        htmlElement.setAttribute('data-bs-theme', savedTheme);
        updateSwitches(savedTheme === 'dark');
    }

    // Cập nhật trạng thái các switcher
    function updateSwitches(isDark) {
        if (themeSwitch) themeSwitch.checked = isDark;
        if (sidebarThemeSwitch) sidebarThemeSwitch.checked = isDark;
        
        // Cập nhật text
        if (themeText) {
            themeText.innerHTML = isDark ? 
                '<i class="bi bi-sun me-2"></i>Chế độ sáng' : 
                '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
        }
        
        if (sidebarThemeText) {
            sidebarThemeText.innerHTML = isDark ? 
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

    // Đăng ký sự kiện cho theme switch chính
    if (themeSwitch) {
        // Thêm sự kiện click thay vì change
        themeSwitch.addEventListener('click', function(e) {
            e.stopPropagation(); // Ngăn sự kiện lan ra container
            changeTheme(this.checked);
        });
    }
    
    // Đăng ký sự kiện cho sidebar theme switch
    if (sidebarThemeSwitch) {
        // Thêm sự kiện click thay vì change
        sidebarThemeSwitch.addEventListener('click', function(e) {
            e.stopPropagation(); // Ngăn sự kiện lan ra container
            changeTheme(this.checked);
        });
    }
    
    // Đánh dấu theme switcher container khi hover
    const themeSwitcherContainer = document.getElementById('themeSwitcher');
    const sidebarThemeSwitcherContainer = document.getElementById('sidebarThemeSwitcher');
    
    if (themeSwitcherContainer) {
        themeSwitcherContainer.addEventListener('mouseenter', function() {
            this.classList.add('active');
        });
        
        themeSwitcherContainer.addEventListener('mouseleave', function() {
            this.classList.remove('active');
        });
        
        // Thêm sự kiện click để đổi theme khi nhấp vào container
        themeSwitcherContainer.addEventListener('click', function(e) {
            // Ngăn chặn hành vi mặc định của <a>
            e.preventDefault();
            
            // Nếu click vào switch thì bỏ qua để sự kiện click của switch xử lý
            if (e.target !== themeSwitch && !themeSwitch.contains(e.target)) {
                // Đảo ngược trạng thái hiện tại
                themeSwitch.checked = !themeSwitch.checked;
                changeTheme(themeSwitch.checked);
            }
        });
    }
    
    if (sidebarThemeSwitcherContainer) {
        sidebarThemeSwitcherContainer.addEventListener('mouseenter', function() {
            this.classList.add('active');
        });
        
        sidebarThemeSwitcherContainer.addEventListener('mouseleave', function() {
            this.classList.remove('active');
        });
        
        // Thêm sự kiện click để đổi theme khi nhấp vào container
        sidebarThemeSwitcherContainer.addEventListener('click', function(e) {
            // Ngăn chặn hành vi mặc định của <a>
            e.preventDefault();
            
            // Nếu click vào switch thì bỏ qua để sự kiện click của switch xử lý
            if (e.target !== sidebarThemeSwitch && !sidebarThemeSwitch.contains(e.target)) {
                // Đảo ngược trạng thái hiện tại
                sidebarThemeSwitch.checked = !sidebarThemeSwitch.checked;
                changeTheme(sidebarThemeSwitch.checked);
            }
        });
    }
    
    // Khởi chạy lần đầu tiên mà không hiển thị thông báo
    changeTheme(savedTheme === 'dark', false);
    
    // Thêm phương thức toàn cục để các phần khác có thể sử dụng
    window.changeTheme = changeTheme;
}

export { saveTheme, applyTheme, initThemeSwitcher }; 