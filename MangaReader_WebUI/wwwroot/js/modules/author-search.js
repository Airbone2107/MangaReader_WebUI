function createAuthorSearch(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const input = container.querySelector('.author-search-input');
    const resultsContainer = container.querySelector('.author-search-results');
    const selectedContainer = container.querySelector('.selected-authors-list');
    const hiddenInput = container.querySelector('input[type="hidden"]');
    const wrapper = container.querySelector('.author-search-input-wrapper');
    
    if (!input || !resultsContainer || !selectedContainer || !hiddenInput || !wrapper) return;
    
    let debounceTimer;
    const selectedItems = new Map();

    // Restore state from hidden input on initialization
    const initialIds = hiddenInput.value.split(',').filter(Boolean);
    initialIds.forEach(id => {
        // We don't have the name yet, add a placeholder
        // A better approach would be to fetch names for initial IDs
        if (!selectedItems.has(id)) {
            selectedItems.set(id, `ID: ${id.substring(0, 8)}...`);
            renderSelectedItems();
        }
    });


    function renderSelectedItems() {
        selectedContainer.innerHTML = '';
        selectedItems.forEach((name, id) => {
            const badge = document.createElement('div');
            badge.className = 'author-badge';
            badge.dataset.id = id;
            badge.innerHTML = `
                <span>${name}</span>
                <span class="remove-author-btn" role="button" title="Xóa">&times;</span>
            `;
            selectedContainer.appendChild(badge);
        });
        updateHiddenInput();
    }

    function updateHiddenInput() {
        hiddenInput.value = Array.from(selectedItems.keys()).join(',');
    }

    function hideResults() {
        resultsContainer.style.display = 'none';
        resultsContainer.innerHTML = '';
    }

    async function search(term) {
        if (term.length < 2) {
            hideResults();
            return;
        }

        resultsContainer.style.display = 'block';
        resultsContainer.innerHTML = '<div class="loading-item">Đang tìm...</div>';

        try {
            const response = await fetch(`/api/manga/search-authors?nameFilter=${encodeURIComponent(term)}`);
            if (!response.ok) throw new Error('Network response was not ok');
            
            const data = await response.json();
            resultsContainer.innerHTML = '';

            if (data.length === 0) {
                resultsContainer.innerHTML = '<div class="no-results-item">Không tìm thấy kết quả.</div>';
            } else {
                data.forEach(item => {
                    if (selectedItems.has(item.id)) return; // Don't show already selected items

                    const resultItem = document.createElement('div');
                    resultItem.className = 'author-result-item';
                    resultItem.textContent = item.name;
                    resultItem.dataset.id = item.id;
                    resultItem.dataset.name = item.name;
                    resultsContainer.appendChild(resultItem);
                });
            }
        } catch (error) {
            console.error('Lỗi tìm kiếm tác giả:', error);
            resultsContainer.innerHTML = '<div class="no-results-item">Lỗi khi tìm kiếm.</div>';
        }
    }

    input.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            search(input.value);
        }, 300);
    });

    input.addEventListener('focus', () => {
        if (input.value.length >= 2) {
            search(input.value);
        }
    });

    resultsContainer.addEventListener('click', (e) => {
        if (e.target.classList.contains('author-result-item')) {
            const { id, name } = e.target.dataset;
            if (!selectedItems.has(id)) {
                selectedItems.set(id, name);
                renderSelectedItems();
                input.value = '';
                hideResults();
                input.focus();
            }
        }
    });
    
    selectedContainer.addEventListener('click', (e) => {
        if (e.target.classList.contains('remove-author-btn')) {
            const badge = e.target.closest('.author-badge');
            const id = badge.dataset.id;
            if (selectedItems.has(id)) {
                selectedItems.delete(id);
                renderSelectedItems();
            }
        }
    });
    
    // Allow focusing the input by clicking the wrapper
    wrapper.addEventListener('click', (e) => {
        if (e.target === wrapper) {
            input.focus();
        }
    });

    document.addEventListener('click', (e) => {
        if (!container.contains(e.target)) {
            hideResults();
        }
    });
}

// Export the function to be used in other modules
export { createAuthorSearch }; 