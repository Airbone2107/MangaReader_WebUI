/**
 * main.js - File chính để import và khởi tạo tất cả các module
 */

// Import các module
import { updateActiveSidebarLink, initSidebar } from './modules/sidebar.js';
import { initHtmxHandlers } from './modules/htmx-handlers.js';
import { 
    cleanupActiveLinks, 
    initTooltips, 
    initLazyLoading, 
    initBackToTop, 
    initResponsive, 
    fixAccordionIssues, 
    adjustFooterPosition,
    createDefaultImage 
} from './modules/ui.js';
import { initThemeSwitcher } from './modules/theme.js';
import { initToasts } from './modules/toast.js';
import { initQuickSearch } from './modules/search.js';
import { initReadingState } from './modules/reading-state.js';
import { initErrorHandling } from './modules/error-handling.js';
import { initMangaDetailsPage } from './modules/manga-details.js';

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
    
    // Khởi tạo chức năng tìm kiếm nhanh
    initQuickSearch();
    
    // Tạo ảnh mặc định nếu chưa có
    createDefaultImage();
    
    // Khởi tạo chức năng hiển thị thông báo
    initToasts();
    
    // Khởi tạo chức năng lưu trạng thái đọc
    initReadingState();
    
    // Khởi tạo chức năng chuyển đổi chế độ tối/sáng
    initThemeSwitcher();
    
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
    
    // Khởi tạo sidebar menu
    initSidebar();
    
    // Khởi tạo chức năng trang chi tiết manga
    initMangaDetailsPage();
    
    // Khởi tạo xử lý HTMX
    initHtmxHandlers();
    
    // Đánh dấu việc khởi tạo hoàn tất
    console.log('Manga Reader Web: Tất cả các module đã được khởi tạo thành công.');
}); 