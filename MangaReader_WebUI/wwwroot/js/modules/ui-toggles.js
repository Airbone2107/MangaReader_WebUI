/**
 * ui-toggles.js - Quản lý các chức năng chuyển đổi UI (chế độ sáng/tối)
 */

// --- Theme Switcher Constants ---
const THEME_KEY = 'theme';
const THEME_DARK = 'dark';
const THEME_LIGHT = 'light';

/**
 * Khởi tạo tất cả các nút chuyển đổi UI
 */
export function initUIToggles() {
    console.log("[UI Toggles] Initializing UI toggles...");
    initCustomThemeSwitcherInternal();
}

// --- Theme Switcher Logic ---

/**
 * Lưu chủ đề hiện tại vào localStorage
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 */
function saveTheme(theme) {
    localStorage.setItem(THEME_KEY, theme);
}

/**
 * Lấy chủ đề đã lưu hoặc chủ đề hệ thống mặc định
 * @returns {string} - Chủ đề ('light' hoặc 'dark')
 */
function getSavedTheme() {
    const saved = localStorage.getItem(THEME_KEY);
    if (saved) {
        return saved;
    }
    // Fallback kiểm tra chế độ màu hệ thống
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return THEME_DARK;
    }
    return THEME_LIGHT; // Mặc định là light
}

/**
 * Áp dụng chủ đề cho trang web và cập nhật UI của switcher
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 * @param {boolean} [showNotification=false] - Có hiển thị toast thông báo không
 */
function applyTheme(theme, showNotification = false) {
    document.documentElement.setAttribute('data-bs-theme', theme);

    // Cập nhật meta tag theme-color
    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
        metaThemeColor.setAttribute('content', theme === THEME_DARK ? '#121318' : '#0d6efd');
    }

    // Cập nhật UI của switcher
    updateThemeSwitcherUI(theme);

    // Hiển thị thông báo nếu cần
    if (showNotification && typeof window.showToast === 'function') {
        window.showToast('Thông báo', `Đã chuyển sang chế độ ${theme === THEME_DARK ? 'tối' : 'sáng'}!`, 'info');
    }
    // Xóa bỏ inline style color trên các nav-link active sau khi đổi theme
    if (typeof window.cleanupActiveLinks === 'function') {
        window.cleanupActiveLinks();
    }
}

/**
 * Cập nhật giao diện của custom theme switcher
 * @param {string} theme - Chủ đề hiện tại ('light' hoặc 'dark')
 */
function updateThemeSwitcherUI(theme) {
    const switcherItem = document.getElementById('customThemeSwitcherItem');
    const switcherText = document.getElementById('customThemeSwitcherText');
    const switcherIcon = document.getElementById('customThemeIcon');

    if (!switcherItem || !switcherText || !switcherIcon) {
        console.warn("[Theme] Không tìm thấy các thành phần của custom theme switcher.");
        return;
    }

    const isDark = theme === THEME_DARK;

    // Cập nhật class cho switch trực quan
    if (isDark) {
        switcherItem.classList.add('dark-mode');
    } else {
        switcherItem.classList.remove('dark-mode');
    }

    // Cập nhật icon và text
    if (isDark) {
        switcherIcon.className = 'bi bi-sun me-2';
        switcherText.childNodes[switcherText.childNodes.length - 1].nodeValue = ' Chế độ sáng';
    } else {
        switcherIcon.className = 'bi bi-moon-stars me-2';
        switcherText.childNodes[switcherText.childNodes.length - 1].nodeValue = ' Chế độ tối';
    }
    console.log(`[Theme] UI updated for ${theme} mode.`);
}

/**
 * Khởi tạo chức năng chuyển đổi chế độ tối/sáng tùy chỉnh (Internal)
 */
function initCustomThemeSwitcherInternal() {
    console.log("[Theme] Initializing custom theme switcher internal...");
    const switcherItem = document.getElementById('customThemeSwitcherItem');

    if (!switcherItem) {
        return;
    }

    const initialTheme = getSavedTheme();
    console.log(`[Theme] Initial theme: ${initialTheme}`);
    applyTheme(initialTheme, false);

    const handleSwitcherClick = (event) => {
        event.preventDefault();
        event.stopPropagation();

        const currentTheme = document.documentElement.getAttribute('data-bs-theme');
        const newTheme = currentTheme === THEME_DARK ? THEME_LIGHT : THEME_DARK;

        console.log(`[Theme] Switching from ${currentTheme} to ${newTheme}`);
        applyTheme(newTheme, true);
        saveTheme(newTheme);
    };

    if (switcherItem._themeClickListener) {
        switcherItem.removeEventListener('click', switcherItem._themeClickListener);
    }
    switcherItem._themeClickListener = handleSwitcherClick;
    switcherItem.addEventListener('click', handleSwitcherClick);
    console.log("[Theme] Custom theme switcher initialized and click listener attached.");

    if (!localStorage.getItem(THEME_KEY)) {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        const handleSystemThemeChange = (e) => {
            if (!localStorage.getItem(THEME_KEY)) {
                const systemTheme = e.matches ? THEME_DARK : THEME_LIGHT;
                console.log(`[Theme] System theme changed to: ${systemTheme}`);
                applyTheme(systemTheme, false);
            }
        };
        if (mediaQuery._systemThemeListener) {
            mediaQuery.removeEventListener('change', mediaQuery._systemThemeListener);
        }
        mediaQuery._systemThemeListener = handleSystemThemeChange;
        mediaQuery.addEventListener('change', handleSystemThemeChange);
        console.log("[Theme] Added listener for system theme changes.");
    }
}

// Export hàm khởi tạo chính
export default {
    initUIToggles
};
