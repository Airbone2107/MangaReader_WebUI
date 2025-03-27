/**
 * Manga details module
 * Xử lý các chức năng JavaScript cho trang chi tiết manga
 */

// Import các hàm từ các module khác
import { initTooltips } from './ui.js';

/**
 * Tính toán và điều chỉnh chiều cao cho details-manga-header-background
 * để đảm bảo hiển thị đúng với nội dung
 */
function adjustHeaderBackgroundHeight() {
    const siteHeader = document.querySelector('.site-header');
    const headerBackground = document.querySelector('.details-manga-header-background');
    const headerContainer = document.querySelector('.details-manga-header-container');
    const titleSection = document.querySelector('.details-manga-info-title');
    
    if (headerContainer && titleSection && headerBackground) {
        // Lấy vị trí của header container so với trang
        const containerRect = headerContainer.getBoundingClientRect();
        
        // Lấy vị trí và kích thước của titleSection
        const titleRect = titleSection.getBoundingClientRect();
        
        // Tính chiều cao site-header nếu có
        let siteHeaderHeight = 0;
        if (siteHeader) {
            siteHeaderHeight = siteHeader.offsetHeight;
            console.log('Chiều cao site-header: ' + siteHeaderHeight + 'px');
        }
        
        // Tính toán chiều cao cần thiết cho background:
        // 1. Vị trí đầu trang đến đầu container (bao gồm cả site-header)
        // 2. Cộng thêm chiều cao từ đầu container đến cuối titleSection
        // 3. Thêm padding để trông đẹp hơn
        const containerTop = window.scrollY + containerRect.top; // Vị trí thực của container
        const titleHeight = titleRect.height;
        const titleOffsetTop = titleRect.top - containerRect.top; // Vị trí tương đối của title trong container
        
        // Tính chiều cao background bằng site-header + khoảng cách đến hết title + padding
        const padding = 25; // thêm padding để nội dung không bị sát đáy background
        const headerHeight = containerTop + titleOffsetTop + titleHeight + padding;
        
        // Đặt chiều cao cho background
        headerBackground.style.height = headerHeight + 'px';
        
        console.log('Chiều cao đến hết title: ' + (titleOffsetTop + titleHeight) + 'px');
        console.log('Tổng chiều cao details-manga-header-background: ' + headerHeight + 'px');
    }
}

/**
 * Khởi tạo tất cả chức năng liên quan đến chi tiết manga
 */
function initMangaDetailsPage() {
    // Kiểm tra xem đang ở trang chi tiết manga không trước khi khởi tạo
    if (document.querySelector('.details-manga-header-background')) {
        // Khởi tạo tooltips
        initTooltips();
        
        // Gọi hàm khi trang tải xong và khi cửa sổ thay đổi kích thước
        // Đợi một chút để đảm bảo các element đã được render đầy đủ
        setTimeout(adjustHeaderBackgroundHeight, 100);
        
        // Chỉ thêm event listener cho resize, loại bỏ scroll listener vì không cần thiết
        window.addEventListener('resize', adjustHeaderBackgroundHeight);
        
        console.log('Đã khởi tạo trang chi tiết manga');
    }
}

// Export các hàm để có thể sử dụng ở file khác
export { adjustHeaderBackgroundHeight, initMangaDetailsPage }; 