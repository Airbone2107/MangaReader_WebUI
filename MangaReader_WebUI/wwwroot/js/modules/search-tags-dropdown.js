(function() {
    'use strict';

    const DEBUG_MODE = true; // Chuyển thành false khi deploy production

    function log(message, ...optionalParams) {
        if (DEBUG_MODE) {
            console.log('[SearchTagsDropdown]', message, ...optionalParams);
        }
    }

    function debugLog(message, ...optionalParams) {
        if (DEBUG_MODE) {
            console.debug('[SearchTagsDropdown DEBUG]', message, ...optionalParams);
        }
    }

    // --- Các biến và hằng số ---
    let tagsDataCache = null; // Cache dữ liệu tags từ API
    const selectedIncludeTags = new Map(); // Lưu các tag được chọn (ID -> Name)
    const selectedExcludeTags = new Map(); // Lưu các tag bị loại trừ (ID -> Name)

    // DOM Elements (sẽ được lấy trong hàm init)
    let tagsSelectionEl, tagsDropdownEl, tagsContainerEl, selectedTagsDisplayEl;
    let includedTagsInputEl, excludedTagsInputEl, tagSearchInputEl, closeTagsButtonEl;
    let includedTagsModeBoxEl, includedTagsModeTextEl, includedTagsModeInputEl;
    let excludedTagsModeBoxEl, excludedTagsModeTextEl, excludedTagsModeInputEl;
    let emptyTagsMessageEl;

    // --- Hàm khởi tạo chính ---
    function init() {
        log('Khởi tạo module dropdown tags...');

        // Lấy các phần tử DOM cần thiết
        tagsSelectionEl = document.getElementById('mangaTagsSelection');
        tagsDropdownEl = document.getElementById('mangaTagsDropdown');
        tagsContainerEl = document.getElementById('tagsContainer');
        selectedTagsDisplayEl = document.getElementById('selectedTagsDisplay');
        includedTagsInputEl = document.getElementById('selectedTags'); // Lưu ý: ID hiện tại là selectedTags
        excludedTagsInputEl = document.getElementById('excludedTags');
        tagSearchInputEl = document.getElementById('tagSearchInput');
        closeTagsButtonEl = document.getElementById('closeTagsDropdown');
        emptyTagsMessageEl = document.getElementById('emptyTagsMessage');

        includedTagsModeBoxEl = document.getElementById('includedTagsModeBox');
        includedTagsModeTextEl = document.getElementById('includedTagsModeText');
        includedTagsModeInputEl = document.getElementById('includedTagsMode');

        excludedTagsModeBoxEl = document.getElementById('excludedTagsModeBox');
        excludedTagsModeTextEl = document.getElementById('excludedTagsModeText');
        excludedTagsModeInputEl = document.getElementById('excludedTagsMode');

        if (!tagsSelectionEl || !tagsDropdownEl || !tagsContainerEl || !selectedTagsDisplayEl || !includedTagsInputEl || !excludedTagsInputEl || !tagSearchInputEl || !closeTagsButtonEl || !includedTagsModeBoxEl || !excludedTagsModeBoxEl) {
            log('Lỗi: Thiếu một hoặc nhiều phần tử DOM cần thiết cho dropdown tags. Module sẽ không hoạt động.');
            return;
        }

        // Khôi phục trạng thái tag đã chọn từ input ẩn khi trang tải lại (quan trọng cho HTMX)
        restoreSelectedTagsFromInput();

        // Gắn các event listeners
        attachEventListeners();

        log('Module dropdown tags đã khởi tạo xong.');
    }

    function restoreSelectedTagsFromInput() {
        log('Khôi phục trạng thái tags từ input ẩn.');
        selectedIncludeTags.clear();
        selectedExcludeTags.clear();

        if (includedTagsInputEl && includedTagsInputEl.value) {
            const ids = includedTagsInputEl.value.split(',').filter(Boolean);
            ids.forEach(id => {
                // Tên sẽ được cập nhật sau khi loadTags
                selectedIncludeTags.set(id, `Tag ${id}`);
            });
            log('Đã khôi phục included tags:', selectedIncludeTags);
        }

        if (excludedTagsInputEl && excludedTagsInputEl.value) {
            const ids = excludedTagsInputEl.value.split(',').filter(Boolean);
            ids.forEach(id => {
                // Tên sẽ được cập nhật sau khi loadTags
                selectedExcludeTags.set(id, `Tag ${id}`);
            });
            log('Đã khôi phục excluded tags:', selectedExcludeTags);
        }
        // Gọi updateSelectedTagsDisplay ngay sau khi khôi phục để UI đồng bộ ban đầu
        // Tuy nhiên, tên tag có thể chưa đúng nếu API chưa load.
        // Tên tag sẽ được cập nhật chính xác sau khi loadTags() thành công.
        updateSelectedTagsUI();
    }

    function attachEventListeners() {
        log('Gắn các event listeners...');

        // Mở/đóng dropdown
        tagsSelectionEl.removeEventListener('click', handleToggleDropdown); // Xóa listener cũ nếu có
        tagsSelectionEl.addEventListener('click', handleToggleDropdown);

        // Đóng dropdown bằng nút "Đóng"
        closeTagsButtonEl.removeEventListener('click', handleCloseDropdown);
        closeTagsButtonEl.addEventListener('click', handleCloseDropdown);

        // Đóng dropdown khi click ra ngoài
        document.removeEventListener('click', handleClickOutsideDropdown);
        document.addEventListener('click', handleClickOutsideDropdown);

        // Tìm kiếm tags
        tagSearchInputEl.removeEventListener('input', handleTagSearch);
        tagSearchInputEl.addEventListener('input', handleTagSearch);

        // Click vào tag item trong dropdown (sử dụng event delegation)
        tagsContainerEl.removeEventListener('click', handleTagItemClick);
        tagsContainerEl.addEventListener('click', handleTagItemClick);

        // Click vào nút xóa tag badge (sử dụng event delegation)
        selectedTagsDisplayEl.removeEventListener('click', handleRemoveTagBadge);
        selectedTagsDisplayEl.addEventListener('click', handleRemoveTagBadge);

        // Thay đổi chế độ AND/OR cho included tags
        includedTagsModeBoxEl.removeEventListener('click', toggleIncludedTagsMode);
        includedTagsModeBoxEl.addEventListener('click', toggleIncludedTagsMode);

        // Thay đổi chế độ AND/OR cho excluded tags
        excludedTagsModeBoxEl.removeEventListener('click', toggleExcludedTagsMode);
        excludedTagsModeBoxEl.addEventListener('click', toggleExcludedTagsMode);
        
        log('Đã gắn xong các event listeners.');
    }

    function handleToggleDropdown() {
        debugLog('Tags selection clicked, toggling dropdown.');
        if (tagsDropdownEl.style.display === 'block') {
            tagsDropdownEl.style.display = 'none';
        } else {
            tagsDropdownEl.style.display = 'block';
            if (!tagsDataCache) { // Chỉ load tags nếu chưa có cache
                loadTagsFromAPI();
            } else {
                 // Nếu đã có cache, render lại để đảm bảo trạng thái selected/excluded đúng
                renderTagsToDOM(tagsDataCache);
            }
            // Focus vào ô tìm kiếm
            setTimeout(() => tagSearchInputEl.focus(), 0);
        }
    }

    function handleCloseDropdown() {
        debugLog('Close button clicked.');
        tagsDropdownEl.style.display = 'none';
    }

    function handleClickOutsideDropdown(event) {
        if (!tagsSelectionEl.contains(event.target) && !tagsDropdownEl.contains(event.target)) {
            if (tagsDropdownEl.style.display === 'block') {
                debugLog('Clicked outside, closing dropdown.');
                tagsDropdownEl.style.display = 'none';
            }
        }
    }

    async function loadTagsFromAPI() {
        log('Đang tải danh sách tags từ API...');
        tagsContainerEl.innerHTML = `
            <div class="text-center py-3">
                <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                <span>Đang tải danh sách thẻ...</span>
            </div>
        `;

        try {
            const response = await fetch('/api/manga/tags'); // Endpoint đã được định nghĩa trong MangaController
            if (!response.ok) {
                throw new Error(`Lỗi API: ${response.status}`);
            }
            const result = await response.json();
            if (result.success && result.data) {
                tagsDataCache = result.data; // Cache lại dữ liệu
                log('Tải tags thành công:', tagsDataCache);
                renderTagsToDOM(tagsDataCache);
                // Sau khi render, cập nhật lại tên cho các tag đã chọn (nếu có)
                updateSelectedTagNamesFromCache();
                updateSelectedTagsUI(); // Cập nhật lại UI của badge sau khi có tên
            } else {
                throw new Error(result.error || 'Dữ liệu tags không hợp lệ');
            }
        } catch (error) {
            log('Lỗi khi tải tags:', error);
            tagsContainerEl.innerHTML = `<div class="alert alert-danger m-2">Không thể tải danh sách thẻ.</div>`;
        }
    }
    
    function updateSelectedTagNamesFromCache() {
        if (!tagsDataCache || !tagsDataCache.data) return;

        const allTagsMap = new Map();
        tagsDataCache.data.forEach(tag => {
            const name = tag.attributes?.name?.vi || tag.attributes?.name?.en || tag.id;
            allTagsMap.set(tag.id, name);
        });

        selectedIncludeTags.forEach((oldName, id) => {
            if (allTagsMap.has(id)) {
                selectedIncludeTags.set(id, allTagsMap.get(id));
            }
        });
        selectedExcludeTags.forEach((oldName, id) => {
            if (allTagsMap.has(id)) {
                selectedExcludeTags.set(id, allTagsMap.get(id));
            }
        });
        log('Đã cập nhật tên cho các tags đã chọn từ cache.');
    }

    function renderTagsToDOM(apiResult) {
        if (!apiResult || !apiResult.data) {
            tagsContainerEl.innerHTML = '<div class="p-2 text-muted">Không có thẻ nào.</div>';
            return;
        }
        tagsContainerEl.innerHTML = ''; // Xóa nội dung cũ

        const tagsByGroup = {};
        apiResult.data.forEach(tag => {
            const attributes = tag.attributes || {};
            const group = attributes.group || 'other';
            if (!tagsByGroup[group]) {
                tagsByGroup[group] = [];
            }
            tagsByGroup[group].push({
                id: tag.id,
                name: attributes.name?.vi || attributes.name?.en || tag.id, // Ưu tiên tên TV, rồi EN
                description: attributes.description?.vi || attributes.description?.en || ''
            });
        });

        const groupOrder = ['genre', 'theme', 'format', 'content', 'demographic', 'other'];
        groupOrder.forEach(groupKey => {
            if (tagsByGroup[groupKey]) {
                const groupDiv = document.createElement('div');
                groupDiv.className = 'manga-tag-group';
                
                const groupTitle = document.createElement('div');
                groupTitle.className = 'manga-tag-group-title';
                groupTitle.textContent = translateGroupName(groupKey);
                groupDiv.appendChild(groupTitle);

                const listDiv = document.createElement('div');
                listDiv.className = 'manga-tag-list';

                tagsByGroup[groupKey].sort((a, b) => a.name.localeCompare(b.name, 'vi')).forEach(tag => {
                    const tagItem = document.createElement('div');
                    tagItem.className = 'manga-tag-item';
                    tagItem.dataset.tagId = tag.id;
                    
                    const tagNameSpan = document.createElement('span');
                    tagNameSpan.className = 'manga-tag-name';
                    tagNameSpan.textContent = tag.name;
                    if (tag.description) {
                        tagItem.title = tag.description; // Thêm tooltip
                    }
                    tagItem.appendChild(tagNameSpan);

                    // Cập nhật trạng thái visual
                    if (selectedIncludeTags.has(tag.id)) {
                        tagItem.classList.add('selected');
                    } else if (selectedExcludeTags.has(tag.id)) {
                        tagItem.classList.add('excluded');
                    }
                    
                    listDiv.appendChild(tagItem);
                });
                groupDiv.appendChild(listDiv);
                tagsContainerEl.appendChild(groupDiv);
            }
        });
        log('Đã render tags vào DOM.');
    }

    function translateGroupName(groupKey) {
        const translations = {
            'genre': 'Thể loại',
            'theme': 'Chủ đề',
            'format': 'Định dạng',
            'content': 'Nội dung',
            'demographic': 'Đối tượng',
            'other': 'Khác'
        };
        return translations[groupKey.toLowerCase()] || groupKey;
    }

    function handleTagItemClick(event) {
        const tagItem = event.target.closest('.manga-tag-item');
        if (!tagItem) return;

        event.preventDefault();
        event.stopPropagation();

        const tagId = tagItem.dataset.tagId;
        const tagName = tagItem.querySelector('.manga-tag-name').textContent;
        debugLog(`Tag item clicked: ID=${tagId}, Name=${tagName}`);

        const isIncluded = selectedIncludeTags.has(tagId);
        const isExcluded = selectedExcludeTags.has(tagId);

        if (!isIncluded && !isExcluded) {
            // Chưa chọn -> Chọn Include
            selectedIncludeTags.set(tagId, tagName);
            tagItem.classList.add('selected');
            tagItem.classList.remove('excluded');
            debugLog(`Tag ${tagId} switched to Included.`);
        } else if (isIncluded) {
            // Đang Include -> Chuyển sang Exclude
            selectedIncludeTags.delete(tagId);
            selectedExcludeTags.set(tagId, tagName);
            tagItem.classList.remove('selected');
            tagItem.classList.add('excluded');
            debugLog(`Tag ${tagId} switched from Included to Excluded.`);
        } else { // Đang Exclude
            // Đang Exclude -> Bỏ chọn
            selectedExcludeTags.delete(tagId);
            tagItem.classList.remove('excluded');
            tagItem.classList.remove('selected'); // Đảm bảo cả selected cũng bị xóa
            debugLog(`Tag ${tagId} switched from Excluded to Unselected.`);
        }

        updateSelectedTagsUI();
        updateHiddenInputs();
    }
    
    function handleRemoveTagBadge(event) {
        const removeButton = event.target.closest('.manga-tag-remove');
        if (!removeButton) return;

        const tagBadge = removeButton.closest('.manga-tag-badge');
        const tagId = tagBadge.dataset.tagId;
        debugLog(`Remove badge clicked for Tag ID: ${tagId}`);

        // Xóa khỏi cả hai map (nếu có)
        let removedFrom = '';
        if (selectedIncludeTags.has(tagId)) {
            selectedIncludeTags.delete(tagId);
            removedFrom = 'Included';
        }
        if (selectedExcludeTags.has(tagId)) {
            selectedExcludeTags.delete(tagId);
            removedFrom = removedFrom ? `${removedFrom} and Excluded` : 'Excluded';
        }
        log(`Tag ${tagId} removed from ${removedFrom}.`);

        // Cập nhật UI của item trong dropdown (nếu dropdown đang mở)
        const tagItemInDropdown = tagsContainerEl.querySelector(`.manga-tag-item[data-tag-id="${tagId}"]`);
        if (tagItemInDropdown) {
            tagItemInDropdown.classList.remove('selected', 'excluded');
        }

        updateSelectedTagsUI();
        updateHiddenInputs();
    }

    function updateSelectedTagsUI() {
        selectedTagsDisplayEl.innerHTML = ''; // Xóa các badge cũ

        if (selectedIncludeTags.size === 0 && selectedExcludeTags.size === 0) {
            // Luôn tạo mới thông báo trống để đảm bảo nó hiển thị
            const empty = document.createElement('span');
            empty.id = 'emptyTagsMessage';
            empty.className = 'manga-tags-empty';
            empty.textContent = 'Chưa có thẻ nào được chọn. Bấm để chọn thẻ.';
            selectedTagsDisplayEl.appendChild(empty);
            emptyTagsMessageEl = empty; // Cập nhật tham chiếu
            return;
        }

        // Nếu có tag được chọn, không hiển thị thông báo trống
        selectedIncludeTags.forEach((name, id) => {
            selectedTagsDisplayEl.appendChild(createTagBadgeElement(id, name, false));
        });
        selectedExcludeTags.forEach((name, id) => {
            selectedTagsDisplayEl.appendChild(createTagBadgeElement(id, name, true));
        });
        debugLog('UI của selected/excluded tags đã được cập nhật.');
    }

    function createTagBadgeElement(id, name, isExcluded) {
        const badge = document.createElement('div');
        badge.className = 'manga-tag-badge';
        if (isExcluded) {
            badge.classList.add('excluded');
        }
        badge.dataset.tagId = id;

        const nameSpan = document.createElement('span');
        nameSpan.className = 'manga-tag-name';
        nameSpan.textContent = name;
        badge.appendChild(nameSpan);

        const removeSpan = document.createElement('span');
        removeSpan.className = 'manga-tag-remove';
        removeSpan.innerHTML = '<i class="bi bi-x"></i>';
        badge.appendChild(removeSpan);
        return badge;
    }

    function updateHiddenInputs() {
        includedTagsInputEl.value = Array.from(selectedIncludeTags.keys()).join(',');
        excludedTagsInputEl.value = Array.from(selectedExcludeTags.keys()).join(',');
        debugLog('Input ẩn đã được cập nhật. Included:', includedTagsInputEl.value, 'Excluded:', excludedTagsInputEl.value);
    }

    function handleTagSearch() {
        const searchTerm = tagSearchInputEl.value.toLowerCase().trim();
        debugLog('Đang tìm kiếm tags với từ khóa:', searchTerm);

        const tagItems = tagsContainerEl.querySelectorAll('.manga-tag-item');
        const tagGroups = tagsContainerEl.querySelectorAll('.manga-tag-group');

        tagItems.forEach(item => {
            const tagName = item.querySelector('.manga-tag-name').textContent.toLowerCase();
            if (searchTerm === '' || tagName.includes(searchTerm)) {
                item.style.display = '';
            } else {
                item.style.display = 'none';
            }
        });

        tagGroups.forEach(group => {
            // Kiểm tra xem có tag item nào trong group được hiển thị không
            const visibleItemsInGroup = group.querySelectorAll('.manga-tag-item:not([style*="display: none"])');
            if (visibleItemsInGroup.length > 0) {
                group.style.display = '';
            } else {
                group.style.display = 'none';
            }
        });
    }

    function toggleIncludedTagsMode() {
        debugLog('Toggle included tags mode.');
        if (includedTagsModeInputEl.value === 'AND') {
            includedTagsModeInputEl.value = 'OR';
            includedTagsModeTextEl.textContent = 'HOẶC';
            includedTagsModeBoxEl.classList.remove('tag-mode-and');
            includedTagsModeBoxEl.classList.add('tag-mode-or');
        } else {
            includedTagsModeInputEl.value = 'AND';
            includedTagsModeTextEl.textContent = 'VÀ';
            includedTagsModeBoxEl.classList.remove('tag-mode-or');
            includedTagsModeBoxEl.classList.add('tag-mode-and');
        }
        log('Included tags mode changed to:', includedTagsModeInputEl.value);
    }

    function toggleExcludedTagsMode() {
        debugLog('Toggle excluded tags mode.');
        if (excludedTagsModeInputEl.value === 'OR') {
            excludedTagsModeInputEl.value = 'AND';
            excludedTagsModeTextEl.textContent = 'VÀ';
            excludedTagsModeBoxEl.classList.remove('tag-mode-or');
            excludedTagsModeBoxEl.classList.add('tag-mode-and');
        } else {
            excludedTagsModeInputEl.value = 'OR';
            excludedTagsModeTextEl.textContent = 'HOẶC';
            excludedTagsModeBoxEl.classList.remove('tag-mode-and');
            excludedTagsModeBoxEl.classList.add('tag-mode-or');
        }
        log('Excluded tags mode changed to:', excludedTagsModeInputEl.value);
    }

    // --- Export hàm init để gọi từ main.js hoặc HTMX handlers ---
    window.initSearchTagsDropdown = init;

})(); 