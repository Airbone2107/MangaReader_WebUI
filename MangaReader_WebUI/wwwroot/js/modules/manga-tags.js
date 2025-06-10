/**
 * manga-tags.js - Quản lý xử lý và hiển thị danh sách thẻ từ MangaDex API
 */

// Biến lưu trữ danh sách thẻ đã tải
let tagsData = null;
let selectedTags = new Map(); // Sử dụng Map để lưu trữ các thẻ đã chọn (includedTags)
let excludedTags = new Map(); // Sử dụng Map để lưu trữ các thẻ loại trừ (excludedTags)

/**
 * Khởi tạo chức năng thẻ trong form tìm kiếm
 * DEPRECATED: Đã được thay thế bằng search-tags-dropdown.js
 */
function initTagsInSearchForm() {
    console.log('Manga tags module đã được thay thế bởi search-tags-dropdown.js');
    // Hàm này giờ không làm gì cả, chức năng đã được chuyển sang search-tags-dropdown.js
    return;
    
    /* --- CODE CŨ ĐÃ BỊ VÔ HIỆU HÓA ---
    console.log('Đang khởi tạo module quản lý thẻ manga...');
    
    // Các phần tử DOM chính
    const tagsSelection = document.getElementById('mangaTagsSelection');
    const tagsDropdown = document.getElementById('mangaTagsDropdown');
    const tagsContainer = document.getElementById('tagsContainer');
    const selectedTagsDisplay = document.getElementById('selectedTagsDisplay');
    const selectedTagsInput = document.getElementById('selectedTags');
    const excludedTagsInput = document.getElementById('excludedTags');
    const tagSearchInput = document.getElementById('tagSearchInput');
    const closeTagsButton = document.getElementById('closeTagsDropdown');
    
    // Phần tử cho chế độ thẻ bắt buộc (includedTagsMode)
    const includedTagsModeBox = document.getElementById('includedTagsModeBox');
    const includedTagsModeText = document.getElementById('includedTagsModeText');
    const includedTagsModeInput = document.getElementById('includedTagsMode');
    
    // Phần tử cho chế độ thẻ loại trừ (excludedTagsMode)
    const excludedTagsModeBox = document.getElementById('excludedTagsModeBox');
    const excludedTagsModeText = document.getElementById('excludedTagsModeText');
    const excludedTagsModeInput = document.getElementById('excludedTagsMode');
    
    // Nếu không tìm thấy các phần tử cần thiết, thoát
    if (!tagsSelection || !tagsDropdown || !tagsContainer) {
        console.log('Không tìm thấy các phần tử cần thiết cho module quản lý thẻ.');
        return;
    }
    
    // Khởi tạo danh sách thẻ đã chọn từ input ẩn
    initSelectedTags();
    
    // Hiển thị/ẩn dropdown khi click vào selection box
    tagsSelection.addEventListener('click', function() {
        if (tagsDropdown.style.display === 'block') {
            tagsDropdown.style.display = 'none';
        } else {
            tagsDropdown.style.display = 'block';
            loadTags();
            
            // Focus vào ô tìm kiếm
            if (tagSearchInput) {
                setTimeout(() => tagSearchInput.focus(), 100);
            }
        }
    });
    
    // Đóng dropdown khi click vào nút đóng
    if (closeTagsButton) {
        closeTagsButton.addEventListener('click', function() {
            tagsDropdown.style.display = 'none';
        });
    }
    
    // Đóng dropdown khi click ra ngoài
    document.addEventListener('click', function(e) {
        if (!tagsSelection.contains(e.target) && !tagsDropdown.contains(e.target)) {
            tagsDropdown.style.display = 'none';
        }
    });
    
    // Xử lý tìm kiếm thẻ
    if (tagSearchInput) {
        tagSearchInput.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase().trim();
            const tagItems = document.querySelectorAll('.manga-tag-item');
            const tagGroups = document.querySelectorAll('.manga-tag-group');
            
            let visibleCount = 0;
            
            tagItems.forEach(item => {
                const tagName = item.querySelector('.manga-tag-name').textContent.toLowerCase();
                if (searchTerm === '' || tagName.includes(searchTerm)) {
                    item.style.display = '';
                    visibleCount++;
                } else {
                    item.style.display = 'none';
                }
            });
            
            // Ẩn/hiện các nhóm thẻ dựa trên số thẻ hiển thị
            tagGroups.forEach(group => {
                const visibleItems = group.querySelectorAll('.manga-tag-item[style="display: none;"]');
                if (visibleItems.length === group.querySelectorAll('.manga-tag-item').length) {
                    group.style.display = 'none';
                } else {
                    group.style.display = '';
                }
            });
        });
    }
    
    // Xử lý thay đổi chế độ thẻ cho includedTagsMode khi bấm vào box
    if (includedTagsModeBox && includedTagsModeText && includedTagsModeInput) {
        includedTagsModeBox.addEventListener('click', function() {
            const currentMode = includedTagsModeInput.value;
            
            if (currentMode === 'AND') {
                includedTagsModeInput.value = 'OR';
                includedTagsModeText.textContent = 'HOẶC';
                includedTagsModeBox.classList.remove('tag-mode-and');
                includedTagsModeBox.classList.add('tag-mode-or');
            } else {
                includedTagsModeInput.value = 'AND';
                includedTagsModeText.textContent = 'VÀ';
                includedTagsModeBox.classList.remove('tag-mode-or');
                includedTagsModeBox.classList.add('tag-mode-and');
            }
        });
    }
    
    // Xử lý thay đổi chế độ thẻ cho excludedTagsMode khi bấm vào box
    if (excludedTagsModeBox && excludedTagsModeText && excludedTagsModeInput) {
        excludedTagsModeBox.addEventListener('click', function() {
            const currentMode = excludedTagsModeInput.value;
            
            if (currentMode === 'OR') {
                excludedTagsModeInput.value = 'AND';
                excludedTagsModeText.textContent = 'VÀ';
                excludedTagsModeBox.classList.remove('tag-mode-or');
                excludedTagsModeBox.classList.add('tag-mode-and');
            } else {
                excludedTagsModeInput.value = 'OR';
                excludedTagsModeText.textContent = 'HOẶC';
                excludedTagsModeBox.classList.remove('tag-mode-and');
                excludedTagsModeBox.classList.add('tag-mode-or');
            }
        });
    }
    
    // Xử lý xóa thẻ khi click vào nút xóa trong các thẻ đã hiển thị
    document.addEventListener('click', function(e) {
        if (e.target.closest('.manga-tag-remove')) {
            const tagBadge = e.target.closest('.manga-tag-badge');
            const tagId = tagBadge.dataset.tagId;
            const isExcluded = tagBadge.classList.contains('excluded');
            
            // Xóa thẻ khỏi danh sách tương ứng
            if (isExcluded) {
                excludedTags.delete(tagId);
            } else {
                selectedTags.delete(tagId);
            }
            
            // Cập nhật hiển thị và input hidden
            updateSelectedTagsDisplay();
            updateTagsInput();
            
            // Cập nhật trạng thái checkbox trong danh sách
            const tagItem = document.querySelector(`.manga-tag-item[data-tag-id="${tagId}"]`);
            if (tagItem) {
                const checkbox = tagItem.querySelector('input[type="checkbox"]');
                if (checkbox) {
                    checkbox.checked = false;
                }
                tagItem.classList.remove('selected', 'excluded');
            }
        }
    });
    */
}

