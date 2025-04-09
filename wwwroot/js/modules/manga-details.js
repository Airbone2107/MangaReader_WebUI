/**
 * Manga details module
 * Xử lý các chức năng JavaScript cho trang chi tiết manga
 */

// Import các hàm từ các module khác
import { initTooltips } from './ui.js';

/**
 * Tính toán và điều chỉnh chiều cao cho details-manga-header-background
 * để đảm bảo hiển thị đúng với nội dung
 */
function adjustHeaderBackgroundHeight() {
    const siteHeader = document.querySelector('.site-header');
    const headerBackground = document.querySelector('.details-manga-header-background');
    const headerContainer = document.querySelector('.details-manga-header-container');
    const titleSection = document.querySelector('.details-manga-info-title');
    
    if (headerContainer && titleSection && headerBackground) {
        // Lấy vị trí của header container so với trang
        const containerRect = headerContainer.getBoundingClientRect();
        
        // Lấy vị trí và kích thước của titleSection
        const titleRect = titleSection.getBoundingClientRect();
        
        // Tính chiều cao site-header nếu có
        let siteHeaderHeight = 0;
        if (siteHeader) {
            siteHeaderHeight = siteHeader.offsetHeight;
            console.log('Chiều cao site-header: ' + siteHeaderHeight + 'px');
        }
        
        // Tính toán chiều cao cần thiết cho background:
        // 1. Vị trí đầu trang đến đầu container (bao gồm cả site-header)
        // 2. Cộng thêm chiều cao từ đầu container đến cuối titleSection
        // 3. Thêm padding để trông đẹp hơn
        const containerTop = window.scrollY + containerRect.top; // Vị trí thực của container
        const titleHeight = titleRect.height;
        const titleOffsetTop = titleRect.top - containerRect.top; // Vị trí tương đối của title trong container
        
        // Tính chiều cao background bằng site-header + khoảng cách đến hết title + padding
        const padding = 25; // thêm padding để nội dung không bị sát đáy background
        const headerHeight = containerTop + titleOffsetTop + titleHeight + padding;
        
        // Đặt chiều cao cho background
        headerBackground.style.height = headerHeight + 'px';
        
        console.log('Chiều cao đến hết title: ' + (titleOffsetTop + titleHeight) + 'px');
        console.log('Tổng chiều cao details-manga-header-background: ' + headerHeight + 'px');
    }
}

/**
 * Khởi tạo lại tất cả các dropdown một cách đơn giản hơn
 */
function initDropdowns() {
    console.log('Khởi tạo lại tất cả dropdown...');
    
    // Xóa bỏ tất cả event listener cũ bằng cách clone các phần tử
    document.querySelectorAll('.custom-language-header, .custom-volume-header').forEach(header => {
        const clone = header.cloneNode(true);
        header.parentNode.replaceChild(clone, header);
    });
    
    // Thêm lại event listener cho language headers
    document.querySelectorAll('.custom-language-header').forEach(header => {
        header.addEventListener('click', function(e) {
            e.stopPropagation();
            const langId = this.getAttribute('data-lang-id');
            console.log('Click vào language header:', langId);
            this.classList.toggle('active');
        });
    });
    
    // Thêm lại event listener cho volume headers
    document.querySelectorAll('.custom-volume-header').forEach(header => {
        header.addEventListener('click', function(e) {
            e.stopPropagation();
            const volumeId = this.getAttribute('data-volume-id');
            console.log('Click vào volume header:', volumeId);
            this.classList.toggle('active');
        });
    });
    
    // Mở ngôn ngữ đầu tiên theo mặc định
    const firstLangHeader = document.querySelector('.custom-language-header');
    if (firstLangHeader && !firstLangHeader.classList.contains('active')) {
        firstLangHeader.classList.add('active');
        
        // Khi mở ngôn ngữ đầu tiên, cũng mở luôn volume đầu tiên
        const firstVolumeHeader = document.querySelector('.custom-volume-header');
        if (firstVolumeHeader && !firstVolumeHeader.classList.contains('active')) {
            firstVolumeHeader.classList.add('active');
        }
        
        // Mở tất cả các volume trong ngôn ngữ đầu tiên
        const langId = firstLangHeader.getAttribute('data-lang-id');
        const firstLangContent = document.querySelector(`#lang-content-${langId}`);
        if (firstLangContent) {
            const firstLangVolumeHeaders = firstLangContent.querySelectorAll('.custom-volume-header');
            firstLangVolumeHeaders.forEach(volHeader => {
                if (!volHeader.classList.contains('active')) {
                    volHeader.classList.add('active');
                }
            });
            console.log('Đã mở tất cả volume của ngôn ngữ đầu tiên:', firstLangVolumeHeaders.length, 'volume');
        } else {
            console.log('Không tìm thấy phần nội dung của ngôn ngữ đầu tiên!');
        }
    }
}

/**
 * Khởi tạo xử lý bộ lọc ngôn ngữ
 */
