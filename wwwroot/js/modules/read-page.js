/**
 * read-page.js - Xử lý JavaScript cho trang đọc chapter
 * 
 * Module này quản lý các chức năng trong trang đọc chapter bao gồm:
 * - Xử lý lazy loading cho ảnh
 * - Điều khiển Reading Sidebar
 * - Xử lý chuyển đổi chapter qua dropdown
 * - Xử lý các nút tùy chỉnh (chế độ đọc, tỷ lệ ảnh)
 */

// Biến lưu trạng thái tỷ lệ ảnh (mặc định là false - tức là 'default')
let isFitWidthMode = false;
const IMAGE_SCALE_MODE_KEY = 'imageScaleMode'; // Key cho localStorage

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
    initImageScaling();
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
 * Khởi tạo lazy loading và xử lý trạng thái cho ảnh
 * @param {string} containerSelector - Selector của container chứa ảnh
 */
function initImageLoading(containerSelector) {
    console.log('[Read Page] Initializing Image Loading Logic v2');

    const container = document.querySelector(containerSelector);

    if (!container) {
        console.warn(`[Read Page] Container ${containerSelector} not found`);
        return;
    }

    // Xử lý các ảnh đã có sẵn trong container (trường hợp tải trang không qua HTMX)
    processImagesInContainer(container);

    // Theo dõi các thay đổi trong container (trường hợp ảnh được thêm vào qua HTMX)
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.addedNodes.length) {
                mutation.addedNodes.forEach(node => {
                    // Chỉ xử lý nếu node được thêm là element và chứa ảnh cần load
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        if (node.matches('.page-image-container')) {
                            processImageContainer(node);
                        } else {
                            // Kiểm tra các ảnh con nếu node thêm vào là container lớn hơn
                            node.querySelectorAll('.page-image-container').forEach(processImageContainer);
                        }
                    }
                });
            }
        });
    });

    observer.observe(container, { childList: true, subtree: true });
}

/**
 * Xử lý tất cả các container ảnh trong một container lớn
 * @param {HTMLElement} container - Container cha chứa các .page-image-container
 */
function processImagesInContainer(container) {
    const imageContainers = container.querySelectorAll('.page-image-container');
    console.log(`[Read Page] Processing ${imageContainers.length} images in container`);
    imageContainers.forEach(processImageContainer);
}

/**
 * Xử lý một container ảnh cụ thể
 * @param {HTMLElement} imgContainer - Phần tử .page-image-container
 */
function processImageContainer(imgContainer) {
    const img = imgContainer.querySelector('img.lazy-load');
    const loadingIndicator = imgContainer.querySelector('.loading-indicator');
    const errorOverlay = imgContainer.querySelector('.error-overlay');
    const retryButton = errorOverlay?.querySelector('.retry-button');

    if (!img || !loadingIndicator || !errorOverlay || !retryButton) {
        console.warn('[Read Page] Missing elements for an image container:', imgContainer);
        imgContainer.classList.add('error'); // Đánh dấu lỗi nếu thiếu cấu trúc
        imgContainer.classList.remove('loading', 'loaded');
        return;
    }

    // Kiểm tra xem ảnh này đã được xử lý chưa (tránh gắn listener nhiều lần)
    if (imgContainer.dataset.imageProcessed === 'true') {
        return;
    }
    imgContainer.dataset.imageProcessed = 'true'; // Đánh dấu đã xử lý

    const dataSrc = img.getAttribute('data-src');
    if (!dataSrc) {
        console.warn('[Read Page] Missing data-src for image:', img);
        imgContainer.classList.add('error');
        imgContainer.classList.remove('loading', 'loaded');
        return;
    }

    function loadImage(src) {
        // Đặt trạng thái loading ngay lập tức
        imgContainer.classList.remove('loaded', 'error');
        imgContainer.classList.add('loading');
        img.src = ''; // Reset src để đảm bảo event 'error' kích hoạt lại đúng cách

        // Gán src sau một khoảng trễ nhỏ
        setTimeout(() => {
            img.src = src;
            console.log(`[Read Page] Loading image: ${src}`);
        }, 0);
    }

    // Gắn listener một lần duy nhất
    img.onload = () => {
        console.log(`[Read Page] Image loaded successfully: ${img.src}`);
        imgContainer.classList.remove('loading', 'error');
        imgContainer.classList.add('loaded');
    };

    img.onerror = () => {
        console.error(`[Read Page] Error loading image: ${img.src || dataSrc}`);
        imgContainer.classList.remove('loading', 'loaded');
        imgContainer.classList.add('error');
    };

    // Xóa listener cũ của nút retry trước khi thêm mới (nếu có)
    const newRetryButton = retryButton.cloneNode(true);
    retryButton.parentNode.replaceChild(newRetryButton, retryButton);
    newRetryButton.addEventListener('click', () => {
        console.log('[Read Page] Retrying image');
        loadImage(`${dataSrc}?t=${Date.now()}`); // Thêm timestamp để force reload
    });

    // Bắt đầu tải ảnh
    loadImage(dataSrc);
}

