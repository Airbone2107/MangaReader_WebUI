/**
 * toast.js - Quản lý các chức năng hiển thị thông báo toast
 */

/**
 * Hiển thị thông báo toast
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại thông báo (success, danger, warning, info)
 * @param {number} duration - Thời gian hiển thị (ms)
 */
function showToast(message, type = 'primary', duration = 3000) {
    // Tạo toast container nếu chưa tồn tại
    if (!document.getElementById('toastContainer')) {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
    }
    
    // Tạo toast
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    // Thêm toast vào container
    document.getElementById('toastContainer').appendChild(toast);
    
    // Hiển thị toast
    const bsToast = new bootstrap.Toast(toast, {
        delay: duration
    });
    bsToast.show();
    
    // Tự động loại bỏ toast sau khi ẩn
    toast.addEventListener('hidden.bs.toast', function() {
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

export { showToast, initToasts }; 