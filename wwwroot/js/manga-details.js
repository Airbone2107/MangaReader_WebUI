/**
 * manga-details.js - JavaScript cho trang chi tiết manga
 * File này được tải trực tiếp từ view Details.cshtml
 */

// Import các hàm cần thiết từ module
import { adjustHeaderBackgroundHeight, initMangaDetailsPage } from './modules/manga-details.js';

document.addEventListener('DOMContentLoaded', function() {
    console.log('Khởi tạo trang chi tiết manga...');
    
    // Gọi hàm khởi tạo cho trang chi tiết
    initMangaDetailsPage();
    
    // Thiết lập xử lý sự kiện HTMX nếu người dùng chuyển đến page này bằng HTMX
    if (window.htmx) {
        console.log('HTMX đã được tải trong trang chi tiết manga');
        
        // Đăng ký xử lý sau khi HTMX tải nội dung
        htmx.on('htmx:afterSwap', function(event) {
            if (document.querySelector('.manga-header-background')) {
                console.log('HTMX đã tải lại trang chi tiết manga');
                setTimeout(adjustHeaderBackgroundHeight, 100);
            }
        });
    }
}); 