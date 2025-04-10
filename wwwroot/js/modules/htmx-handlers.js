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
    // Cập nhật active sidebar link - luôn thực hiện khi có swap
    // Đây là một chức năng toàn cục, cần thực hiện bất kể phần tử nào bị swap
    updateActiveSidebarLink();
    
    // Xử lý khi main-content được swap (toàn bộ trang)
    if (targetElement.id === 'main-content') {
        // Khi toàn bộ trang được swap, chúng ta cần khởi tạo lại tất cả
        if (typeof SearchModule.initSearchPage === 'function') {
            SearchModule.initSearchPage();
        }
        initTooltips();
    }
    // Xử lý khi search-results-and-pagination được swap (kết quả và phân trang)
    else if (targetElement.id === 'search-results-and-pagination') {
        // Gọi lại các hàm init cần thiết cho nội dung mới
        if (typeof SearchModule.initPageGoTo === 'function') {
            SearchModule.initPageGoTo(); // Khởi tạo lại nút "..." nếu có
        }
        
        initTooltips(); // Khởi tạo lại tooltip nếu có
    }
    // Xử lý khi search-results-container được swap (chuyển đổi chế độ xem)
    else if (targetElement.id === 'search-results-container') {
        // Không cần gọi applySavedViewMode() vì nội dung mới đã được render với class đúng từ server
    }
    
    // Điều chỉnh kích thước chữ cho tiêu đề manga trong phần tử đã swap
    if (targetElement.querySelector('.details-manga-title')) {
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
        initTooltips();
    }
    
    // Khởi tạo lại các button Bootstrap như accordion, tabs trong targetElement
    if (targetElement.querySelector('[data-bs-toggle="collapse"], [data-bs-toggle="tab"]')) {
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
        // initMangaDetailsPage() sẽ khởi tạo tất cả chức năng cần thiết cho trang chi tiết
        // bao gồm: điều chỉnh header, khởi tạo dropdown chapter, nút theo dõi, v.v.
        initMangaDetailsPage();
    }
    
    // Khởi tạo lại trang tìm kiếm
    // Nhận diện trang tìm kiếm qua selector '#searchForm'
    if (targetElement.querySelector('#searchForm')) {
        // Khởi tạo trang tìm kiếm và bộ lọc tags
        SearchModule.initSearchPage();
        initTagsInSearchForm();
    }
    
    // Khởi tạo lại phân trang (pagination) nếu có
    // Độc lập với trang, pagination có thể xuất hiện ở nhiều trang khác nhau
    if (targetElement.querySelector('.pagination')) {
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
    const searchResultTargetSelector = "#search-results-and-pagination";

    // --- Xóa các listener cũ liên quan đến ẩn/hiện search results ---
    // (Đã thực hiện ở bước trên)

    // --- Thêm listener mới ---

    // Trước khi gửi request cho khu vực tìm kiếm
    htmx.on('htmx:beforeRequest', function (event) {
        // Kiểm tra xem target của request có phải là khu vực kết quả tìm kiếm không
        const requestTargetElement = event.detail.target;
        const targetAttribute = event.detail.requestConfig.target; // Lấy selector từ hx-target

        // Kiểm tra xem target của request có phải là searchResultTargetSelector không
        // Hoặc phần tử kích hoạt request nằm trong searchResultTargetSelector (ví dụ: nút pagination)
        const isSearchTarget = (targetAttribute && targetAttribute === searchResultTargetSelector) ||
                               (requestTargetElement && requestTargetElement.closest(searchResultTargetSelector));

        if (isSearchTarget) {
            const el = document.querySelector(searchResultTargetSelector);
            if (el) {
                el.classList.add('htmx-request-hide'); // Ẩn khu vực target
            }
        }
    });

    // Sau khi swap xong nội dung cho khu vực tìm kiếm
    htmx.on('htmx:afterSwap', function (event) {
        const swappedElement = event.detail.target; // Phần tử đã được swap

        // Kiểm tra xem phần tử được swap có phải là khu vực kết quả tìm kiếm không
        if (swappedElement && swappedElement.id === searchResultTargetSelector.substring(1)) { // Bỏ dấu #
            // Xóa class ẩn ngay lập tức để hiển thị nội dung mới
            swappedElement.classList.remove('htmx-request-hide');
        }

        // Khởi tạo lại JS cho nội dung mới (Quan trọng - Giữ lại dòng này)
        reinitializeAfterHtmxSwap(swappedElement);
    });

    // Listener để xử lý request hoàn tất (thành công hoặc lỗi)
    htmx.on('htmx:afterRequest', function(event) {
        // Nếu request thất bại, đảm bảo khu vực target không bị ẩn vĩnh viễn
        if (!event.detail.successful) {
            const targetAttribute = event.detail.requestConfig.target;
            const requestTargetElement = event.detail.elt; // Phần tử kích hoạt request

            const isSearchTarget = (targetAttribute && targetAttribute === searchResultTargetSelector) ||
                                   (requestTargetElement && requestTargetElement.closest(searchResultTargetSelector));

            if (isSearchTarget) {
                 const el = document.querySelector(searchResultTargetSelector);
                 if (el) {
                     el.classList.remove('htmx-request-hide');
                 }
            }
        }
    });

    // Xử lý lỗi HTMX (đảm bảo target không bị ẩn)
    htmx.on('htmx:responseError', function(event) {
        console.error("HTMX response error:", event.detail.xhr);

        // Đảm bảo khu vực target không bị ẩn vĩnh viễn khi có lỗi
        const targetAttribute = event.detail.requestConfig.target;
        const requestTargetElement = event.detail.elt; // Phần tử kích hoạt request

        const isSearchTarget = (targetAttribute && targetAttribute === searchResultTargetSelector) ||
                               (requestTargetElement && requestTargetElement.closest(searchResultTargetSelector));

        if (isSearchTarget) {
            const el = document.querySelector(searchResultTargetSelector);
            if (el) {
                el.classList.remove('htmx-request-hide');
            }
        }

        // Hiển thị toast lỗi (nếu có)
        if (window.showToast) {
            window.showToast('Lỗi', 'Đã xảy ra lỗi khi tải nội dung. Vui lòng thử lại.', 'error');
        }
    });

    // Bắt sự kiện popstate (nếu cần cập nhật UI khác)
    window.addEventListener('popstate', function() {
        updateActiveSidebarLink();
    });

    console.log("HTMX Handlers Initialized with simple hide/show for search results.");
}

// Export các hàm cần thiết
export { reinitializeAfterHtmxSwap, initHtmxHandlers };