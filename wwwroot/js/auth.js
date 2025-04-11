/**
 * auth.js - Xử lý xác thực và quản lý thông tin người dùng (Module ES6)
 */

/**
 * Khởi tạo UI xác thực
 * Hàm này gọi checkAuthState để kiểm tra trạng thái đăng nhập khi được gọi
 */
export function initAuthUI() {
    console.log('Auth module: Khởi tạo UI xác thực');
    checkAuthState();
}

/**
 * Kiểm tra trạng thái đăng nhập và cập nhật giao diện
 */
function checkAuthState() {
    fetch('/Auth/GetCurrentUser')
        .then(response => response.json())
        .then(data => {
            updateUserInterface(data);
        })
        .catch(error => {
            console.error('Lỗi khi kiểm tra trạng thái đăng nhập:', error);
            // Mặc định hiển thị giao diện chưa đăng nhập
            updateUserInterface({ isAuthenticated: false });
        });
}

/**
 * Cập nhật giao diện dựa trên trạng thái đăng nhập
 * @param {Object} data - Dữ liệu người dùng từ API
 */
function updateUserInterface(data) {
    const guestUserMenu = document.getElementById('guestUserMenu');
    const authenticatedUserMenu = document.getElementById('authenticatedUserMenu');
    const userNameDisplay = document.getElementById('userNameDisplay');
    const userDropdownToggle = document.getElementById('userDropdownToggle');
    
    if (data.isAuthenticated && data.user) {
        // Người dùng đã đăng nhập
        if (guestUserMenu) guestUserMenu.classList.add('d-none');
        if (authenticatedUserMenu) authenticatedUserMenu.classList.remove('d-none');
        
        // Hiển thị tên người dùng
        if (userNameDisplay) {
            userNameDisplay.textContent = data.user.displayName;
            userNameDisplay.classList.remove('d-none');
        }
        
        // Hiển thị icon người dùng 
        if (userDropdownToggle) {
            const icon = userDropdownToggle.querySelector('.user-icon');
            if (icon) icon.style.display = '';
        }
        
    } else {
        // Người dùng chưa đăng nhập
        if (guestUserMenu) guestUserMenu.classList.remove('d-none');
        if (authenticatedUserMenu) authenticatedUserMenu.classList.add('d-none');
        
        // Ẩn tên người dùng
        if (userNameDisplay) userNameDisplay.classList.add('d-none');
        
        // Đảm bảo hiển thị icon mặc định
        if (userDropdownToggle) {
            const icon = userDropdownToggle.querySelector('.user-icon');
            if (icon) icon.style.display = '';
        }
    }
}