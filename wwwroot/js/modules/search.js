/**
 * search.js - Quản lý các chức năng liên quan đến tìm kiếm
 */

/**
 * Khởi tạo chức năng tìm kiếm nhanh
 */
function initQuickSearch() {
    const searchForm = document.getElementById('quickSearchForm');
    const searchInput = document.getElementById('quickSearchInput');
    
    if (!searchForm || !searchInput) return;
    
    // Xử lý sự kiện submit form
    searchForm.addEventListener('submit', function(event) {
        const searchTerm = searchInput.value.trim();
        if (!searchTerm) {
            event.preventDefault(); // Ngăn submit nếu không có từ khóa
        }
    });
    
    // Tự động focus vào ô tìm kiếm khi nhấn Ctrl+K hoặc /
    document.addEventListener('keydown', function(event) {
        // Nếu người dùng nhấn Ctrl+K hoặc / khi không đang focus vào ô input
        if ((event.ctrlKey && event.key === 'k') || (event.key === '/' && document.activeElement.tagName !== 'INPUT')) {
            event.preventDefault();
            searchInput.focus();
        }
    });
}

/**
 * Manga Search Module
 * Xử lý tất cả các tính năng tìm kiếm và bộ lọc manga
 */

// Đảm bảo code chỉ chạy khi DOM đã sẵn sàng
document.addEventListener('DOMContentLoaded', initSearchPage);

// Khởi tạo script khi HTMX tải lại nội dung
document.addEventListener('htmx:afterSwap', function(event) {
    // Chỉ khởi tạo lại khi nội dung chứa form tìm kiếm
    if (event.detail.target.id === 'main-content' && document.getElementById('searchForm')) {
        initSearchPage();
        
        // Đảm bảo CSS được tải
        if (!document.querySelector('link[href*="search.css"]')) {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = '/css/pages/search.css';
            document.head.appendChild(link);
        }
        
        // Đồng bộ theme cho nội dung mới
        syncThemeForSearchCard();
    }
});

/**
 * Hàm khởi tạo module
 */
function init() {
    // Khởi tạo tìm kiếm nhanh (toàn cục)
    initQuickSearch();
    
    // Kiểm tra xem có đang ở trang tìm kiếm nâng cao không
    const searchForm = document.getElementById('searchForm');
    if (!searchForm) {
        return;
    }
    
    // Khởi tạo bộ lọc nâng cao
    initAdvancedFilter();
    
    // Khởi tạo các filter dropdowns
    initFilterDropdowns();
    
    // Xử lý nút reset filter
    setupResetFilters();
    
    // Đồng bộ theme cho search card
    syncThemeForSearchCard();
}

/**
 * Khởi tạo bộ lọc nâng cao
 */
function initAdvancedFilter() {
    const filterToggle = document.getElementById('filterToggle');
    const filterContainer = document.getElementById('filterContainer');
    
    if (!filterToggle || !filterContainer) {
        console.warn('Filter toggle or container not found!');
        return;
    }
    
    // Kiểm tra trạng thái của mỗi dropdown để xem có nên hiển thị bộ lọc ngay từ đầu không
    const hasActiveFilters = checkForActiveFilters();
    
    // Xóa event listener cũ (nếu có) để tránh duplicate
    const newFilterToggle = filterToggle.cloneNode(true);
    filterToggle.parentNode.replaceChild(newFilterToggle, filterToggle);
    
    // Cập nhật trạng thái hiển thị dựa trên bộ lọc hoạt động
    if (hasActiveFilters) {
        filterContainer.style.display = 'block';
        newFilterToggle.classList.add('active');
        } else {
        filterContainer.style.display = 'none';
        newFilterToggle.classList.remove('active');
    }
    
    // Thêm event listener mới
    newFilterToggle.addEventListener('click', function() {
        const isVisible = filterContainer.style.display === 'block';
        
        if (isVisible) {
            filterContainer.style.display = 'none';
            newFilterToggle.classList.remove('active');
        } else {
            filterContainer.style.display = 'block';
            newFilterToggle.classList.add('active');
        }
    });
}

/**
 * Kiểm tra xem có bộ lọc nào đang được áp dụng hay không
 */