function initLanguageFilter() {
    // Lấy tất cả các nút lọc ngôn ngữ
    const filterButtons = document.querySelectorAll('.language-filter-btn');
    
    console.log('Tìm thấy', filterButtons.length, 'nút lọc ngôn ngữ');
    
    // Xóa tất cả event listeners cũ nếu có
    filterButtons.forEach(button => {
        const newButton = button.cloneNode(true);
        button.parentNode.replaceChild(newButton, button);
    });
    
    // Thêm lại event listeners
    document.querySelectorAll('.language-filter-btn').forEach(button => {
        button.addEventListener('click', function() {
            // Xóa class active khỏi tất cả các nút
            document.querySelectorAll('.language-filter-btn').forEach(btn => {
                btn.classList.remove('active');
            });
            
            // Thêm class active vào nút được click
            this.classList.add('active');
            
            // Lấy ngôn ngữ cần lọc
            const lang = this.getAttribute('data-lang');
            console.log('Đã chọn lọc ngôn ngữ:', lang);
            
            // Lấy tất cả các phần ngôn ngữ
            const languageSections = document.querySelectorAll('.custom-language-section');
            
            // Hiển thị/ẩn các phần ngôn ngữ dựa trên ngôn ngữ đã chọn
            languageSections.forEach(section => {
                const sectionLang = section.getAttribute('data-language');
                
                if (lang === 'all' || lang === sectionLang) {
                    section.style.display = 'block';
                    
                    // Mở dropdown ngôn ngữ được chọn nếu chỉ hiển thị một ngôn ngữ
                    if (lang !== 'all') {
                        const header = section.querySelector('.custom-language-header');
                        if (header && !header.classList.contains('active')) {
                            header.classList.add('active');
                        }
                    }
                } else {
                    section.style.display = 'none';
                }
            });
        });
    });
}

/**
 * Khởi tạo xử lý cho chapter items
 */
function initChapterItems() {
    // Không cần thêm event listener cho chapter items vì đã là thẻ <a>
    // Nhưng ta có thể thêm logic để đánh dấu các chapter đã đọc hoặc xử lý khác
    console.log('Đã khởi tạo chapter items');
}

/**
 * Khởi tạo nút theo dõi/hủy theo dõi manga
 */
function initFollowButton() {
    const followBtn = document.getElementById('followBtn');
    if (followBtn) {
        // Xóa event listener cũ (nếu có) bằng cách clone nút
        const newFollowBtn = followBtn.cloneNode(true);
        followBtn.parentNode.replaceChild(newFollowBtn, followBtn);
        
        // Thêm lại event listener
        newFollowBtn.addEventListener('click', function() {
            toggleFollow(this);
        });
        
        console.log('Đã khởi tạo nút theo dõi/hủy theo dõi');
    }
}

/**
 * Thay đổi trạng thái theo dõi manga
 */
function toggleFollow(button) {
    const mangaId = button.getAttribute('data-id');
    // Chúng ta không cần isFollowing ở đây nữa,
    // vì proxy sẽ xác định hành động, nhưng giữ lại để UI được chủ động nếu muốn.
    // const isCurrentlyFollowing = button.getAttribute('data-following') === 'true';

    // Sử dụng endpoint proxy duy nhất
    const endpoint = '/api/proxy/toggle-follow';

    // Hiển thị trạng thái đang xử lý
    const originalContent = button.innerHTML;
    button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...';
    button.disabled = true;

    fetch(endpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // ** KHÔNG cần header Authorization ở đây - C# proxy xử lý nó **
        },
        body: JSON.stringify({ mangaId: mangaId }) // Gửi mangaId trong phần thân
    })
    .then(response => {
        // Kiểm tra unauthorized trước (được trả về bởi proxy)
        if (response.status === 401) {
             // Sử dụng showToast toàn cục nếu đã định nghĩa, nếu không thì alert
             const toastFunc = window.showToast || alert;
             toastFunc('Lỗi', 'Vui lòng đăng nhập để thực hiện thao tác này.', 'error');
             setTimeout(() => {
                 window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
             }, 1500);
             return Promise.reject({ status: 401, message: 'Unauthorized' }); // Từ chối với status
        }
        // Kiểm tra nếu response là ok, nếu không thì parse JSON lỗi
        if (!response.ok) {
            // Thử phân tích JSON lỗi từ proxy
            return response.json().then(errorData => Promise.reject(errorData || { message: `Lỗi ${response.status}` }));
        }
        return response.json();
    })
    .then(data => {
        // Khôi phục nút
        button.disabled = false;
        const toastFunc = window.showToast || alert; // Sử dụng hàm toast toàn cục

        if (data.success) {
            // Cập nhật UI dựa trên trạng thái MỚI NHẬN ĐƯỢC từ proxy
            const newFollowingState = data.isFollowing;
            button.setAttribute('data-following', newFollowingState.toString().toLowerCase());

            if (newFollowingState) {
                button.innerHTML = '<i class="bi bi-bookmark-check-fill me-2"></i><span>Đang theo dõi</span>';
                toastFunc('Thành công', data.message || 'Đã theo dõi truyện', 'success');
            } else {
                button.innerHTML = '<i class="bi bi-bookmark-plus me-2"></i><span>Theo dõi</span>';
                toastFunc('Thành công', data.message || 'Đã hủy theo dõi truyện', 'success');
            }
        } else {
            // Khôi phục nút về trạng thái ban đầu
            button.innerHTML = originalContent;
            // Hiển thị thông báo lỗi
            toastFunc('Lỗi', data.message || 'Không thể cập nhật trạng thái theo dõi', 'error');
        }
    })
    .catch(error => {
        // Xử lý lỗi mạng hoặc lỗi phân tích JSON hoặc các promise bị từ chối
        button.innerHTML = originalContent;
        button.disabled = false;
        const toastFunc = window.showToast || alert; // Sử dụng hàm toast toàn cục

        if (error && error.status === 401) {
            // Đã xử lý chuyển hướng, chỉ ghi log
            console.log("Phát hiện truy cập không được ủy quyền.");
        } else {
            console.error('Lỗi khi toggle follow:', error);
            const errorMessage = (error && error.message) ? error.message : 'Đã xảy ra lỗi mạng hoặc lỗi xử lý. Vui lòng thử lại sau.';
            toastFunc('Lỗi', errorMessage, 'error');
        }
    });
}

