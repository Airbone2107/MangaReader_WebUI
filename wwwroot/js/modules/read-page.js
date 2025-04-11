/**
 * read-page.js - Xử lý JavaScript cho trang đọc chapter
 * 
 * Module này quản lý các chức năng trong trang đọc chapter bao gồm:
 * - Xử lý lazy loading cho ảnh
 * - Điều khiển Reading Sidebar
 * - Xử lý chuyển đổi chapter qua dropdown
 * - Xử lý các nút tùy chỉnh (chế độ đọc, tỷ lệ ảnh)
 */

/**
 * Khởi tạo các chức năng cho trang đọc chapter
 */
function initReadPage() {
    console.log('[Read Page] Initializing Read Page features');
    initSidebarToggle();
    initContentAreaClickToOpenSidebar();
    initChapterDropdownNav();
    initImageLoading('#chapterImagesContainer');
    initPlaceholderButtons();
}

/**
 * Khởi tạo toggle sidebar
 */
function initSidebarToggle() {
    console.log('[Read Page] Initializing Reading Sidebar Toggle');
    
    const sidebarToggleBtn = document.getElementById('readingSidebarToggle');
    const sidebar = document.getElementById('readingSidebar');
    const closeBtn = document.getElementById('closeSidebarBtn');
    
    if (!sidebarToggleBtn || !sidebar) {
        console.warn('[Read Page] Missing elements for sidebar toggle');
        return;
    }
    
    // Xử lý mở sidebar
    sidebarToggleBtn.addEventListener('click', () => {
        openSidebar();
    });
    
    // Hàm mở sidebar
    function openSidebar() {
        sidebar.classList.add('open');
    }
    
    // Hàm đóng sidebar
    function closeSidebar() {
        sidebar.classList.remove('open');
    }
    
    // Xử lý đóng sidebar
    closeBtn.addEventListener('click', closeSidebar);
    
    // Xử lý đóng sidebar bằng phím ESC
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && sidebar.classList.contains('open')) {
            closeSidebar();
        }
    });
}

/**
 * Khởi tạo chức năng mở/đóng sidebar khi nhấp vào khu vực đọc truyện
 */
function initContentAreaClickToOpenSidebar() {
    console.log('[Read Page] Initializing Click-to-Open/Close Sidebar');
    
    const imageContainer = document.getElementById('chapterImagesContainer');
    const sidebar = document.getElementById('readingSidebar');
    const pinBtn = document.getElementById('pinSidebarBtn');
    
    if (!imageContainer || !sidebar) {
        console.warn('[Read Page] Missing elements for click-to-open/close sidebar');
        return;
    }
    
    // Thêm chức năng placeholder cho nút ghim
    if (pinBtn) {
        pinBtn.addEventListener('click', () => {
            console.log('[Read Page] Pin sidebar button clicked - Feature planned for future implementation');
        });
    }
    
    // Sử dụng event delegation để tránh xung đột với các sự kiện khác
    imageContainer.addEventListener('click', (event) => {
        // Đảm bảo click là trực tiếp vào container, không phải vào các phần tử con khác
        if (event.target === imageContainer || event.target.closest('.page-image-container')) {
            if (sidebar.classList.contains('open')) {
                // Nếu sidebar đang mở, đóng nó lại
                sidebar.classList.remove('open');
                console.log('[Read Page] Sidebar closed by image container click');
            } else {
                // Nếu sidebar đang đóng, mở ra
                sidebar.classList.add('open');
                console.log('[Read Page] Sidebar opened by image container click');
            }
        }
    });
}

/**
 * Khởi tạo dropdown chọn chapter
 */
function initChapterDropdownNav() {
    console.log('[Read Page] Initializing Chapter Dropdown Navigation');
    
    const chapterSelect = document.getElementById('chapterSelect');
    
    if (!chapterSelect) {
        console.warn('[Read Page] Missing chapterSelect element');
        return;
    }
    
    chapterSelect.addEventListener('change', function() {
        const chapterId = this.value;
        if (!chapterId) return;
        
        console.log(`[Read Page] Chapter selected: ${chapterId}`);
        
        // Tạo URL mới
        const newUrl = `/Chapter/Read/${chapterId}`;
        
        // Sử dụng htmx để chuyển trang
        if (window.htmx) {
            htmx.ajax('GET', newUrl, {
                target: '#main-content',
                swap: 'innerHTML',
                pushUrl: true
            });
        } else {
            // Fallback nếu không có htmx
            window.location.href = newUrl;
        }
    });
}

