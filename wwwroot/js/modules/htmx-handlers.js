/**
 * htmx-handlers.js - Quản lý tất cả chức năng liên quan đến HTMX
 * 
 * File này đóng vai trò quan trọng trong việc đảm bảo các chức năng JavaScript vẫn hoạt động 
 * sau khi HTMX thay đổi nội dung (swap). Nó chịu trách nhiệm khởi tạo lại các chức năng JavaScript
 * cần thiết cho nội dung mới mà không cần load lại toàn bộ trang.
 * 
 * Các nguyên tắc chính:
 * 1. Chỉ khởi tạo lại những gì cần thiết dựa trên nội dung đã được swap
 * 2. Dọn dẹp (dispose) các instance cũ trước khi tạo mới
 * 3. Sử dụng event delegation khi có thể
 */

// Import các hàm từ các module khác (sẽ được sử dụng trong HTMX)
import { updateActiveSidebarLink } from './sidebar.js';
import { initTooltips, adjustMangaTitles } from './ui.js';
import { adjustHeaderBackgroundHeight, initMangaDetailsPage } from './manga-details.js';
import { initTagsInSearchForm } from './manga-tags.js';
import SearchModule from './search.js';

/**
 * Khởi tạo lại các chức năng cần thiết sau khi HTMX cập nhật nội dung
 * 
 * Hàm này được gọi sau mỗi lần HTMX swap nội dung, với tham số targetElement là
 * phần tử đã được swap. Điều này cho phép chúng ta chỉ tập trung vào việc khởi tạo
 * lại các chức năng cần thiết cho phần tử đó thay vì toàn bộ trang.
 * 
 * @param {HTMLElement} targetElement - Phần tử được HTMX swap
 */
