/**
 * custom-dropdown.js - Module quản lý hoạt động của custom dropdown
 */

/**
 * Khởi tạo các custom dropdown trên trang
 * Hàm này tìm tất cả các dropdown và gắn các event listener cần thiết
 */
export function initCustomDropdowns() {
    console.log('Initializing custom dropdowns');
    
    // Lấy tất cả các dropdown trên trang
    const dropdowns = document.querySelectorAll('.custom-user-dropdown');
    
    // Nếu không có dropdown nào, kết thúc xử lý
    if (!dropdowns.length) {
        console.log('No custom dropdowns found');
        return;
    }
    
    // Đăng ký event listener để đóng dropdown khi click bên ngoài
    document.removeEventListener('click', closeDropdownsOnClickOutside);
    document.addEventListener('click', closeDropdownsOnClickOutside);
    
    // Xử lý từng dropdown riêng biệt
    dropdowns.forEach(dropdown => {
        // Tìm nút toggle và menu của dropdown này
        const toggleButton = dropdown.querySelector('.dropdown-toggle-btn');
        
        if (!toggleButton) return; // Bỏ qua nếu không tìm thấy nút toggle
        
        // Xóa bỏ listener cũ (tránh duplicate)
        toggleButton.removeEventListener('click', toggleDropdown);
        
        // Đăng ký listener mới
        toggleButton.addEventListener('click', toggleDropdown);
        
        console.log('Custom dropdown initialized:', dropdown.id || 'unnamed');
    });
}

/**
 * Mở/đóng dropdown khi click vào nút toggle
 * @param {Event} event - Sự kiện click
 */
function toggleDropdown(event) {
    event.stopPropagation(); // Ngăn event lan ra ngoài
    
    // Lấy dropdown container (phần tử cha của nút toggle)
    const dropdown = this.closest('.custom-user-dropdown');
    if (!dropdown) return;
    
    // Đóng tất cả các dropdown khác trước
    closeAllDropdowns(dropdown);
    
    // Toggle class 'show' trên dropdown hiện tại
    dropdown.classList.toggle('show');
    
    // Cập nhật aria-expanded
    this.setAttribute('aria-expanded', dropdown.classList.contains('show'));
    
    console.log(`Dropdown ${dropdown.id || 'unnamed'} toggled:`, dropdown.classList.contains('show'));
}

/**
 * Đóng tất cả các dropdown, trừ dropdown được chỉ định
 * @param {HTMLElement|null} excludeDropdown - Dropdown không đóng (nếu có)
 */
function closeAllDropdowns(excludeDropdown = null) {
    const dropdowns = document.querySelectorAll('.custom-user-dropdown.show');
    
    dropdowns.forEach(dropdown => {
        // Bỏ qua dropdown được chỉ định
        if (excludeDropdown && dropdown === excludeDropdown) return;
        
        // Đóng dropdown
        dropdown.classList.remove('show');
        
        // Cập nhật aria-expanded của nút toggle
        const toggleButton = dropdown.querySelector('.dropdown-toggle-btn');
        if (toggleButton) {
            toggleButton.setAttribute('aria-expanded', 'false');
        }
    });
}

/**
 * Đóng tất cả các dropdown khi click bên ngoài
 * @param {Event} event - Sự kiện click
 */
function closeDropdownsOnClickOutside(event) {
    // Kiểm tra xem click có phải bên ngoài dropdown không
    if (!event.target.closest('.custom-user-dropdown')) {
        closeAllDropdowns();
    }
}

// Export các hàm cần thiết
export default {
    initCustomDropdowns
};