function checkForActiveFilters() {
    // Kiểm tra các trường tìm kiếm
    const authorField = document.querySelector('input[name="authorOrArtist"]');
    if (authorField && authorField.value.trim()) return true;
    
    const yearField = document.querySelector('input[name="year"]');
    if (yearField && yearField.value.trim()) return true;
    
    // Kiểm tra các radio không mặc định
    const statusRadios = document.querySelectorAll('input[name="status"]:checked');
    if (statusRadios.length && statusRadios[0].id !== 'statusAll') return true;
    
    const demoRadios = document.querySelectorAll('input[name="publicationDemographic"]:checked');
    if (demoRadios.length && demoRadios[0].id !== 'demoAll') return true;
    
    // Kiểm tra ngôn ngữ được chọn
    const langChecks = document.querySelectorAll('input[name="availableTranslatedLanguage"]:checked');
    if (langChecks.length > 1 || (langChecks.length === 1 && langChecks[0].id !== 'langVi')) return true;
    
    // Kiểm tra thẻ được chọn
    const selectedTags = document.getElementById('selectedTags');
    if (selectedTags && selectedTags.value) return true;
    
    // Kiểm tra sắp xếp không mặc định
    const sortBy = document.querySelector('input[name="sortBy"]:checked');
    if (sortBy && sortBy.value !== 'latest') return true;
    
    // Kiểm tra kích thước trang không mặc định
    const pageSize = document.querySelector('input[name="pageSize"]:checked');
    if (pageSize && pageSize.value !== '24') return true;
    
    return false;
}

/**
 * Khởi tạo các filter dropdowns
 */
function initFilterDropdowns() {
    // Cập nhật text cho các dropdown khi chọn
    document.querySelectorAll('.filter-dropdown').forEach(dropdown => {
        const toggle = dropdown.querySelector('.dropdown-toggle');
        const menu = dropdown.querySelector('.dropdown-menu');
        const checkboxes = dropdown.querySelectorAll('input[type="checkbox"]');
        const radios = dropdown.querySelectorAll('input[type="radio"]');
        const selectedText = toggle.querySelector('.selected-text');
        
        if (!toggle || !menu || !selectedText) return;
        
        // Xử lý sự kiện khi chọn checkbox
        checkboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function() {
                updateDropdownText(dropdown);
            });
        });
        
        // Xử lý sự kiện khi chọn radio
        radios.forEach(radio => {
            radio.addEventListener('change', function() {
                updateDropdownText(dropdown);
                // Đóng dropdown sau khi chọn radio
                if (window.bootstrap && window.bootstrap.Dropdown) {
                    const dropdownInstance = bootstrap.Dropdown.getInstance(toggle);
                    if (dropdownInstance) {
                        dropdownInstance.hide();
                    }
                }
            });
        });
        
        // Cập nhật text ban đầu
        updateDropdownText(dropdown);
    });
}

/**
 * Cập nhật text hiển thị cho dropdown
 */
function updateDropdownText(dropdown) {
    const toggle = dropdown.querySelector('.dropdown-toggle');
    const selectedText = toggle.querySelector('.selected-text');
    const checkboxes = dropdown.querySelectorAll('input[type="checkbox"]:checked');
    const radios = dropdown.querySelectorAll('input[type="radio"]:checked');
    
    if (!selectedText) return;
    
    // Xử lý hiển thị cho các checkbox
    if (checkboxes.length > 0) {
        if (checkboxes.length <= 2) {
            const labels = Array.from(checkboxes).map(cb => {
                const label = document.querySelector(`label[for="${cb.id}"]`);
                return label ? label.textContent.trim() : '';
            }).filter(Boolean);
            
            selectedText.textContent = labels.join(', ');
        } else {
            selectedText.textContent = `${checkboxes.length} đã chọn`;
        }
    } 
    // Xử lý hiển thị cho các radio
    else if (radios.length > 0) {
        const label = document.querySelector(`label[for="${radios[0].id}"]`);
        selectedText.textContent = label ? label.textContent.trim() : radios[0].value;
    }
    // Trường hợp không có gì được chọn
    else {
        selectedText.textContent = 'Chọn...';
    }
}

/**
 * Đồng bộ theme giữa các phần tử trong search card
 */
