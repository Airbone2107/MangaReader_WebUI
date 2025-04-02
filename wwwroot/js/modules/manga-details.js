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
 * Khởi tạo lại tất cả các dropdown một cách đơn giản hơn
 */
function initDropdowns() {
    console.log('Khởi tạo lại tất cả dropdown...');
    
    // Xóa bỏ tất cả event listener cũ bằng cách clone các phần tử
    document.querySelectorAll('.custom-language-header, .custom-volume-header').forEach(header => {
        const clone = header.cloneNode(true);
        header.parentNode.replaceChild(clone, header);
    });
    
    // Thêm lại event listener cho language headers
    document.querySelectorAll('.custom-language-header').forEach(header => {
        header.addEventListener('click', function(e) {
            e.stopPropagation();
            const langId = this.getAttribute('data-lang-id');
            console.log('Click vào language header:', langId);
            this.classList.toggle('active');
        });
    });
    
    // Thêm lại event listener cho volume headers
    document.querySelectorAll('.custom-volume-header').forEach(header => {
        header.addEventListener('click', function(e) {
            e.stopPropagation();
            const volumeId = this.getAttribute('data-volume-id');
            console.log('Click vào volume header:', volumeId);
            this.classList.toggle('active');
        });
    });
    
    // Mở ngôn ngữ đầu tiên theo mặc định
    const firstLangHeader = document.querySelector('.custom-language-header');
    if (firstLangHeader && !firstLangHeader.classList.contains('active')) {
        firstLangHeader.classList.add('active');
        
        // Khi mở ngôn ngữ đầu tiên, cũng mở luôn volume đầu tiên
        const firstVolumeHeader = document.querySelector('.custom-volume-header');
        if (firstVolumeHeader && !firstVolumeHeader.classList.contains('active')) {
            firstVolumeHeader.classList.add('active');
        }
        
        // Mở tất cả các volume trong ngôn ngữ đầu tiên
        const langId = firstLangHeader.getAttribute('data-lang-id');
        const firstLangContent = document.querySelector(`#lang-content-${langId}`);
        if (firstLangContent) {
            const firstLangVolumeHeaders = firstLangContent.querySelectorAll('.custom-volume-header');
            firstLangVolumeHeaders.forEach(volHeader => {
                if (!volHeader.classList.contains('active')) {
                    volHeader.classList.add('active');
                }
            });
            console.log('Đã mở tất cả volume của ngôn ngữ đầu tiên:', firstLangVolumeHeaders.length, 'volume');
        } else {
            console.log('Không tìm thấy phần nội dung của ngôn ngữ đầu tiên!');
        }
    }
}

/**
 * Khởi tạo xử lý bộ lọc ngôn ngữ
 */
function initLanguageFilter() {
    // Lấy tất cả các nút lọc ngôn ngữ
    const filterButtons = document.querySelectorAll('.language-filter-btn');
    
    console.log('Tìm thấy', filterButtons.length, 'nút lọc ngôn ngữ');
    
    // Xóa tất cả event listeners cũ nếu có
    filterButtons.forEach(button => {
        const newButton = button.cloneNode(true);
        button.parentNode.replaceChild(newButton, button);
    });
    
    // Thêm lại event listeners
    document.querySelectorAll('.language-filter-btn').forEach(button => {
        button.addEventListener('click', function() {
            // Xóa class active khỏi tất cả các nút
            document.querySelectorAll('.language-filter-btn').forEach(btn => {
                btn.classList.remove('active');
            });
            
            // Thêm class active vào nút được click
            this.classList.add('active');
            
            // Lấy ngôn ngữ cần lọc
            const lang = this.getAttribute('data-lang');
            console.log('Đã chọn lọc ngôn ngữ:', lang);
            
            // Lấy tất cả các phần ngôn ngữ
            const languageSections = document.querySelectorAll('.custom-language-section');
            
            // Hiển thị/ẩn các phần ngôn ngữ dựa trên ngôn ngữ đã chọn
            languageSections.forEach(section => {
                const sectionLang = section.getAttribute('data-language');
                
                if (lang === 'all' || lang === sectionLang) {
                    section.style.display = 'block';
                    
                    // Mở dropdown ngôn ngữ được chọn nếu chỉ hiển thị một ngôn ngữ
                    if (lang !== 'all') {
                        const header = section.querySelector('.custom-language-header');
                        if (header && !header.classList.contains('active')) {
                            header.classList.add('active');
                        }
                    }
                } else {
                    section.style.display = 'none';
                }
            });
        });
    });
}

/**
 * Khởi tạo xử lý cho chapter items
 */
function initChapterItems() {
    // Không cần thêm event listener cho chapter items vì đã là thẻ <a>
    // Nhưng ta có thể thêm logic để đánh dấu các chapter đã đọc hoặc xử lý khác
    console.log('Đã khởi tạo chapter items');
}

/**
 * Khởi tạo tất cả chức năng liên quan đến chi tiết manga
 */
function initMangaDetailsPage() {
    // Kiểm tra xem đang ở trang chi tiết manga không trước khi khởi tạo
    if (document.querySelector('.details-manga-header-background')) {
        // Khởi tạo tooltips
        initTooltips();
        
        // Khởi tạo tất cả dropdown
        initDropdowns();
        
        // Khởi tạo bộ lọc ngôn ngữ
        initLanguageFilter();
        
        // Khởi tạo xử lý cho chapter items
        initChapterItems();
        
        // Gọi hàm khi trang tải xong và khi cửa sổ thay đổi kích thước
        // Đợi một chút để đảm bảo các element đã được render đầy đủ
        setTimeout(adjustHeaderBackgroundHeight, 100);
        
        // Chỉ thêm event listener cho resize, loại bỏ scroll listener vì không cần thiết
        window.addEventListener('resize', adjustHeaderBackgroundHeight);
        
        console.log('Đã khởi tạo trang chi tiết manga');
    }
}

/**
 * Khởi tạo lại các tính năng sau khi HTMX tải nội dung
 */
function initAfterHtmxLoad() {
    // Khởi tạo lại tất cả dropdown
    initDropdowns();
    
    // Khởi tạo lại bộ lọc ngôn ngữ
    initLanguageFilter();
    
    // Khởi tạo lại xử lý cho chapter items
    initChapterItems();
    
    // Điều chỉnh lại chiều cao background
    setTimeout(adjustHeaderBackgroundHeight, 100);
}

// Export các hàm để có thể sử dụng ở file khác
export { adjustHeaderBackgroundHeight, initMangaDetailsPage, initAfterHtmxLoad, initDropdowns, initChapterItems }; 