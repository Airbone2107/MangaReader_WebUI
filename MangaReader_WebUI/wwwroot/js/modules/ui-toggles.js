/**
 * ui-toggles.js - Quản lý các chức năng chuyển đổi UI (chế độ sáng/tối, nguồn truyện)
 */

// --- Theme Switcher Constants ---
const THEME_KEY = 'theme';
const THEME_DARK = 'dark';
const THEME_LIGHT = 'light';

// --- Source Switcher Constants ---
const SOURCE_KEY = 'mangaSource'; // Key cho localStorage và cookie
const SOURCE_MANGADEX = 'MangaDex';
const SOURCE_MANGAREADERLIB = 'MangaReaderLib';
const SOURCE_COOKIE_NAME = 'MangaSource'; // Tên cookie để backend đọc

/**
 * Khởi tạo tất cả các nút chuyển đổi UI
 */
export function initUIToggles() {
    console.log("[UI Toggles] Initializing all UI toggles...");
    initCustomThemeSwitcherInternal();
    initCustomSourceSwitcherInternal();
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


// --- Source Switcher Logic ---

/**
 * Lưu nguồn truyện hiện tại vào localStorage và cookie
 * @param {string} source - Nguồn truyện ('MangaDex' hoặc 'MangaReaderLib')
 */
function saveMangaSource(source) {
    localStorage.setItem(SOURCE_KEY, source);
    const expires = new Date();
    expires.setFullYear(expires.getFullYear() + 1); // Cookie hết hạn sau 1 năm
    document.cookie = `${SOURCE_COOKIE_NAME}=${source}; expires=${expires.toUTCString()}; path=/; SameSite=Lax`;
    console.log(`[Source] Đã lưu nguồn truyện vào localStorage: ${source} và cookie: ${SOURCE_COOKIE_NAME}`);
}

/**
 * Lấy nguồn truyện đã lưu từ localStorage hoặc cookie
 * @returns {string} - Nguồn truyện ('MangaDex' hoặc 'MangaReaderLib')
 */
function getSavedMangaSource() {
    const saved = localStorage.getItem(SOURCE_KEY);
    if (saved) {
        return saved;
    }
    // Fallback đọc từ cookie nếu localStorage không có
    const cookieValue = document.cookie.split('; ').find(row => row.startsWith(`${SOURCE_COOKIE_NAME}=`));
    if (cookieValue) {
        const source = cookieValue.split('=')[1];
        localStorage.setItem(SOURCE_KEY, source); // Lưu vào localStorage cho lần sau
        return source;
    }
    return SOURCE_MANGADEX; // Mặc định
}

/**
 * Áp dụng nguồn truyện cho UI và kích hoạt tải lại trang
 * @param {string} source - Nguồn truyện ('MangaDex' hoặc 'MangaReaderLib')
 * @param {boolean} [showNotification=false] - Có hiển thị toast thông báo không
 */
function applyMangaSource(source, showNotification = false) {
    // Cập nhật UI của switcher
    updateSourceSwitcherUI(source);

    // Hiển thị thông báo nếu cần
    if (showNotification && typeof window.showToast === 'function') {
        window.showToast('Thông báo', `Đã chuyển nguồn truyện sang ${source === SOURCE_MANGADEX ? 'MangaDex' : 'MangaReaderLib'}!`, 'info');
    }

    // Kích hoạt HTMX để tải lại trang hiện tại
    // Điều này đảm bảo backend nhận được cookie mới và tải dữ liệu từ nguồn đúng
    if (window.htmx) {
        console.log(`[Source] Kích hoạt HTMX tải lại trang hiện tại để áp dụng nguồn mới: ${source}`);
        // Lấy URL hiện tại để tải lại
        const currentPath = window.location.pathname + window.location.search;
        htmx.ajax('GET', currentPath, {
            target: '#main-content', // Target nội dung chính
            swap: 'innerHTML',        // Swap nội dung
            pushUrl: false            // Không đẩy URL vào lịch sử (vì chỉ tải lại trang hiện tại)
        });
    } else {
        console.warn("[Source] HTMX không khả dụng. Tải lại trang hoàn toàn.");
        window.location.reload(); // Fallback nếu HTMX không khả dụng
    }
}

/**
 * Cập nhật giao diện của custom source switcher
 * @param {string} source - Nguồn truyện hiện tại ('MangaDex' hoặc 'MangaReaderLib')
 */
function updateSourceSwitcherUI(source) {
    const switcherItem = document.getElementById('customSourceSwitcherItem');
    const switcherText = document.getElementById('customSourceSwitcherText');
    const switcherIcon = document.getElementById('customSourceIcon');

    if (!switcherItem || !switcherText || !switcherIcon) {
        console.warn("[Source] Không tìm thấy các thành phần của custom source switcher.");
        return;
    }

    const isMangaReaderLib = source === SOURCE_MANGAREADERLIB;

    // Cập nhật class cho switch trực quan
    if (isMangaReaderLib) {
        switcherItem.classList.add('mangareader-source');
    } else {
        switcherItem.classList.remove('mangareader-source');
    }

    // Cập nhật icon và text
    if (isMangaReaderLib) {
        switcherIcon.className = 'bi bi-cloud-check-fill me-2'; // Icon khi là MangaReaderLib
        switcherText.childNodes[switcherText.childNodes.length - 1].nodeValue = ' Nguồn: MangaReaderLib';
    } else {
        switcherIcon.className = 'bi bi-cloud-arrow-down me-2'; // Icon khi là MangaDex
        switcherText.childNodes[switcherText.childNodes.length - 1].nodeValue = ' Nguồn: MangaDex';
    }
    console.log(`[Source] UI updated for ${source} mode.`);
}

/**
 * Khởi tạo chức năng chuyển đổi nguồn truyện (Internal)
 */
function initCustomSourceSwitcherInternal() {
    console.log("[Source] Initializing custom source switcher internal...");
    const switcherItem = document.getElementById('customSourceSwitcherItem');

    if (!switcherItem) {
        return;
    }

    const initialSource = getSavedMangaSource();
    console.log(`[Source] Initial source: ${initialSource}`);
    // Áp dụng UI ban đầu mà không cần tải lại trang
    updateSourceSwitcherUI(initialSource); 
    saveMangaSource(initialSource); // Đảm bảo cookie được set

    // --- Xử lý Event Listener ---
    const handleSwitcherClick = (event) => {
        event.preventDefault();
        event.stopPropagation();

        const currentSource = getSavedMangaSource();
        const newSource = currentSource === SOURCE_MANGADEX ? SOURCE_MANGAREADERLIB : SOURCE_MANGADEX;

        console.log(`[Source] Switching from ${currentSource} to ${newSource}`);
        saveMangaSource(newSource); // Lưu nguồn mới vào localStorage và cookie
        applyMangaSource(newSource, true); // Áp dụng UI và kích hoạt tải lại
    };

    if (switcherItem._sourceClickListener) {
        switcherItem.removeEventListener('click', switcherItem._sourceClickListener);
    }
    switcherItem._sourceClickListener = handleSwitcherClick;
    switcherItem.addEventListener('click', handleSwitcherClick);
    console.log("[Source] Custom source switcher initialized and click listener attached.");
}

// Export hàm khởi tạo chính
export default {
    initUIToggles
};
