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

// Lưu ý: Tất cả các sự kiện HTMX được xử lý trong htmx-handlers.js

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
    const authorField = document.querySelector('input[name="authors"]');
    if (authorField && authorField.value.trim()) return true;
    
    const artistField = document.querySelector('input[name="artists"]');
    if (artistField && artistField.value.trim()) return true;
    
    const yearField = document.querySelector('input[name="year"]');
    if (yearField && yearField.value.trim()) return true;
    
    // Kiểm tra các checkbox được chọn
    const statusChecks = document.querySelectorAll('input[name="status"]:checked');
    if (statusChecks.length) return true;
    
    const demoChecks = document.querySelectorAll('input[name="publicationDemographic"]:checked');
    if (demoChecks.length) return true;
    
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
    
    // Kiểm tra nội dung không mặc định
    const contentRating = document.querySelectorAll('input[name="contentRating"]:checked');
    const defaultRatings = ['safe', 'suggestive', 'erotica'];
    if (contentRating.length !== defaultRatings.length) return true;
    
    // Kiểm tra xem có phải tất cả các giá trị mặc định đều được chọn
    const checkedValues = Array.from(contentRating).map(c => c.value);
    for (const rating of defaultRatings) {
        if (!checkedValues.includes(rating)) return true;
    }
    
    return false;
}

/**
 * Khởi tạo các filter dropdowns
 */
function initFilterDropdowns() {
    // Cập nhật text cho các dropdown khi chọn
    document.querySelectorAll('.filter-dropdown').forEach(dropdown => {
        const toggle = dropdown.querySelector('.filter-toggle-btn');
        const menu = dropdown.querySelector('.filter-menu-content');
        const checkboxes = dropdown.querySelectorAll('input[type="checkbox"]');
        const radios = dropdown.querySelectorAll('input[type="radio"]');
        const selectedText = toggle.querySelector('.selected-text');
        
        if (!toggle || !menu || !selectedText) return;
        
        // Xóa event listeners cũ
        const newToggle = toggle.cloneNode(true);
        toggle.parentNode.replaceChild(newToggle, toggle);
        
        // Thêm event listener mới cho việc toggle dropdown
        newToggle.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            // Đóng tất cả các dropdowns khác
            document.querySelectorAll('.filter-dropdown.show').forEach(openDropdown => {
                if (openDropdown !== dropdown) {
                    openDropdown.classList.remove('show');
                    const openMenu = openDropdown.querySelector('.filter-menu-content');
                    if (openMenu) openMenu.style.display = 'none';
                }
            });
            
            // Toggle dropdown hiện tại
            const isVisible = dropdown.classList.contains('show');
            
            if (isVisible) {
                dropdown.classList.remove('show');
                menu.style.display = 'none';
            } else {
                dropdown.classList.add('show');
                menu.style.display = 'block';
            }
        });
        
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
                
                // Đóng dropdown sau khi chọn radio (vì chỉ chọn được một lựa chọn)
                dropdown.classList.remove('show');
                menu.style.display = 'none';
            });
        });
        
        // Thêm xử lý click cho toàn bộ filter-option
        dropdown.querySelectorAll('.filter-option').forEach(option => {
            // Xóa event listener cũ (nếu có)
            const newOption = option.cloneNode(true);
            option.parentNode.replaceChild(newOption, option);
            
            // Tìm input và label trong filter-option mới
            const input = newOption.querySelector('input[type="checkbox"], input[type="radio"]');
            const label = newOption.querySelector('.filter-option-label');
            
            if (input) {
                // Thêm event listener cho input để cập nhật text
                input.addEventListener('change', function() {
                    updateDropdownText(dropdown);
                    
                    // Đóng dropdown sau khi chọn radio
                    if (input.type === 'radio') {
                        dropdown.classList.remove('show');
                        menu.style.display = 'none';
                    }
                });
                
                // Thêm event listener cho label
                if (label) {
                    label.addEventListener('click', function(e) {
                        e.preventDefault();
                        e.stopPropagation();
                        
                        // Đảo trạng thái checkbox
                        if (input.type === 'checkbox') {
                            input.checked = !input.checked;
                            // Trigger sự kiện change
                            const event = new Event('change');
                            input.dispatchEvent(event);
                        }
                        // Chọn radio
                        else if (input.type === 'radio') {
                            input.checked = true;
                            // Trigger sự kiện change
                            const event = new Event('change');
                            input.dispatchEvent(event);
                        }
                    });
                }
            }
            
            // Thêm event listener cho toàn bộ filter-option
            newOption.addEventListener('click', function(e) {
                // Ngăn sự kiện bubble lên
                e.stopPropagation();
                
                // Nếu click vào input hoặc label, không làm gì thêm
                if (e.target === input || e.target === label || e.target.closest('label')) {
                    return;
                }
                
                if (input) {
                    // Đảo trạng thái checkbox
                    if (input.type === 'checkbox') {
                        input.checked = !input.checked;
                        // Trigger sự kiện change
                        const event = new Event('change');
                        input.dispatchEvent(event);
                    }
                    // Chọn radio
                    else if (input.type === 'radio') {
                        input.checked = true;
                        // Trigger sự kiện change
                        const event = new Event('change');
                        input.dispatchEvent(event);
                    }
                }
            });
        });
        
        // Cập nhật text ban đầu
        updateDropdownText(dropdown);
    });
    
    // Đóng dropdown khi click ra ngoài
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.filter-dropdown')) {
            document.querySelectorAll('.filter-dropdown.show').forEach(dropdown => {
                dropdown.classList.remove('show');
                const menu = dropdown.querySelector('.filter-menu-content');
                if (menu) menu.style.display = 'none';
            });
        }
    });
}