/**
 * Khởi tạo danh sách thẻ đã chọn từ input ẩn
 */
function initSelectedTags() {
    // Xóa danh sách thẻ đã chọn cũ
    selectedTags.clear();
    excludedTags.clear();
    
    // Khởi tạo includedTags
    const selectedTagsInput = document.getElementById('selectedTags');
    if (selectedTagsInput && selectedTagsInput.value) {
        const tagIds = selectedTagsInput.value.split(',').filter(Boolean);
        tagIds.forEach(tagId => {
            selectedTags.set(tagId, tagId); // Tạm thời sử dụng ID làm tên
        });
    }
    
    // Khởi tạo excludedTags
    const excludedTagsInput = document.getElementById('excludedTags');
    if (excludedTagsInput && excludedTagsInput.value) {
        const tagIds = excludedTagsInput.value.split(',').filter(Boolean);
        tagIds.forEach(tagId => {
            excludedTags.set(tagId, tagId); // Tạm thời sử dụng ID làm tên
        });
    }
}

/**
 * Tải danh sách thẻ từ API
 */
async function loadTags() {
    // Nếu đã tải dữ liệu, không cần tải lại
    if (tagsData) {
        renderTags(tagsData);
        return;
    }
    
    try {
        // Hiển thị spinner
        const tagsContainer = document.getElementById('tagsContainer');
        if (tagsContainer) {
            tagsContainer.innerHTML = `
                <div class="text-center py-3">
                    <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                    <span>Đang tải danh sách thẻ...</span>
                </div>
            `;
        }
        
        // Tải danh sách thẻ
        const response = await fetch('/api/manga/tags');
        if (!response.ok) {
            throw new Error(`Lỗi khi tải danh sách thẻ: ${response.status}`);
        }
        
        tagsData = await response.json();
        renderTags(tagsData);
    } catch (error) {
        console.error('Lỗi khi tải danh sách thẻ:', error);
        
        // Hiển thị thông báo lỗi
        const tagsContainer = document.getElementById('tagsContainer');
        if (tagsContainer) {
            tagsContainer.innerHTML = `
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    Không thể tải danh sách thẻ. Vui lòng thử lại sau.
                </div>
            `;
        }
    }
}

