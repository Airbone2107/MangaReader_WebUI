/**
 * sidebar.js - Quản lý tất cả chức năng liên quan đến sidebar
 */

/**
 * Cập nhật trạng thái active cho các liên kết trong sidebar dựa vào URL hiện tại
 */
function updateActiveSidebarLink() {
    const currentPath = window.location.pathname;
    const sidebarLinks = document.querySelectorAll('.sidebar-nav-link');
    
    // Xóa active class từ tất cả các liên kết
    sidebarLinks.forEach(link => link.classList.remove('active'));
    
    // Thêm active class cho liên kết tương ứng với trang hiện tại
    sidebarLinks.forEach(link => {
        const linkPath = link.getAttribute('href');
        if (linkPath) {
            // Xác định chính xác các điều kiện để đánh dấu link active
            const isExactMatch = linkPath === currentPath;
            const isIndexAction = (currentPath === '/Home' || currentPath === '/') && 
                                 (linkPath === '/' || linkPath === '/Home' || linkPath === '/Home/Index');
            const isMangaLink = linkPath === '/Manga' && currentPath === '/Manga';
            const isSubdirectory = currentPath.startsWith(linkPath + '/') && 
                                  linkPath !== '/' && linkPath !== '/Home';
            
            if (isExactMatch || isIndexAction || isMangaLink || isSubdirectory) {
                link.classList.add('active');
            }
        }
    });
}

/**
 * Khởi tạo sidebar menu và xử lý đồng bộ theme
 */
function initSidebar() {
    const sidebar = document.getElementById('sidebarMenu');
    const sidebarToggler = document.getElementById('sidebarToggler');
    const closeSidebarBtn = document.getElementById('closeSidebar');
    const sidebarBackdrop = document.getElementById('sidebarBackdrop');
    const siteHeader = document.querySelector('.site-header');
    
    // Đánh dấu menu item active dựa trên URL hiện tại
    updateActiveSidebarLink();
    
    // Hàm lưu trạng thái sidebar
    function saveSidebarState(state) {
        localStorage.setItem('sidebarState', state);
    }
    
    // Hàm lấy trạng thái sidebar
    function getSidebarState() {
        return localStorage.getItem('sidebarState') || 'closed';
    }
    
    // Hàm mở sidebar
    function openSidebar() {
        document.body.classList.add('sidebar-open');
        saveSidebarState('open');
        
        // Xử lý responsive
        if (window.innerWidth < 992) {
            document.body.classList.add('mobile-sidebar');
            if (sidebarBackdrop) {
                sidebarBackdrop.style.display = 'block';
                // Thêm một chút delay trước khi hiển thị backdrop để tạo hiệu ứng mượt mà
                setTimeout(() => {
                    sidebarBackdrop.classList.add('visible');
                }, 10);
            }
        } else {
            // Khi mở sidebar trên desktop, thêm hiệu ứng mở rộng cho thanh tìm kiếm ngay lập tức
            const searchContainer = document.querySelector('.search-container');
            if (searchContainer) {
                searchContainer.classList.add('search-expanded');
            }
        }
    }
    
    // Hàm đóng sidebar
    function closeSidebar() {
        document.body.classList.remove('sidebar-open');
        document.body.classList.remove('mobile-sidebar');
        saveSidebarState('closed');
        
        if (sidebarBackdrop) {
            sidebarBackdrop.classList.remove('visible');
            // Đợi hiệu ứng fade out hoàn tất trước khi ẩn hoàn toàn
            setTimeout(() => {
                sidebarBackdrop.style.display = 'none';
            }, 300);
        }
        
        // Khi đóng sidebar, loại bỏ class mở rộng của thanh tìm kiếm
        const searchContainer = document.querySelector('.search-container');
        if (searchContainer) {
            searchContainer.classList.remove('search-expanded');
        }
    }
    
    // Xử lý trạng thái ban đầu
    const initialState = getSidebarState();
    if (initialState === 'open' && window.innerWidth >= 992) {
        openSidebar();
    }
    
    // Xử lý sự kiện click cho nút sidebar-toggler
    if (sidebarToggler) {
        sidebarToggler.addEventListener('click', function(e) {
            e.preventDefault();
            openSidebar();
        });
    }
    
    // Xử lý sự kiện click cho nút đóng sidebar
    if (closeSidebarBtn) {
        closeSidebarBtn.addEventListener('click', function() {
            closeSidebar();
        });
    }
    
    // Xử lý click backdrop để đóng sidebar
    if (sidebarBackdrop) {
        sidebarBackdrop.addEventListener('click', function() {
            closeSidebar();
        });
    }
    
    // Đóng sidebar khi nhấn phím Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && document.body.classList.contains('sidebar-open')) {
            closeSidebar();
        }
    });
    
    // Xử lý header ẩn hiện khi cuộn
    let lastScrollTop = 0;
    let scrollThreshold = 100; // Ngưỡng cuộn để ẩn header
    
    window.addEventListener('scroll', function() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        
        // Nếu đang cuộn xuống và đã cuộn quá ngưỡng
        if (scrollTop > lastScrollTop && scrollTop > scrollThreshold) {
            siteHeader.classList.add('header-hidden');
        }
        // Nếu đang cuộn lên hoặc ở đầu trang
        else if (scrollTop < lastScrollTop || scrollTop <= scrollThreshold) {
            siteHeader.classList.remove('header-hidden');
        }
        
        lastScrollTop = scrollTop;
    });
    
    // Xử lý responsive sidebar
    function handleResize() {
        const isMobileScreen = window.innerWidth < 992;
        
        if (isMobileScreen && document.body.classList.contains('sidebar-open')) {
            document.body.classList.add('mobile-sidebar');
            if (sidebarBackdrop) sidebarBackdrop.style.display = 'block';
        } else if (!isMobileScreen) {
            document.body.classList.remove('mobile-sidebar');
            if (sidebarBackdrop) sidebarBackdrop.style.display = 'none';
            
            // Khôi phục trạng thái sidebar dựa trên localStorage nếu ở desktop
            const savedState = getSidebarState();
            if (savedState === 'open' && !document.body.classList.contains('sidebar-open')) {
                document.body.classList.add('sidebar-open');
            }
        }
    }
    
    window.addEventListener('resize', handleResize);
    handleResize(); // Gọi lần đầu khi tải trang
    
    // Lưu trạng thái sidebar vào localStorage khi thay đổi
    const observeSidebarState = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.attributeName === 'class') {
                const isSidebarOpen = document.body.classList.contains('sidebar-open');
                localStorage.setItem('sidebarState', isSidebarOpen ? 'open' : 'closed');
            }
        });
    });
    observeSidebarState.observe(document.body, { attributes: true });
}

// Export các hàm để sử dụng trong các module khác
export { updateActiveSidebarLink, initSidebar }; 