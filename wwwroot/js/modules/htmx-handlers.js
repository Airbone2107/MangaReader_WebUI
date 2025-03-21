/**
 * htmx-handlers.js - Quản lý tất cả chức năng liên quan đến HTMX
 */

// Import các hàm từ các module khác (sẽ được sử dụng trong HTMX)
import { updateActiveSidebarLink } from './sidebar.js';
import { initTooltips } from './ui.js';

/**
 * Khởi tạo lại các chức năng cần thiết sau khi HTMX cập nhật nội dung
 */
function reinitializeAfterHtmxSwap() {
    // Cập nhật active sidebar link
    updateActiveSidebarLink();
    
    // Khởi tạo lại Bootstrap dropdowns
    document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(function(dropdownToggle) {
        try {
            // Nếu đã có dropdown, hủy bỏ nó trước khi tạo mới
            var oldDropdown = bootstrap.Dropdown.getInstance(dropdownToggle);
            if (oldDropdown) {
                oldDropdown.dispose();
            }
            // Tạo mới dropdown
            new bootstrap.Dropdown(dropdownToggle);
        } catch (e) {
            console.error('Lỗi khi khởi tạo lại dropdown:', e);
        }
    });
    
    // Khởi tạo lại tooltips
    initTooltips();
    
    // Khởi tạo lại sự kiện cho nút sidebar toggle
    const sidebarToggler = document.getElementById('sidebarToggler');
    if (sidebarToggler) {
        // Xóa bỏ tất cả event listener hiện tại
        const newSidebarToggler = sidebarToggler.cloneNode(true);
        sidebarToggler.parentNode.replaceChild(newSidebarToggler, sidebarToggler);
        
        // Thêm event listener mới
        newSidebarToggler.addEventListener('click', function(e) {
            e.preventDefault();
            document.body.classList.add('sidebar-open');
            localStorage.setItem('sidebarState', 'open');
            
            // Xử lý responsive
            if (window.innerWidth < 992) {
                document.body.classList.add('mobile-sidebar');
                const sidebarBackdrop = document.getElementById('sidebarBackdrop');
                if (sidebarBackdrop) sidebarBackdrop.style.display = 'block';
            } else {
                // Khi mở sidebar trên desktop, thêm hiệu ứng mở rộng cho thanh tìm kiếm ngay lập tức
                const searchContainer = document.querySelector('.search-container');
                if (searchContainer) {
                    searchContainer.classList.add('search-expanded');
                }
            }
        });
    }
    
    // Khởi tạo lại chức năng theme switcher
    const themeSwitch = document.getElementById('themeSwitch');
    if (themeSwitch) {
        // Cập nhật trạng thái switch dựa vào theme hiện tại
        const theme = document.documentElement.getAttribute('data-bs-theme');
        themeSwitch.checked = theme === 'dark';
        
        // Xóa bỏ tất cả event listener hiện tại
        const newThemeSwitch = themeSwitch.cloneNode(true);
        themeSwitch.parentNode.replaceChild(newThemeSwitch, themeSwitch);
        
        // Thêm event listener mới
        newThemeSwitch.addEventListener('change', function() {
            const theme = this.checked ? 'dark' : 'light';
            document.documentElement.setAttribute('data-bs-theme', theme);
            localStorage.setItem('theme', theme);
            
            // Cập nhật text
            const themeText = document.getElementById('themeText');
            if (themeText) {
                themeText.innerHTML = this.checked ? 
                    '<i class="bi bi-sun me-2"></i>Chế độ sáng' : 
                    '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
            }
            
            window.showToast(`Đã chuyển sang chế độ ${theme === 'dark' ? 'tối' : 'sáng'}!`, 'info');
        });
    }
}

/**
 * Khởi tạo các sự kiện xử lý HTMX
 */
function initHtmxHandlers() {
    // Hiển thị loading spinner khi bắt đầu request HTMX
    htmx.on('htmx:beforeRequest', function(event) {
        if (event.detail.target.id === 'main-content') {
            document.getElementById('content-loading-spinner').style.display = 'block';
        }
    });
    
    // Ẩn loading spinner khi request hoàn thành
    htmx.on('htmx:afterRequest', function() {
        document.getElementById('content-loading-spinner').style.display = 'none';
    });
    
    // Cập nhật và khởi tạo lại các chức năng sau khi nội dung được tải bằng HTMX
    htmx.on('htmx:afterSwap', function() {
        // Khởi tạo lại tất cả các chức năng cần thiết
        reinitializeAfterHtmxSwap();
    });
    
    // Xử lý lỗi HTMX
    htmx.on('htmx:responseError', function() {
        document.getElementById('content-loading-spinner').style.display = 'none';
        window.showToast('Đã xảy ra lỗi khi tải nội dung', 'danger');
    });
    
    // Bắt sự kiện popstate (khi người dùng nhấn nút back/forward trình duyệt)
    window.addEventListener('popstate', function() {
        updateActiveSidebarLink();
    });
}

export { reinitializeAfterHtmxSwap, initHtmxHandlers }; 