/**
 * Hiển thị danh sách thẻ từ API
 * @param {Object} data - Dữ liệu thẻ từ API
 */
function renderTags(data) {
    const container = document.getElementById('tagsContainer');
    if (!container) return;
    
    // Xóa nội dung cũ
    container.innerHTML = '';
    
    // Kiểm tra dữ liệu
    if (!data || !data.data || !Array.isArray(data.data)) {
        container.innerHTML = `
            <div class="alert alert-warning">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                Không có dữ liệu thẻ.
            </div>
        `;
        return;
    }
    
    // Phân loại thẻ theo nhóm
    const tagsByGroup = {};
    data.data.forEach(tag => {
        const attributes = tag.attributes || {};
        const group = attributes.group || 'other';
        
        if (!tagsByGroup[group]) {
            tagsByGroup[group] = [];
        }
        
        // Lấy tên thẻ theo thứ tự ưu tiên: Tiếng Việt > Tiếng Anh > ID
        const tagName = attributes.name?.vi || attributes.name?.en || tag.id;
        
        tagsByGroup[group].push({
            id: tag.id,
            name: tagName,
            description: attributes.description?.vi || attributes.description?.en || ''
        });
    });
    
    // Sắp xếp các nhóm theo thứ tự ưu tiên
    const groupOrder = ['genre', 'theme', 'format', 'content', 'demographic', 'other'];
    const sortedGroups = Object.keys(tagsByGroup).sort((a, b) => {
        return groupOrder.indexOf(a) - groupOrder.indexOf(b);
    });
    
    // Hiển thị từng nhóm
    sortedGroups.forEach(group => {
        // Dịch tên nhóm
        const groupName = translateGroupName(group);
        
        // Tạo phần tử nhóm
        const tagGroup = document.createElement('div');
        tagGroup.className = 'manga-tag-group';
        
        // Tạo tiêu đề nhóm
        const groupTitle = document.createElement('div');
        groupTitle.className = 'manga-tag-group-title';
        groupTitle.textContent = groupName;
        tagGroup.appendChild(groupTitle);
        
        // Tạo danh sách thẻ trong nhóm
        const tagList = document.createElement('div');
        tagList.className = 'manga-tag-list';
        
        // Sắp xếp thẻ theo tên
        tagsByGroup[group].sort((a, b) => a.name.localeCompare(b.name)).forEach(tag => {
            // Xác định trạng thái của tag
            const isIncluded = selectedTags.has(tag.id);
            const isExcluded = excludedTags.has(tag.id);
            
            // Cập nhật tên thẻ trong maps nếu đã chọn
            if (isIncluded) {
                selectedTags.set(tag.id, tag.name);
            } else if (isExcluded) {
                excludedTags.set(tag.id, tag.name);
            }
            
            // Tạo item thẻ
            const tagItem = document.createElement('div');
            tagItem.className = 'manga-tag-item';
            tagItem.dataset.tagId = tag.id;
            
            // Thêm class dựa trên trạng thái
            if (isIncluded) {
                tagItem.classList.add('selected');
            } else if (isExcluded) {
                tagItem.classList.add('excluded');
            }
            
            // Tạo label
            const label = document.createElement('span');
            label.className = 'manga-tag-name';
            label.textContent = tag.name;
            
            // Thêm tooltip nếu có mô tả
            if (tag.description) {
                label.title = tag.description;
                // Thêm bootstrap tooltip nếu có
                if (window.bootstrap && window.bootstrap.Tooltip) {
                    new bootstrap.Tooltip(label, {
                        placement: 'top',
                        delay: {show: 500, hide: 100}
                    });
                }
            }
            
            // Thêm các phần tử vào item
            tagItem.appendChild(label);
            
            // Xử lý sự kiện click
            tagItem.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                // Xác định trạng thái hiện tại
                const isIncluded = selectedTags.has(tag.id);
                const isExcluded = excludedTags.has(tag.id);
                
                // Đảo trạng thái theo chu kỳ: Không chọn -> Included -> Excluded -> Không chọn
                if (!isIncluded && !isExcluded) {
                    // Thêm vào includedTags
                    selectedTags.set(tag.id, tag.name);
                    smoothToggleClass(tagItem, 'selected', true);
                    smoothToggleClass(tagItem, 'excluded', false);
                } else if (isIncluded) {
                    // Chuyển từ includedTags sang excludedTags
                    selectedTags.delete(tag.id);
                    excludedTags.set(tag.id, tag.name);
                    smoothToggleClass(tagItem, 'selected', false);
                    smoothToggleClass(tagItem, 'excluded', true);
                } else {
                    // Xóa khỏi excludedTags (không chọn)
                    excludedTags.delete(tag.id);
                    smoothToggleClass(tagItem, 'excluded', false);
                    smoothToggleClass(tagItem, 'selected', false);
                }
                
                // Cập nhật hiển thị
                updateSelectedTagsDisplay();
                updateTagsInput();
            });
            
            tagList.appendChild(tagItem);
        });
        
        tagGroup.appendChild(tagList);
        container.appendChild(tagGroup);
    });
    
    // Cập nhật hiển thị của các thẻ đã chọn
    updateSelectedTagsDisplay();
}