function reinitializeAfterHtmxSwap(targetElement) {
    console.log('Đang khởi tạo lại chức năng sau HTMX swap cho: ', targetElement.id);
    
    // Cập nhật active sidebar link - luôn thực hiện khi có swap
    // Đây là một chức năng toàn cục, cần thực hiện bất kể phần tử nào bị swap
    updateActiveSidebarLink();
    
    // Áp dụng lại chế độ xem (grid/list) nếu đang là search-results-container
    if (targetElement.id === 'search-results-container') {
        console.log('HTMX Handlers: Áp dụng lại chế độ xem đã lưu');
        if (typeof SearchModule.applySavedViewMode === 'function') {
            SearchModule.applySavedViewMode();
        }
    }
    
    // Điều chỉnh kích thước chữ cho tiêu đề manga trong phần tử đã swap
    if (targetElement.querySelector('.details-manga-title')) {
        console.log('Điều chỉnh kích thước chữ cho tiêu đề manga trong phần tử đã swap');
        adjustMangaTitles(targetElement);
    }
    
    // ---------- KHỞI TẠO GIAO DIỆN BOOTSTRAP ----------
    
    /**
     * Phần này tái khởi tạo các component Bootstrap (dropdown, tooltip, collapse, tab)
     * trong phần tử đã được swap. Quy trình cho mỗi loại component:
     * 1. Kiểm tra xem phần tử đã swap có chứa component cần khởi tạo không
     * 2. Nếu có, dọn dẹp (dispose) instance cũ trước
     * 3. Tạo instance mới
     */
    
    // Khởi tạo lại Bootstrap dropdowns nếu có trong targetElement
    if (targetElement.querySelector('[data-bs-toggle="dropdown"]')) {
        console.log('Khởi tạo lại dropdown cho targetElement');
        targetElement.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(function(dropdownToggle) {
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
    }
    
    // Khởi tạo lại tooltips nếu có trong targetElement
    // Lưu ý: initTooltips() là hàm được import từ ui.js
    if (targetElement.querySelector('[data-bs-toggle="tooltip"]')) {
        console.log('Khởi tạo lại tooltips');
        initTooltips();
    }
    
    // Khởi tạo lại các button Bootstrap như accordion, tabs trong targetElement
    if (targetElement.querySelector('[data-bs-toggle="collapse"], [data-bs-toggle="tab"]')) {
        console.log('Khởi tạo lại accordion và tabs');
        targetElement.querySelectorAll('[data-bs-toggle]').forEach(function(element) {
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
    }
    
    // ---------- KHỞI TẠO CÁC TRANG CỤ THỂ ----------
    
    /**
     * Phần này tái khởi tạo các chức năng của các trang cụ thể (chi tiết manga, tìm kiếm)
     * dựa trên selector đặc biệt trong phần tử đã swap.
     * Mỗi điều kiện kiểm tra một trang/chức năng cụ thể và chỉ khởi tạo khi cần.
     */
    
    // Khởi tạo lại trang chi tiết manga nếu đang ở trang đó
    // Nhận diện trang chi tiết manga qua selector '.details-manga-header-background'
    if (targetElement.querySelector('.details-manga-header-background') || targetElement.classList.contains('details-manga-details-container')) {
        console.log('HTMX Handlers: Khởi tạo lại tính năng trang chi tiết manga');
        // initMangaDetailsPage() sẽ khởi tạo tất cả chức năng cần thiết cho trang chi tiết
        // bao gồm: điều chỉnh header, khởi tạo dropdown chapter, nút theo dõi, v.v.
        initMangaDetailsPage();
    }
    
    // Khởi tạo lại trang tìm kiếm
    // Nhận diện trang tìm kiếm qua selector '#searchForm'
    if (targetElement.querySelector('#searchForm')) {
        console.log('HTMX Handlers: Khởi tạo lại tính năng trang tìm kiếm');
        // Khởi tạo trang tìm kiếm và bộ lọc tags
        SearchModule.initSearchPage();
        initTagsInSearchForm();
    }
    
    // Khởi tạo lại phân trang (pagination) nếu có
    // Độc lập với trang, pagination có thể xuất hiện ở nhiều trang khác nhau
    if (targetElement.querySelector('.pagination')) {
        console.log('HTMX Handlers: Khởi tạo lại tính năng phân trang');
        if (typeof SearchModule.initPageGoTo === 'function') {
            SearchModule.initPageGoTo();
        }
    }
    
    // ---------- KHỞI TẠO CÁC THÀNH PHẦN CHUNG ----------
    
    /**
     * Phần này tái khởi tạo các thành phần UI chung (sidebar toggle, theme switcher)
     * Kỹ thuật quan trọng: thay vì chỉ thêm event listener mới, chúng ta 
     * clone phần tử và thay thế phần tử cũ để tránh duplicate event listeners
     */
    
    // Khởi tạo lại sự kiện cho nút sidebar toggle
    const sidebarToggler = targetElement.querySelector('#sidebarToggler');
    if (sidebarToggler) {
        console.log('Khởi tạo lại sự kiện nút sidebar toggle');
        // Xóa bỏ tất cả event listener hiện tại bằng cách clone node
        // Đây là kỹ thuật an toàn để tránh duplicate event listeners
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
    const themeSwitch = targetElement.querySelector('#themeSwitch');
    if (themeSwitch) {
        console.log('Khởi tạo lại sự kiện theme switcher');
        // Cập nhật trạng thái switch dựa vào theme hiện tại
        const theme = document.documentElement.getAttribute('data-bs-theme');
        themeSwitch.checked = theme === 'dark';
        
        // Xóa bỏ tất cả event listener hiện tại bằng cách clone node
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
            
            window.showToast('Thông báo', `Đã chuyển sang chế độ ${theme === 'dark' ? 'tối' : 'sáng'}!`, 'info');
        });
    }
    
    // Khởi tạo lại nút reset bộ lọc trong Search.cshtml
    const resetFiltersBtn = targetElement.querySelector('#resetFilters');
    if (resetFiltersBtn) {
        console.log('Khởi tạo lại sự kiện nút reset bộ lọc');
        // Xóa bỏ tất cả event listener hiện tại
        const newResetFiltersBtn = resetFiltersBtn.cloneNode(true);
        resetFiltersBtn.parentNode.replaceChild(newResetFiltersBtn, resetFiltersBtn);
        
        // Thêm event listener mới
        newResetFiltersBtn.addEventListener('click', function() {
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
            
            // Reset tags nếu có
            const includedTagsInput = document.getElementById('includedTags');
            const excludedTagsInput = document.getElementById('excludedTags');
            if (includedTagsInput) includedTagsInput.value = '';
            if (excludedTagsInput) excludedTagsInput.value = '';
            
            // Reset selected tags display
            const selectedTagsDisplay = document.getElementById('selectedTagsDisplay');
            if (selectedTagsDisplay) {
                selectedTagsDisplay.innerHTML = '<span class="manga-tags-empty" id="emptyTagsMessage">Chưa có thẻ nào được chọn. Bấm để chọn thẻ.</span>';
            }
            
            // Reset tags mode
            const includedTagsModeInput = document.getElementById('includedTagsMode');
            const includedTagsModeBox = document.getElementById('includedTagsModeBox');
            const includedTagsModeText = document.getElementById('includedTagsModeText');
            
            if (includedTagsModeInput && includedTagsModeBox && includedTagsModeText) {
                includedTagsModeInput.value = 'AND';
                includedTagsModeText.textContent = 'VÀ';
                includedTagsModeBox.classList.remove('tag-mode-or');
                includedTagsModeBox.classList.add('tag-mode-and');
            }
            
            // Reset excluded tags mode
            const excludedTagsModeInput = document.getElementById('excludedTagsMode');
            const excludedTagsModeBox = document.getElementById('excludedTagsModeBox');
            const excludedTagsModeText = document.getElementById('excludedTagsModeText');
            
            if (excludedTagsModeInput && excludedTagsModeBox && excludedTagsModeText) {
                excludedTagsModeInput.value = 'OR';
                excludedTagsModeText.textContent = 'HOẶC';
                excludedTagsModeBox.classList.remove('tag-mode-and');
                excludedTagsModeBox.classList.add('tag-mode-or');
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
    htmx.on('htmx:afterSwap', function(event) {
        // Lấy phần tử target đã được swap
        const targetElement = event.detail.target;
        console.log('HTMX afterSwap triggered for target:', targetElement.id);
        
        // Khởi tạo lại tất cả các chức năng cần thiết với targetElement
        reinitializeAfterHtmxSwap(targetElement);
        
        // Kiểm tra sự tồn tại của manga-header-background một lần duy nhất
        // và chỉ gọi hàm adjustHeaderBackgroundHeight nếu nó tồn tại
        if (targetElement.querySelector('.details-manga-header-background')) {
            // Đợi một chút để đảm bảo DOM đã được render đầy đủ trước khi điều chỉnh
            setTimeout(function() {
                // adjustHeaderBackgroundHeight đã được gọi trong initMangaDetailsPage
            }, 100);
        }
    });
    
    // Xử lý lỗi HTMX
    htmx.on('htmx:responseError', function() {
        document.getElementById('content-loading-spinner').style.display = 'none';
        window.showToast('Lỗi', 'Đã xảy ra lỗi khi tải nội dung', 'error');
    });
    
    // Bắt sự kiện popstate (khi người dùng nhấn nút back/forward trình duyệt)
    window.addEventListener('popstate', function() {
        updateActiveSidebarLink();
    });
}

export { reinitializeAfterHtmxSwap, initHtmxHandlers };