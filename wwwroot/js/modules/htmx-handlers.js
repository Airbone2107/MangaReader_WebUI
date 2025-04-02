/**
 * htmx-handlers.js - Quản lý tất cả chức năng liên quan đến HTMX
 */

// Import các hàm từ các module khác (sẽ được sử dụng trong HTMX)
import { updateActiveSidebarLink } from './sidebar.js';
import { initTooltips } from './ui.js';
import { adjustHeaderBackgroundHeight, initMangaDetailsPage } from './manga-details.js';

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
    
    // Khởi tạo lại trang chi tiết manga nếu đang ở trang đó
    initMangaDetailsPage();
    
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
    
    // Khởi tạo lại sự kiện cho nút lọc ngôn ngữ trong Details.cshtml
    const languageFilters = document.querySelectorAll('input[name="language"]');
    if (languageFilters.length > 0) {
        languageFilters.forEach(filter => {
            filter.addEventListener('change', function() {
                const lang = this.value;
                const chapterItems = document.querySelectorAll('.chapter-lang-item');
                
                if (lang === 'all') {
                    chapterItems.forEach(item => item.style.display = 'block');
                } else {
                    chapterItems.forEach(item => {
                        if (item.getAttribute('data-language') === lang) {
                            item.style.display = 'block';
                        } else {
                            item.style.display = 'none';
                        }
                    });
                }
            });
        });
        
        // Kích hoạt lọc mặc định nếu có
        const activeFilter = document.querySelector('input[name="language"]:checked');
        if (activeFilter) {
            activeFilter.dispatchEvent(new Event('change'));
        }
    }
    
    // Khởi tạo lại sự kiện cho nút theo dõi trong Details.cshtml
    const followBtn = document.getElementById('followBtn');
    if (followBtn) {
        followBtn.addEventListener('click', function() {
            let isFollowing = this.getAttribute('data-following') === 'true';
            isFollowing = !isFollowing;
            this.setAttribute('data-following', isFollowing.toString());
            
            // Cập nhật trạng thái và lưu vào localStorage
            const mangaId = this.getAttribute('data-id');
            let followedList = JSON.parse(localStorage.getItem('followed_manga') || '[]');
            
            if (isFollowing) {
                if (!followedList.includes(mangaId)) {
                    followedList.push(mangaId);
                }
            } else {
                const index = followedList.indexOf(mangaId);
                if (index !== -1) {
                    followedList.splice(index, 1);
                }
            }
            
            localStorage.setItem('followed_manga', JSON.stringify(followedList));
            
            // Cập nhật UI
            if (isFollowing) {
                this.innerHTML = '<i class="bi bi-bookmark-check-fill me-2"></i>Đang theo dõi';
                window.showToast('Đã thêm vào danh sách theo dõi!', 'success');
            } else {
                this.innerHTML = '<i class="bi bi-bookmark-plus me-2"></i>Theo dõi';
                window.showToast('Đã xóa khỏi danh sách theo dõi!', 'info');
            }
        });
    }
    
    // Khởi tạo lại các button Bootstrap như accordion, tabs, v.v.
    document.querySelectorAll('[data-bs-toggle]').forEach(function(element) {
        const toggleType = element.getAttribute('data-bs-toggle');
        
        if (toggleType === 'collapse' || toggleType === 'tab') {
            try {
                // Kiểm tra nếu đã có instance
                const instance = bootstrap[toggleType.charAt(0).toUpperCase() + toggleType.slice(1)].getInstance(element);
                if (instance) {
                    instance.dispose();
                }
                // Tạo instance mới
                new bootstrap[toggleType.charAt(0).toUpperCase() + toggleType.slice(1)](element);
            } catch (e) {
                console.error(`Lỗi khi khởi tạo lại ${toggleType}:`, e);
            }
        }
    });
    
    // Khởi tạo lại nút reset bộ lọc trong Search.cshtml
    const resetFiltersBtn = document.getElementById('resetFilters');
    if (resetFiltersBtn) {
        resetFiltersBtn.addEventListener('click', function() {
            document.querySelectorAll('input[name="status"]').forEach(input => {
                input.checked = input.id === 'statusAll';
            });
            
            document.querySelectorAll('input[name="genres"]').forEach(input => {
                input.checked = false;
            });
            
            const sortBySelect = document.querySelector('select[name="sortBy"]');
            if (sortBySelect) {
                sortBySelect.value = 'latest';
            }
            
            // Submit form
            document.getElementById('searchForm').submit();
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
        
        // Kiểm tra sự tồn tại của manga-header-background một lần duy nhất
        // và chỉ gọi hàm adjustHeaderBackgroundHeight nếu nó tồn tại
        if (document.querySelector('.details-manga-header-background')) {
            // Đợi một chút để đảm bảo DOM đã được render đầy đủ trước khi điều chỉnh
            setTimeout(adjustHeaderBackgroundHeight, 100);
        }
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