/**
 * Cập nhật hiển thị các thẻ đã chọn
 */
function updateSelectedTagsDisplay() {
    const selectedTagsDisplay = document.getElementById('selectedTagsDisplay');
    if (!selectedTagsDisplay) return;
    
    // Xóa tất cả các thẻ hiện tại
    selectedTagsDisplay.innerHTML = '';
    
    if (selectedTags.size === 0 && excludedTags.size === 0) {
        // Hiển thị thông báo trống
        const empty = document.createElement('span');
        empty.id = 'emptyTagsMessage';
        empty.className = 'manga-tags-empty';
        empty.textContent = 'Chưa có thẻ nào được chọn. Bấm để chọn thẻ.';
        selectedTagsDisplay.appendChild(empty);
    } else {
        // Hiển thị các thẻ đã chọn (includedTags)
        selectedTags.forEach((name, id) => {
            const tagBadge = createTagBadge(id, name, false);
            selectedTagsDisplay.appendChild(tagBadge);
        });
        
        // Hiển thị các thẻ loại trừ (excludedTags)
        excludedTags.forEach((name, id) => {
            const tagBadge = createTagBadge(id, name, true);
            selectedTagsDisplay.appendChild(tagBadge);
        });
    }
}

/**
 * Tạo phần tử hiển thị tag badge
 * @param {string} id - ID của tag
 * @param {string} name - Tên của tag
 * @param {boolean} isExcluded - Có phải là tag loại trừ không
 * @returns {HTMLElement} - Phần tử badge
 */
function createTagBadge(id, name, isExcluded) {
    const tagBadge = document.createElement('div');
    tagBadge.className = 'manga-tag-badge';
    tagBadge.dataset.tagId = id;
    
    // Thêm class loại trừ nếu cần
    if (isExcluded) {
        tagBadge.classList.add('excluded');
    }
    
    const tagName = document.createElement('span');
    tagName.className = 'manga-tag-name';
    tagName.textContent = name;
    
    const tagRemove = document.createElement('span');
    tagRemove.className = 'manga-tag-remove';
    tagRemove.innerHTML = '<i class="bi bi-x"></i>';
    
    tagBadge.appendChild(tagName);
    tagBadge.appendChild(tagRemove);
    
    return tagBadge;
}

/**
 * Cập nhật input ẩn chứa danh sách thẻ đã chọn
 */
function updateTagsInput() {
    // Cập nhật selectedTags (includedTags)
    const selectedTagsInput = document.getElementById('selectedTags');
    if (selectedTagsInput) {
        // Chuyển map thành mảng ID
        const tagIds = Array.from(selectedTags.keys());
        selectedTagsInput.value = tagIds.join(',');
    }
    
    // Cập nhật excludedTags
    const excludedTagsInput = document.getElementById('excludedTags');
    if (excludedTagsInput) {
        // Chuyển map thành mảng ID
        const tagIds = Array.from(excludedTags.keys());
        excludedTagsInput.value = tagIds.join(',');
    }
}

/**
 * Dịch tên nhóm sang tiếng Việt
 * @param {string} group - Tên nhóm gốc
 * @returns {string} - Tên nhóm đã dịch
 */
function translateGroupName(group) {
    const translations = {
        'genre': 'Thể loại',
        'theme': 'Chủ đề',
        'format': 'Định dạng',
        'content': 'Nội dung',
        'demographic': 'Đối tượng',
        'other': 'Khác'
    };
    
    return translations[group] || group;
}

/**
 * Áp dụng/gỡ bỏ class mà không có hiệu ứng ánh lên
 * @param {HTMLElement} element - Phần tử cần thay đổi
 * @param {string} className - Tên class cần thay đổi
 * @param {boolean} add - True để thêm, False để xóa
 */
function smoothToggleClass(element, className, add) {
    if (add) {
        // Thêm class không có hiệu ứng fade
        element.classList.add(className);
    } else {
        // Xóa class không có hiệu ứng fade
        if (element.classList.contains(className)) {
            element.classList.remove(className);
        }
    }
}

// Export function
export { initTagsInSearchForm };
