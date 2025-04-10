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
    console.log("Initializing generic HTMX handlers with loading state...");

    // Lưu trữ target element đang loading để xử lý lỗi
    let loadingTargetElement = null;

    // Trước khi gửi request
    htmx.on('htmx:beforeRequest', function (event) {
        // Xác định phần tử target
        const requestConfig = event.detail.requestConfig;
        let targetElement = null;

        // Ưu tiên lấy target từ requestConfig.target
        if (requestConfig.target) {
            // Kiểm tra xem requestConfig.target là chuỗi selector hay đối tượng DOM
            if (typeof requestConfig.target === 'string') {
                try {
                    // Cố gắng querySelector với chuỗi target
                    targetElement = document.querySelector(requestConfig.target);
                    if (!targetElement) {
                         console.warn(`[HTMX BeforeRequest] Target element not found for selector: ${requestConfig.target}`);
                    }
                } catch (e) {
                     // Nếu querySelector lỗi (selector không hợp lệ), ghi log và fallback
                     console.error(`[HTMX BeforeRequest] Invalid selector provided for target: '${requestConfig.target}'`, e);
                     targetElement = event.detail.elt; // Fallback về phần tử kích hoạt
                     console.log('[HTMX BeforeRequest] Fallback target to triggering element due to invalid selector:', targetElement);
                }
            } else if (requestConfig.target instanceof Element) {
                // Nếu requestConfig.target đã là một đối tượng DOM
                targetElement = requestConfig.target;
                console.log('[HTMX BeforeRequest] Target is already a DOM element:', targetElement);
            } else {
                 // Trường hợp target không phải chuỗi cũng không phải Element
                 console.warn('[HTMX BeforeRequest] requestConfig.target is neither a string nor an Element:', requestConfig.target);
                 targetElement = event.detail.elt; // Fallback về phần tử kích hoạt
                 console.log('[HTMX BeforeRequest] Fallback target to triggering element:', targetElement);
            }
        } else {
            // Nếu không có target rõ ràng, sử dụng phần tử kích hoạt
            targetElement = event.detail.elt;
            console.log('[HTMX BeforeRequest] No explicit target found, using triggering element:', targetElement);
        }

        // Chỉ thêm class loading nếu targetElement là một Element hợp lệ
        if (targetElement instanceof Element) {
            console.log('[HTMX BeforeRequest] Adding loading state to target:', targetElement);
            targetElement.classList.add('htmx-loading-target');
            loadingTargetElement = targetElement; // Lưu lại target đang load
        } else {
            console.warn('[HTMX BeforeRequest] Could not determine a valid target element for loading state. Target:', targetElement);
            loadingTargetElement = null;
        }

        // Xử lý spinner toàn cục (nếu vẫn muốn giữ lại cho main-content)
        if (targetElement && targetElement.id === 'main-content') {
             const mainSpinner = document.getElementById('content-loading-spinner');
             if (mainSpinner) mainSpinner.style.display = 'block';
        }
    });

     // Sau khi swap xong nội dung
     htmx.on('htmx:afterSwap', function (event) {
        const swappedElement = event.detail.target; // Phần tử đã được swap

        if (swappedElement && swappedElement instanceof Element) {
            console.log('[HTMX AfterSwap] Removing loading state from swapped target:', swappedElement);
            // Xóa class loading state khỏi phần tử MỚI được swap vào
            swappedElement.classList.remove('htmx-loading-target');
        } else {
             console.warn('[HTMX AfterSwap] Swapped target is not a valid Element:', swappedElement);
        }

        // Khởi tạo lại JS cho nội dung mới (Quan trọng - Giữ lại dòng này)
        reinitializeAfterHtmxSwap(swappedElement);

        loadingTargetElement = null; // Reset target đang load
    });

    // Sau khi request hoàn tất (thành công hoặc lỗi) - Dọn dẹp nếu swap không xảy ra
    htmx.on('htmx:afterRequest', function(event) {
        // Ẩn spinner toàn cục (nếu có)
        const mainSpinner = document.getElementById('content-loading-spinner');
        if (mainSpinner) mainSpinner.style.display = 'none';

        // Nếu request không thành công VÀ có target đang loading (swap chưa xảy ra)
        if (!event.detail.successful && loadingTargetElement && loadingTargetElement instanceof Element) {
            console.warn('[HTMX AfterRequest - Error] Request failed, removing loading state from:', loadingTargetElement);
            loadingTargetElement.classList.remove('htmx-loading-target');
        }
        // Nếu request thành công nhưng không có swap (ví dụ: hx-swap="none")
        else if (event.detail.successful && loadingTargetElement && loadingTargetElement instanceof Element && !event.detail.xhr.getResponseHeader('HX-Trigger')) {
             // Kiểm tra xem có swap xảy ra không (cách này có thể không hoàn hảo)
             // Cách tốt hơn là kiểm tra xem loadingTargetElement có còn class loading không
             if(loadingTargetElement.classList.contains('htmx-loading-target')) {
                 console.log('[HTMX AfterRequest] Request successful but no swap detected, removing loading state from:', loadingTargetElement);
                 loadingTargetElement.classList.remove('htmx-loading-target');
             }
        }

         // Reset target đang load nếu request kết thúc (dù thành công hay thất bại)
         // và target đó không còn class loading (đã được xử lý bởi afterSwap hoặc ở trên)
         if (loadingTargetElement && loadingTargetElement instanceof Element && !loadingTargetElement.classList.contains('htmx-loading-target')) {
             loadingTargetElement = null;
         }
    });

    // Xử lý lỗi response (trước khi swap)
    htmx.on('htmx:responseError', function(event) {
        console.error("HTMX response error:", event.detail.xhr);

        // Nếu có target đang loading, xóa trạng thái loading
        if (loadingTargetElement && loadingTargetElement instanceof Element) {
            console.warn('[HTMX ResponseError] Removing loading state due to response error from:', loadingTargetElement);
            loadingTargetElement.classList.remove('htmx-loading-target');
            loadingTargetElement = null; // Reset
        }

        // Hiển thị toast lỗi (nếu có)
        if (window.showToast) {
            window.showToast('Lỗi', 'Đã xảy ra lỗi khi tải nội dung. Vui lòng thử lại.', 'error');
        }
    });

     // Xử lý lỗi swap (sau khi nhận response nhưng trước khi swap)
     htmx.on('htmx:swapError', function(event) {
        console.error("HTMX swap error:", event.detail.error);

        // Nếu có target đang loading, xóa trạng thái loading
        if (loadingTargetElement && loadingTargetElement instanceof Element) {
            console.warn('[HTMX SwapError] Removing loading state due to swap error from:', loadingTargetElement);
            loadingTargetElement.classList.remove('htmx-loading-target');
            loadingTargetElement = null; // Reset
        }

        // Hiển thị toast lỗi (nếu có)
         if (window.showToast) {
             window.showToast('Lỗi', 'Đã xảy ra lỗi khi cập nhật giao diện.', 'error');
         }
     });


    // Bắt sự kiện popstate (nếu cần cập nhật UI khác)
    window.addEventListener('popstate', function() {
        updateActiveSidebarLink();
    });

    console.log("Generic HTMX Handlers Initialized.");
}

// Export các hàm cần thiết
export { reinitializeAfterHtmxSwap, initHtmxHandlers };