/**
 * Khởi tạo lazy loading cho ảnh
 * @param {string} containerSelector - Selector của container chứa ảnh
 */
function initImageLoading(containerSelector) {
    console.log('[Read Page] Initializing Image Loading Logic');
    
    const container = document.querySelector(containerSelector);
    
    if (!container) {
        console.warn(`[Read Page] Container ${containerSelector} not found`);
        return;
    }
    
    // Chờ cho content của container được load
    const observer = new MutationObserver((mutations) => {
        // Sau khi content được load qua hx-trigger
        mutations.forEach((mutation) => {
            if (mutation.type === 'childList') {
                // Kiểm tra nếu đã load partial view với ảnh
                const imageContainers = container.querySelectorAll('.page-image-container');
                
                if (imageContainers.length > 0) {
                    console.log(`[Read Page] Found ${imageContainers.length} images to load`);
                    observer.disconnect(); // Ngừng theo dõi vì đã tìm thấy ảnh
                    
                    // Khởi tạo lazy load cho từng ảnh
                    imageContainers.forEach((imgContainer, index) => {
                        const img = imgContainer.querySelector('img.lazy-load');
                        const loadingIndicator = imgContainer.querySelector('.loading-indicator');
                        const errorOverlay = imgContainer.querySelector('.error-overlay');
                        const retryButton = errorOverlay?.querySelector('.retry-button');
                        
                        if (!img || !loadingIndicator || !errorOverlay || !retryButton) {
                            console.warn(`[Read Page] Missing elements for image ${index}`);
                            return;
                        }
                        
                        const dataSrc = img.getAttribute('data-src');
                        if (!dataSrc) {
                            console.warn(`[Read Page] Missing data-src for image ${index}`);
                            return;
                        }
                        
                        // Function to load the image
                        function loadImage(src) {
                            // Show loading indicator, hide error overlay
                            loadingIndicator.style.display = 'flex';
                            errorOverlay.style.display = 'none';
                            
                            // Set image source
                            img.src = src;
                            
                            console.log(`[Read Page] Loading image ${index}: ${src}`);
                        }
                        
                        // Load the image initially
                        loadImage(dataSrc);
                        
                        // Handle successful image load
                        img.addEventListener('load', () => {
                            console.log(`[Read Page] Image ${index} loaded successfully`);
                            loadingIndicator.style.display = 'none';
                        });
                        
                        // Handle image load error
                        img.addEventListener('error', () => {
                            console.error(`[Read Page] Error loading image ${index}`);
                            loadingIndicator.style.display = 'none';
                            errorOverlay.style.display = 'flex';
                        });
                        
                        // Handle retry button click
                        retryButton.addEventListener('click', () => {
                            console.log(`[Read Page] Retrying image ${index}`);
                            // Add timestamp to URL to force reload
                            loadImage(`${dataSrc}?t=${Date.now()}`);
                        });
                    });
                }
            }
        });
    });
    
    // Start observing changes to the container
    observer.observe(container, { childList: true });
}

/**
 * Khởi tạo các nút placeholder (chế độ đọc, tỷ lệ ảnh)
 */
function initPlaceholderButtons() {
    console.log('[Read Page] Initializing Placeholder Buttons');
    
    const readingModeBtn = document.getElementById('readingModeBtn');
    const imageScaleBtn = document.getElementById('imageScaleBtn');
    
    if (readingModeBtn) {
        readingModeBtn.addEventListener('click', () => {
            console.log('[Read Page] Reading Mode button clicked - Feature planned for future implementation');
        });
    }
    
    if (imageScaleBtn) {
        imageScaleBtn.addEventListener('click', () => {
            console.log('[Read Page] Image Scale button clicked - Feature planned for future implementation');
        });
    }
}

export { initReadPage, initImageLoading, initSidebarToggle, initChapterDropdownNav, initContentAreaClickToOpenSidebar }; 