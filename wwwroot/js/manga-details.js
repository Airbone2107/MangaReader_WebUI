/**
 * manga-details.js - JavaScript cho trang chi tiết manga
 * File này được tải trực tiếp từ view Details.cshtml
 */

// Import các hàm cần thiết từ module
import { 
    adjustHeaderBackgroundHeight, 
    initMangaDetailsPage, 
    initAfterHtmxLoad,
    initDropdowns,
    initChapterItems,
    initFollowButton,
    toggleFollow,
    showToast 
} from './modules/manga-details.js';

// Tạo một phiên bản window-level của toggleFollow để có thể truy cập từ các thành phần HTML
window.toggleFollow = toggleFollow;
window.showToast = showToast;

document.addEventListener('DOMContentLoaded', function() {
    console.log('manga-details.js: Khởi tạo trang chi tiết manga...');
    
    // Gọi hàm khởi tạo cho trang chi tiết
    initMangaDetailsPage();
    
    // Thiết lập xử lý sự kiện HTMX nếu người dùng chuyển đến page này bằng HTMX
    if (window.htmx) {
        console.log('manga-details.js: HTMX đã được tải trong trang chi tiết manga');
        
        // Đăng ký xử lý sau khi HTMX tải nội dung
        htmx.on('htmx:afterSwap', function(event) {
            if (document.querySelector('.details-manga-header-background')) {
                console.log('manga-details.js: HTMX đã tải lại trang chi tiết manga');
                
                // Đảm bảo chờ một chút để DOM cập nhật hoàn toàn
                setTimeout(function() {
                    console.log('manga-details.js: Khởi tạo lại các tính năng sau HTMX');
                    initAfterHtmxLoad();
                }, 100);
            }
        });
    }
}); 