function syncThemeForSearchCard() {
    // Áp dụng class bg-body cho các phần tử cần thiết
    const isDarkMode = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    
    // Cập nhật màu nền cho dropdown menu
    document.querySelectorAll('.dropdown-menu').forEach(menu => {
        if (isDarkMode) {
            menu.classList.add('bg-dark');
            menu.classList.remove('bg-white');
        } else {
            menu.classList.add('bg-white');
            menu.classList.remove('bg-dark');
        }
    });
    
    // Cập nhật màu nền cho input group text
    document.querySelectorAll('.input-group-text:not(.bg-primary)').forEach(item => {
        if (isDarkMode) {
            item.classList.add('bg-dark');
            item.classList.remove('bg-light');
        } else {
            item.classList.add('bg-light');
            item.classList.remove('bg-dark');
        }
    });
}

/**
 * Khởi tạo trang tìm kiếm
 */
function initSearchPage() {
    init();
}

/**
 * Thiết lập nút xóa bộ lọc
 */
function setupResetFilters() {
    const resetButton = document.getElementById('resetFilters');
    if (!resetButton) return;
    
    resetButton.addEventListener('click', function(e) {
        e.preventDefault();
        
        // Reset các input text
        document.querySelectorAll('input[type="text"], input[type="number"]').forEach(input => {
            if (input.name !== 'title') {  // Giữ lại tiêu đề nếu có
                input.value = '';
            }
        });
        
        // Reset các radio button về mặc định
        document.querySelectorAll('.filter-dropdown').forEach(dropdown => {
            const radios = dropdown.querySelectorAll('input[type="radio"]');
            if (radios.length > 0) {
                // Chọn radio đầu tiên (thường là "Tất cả")
                radios[0].checked = true;
            }
        });
        
        // Reset các checkbox về mặc định
        document.querySelectorAll('input[name="contentRating"]').forEach(cb => {
            cb.checked = true;
        });
        
        // Reset language checkbox
        document.querySelectorAll('input[name="availableTranslatedLanguage"]').forEach(cb => {
            // Chỉ giữ ngôn ngữ Việt mặc định
            cb.checked = cb.id === 'langVi';
        });
        
        // Reset included tags
        const selectedTagsInput = document.getElementById('selectedTags');
        if (selectedTagsInput) {
            selectedTagsInput.value = '';
        }
        
        // Reset excluded tags
        const excludedTagsInput = document.getElementById('excludedTags');
        if (excludedTagsInput) {
            excludedTagsInput.value = '';
        }
        
        // Clear tag display
        const selectedTagsDisplay = document.getElementById('selectedTagsDisplay');
        if (selectedTagsDisplay) {
            selectedTagsDisplay.innerHTML = '<span class="manga-tags-empty" id="emptyTagsMessage">Chưa có thẻ nào được chọn. Bấm để chọn thẻ.</span>';
        }
        
        // Reset tags mode to AND for includedTagsMode
        const includedTagsModeInput = document.getElementById('includedTagsMode');
        const includedTagsModeBox = document.getElementById('includedTagsModeBox');
        const includedTagsModeText = document.getElementById('includedTagsModeText');
        
        if (includedTagsModeInput && includedTagsModeBox && includedTagsModeText) {
            includedTagsModeInput.value = 'AND';
            includedTagsModeText.textContent = 'VÀ';
            includedTagsModeBox.classList.remove('tag-mode-or');
            includedTagsModeBox.classList.add('tag-mode-and');
        }
        
        // Reset tags mode to OR for excludedTagsMode (default)
        const excludedTagsModeInput = document.getElementById('excludedTagsMode');
        const excludedTagsModeBox = document.getElementById('excludedTagsModeBox');
        const excludedTagsModeText = document.getElementById('excludedTagsModeText');
        
        if (excludedTagsModeInput && excludedTagsModeBox && excludedTagsModeText) {
            excludedTagsModeInput.value = 'OR';
            excludedTagsModeText.textContent = 'HOẶC';
            excludedTagsModeBox.classList.remove('tag-mode-and');
            excludedTagsModeBox.classList.add('tag-mode-or');
        }
        
        // Cập nhật hiển thị của dropdown
        document.querySelectorAll('.filter-dropdown').forEach(dropdown => {
            updateDropdownText(dropdown);
        });
        
        // Submit form sau khi reset
        document.getElementById('searchForm').submit();
    });
}

// Xuất API module
export default {
    init,
    initSearchPage
};