/**
 * Cập nhật text hiển thị cho dropdown
 */
function updateDropdownText(dropdown) {
    const toggle = dropdown.querySelector('.filter-toggle-btn');
    const selectedText = toggle.querySelector('.selected-text');
    const checkboxes = dropdown.querySelectorAll('input[type="checkbox"]:checked');
    const allCheckboxes = dropdown.querySelectorAll('input[type="checkbox"]');
    const radios = dropdown.querySelectorAll('input[type="radio"]:checked');
    
    if (!selectedText) return;
    
    // Kiểm tra một số trường hợp đặc biệt
    const isContentRating = dropdown.closest('.col-md-4')?.querySelector('.filter-dropdown-label')?.textContent.includes('Mức độ nội dung');
    
    // Xử lý đặc biệt cho Mức độ nội dung
    if (isContentRating) {
        const contentSafe = dropdown.querySelector('#contentSafe')?.checked;
        const contentSuggestive = dropdown.querySelector('#contentSuggestive')?.checked;
        const contentErotica = dropdown.querySelector('#contentErotica')?.checked;
        const contentPornographic = dropdown.querySelector('#contentPornographic')?.checked;
        
        // Trường hợp mặc định khi chọn safe, suggestive, erotica
        if (contentSafe && contentSuggestive && contentErotica && !contentPornographic) {
            selectedText.textContent = "An Toàn, Nhạy cảm, R18";
            return;
        }
        // Trường hợp chọn tất cả
        else if (contentSafe && contentSuggestive && contentErotica && contentPornographic) {
            selectedText.textContent = "Tất cả";
            return;
        }
        // Trường hợp không chọn gì
        else if (!contentSafe && !contentSuggestive && !contentErotica && !contentPornographic) {
            selectedText.textContent = "Tất cả";
            return;
        }
    }
    
    // Kiểm tra xem tất cả checkbox có được chọn hay không
    const allSelected = allCheckboxes.length > 0 && checkboxes.length === allCheckboxes.length;
    
    // Kiểm tra xem không có checkbox nào được chọn
    const noneSelected = allCheckboxes.length > 0 && checkboxes.length === 0;
    
    // Xử lý hiển thị cho các checkbox
    if (checkboxes.length > 0) {
        // Nếu tất cả đều được chọn, hiển thị "Tất cả"
        if (allSelected) {
            selectedText.textContent = "Tất cả";
            return;
        }
        
        const labels = Array.from(checkboxes).map(cb => {
            const label = document.querySelector(`label[for="${cb.id}"]`);
            return label ? label.textContent.trim() : '';
        }).filter(Boolean);
        
        // Đảm bảo có độ dài tối thiểu
        const minChars = 20; 
        
        // Tính độ dài văn bản tối đa dựa trên chiều rộng của toggle button
        let maxWidth = toggle.offsetWidth * 0.8; // Sử dụng 80% chiều rộng của nút
        if (maxWidth < 100) maxWidth = 200; // Đảm bảo có kích thước tối thiểu nếu DOM chưa tải xong
        
        const avgCharWidth = 8; // Ước tính trung bình độ rộng của mỗi ký tự (px)
        const maxChars = Math.max(minChars, Math.floor(maxWidth / avgCharWidth));
        
        let displayText = labels.join(', ');
        
        if (displayText.length > maxChars) {
            // Cắt text và thêm "..." ở cuối
            let shortenedText = '';
            let currentLength = 0;
            
            for (let i = 0; i < labels.length; i++) {
                if (currentLength + labels[i].length + 2 > maxChars) { // +2 cho dấu phẩy và khoảng trắng
                    shortenedText += ',...';
                    break;
                }
                
                if (i > 0) {
                    shortenedText += ', ';
                    currentLength += 2;
                }
                
                shortenedText += labels[i];
                currentLength += labels[i].length;
            }
            
            displayText = shortenedText;
        }
        
        selectedText.textContent = displayText;
    } 
    // Xử lý hiển thị cho các radio
    else if (radios.length > 0) {
        const label = document.querySelector(`label[for="${radios[0].id}"]`);
        selectedText.textContent = label ? label.textContent.trim() : radios[0].value;
    }
    // Trường hợp không có gì được chọn
    else {
        selectedText.textContent = 'Tất cả';
    }
}