/**
 * Hiển thị toast thông báo
 */
function showToast(title, message, type) {
    // Kiểm tra nếu Bootstrap đã được khởi tạo
    if (typeof bootstrap === 'undefined') {
        console.error('Bootstrap chưa được khởi tạo');
        alert(message);
        return;
    }
    
    // Tạo toast nếu chưa có
    let toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        toastContainer.style.zIndex = '11';
        document.body.appendChild(toastContainer);
    }
    
    // Tạo phần tử toast
    const toastId = 'toast_' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header ${type === 'success' ? 'bg-success text-white' : type === 'error' ? 'bg-danger text-white' : 'bg-info text-white'}">
                <i class="bi ${type === 'success' ? 'bi-check-circle' : type === 'error' ? 'bi-exclamation-triangle' : 'bi-info-circle'} me-2"></i>
                <strong class="me-auto">${title}</strong>
                <button type="button" class="btn-close ${type === 'success' || type === 'error' || type === 'info' ? 'btn-close-white' : ''}" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    // Thêm toast vào container
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    // Lấy phần tử toast
    const toastElement = document.getElementById(toastId);
    
    // Tạo đối tượng Toast của Bootstrap
    const toast = new bootstrap.Toast(toastElement, { delay: 3000 });
    
    // Hiển thị toast
    toast.show();
    
    // Xóa toast sau khi ẩn
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

/**
 * Khởi tạo tất cả chức năng liên quan đến chi tiết manga
 */
function initMangaDetailsPage() {
    // Kiểm tra xem đang ở trang chi tiết manga không trước khi khởi tạo
    if (document.querySelector('.details-manga-header-background')) {
        // Khởi tạo tooltips
        initTooltips();
        
        // Khởi tạo tất cả dropdown
        initDropdowns();
        
        // Khởi tạo bộ lọc ngôn ngữ
        initLanguageFilter();
        
        // Khởi tạo xử lý cho chapter items
        initChapterItems();
        
        // Khởi tạo nút theo dõi/hủy theo dõi
        initFollowButton();
        
        // Gọi hàm khi trang tải xong và khi cửa sổ thay đổi kích thước
        // Đợi một chút để đảm bảo các element đã được render đầy đủ
        setTimeout(adjustHeaderBackgroundHeight, 100);
        
        // Chỉ thêm event listener cho resize, loại bỏ scroll listener vì không cần thiết
        window.addEventListener('resize', adjustHeaderBackgroundHeight);
        
        console.log('Đã khởi tạo trang chi tiết manga');
    }
}

/**
 * Khởi tạo lại các tính năng sau khi HTMX tải nội dung
 */
function initAfterHtmxLoad() {
    // Khởi tạo lại tất cả dropdown
    initDropdowns();
    
    // Khởi tạo lại bộ lọc ngôn ngữ
    initLanguageFilter();
    
    // Khởi tạo lại xử lý cho chapter items
    initChapterItems();
    
    // Khởi tạo lại nút theo dõi/hủy theo dõi
    initFollowButton();
    
    // Điều chỉnh lại chiều cao background
    setTimeout(adjustHeaderBackgroundHeight, 100);
}

// Export các hàm để có thể sử dụng ở file khác
export { 
    adjustHeaderBackgroundHeight, 
    initMangaDetailsPage, 
    initAfterHtmxLoad, 
    initDropdowns, 
    initChapterItems,
    initFollowButton,
    toggleFollow,
    showToast 
}; 