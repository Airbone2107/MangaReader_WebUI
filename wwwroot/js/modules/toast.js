/**
 * toast.js - Quản lý các chức năng hiển thị thông báo toast
 */

/**
 * Hiển thị thông báo toast
 * @param {string} title - Tiêu đề thông báo
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại thông báo (success, error, warning, info)
 * @param {number} duration - Thời gian hiển thị (ms)
 */
function showToast(title, message, type = 'info', duration = 3000) {
    // Kiểm tra nếu Bootstrap đã được khởi tạo
    if (typeof bootstrap === 'undefined') {
        console.error('Bootstrap chưa được khởi tạo');
        alert(message);
        return;
    }
    
    // Tạo toast container nếu chưa tồn tại
    let toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Xác định màu sắc dựa trên loại thông báo
    const headerClass = type === 'success' ? 'bg-success' : 
                       type === 'error' ? 'bg-danger' : 
                       type === 'warning' ? 'bg-warning' : 'bg-info';
                       
    // Xác định icon theo loại thông báo
    const iconClass = type === 'success' ? 'bi-check-circle' : 
                     type === 'error' ? 'bi-exclamation-triangle' : 
                     type === 'warning' ? 'bi-exclamation-circle' : 'bi-info-circle';
    
    // Xác định nếu cần sử dụng btn-close-white (cho header tối màu)
    const closeButtonClass = type === 'warning' ? 'btn-close' : 'btn-close btn-close-white';
    
    // Tạo phần tử toast
    const toastId = 'toast_' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header ${headerClass}">
                <i class="bi ${iconClass} me-2"></i>
                <strong class="me-auto">${title}</strong>
                <button type="button" class="${closeButtonClass}" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    // Thêm toast vào container
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    // Lấy phần tử toast
    const toastElement = document.getElementById(toastId);
    
    // Tạo đối tượng Toast của Bootstrap
    const toast = new bootstrap.Toast(toastElement, { delay: duration });
    
    // Hiển thị toast
    toast.show();
    
    // Xóa toast sau khi ẩn
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

/**
 * Khởi tạo chức năng hiển thị thông báo
 */
function initToasts() {
    // Tạo hàm global để các trang có thể sử dụng
    window.showToast = showToast;
    console.log('Toast module initialized: window.showToast is now available');
}

export { showToast, initToasts }; 