/**
 * main.js - File chính để import và khởi tạo tất cả các module
 */

// Import các module
import { initAuthUI } from './auth.js';
import { initCustomDropdowns } from './modules/custom-dropdown.js';
import { initErrorHandling } from './modules/error-handling.js';
import { initHtmxHandlers, reinitializeAfterHtmxLoad } from './modules/htmx-handlers.js';
import { initMangaDetailsPage } from './modules/manga-details.js';
import { initTagsInSearchForm } from './modules/manga-tags.js';
import { initReadPage } from './modules/read-page.js';
import { initReadingState } from './modules/reading-state.js';
import SearchModule from './modules/search.js';
import { initSidebar } from './modules/sidebar.js';
import { initCustomThemeSwitcher } from './modules/theme.js';
import { initToasts } from './modules/toast.js';
import {
    adjustFooterPosition,
    adjustMangaTitles,
    cleanupActiveLinks,
    createDefaultImage,
    fixAccordionIssues,
    initBackToTop,
    initLazyLoading,
    initResponsive,
    initTooltips
} from './modules/ui.js';

// --- Xử lý Back/Forward và bfcache ---
window.addEventListener('pageshow', function(event) {
  // event.persisted là true nếu trang được tải từ bfcache (back/forward)
  if (event.persisted) {
    console.log('[pageshow] Page loaded from bfcache.');

    // Kiểm tra xem trang này có phải là trang được quản lý bởi HTMX không
    // (Dấu hiệu có thể là sự tồn tại của #main-content hoặc body có thuộc tính hx-boost)
    const mainContent = document.getElementById('main-content');
    const isHtmxManagedPage = mainContent || document.body.hasAttribute('hx-boost'); // Điều chỉnh điều kiện nếu cần

    if (isHtmxManagedPage) {
        console.log('[pageshow] This seems to be an HTMX-managed page restored from bfcache.');
        // Có vẻ như htmx:load không được kích hoạt đúng cách trong trường hợp này (non-HTMX -> HTMX back).
        // => Gọi lại hàm reinitializeAfterHtmxLoad một cách thủ công như một fallback.
        // Chúng ta truyền document.body vì toàn bộ trang đã được khôi phục.
        reinitializeAfterHtmxLoad(document.body);
    } else {
        console.log('[pageshow] This page does not seem to be HTMX-managed. No manual re-init needed.');
        // Đối với trang non-HTMX được khôi phục từ bfcache,
        // nếu cần chạy lại JS nào đó, bạn có thể thêm logic ở đây.
        // Ví dụ: Nếu trang Read cần khởi tạo lại gì đó khi back về nó.
    }
  } else {
      console.log('[pageshow] Page loaded normally (not from bfcache).');
  }
});
// --- Kết thúc xử lý Back/Forward ---

/**
 * Khởi tạo tất cả các module
 */
document.addEventListener('DOMContentLoaded', function() {
    // Xóa bỏ inline style của các active nav-link
    cleanupActiveLinks();
    
    // Khởi tạo tooltips
    initTooltips();
    
    // Khởi tạo lazy loading cho hình ảnh
    initLazyLoading();
    
    // Khởi tạo module tìm kiếm
    // Luôn khởi tạo module để đăng ký các hàm và sự kiện cần thiết
    SearchModule.init();
    console.log('Search module registered');
    
    // Khởi tạo module quản lý thẻ manga
    initTagsInSearchForm();
    console.log('Manga tags module registered');
    
    // Tạo ảnh mặc định nếu chưa có
    createDefaultImage();
    
    // Khởi tạo chức năng hiển thị thông báo
    initToasts();
    
    // Khởi tạo chức năng lưu trạng thái đọc
    initReadingState();
    
    // Khởi tạo UI xác thực
    initAuthUI();
    console.log('Auth module registered');
    
    // Khởi tạo custom dropdowns
    initCustomDropdowns();
    console.log('Custom dropdowns initialized');
    
    // Khởi tạo chức năng chuyển đổi chế độ tối/sáng tùy chỉnh
    initCustomThemeSwitcher();
    
    // Khởi tạo nút back-to-top
    initBackToTop();
    
    // Khởi tạo chức năng xử lý lỗi
    initErrorHandling();
    
    // Khởi tạo chức năng responsive
    initResponsive();
    
    // Khắc phục vấn đề với accordion
    fixAccordionIssues();
    
    // Tự động điều chỉnh vị trí footer
    adjustFooterPosition();
    
    // Điều chỉnh kích thước chữ cho tiêu đề manga
    adjustMangaTitles();
    
    // Khởi tạo sidebar menu
    initSidebar();
    
    // Khởi tạo chức năng trang chi tiết manga có điều kiện
    if (document.querySelector('.details-manga-header-background') || document.querySelector('.details-manga-details-container')) {
        console.log('Main.js: Đang khởi tạo tính năng trang chi tiết manga khi tải trực tiếp.');
        initMangaDetailsPage();
    }
    
    // Khởi tạo chức năng cho trang đọc chapter có điều kiện
    if (document.querySelector('.chapter-reader-container') || document.getElementById('readingSidebar')) {
        console.log('Main.js: Đang khởi tạo tính năng trang đọc chapter khi tải trực tiếp.');
        initReadPage();
    }
    
    // Khởi tạo xử lý HTMX
    initHtmxHandlers();
    
    // Đánh dấu việc khởi tạo hoàn tất
    console.log('Manga Reader Web: Tất cả các module đã được khởi tạo thành công.');
});

// Lưu ý: Tất cả các sự kiện HTMX được xử lý trong htmx-handlers.js
// Không xử lý trùng lặp ở đây