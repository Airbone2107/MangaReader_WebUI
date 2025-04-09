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
    const userDropdown = document.getElementById('userDropdown');
    
    if (data.isAuthenticated && data.user) {
        // Người dùng đã đăng nhập
        if (guestUserMenu) guestUserMenu.classList.add('d-none');
        if (authenticatedUserMenu) authenticatedUserMenu.classList.remove('d-none');
        
        // Hiển thị tên người dùng
        if (userNameDisplay) {
            userNameDisplay.textContent = data.user.displayName;
            userNameDisplay.classList.remove('d-none');
        }
        
        // Thay đổi biểu tượng nếu có ảnh đại diện
        if (data.user.photoUrl && userDropdown) {
            // Thay thế biểu tượng người dùng bằng ảnh đại diện
            const avatar = document.createElement('img');
            avatar.src = data.user.photoUrl;
            avatar.alt = data.user.displayName;
            avatar.className = 'rounded-circle user-avatar';
            avatar.style.width = '24px';
            avatar.style.height = '24px';
            
            // Xóa biểu tượng cũ
            const icon = userDropdown.querySelector('i.bi-person-circle');
            if (icon) {
                userDropdown.replaceChild(avatar, icon);
            }
        }
        
        // Cập nhật các phần khác của trang nếu cần
        // ...
        
    } else {
        // Người dùng chưa đăng nhập
        if (guestUserMenu) guestUserMenu.classList.remove('d-none');
        if (authenticatedUserMenu) authenticatedUserMenu.classList.add('d-none');
        
        // Ẩn tên người dùng
        if (userNameDisplay) userNameDisplay.classList.add('d-none');
    }
}

// Thêm style cho avatar người dùng
function addAvatarStyle() {
    const style = document.createElement('style');
    style.textContent = `
        .user-avatar {
            object-fit: cover;
            border: 2px solid #fff;
        }
    `;
    document.head.appendChild(style);
}

// Gọi hàm thêm style trong quá trình khởi tạo
addAvatarStyle(); 