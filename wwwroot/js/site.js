/**
 * Manga Reader Web - JavaScript chung
 * Chứa các chức năng JavaScript chung cho toàn bộ ứng dụng
 */

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Khởi tạo khi DOM đã sẵn sàng
document.addEventListener('DOMContentLoaded', function() {
    // Khởi tạo tooltips
    initTooltips();
    
    // Khởi tạo lazy loading cho hình ảnh
    initLazyLoading();
    
    // Khởi tạo chức năng tìm kiếm nhanh
    initQuickSearch();
    
    // Khởi tạo chức năng hiển thị thông báo
    initToasts();
    
    // Khởi tạo chức năng lưu trạng thái đọc
    initReadingState();
    
    // Khởi tạo chức năng chuyển đổi chế độ tối/sáng
    initDarkModeToggle();
});

/**
 * Khởi tạo tooltips cho các phần tử có thuộc tính data-bs-toggle="tooltip"
 */
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Khởi tạo lazy loading cho hình ảnh
 */
function initLazyLoading() {
    // Kiểm tra nếu trình duyệt hỗ trợ Intersection Observer
    if ('IntersectionObserver' in window) {
        const lazyImages = document.querySelectorAll('img[loading="lazy"]');
        
        const imageObserver = new IntersectionObserver(function(entries, observer) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    const lazyImage = entry.target;
                    lazyImage.src = lazyImage.dataset.src || lazyImage.src;
                    lazyImage.classList.remove('lazy');
                    imageObserver.unobserve(lazyImage);
                }
            });
        });
        
        lazyImages.forEach(function(lazyImage) {
            imageObserver.observe(lazyImage);
        });
    }
}

/**
 * Khởi tạo chức năng tìm kiếm nhanh
 */
function initQuickSearch() {
    const searchInput = document.getElementById('quickSearchInput');
    if (!searchInput) return;
    
    searchInput.addEventListener('keyup', function(event) {
        if (event.key === 'Enter') {
            const searchTerm = searchInput.value.trim();
            if (searchTerm) {
                window.location.href = `/Manga/Search?title=${encodeURIComponent(searchTerm)}`;
            }
        }
    });
}

/**
 * Hiển thị thông báo toast
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại thông báo (success, danger, warning, info)
 * @param {number} duration - Thời gian hiển thị (ms)
 */
function showToast(message, type = 'info', duration = 3000) {
    // Tạo toast container nếu chưa tồn tại
    if (!document.getElementById('toastContainer')) {
        const toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Tạo toast
    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    // Thêm toast vào container
    document.getElementById('toastContainer').innerHTML += toastHtml;
    
    // Hiển thị toast
    const toastElement = new bootstrap.Toast(document.getElementById(toastId), {
        delay: duration
    });
    toastElement.show();
    
    // Xóa toast sau khi ẩn
    document.getElementById(toastId).addEventListener('hidden.bs.toast', function() {
        this.remove();
    });
}

/**
 * Khởi tạo chức năng hiển thị thông báo
 */
function initToasts() {
    // Tạo hàm global để các trang có thể sử dụng
    window.showToast = showToast;
}

/**
 * Lưu trạng thái đọc của người dùng
 * @param {string} mangaId - ID của manga
 * @param {string} chapterId - ID của chapter
 * @param {number} page - Trang hiện tại
 */
function saveReadingState(mangaId, chapterId, page) {
    if (!mangaId || !chapterId) return;
    
    const readingState = {
        mangaId: mangaId,
        chapterId: chapterId,
        page: page || 1,
        timestamp: Date.now()
    };
    
    // Lưu trạng thái đọc vào localStorage
    localStorage.setItem(`reading_${mangaId}`, JSON.stringify(readingState));
    
    // Lưu vào danh sách đã đọc gần đây
    const recentlyRead = JSON.parse(localStorage.getItem('recently_read') || '[]');
    
    // Kiểm tra nếu manga đã tồn tại trong danh sách
    const existingIndex = recentlyRead.findIndex(item => item.mangaId === mangaId);
    if (existingIndex !== -1) {
        // Xóa manga khỏi vị trí cũ
        recentlyRead.splice(existingIndex, 1);
    }
    
    // Thêm manga vào đầu danh sách
    recentlyRead.unshift(readingState);
    
    // Giới hạn danh sách chỉ lưu 20 manga gần nhất
    if (recentlyRead.length > 20) {
        recentlyRead.pop();
    }
    
    // Lưu danh sách cập nhật
    localStorage.setItem('recently_read', JSON.stringify(recentlyRead));
}

/**
 * Lấy trạng thái đọc của người dùng
 * @param {string} mangaId - ID của manga
 * @returns {Object|null} - Trạng thái đọc hoặc null nếu không tìm thấy
 */
function getReadingState(mangaId) {
    if (!mangaId) return null;
    
    const readingState = localStorage.getItem(`reading_${mangaId}`);
    return readingState ? JSON.parse(readingState) : null;
}

/**
 * Khởi tạo chức năng lưu trạng thái đọc
 */
function initReadingState() {
    // Tạo hàm global để các trang có thể sử dụng
    window.saveReadingState = saveReadingState;
    window.getReadingState = getReadingState;
}

/**
 * Chuyển đổi giữa chế độ tối và sáng
 */
function toggleDarkMode() {
    const isDarkMode = document.body.classList.toggle('dark-mode');
    localStorage.setItem('dark_mode', isDarkMode ? 'enabled' : 'disabled');
    
    // Hiển thị thông báo
    showToast(`Đã chuyển sang chế độ ${isDarkMode ? 'tối' : 'sáng'}`, 'info', 2000);
}

/**
 * Khởi tạo chức năng chuyển đổi chế độ tối/sáng
 */
function initDarkModeToggle() {
    // Kiểm tra trạng thái đã lưu
    const savedDarkMode = localStorage.getItem('dark_mode');
    
    // Nếu người dùng đã chọn chế độ tối trước đó
    if (savedDarkMode === 'enabled') {
        document.body.classList.add('dark-mode');
    }
    // Nếu người dùng chưa chọn và trình duyệt đang ở chế độ tối
    else if (savedDarkMode === null && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.body.classList.add('dark-mode');
        localStorage.setItem('dark_mode', 'enabled');
    }
    
    // Tạo nút chuyển đổi chế độ tối/sáng nếu chưa tồn tại
    if (!document.getElementById('darkModeToggle')) {
        const darkModeButton = document.createElement('button');
        darkModeButton.id = 'darkModeToggle';
        darkModeButton.className = 'btn btn-sm btn-outline-secondary position-fixed bottom-0 end-0 m-3';
        darkModeButton.innerHTML = '<i class="bi bi-moon-stars"></i>';
        darkModeButton.setAttribute('data-bs-toggle', 'tooltip');
        darkModeButton.setAttribute('data-bs-placement', 'left');
        darkModeButton.setAttribute('title', 'Chuyển đổi chế độ tối/sáng');
        darkModeButton.addEventListener('click', toggleDarkMode);
        
        document.body.appendChild(darkModeButton);
        new bootstrap.Tooltip(darkModeButton);
    }
    
    // Tạo hàm global để các trang có thể sử dụng
    window.toggleDarkMode = toggleDarkMode;
}
