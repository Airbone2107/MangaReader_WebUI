/**
 * reading-state.js - Quản lý các chức năng lưu trạng thái đọc
 */

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

export {
    getReadingState,
    initReadingState, saveReadingState
};
