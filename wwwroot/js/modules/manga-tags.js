/**
 * manga-tags.js - Quản lý xử lý và hiển thị danh sách thẻ từ MangaDex API
 */

/**
 * Tải danh sách thẻ từ API MangaDex
 */
async function fetchTags() {
    try {
        console.log('Đang tải danh sách thẻ từ MangaDex...');
        const response = await fetch('/api/manga/tags');
        
        if (!response.ok) {
            throw new Error(`Lỗi khi tải danh sách thẻ: ${response.status}`);
        }
        
        const data = await response.json();
        console.log('Đã tải danh sách thẻ thành công:', data);
        return data;
    } catch (error) {
        console.error('Lỗi khi tải danh sách thẻ:', error);
        return null;
    }
}

/**
 * Phân loại danh sách thẻ theo nhóm
 * @param {Array} tags - Danh sách thẻ từ API
 * @returns {Object} Danh sách thẻ đã phân loại theo nhóm
 */
function categorizeTagsByGroup(tags) {
    if (!tags || !Array.isArray(tags)) return {};
    
    const categorizedTags = {};
    
    tags.forEach(tag => {
        // Lấy dữ liệu từ tag
        const id = tag.id;
        const attributes = tag.attributes || {};
        const name = attributes.name?.vi || attributes.name?.en || 'Không có tên';
        const group = attributes.group || 'other';
        
        // Tạo nhóm nếu chưa tồn tại
        if (!categorizedTags[group]) {
            categorizedTags[group] = [];
        }
        
        // Thêm tag vào nhóm
        categorizedTags[group].push({
            id,
            name,
            description: attributes.description?.vi || attributes.description?.en || '',
        });
    });
    
    return categorizedTags;
}

/**
 * Dịch tên nhóm thẻ sang tiếng Việt
 * @param {string} groupName - Tên nhóm thẻ
 * @returns {string} Tên nhóm thẻ đã dịch
 */
function translateGroupName(groupName) {
    const translations = {
        'genre': 'Thể loại',
        'theme': 'Chủ đề',
        'format': 'Định dạng',
        'content': 'Nội dung',
        'demographic': 'Đối tượng',
        'other': 'Khác'
    };
    
    return translations[groupName] || groupName;
}

/**
 * Tạo HTML cho danh sách thẻ theo nhóm
 * @param {Object} categorizedTags - Danh sách thẻ đã phân loại theo nhóm
 * @param {Array} selectedTags - Danh sách thẻ đã chọn (nếu có)
 * @returns {string} HTML cho danh sách thẻ
 */
function createTagsHTML(categorizedTags, selectedTags = []) {
    let html = '';
    
    // Sắp xếp các nhóm theo thứ tự ưu tiên
    const sortOrder = ['genre', 'theme', 'format', 'content', 'demographic', 'other'];
    const sortedGroups = Object.keys(categorizedTags).sort((a, b) => {
        return sortOrder.indexOf(a) - sortOrder.indexOf(b);
    });
    
    // Tạo HTML cho từng nhóm
    sortedGroups.forEach(group => {
        const tags = categorizedTags[group];
        const groupNameTranslated = translateGroupName(group);
        
        html += `<div class="tag-group mb-3">
            <h6 class="fw-bold">${groupNameTranslated}</h6>
            <div class="d-flex flex-wrap tag-list">`;
        
        // Tạo HTML cho từng thẻ trong nhóm
        tags.forEach(tag => {
            const isSelected = selectedTags.includes(tag.id);
            html += `
                <div class="form-check form-check-inline tag-item" title="${tag.description || ''}">
                    <input class="form-check-input" type="checkbox" name="includedTags[]" 
                        id="tag-${tag.id}" value="${tag.id}" ${isSelected ? 'checked' : ''}>
                    <label class="form-check-label" for="tag-${tag.id}">${tag.name}</label>
                </div>`;
        });
        
        html += `</div></div>`;
    });
    
    return html;
}

/**
 * Khởi tạo xử lý danh sách thẻ trong form tìm kiếm
 */
