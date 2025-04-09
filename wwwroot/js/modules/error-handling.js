/**
 * error-handling.js - Quản lý các chức năng xử lý lỗi
 */

/**
 * Khởi tạo chức năng xử lý lỗi
 */
function initErrorHandling() {
    // Kiểm tra và hiển thị thông báo lỗi từ API
    const errorContainer = document.querySelector('.api-error-container');
    if (errorContainer) {
        const retryButton = errorContainer.querySelector('.retry-button');
        if (retryButton) {
            retryButton.addEventListener('click', function() {
                window.location.reload();
            });
        }
    }

    // Xử lý lỗi khi tải hình ảnh
    document.querySelectorAll('img').forEach(img => {
        img.addEventListener('error', function() {
            this.src = '/images/cover-placeholder.jpg';
        });
    });

    // Thêm event listener cho nút reconnect API
    const reconnectButtons = document.querySelectorAll('.reconnect-api');
    reconnectButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            window.showToast('Thông báo', 'Đang kết nối lại...', 'info');
            
            // Gửi request kiểm tra API
            fetch('/Home/ApiTest')
                .then(response => {
                    if (response.ok) {
                        window.showToast('Thành công', 'Kết nối thành công!', 'success');
                        setTimeout(() => window.location.reload(), 1000);
                    } else {
                        window.showToast('Lỗi', 'Không thể kết nối đến API', 'error');
                    }
                })
                .catch(() => {
                    window.showToast('Lỗi', 'Không thể kết nối đến API', 'error');
                });
        });
    });
}

export { initErrorHandling }; 