/**
 * Khởi tạo nút điều chỉnh tỷ lệ ảnh
 */
function initImageScaling() {
    console.log('[Read Page] Initializing Image Scaling Button');
    const scaleBtn = document.getElementById('imageScaleBtn');
    const scaleText = document.getElementById('imageScaleText');
    const imageContainer = document.getElementById('chapterImagesContainer');

    if (!scaleBtn || !scaleText || !imageContainer) {
        console.warn('[Read Page] Missing elements for image scaling');
        return;
    }

    // Đọc trạng thái đã lưu từ localStorage
    const savedMode = localStorage.getItem(IMAGE_SCALE_MODE_KEY);
    isFitWidthMode = (savedMode === 'fit-width'); // true nếu là fit-width, false nếu là default hoặc null

    // Hàm cập nhật UI và trạng thái
    function updateScaleModeUI() {
        if (isFitWidthMode) {
            imageContainer.classList.add('fit-width-mode');
            scaleText.textContent = 'Tỷ lệ: Căng ngang';
            console.log('[Read Page] Image scale mode set to Fit Width');
        } else {
            imageContainer.classList.remove('fit-width-mode');
            scaleText.textContent = 'Tỷ lệ: Mặc định';
            console.log('[Read Page] Image scale mode set to Default');
        }
    }

    // Hàm lưu trạng thái
    function saveScaleModeState() {
         localStorage.setItem(IMAGE_SCALE_MODE_KEY, isFitWidthMode ? 'fit-width' : 'default');
    }

    // Áp dụng trạng thái ban đầu
    updateScaleModeUI();

    // --- Xử lý Event Listener ---
    // Lưu function handler vào một biến để có thể remove
    const handleScaleButtonClick = () => {
        isFitWidthMode = !isFitWidthMode; // Toggle trạng thái
        updateScaleModeUI();
        saveScaleModeState();
    };

    // Xóa listener cũ trước khi thêm mới (quan trọng khi re-init)
    // Cần một cách để xác định listener cũ, ví dụ lưu trữ nó
    if (scaleBtn._scaleClickListener) {
        scaleBtn.removeEventListener('click', scaleBtn._scaleClickListener);
    }
    // Lưu listener mới vào element để có thể xóa sau này
    scaleBtn._scaleClickListener = handleScaleButtonClick;
    // Thêm listener mới
    scaleBtn.addEventListener('click', handleScaleButtonClick);

    console.log('[Read Page] Image Scaling Button listener attached.');
}

/**
 * Khởi tạo các nút placeholder (chế độ đọc, tỷ lệ ảnh)
 */
function initPlaceholderButtons() {
    console.log('[Read Page] Initializing Placeholder Buttons');
    
    const readingModeBtn = document.getElementById('readingModeBtn');
    
    if (readingModeBtn) {
        readingModeBtn.addEventListener('click', () => {
            console.log('[Read Page] Reading Mode button clicked - Feature planned for future implementation');
        });
    }
}

export { 
    initReadPage, 
    initImageLoading, 
    initSidebarToggle, 
    initChapterDropdownNav, 
    initContentAreaClickToOpenSidebar,
    initImageScaling,
    initPlaceholderButtons
}; 