async function initTagsInSearchForm() {
    const tagsContainer = document.getElementById('tagsContainer');
    if (!tagsContainer) return;
    
    // Hiển thị trạng thái đang tải
    tagsContainer.innerHTML = '<div class="text-center"><div class="spinner-border spinner-border-sm text-primary" role="status"></div> Đang tải danh sách thẻ...</div>';
    
    try {
        // Tải danh sách thẻ từ API
        const tagsData = await fetchTags();
        
        if (!tagsData || !tagsData.data) {
            tagsContainer.innerHTML = '<div class="alert alert-warning">Không thể tải danh sách thẻ. Vui lòng thử lại sau.</div>';
            return;
        }
        
        // Lấy danh sách thẻ đã phân loại
        const categorizedTags = categorizeTagsByGroup(tagsData.data);
        
        // Lấy danh sách thẻ đã chọn (nếu có)
        const selectedTagsElement = document.getElementById('selectedTags');
        const selectedTags = selectedTagsElement ? selectedTagsElement.value.split(',').filter(Boolean) : [];
        
        // Hiển thị danh sách thẻ
        tagsContainer.innerHTML = createTagsHTML(categorizedTags, selectedTags);
        
        // Thêm sự kiện cho các thẻ checkbox
        attachTagsEvents();
    } catch (error) {
        console.error('Lỗi khi khởi tạo danh sách thẻ:', error);
        tagsContainer.innerHTML = '<div class="alert alert-danger">Đã xảy ra lỗi khi tải danh sách thẻ.</div>';
    }
}

/**
 * Gắn các sự kiện cho các thẻ checkbox
 */
function attachTagsEvents() {
    const tagCheckboxes = document.querySelectorAll('input[name="includedTags[]"]');
    
    tagCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            updateSelectedTagsBadges();
        });
    });
    
    // Khởi tạo hiển thị các thẻ đã chọn
    updateSelectedTagsBadges();
}

/**
 * Cập nhật hiển thị badge cho các thẻ đã chọn
 */
function updateSelectedTagsBadges() {
    const selectedTagsContainer = document.getElementById('selectedTagsBadges');
    if (!selectedTagsContainer) return;
    
    const tagCheckboxes = document.querySelectorAll('input[name="includedTags[]"]:checked');
    const tagsCount = tagCheckboxes.length;
    
    // Cập nhật số lượng thẻ đã chọn
    const tagsCountContainer = document.getElementById('tagsCount');
    if (tagsCountContainer) {
        tagsCountContainer.textContent = tagsCount;
    }
    
    // Xóa tất cả badge hiện tại
    selectedTagsContainer.innerHTML = '';
    
    // Thêm badge mới cho mỗi thẻ đã chọn
    tagCheckboxes.forEach(checkbox => {
        const labelElement = document.querySelector(`label[for="${checkbox.id}"]`);
        const tagName = labelElement ? labelElement.textContent : checkbox.value;
        
        const badge = document.createElement('span');
        badge.className = 'badge bg-primary me-1 mb-1';
        badge.innerHTML = `${tagName} <i class="bi bi-x-circle tag-remove" data-tag-id="${checkbox.value}"></i>`;
        selectedTagsContainer.appendChild(badge);
        
        // Thêm sự kiện xóa thẻ
        const removeIcon = badge.querySelector('.tag-remove');
        if (removeIcon) {
            removeIcon.addEventListener('click', function() {
                checkbox.checked = false;
                updateSelectedTagsBadges();
            });
        }
    });
    
    // Hiển thị hoặc ẩn container tùy thuộc vào số lượng thẻ đã chọn
    const selectedTagsGroup = document.getElementById('selectedTagsGroup');
    if (selectedTagsGroup) {
        selectedTagsGroup.style.display = tagsCount > 0 ? 'block' : 'none';
    }
}

// Export các hàm cần thiết
export { 
    initTagsInSearchForm,
    fetchTags,
    categorizeTagsByGroup,
    createTagsHTML
}; 