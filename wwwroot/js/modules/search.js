/**
 * search.js - Quản lý các chức năng liên quan đến tìm kiếm
 */

/**
 * Khởi tạo chức năng tìm kiếm nhanh
 */
function initQuickSearch() {
    const searchForm = document.getElementById('quickSearchForm');
    const searchInput = document.getElementById('quickSearchInput');
    
    if (!searchForm || !searchInput) return;
    
    // Xử lý sự kiện submit form
    searchForm.addEventListener('submit', function(event) {
        const searchTerm = searchInput.value.trim();
        if (!searchTerm) {
            event.preventDefault(); // Ngăn submit nếu không có từ khóa
        }
    });
    
    // Tự động focus vào ô tìm kiếm khi nhấn Ctrl+K hoặc /
    document.addEventListener('keydown', function(event) {
        // Nếu người dùng nhấn Ctrl+K hoặc / khi không đang focus vào ô input
        if ((event.ctrlKey && event.key === 'k') || (event.key === '/' && document.activeElement.tagName !== 'INPUT')) {
            event.preventDefault();
            searchInput.focus();
        }
    });
}

export { initQuickSearch }; 