/**
 * Khởi tạo trang tìm kiếm
 */
function initSearchPage() {
    init();
    
    // Thêm sự kiện load để đảm bảo cập nhật sau khi DOM và hình ảnh đã tải hoàn tất
    window.addEventListener('load', function() {
        // Cập nhật lại text hiển thị của tất cả các dropdown sau khi trang đã tải hoàn toàn
        document.querySelectorAll('.filter-dropdown').forEach(dropdown => {
            updateDropdownText(dropdown);
        });
    });
    
    // Khởi tạo chức năng nhảy trang từ dấu "..."
    initPageGoTo();
}

/**
 * Khởi tạo chức năng nhảy trang từ dấu "..."
 */
function initPageGoTo() {
    // Tìm tất cả nút "..." trong phân trang
    document.querySelectorAll('.page-link.dots').forEach(dotsElement => {
        dotsElement.addEventListener('click', function() {
            // Lấy trạng thái hiện tại của nút
            const pageItem = this.closest('.page-item');
            const gotoDirection = this.getAttribute('data-page-goto');
            
            // Xóa hoàn toàn nội dung hiện tại
            pageItem.innerHTML = '';
            
            // Tạo input để nhập số trang
            const inputElement = document.createElement('input');
            inputElement.type = 'text';
            inputElement.className = 'page-goto-input';
            inputElement.maxLength = 4;
            inputElement.placeholder = '...';
            inputElement.setAttribute('data-page-goto-direction', gotoDirection);
            
            // Thêm input vào DOM
            pageItem.appendChild(inputElement);
            
            // Focus vào input
            inputElement.focus();
            
            // Xử lý khi người dùng nhấn Enter
            inputElement.addEventListener('keydown', function(e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    
                    const pageNumber = parseInt(this.value);
                    const totalPages = getTotalPages();
                    
                    // Kiểm tra tính hợp lệ của số trang
                    if (!isNaN(pageNumber) && pageNumber >= 1 && pageNumber <= totalPages) {
                        // Tạo đường dẫn đến trang được chọn
                        navigateToPage(pageNumber);
                    } else {
                        // Hiển thị thông báo nếu số trang không hợp lệ
                        alert('Vui lòng nhập số trang hợp lệ (1-' + totalPages + ')');
                        this.value = '';
                        this.focus();
                    }
                }
            });
            
            // Xử lý khi người dùng click ra ngoài (blur)
            inputElement.addEventListener('blur', function() {
                // Khôi phục lại dấu "..."
                setTimeout(() => {
                    if (pageItem.contains(this)) {
                        // Tạo lại phần tử span với dấu ...
                        const dotsSpan = document.createElement('span');
                        dotsSpan.className = 'page-link dots';
                        dotsSpan.setAttribute('data-page-goto', gotoDirection);
                        dotsSpan.textContent = '...';
                        
                        // Xóa input
                        pageItem.innerHTML = '';
                        
                        // Thêm dấu ... mới
                        pageItem.appendChild(dotsSpan);
                        
                        // Khởi tạo lại sự kiện cho nút "..."
                        initPageGoTo();
                    }
                }, 200);
            });
        });
    });
}

/**
 * Lấy tổng số trang từ phân trang
 */
function getTotalPages() {
    const paginationInfo = document.querySelector('.text-center.mt-2.text-muted small');
    if (paginationInfo) {
        const text = paginationInfo.textContent;
        const match = text.match(/tổng số (\d+) manga/);
        if (match && match[1]) {
            const totalMangas = parseInt(match[1]);
            const pageSizeElement = document.querySelector('input[name="pageSize"]:checked');
            const pageSize = pageSizeElement ? parseInt(pageSizeElement.value) : 24;
            
            return Math.ceil(totalMangas / pageSize);
        }
    }
    return 0;
}

/**
 * Nhảy đến trang được chỉ định
 */
function navigateToPage(pageNumber) {
    // Lấy URL hiện tại
    const currentUrl = new URL(window.location.href);
    const searchParams = currentUrl.searchParams;
    
    // Cập nhật tham số page
    searchParams.set('page', pageNumber);
    
    // Tạo URL mới
    const newUrl = `${currentUrl.pathname}?${searchParams.toString()}`;
    
    // Thực hiện chuyển trang bằng HTMX
    htmx.ajax('GET', newUrl, { target: '#main-content', pushUrl: true });
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
        
        // Reset các checkbox trạng thái và đối tượng độc giả (unchecked)
        document.querySelectorAll('input[name="status"], input[name="publicationDemographic"]').forEach(cb => {
            cb.checked = false;
        });
        
        // Reset các checkbox nội dung (checked)
        document.querySelectorAll('input[name="contentRating"]').forEach(cb => {
            cb.checked = ['safe', 'suggestive', 'erotica'].includes(cb.value);
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
    initSearchPage,
    initPageGoTo
};