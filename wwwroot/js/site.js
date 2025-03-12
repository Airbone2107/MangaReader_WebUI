/**
 * Manga Reader Web - JavaScript chung
 * Chứa các chức năng JavaScript chung cho toàn bộ ứng dụng
 */

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * Khởi tạo khi DOM đã sẵn sàng
 */
document.addEventListener('DOMContentLoaded', function() {
    // Xóa bỏ inline style của các active nav-link
    cleanupActiveLinks();
    
    // Khởi tạo tooltips
    initTooltips();
    
    // Khởi tạo lazy loading cho hình ảnh
    initLazyLoading();
    
    // Khởi tạo chức năng tìm kiếm nhanh
    initQuickSearch();
    
    // Tạo ảnh mặc định nếu chưa có
    createDefaultImage();
    
    // Khởi tạo chức năng hiển thị thông báo
    initToasts();
    
    // Khởi tạo chức năng lưu trạng thái đọc
    initReadingState();
    
    // Khởi tạo chức năng chuyển đổi chế độ tối/sáng
    initThemeSwitcher();
    
    // Khởi tạo nút back-to-top
    initBackToTop();
    
    // Khởi tạo chức năng xử lý lỗi
    initErrorHandling();
    
    // Khởi tạo chức năng responsive
    initResponsive();
    
    // Khắc phục vấn đề với accordion
    fixAccordionIssues();
    
    // Tự động điều chỉnh vị trí footer
    adjustFooterPosition();
    
    // Khởi tạo sidebar menu
    initSidebar();
});

/**
 * Xóa bỏ inline style của các nav-link active
 */
function cleanupActiveLinks() {
    document.querySelectorAll('.nav-link.active, .sidebar-nav-link.active').forEach(link => {
        // Xóa bỏ thuộc tính style color inline nếu có
        if (link.style.color) {
            link.style.removeProperty('color');
        }
    });
}

/**
 * Khởi tạo tooltips cho các phần tử có thuộc tính data-bs-toggle="tooltip"
 */
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Khởi tạo lazy loading cho hình ảnh
 */
function initLazyLoading() {
    // Kiểm tra nếu trình duyệt hỗ trợ Intersection Observer
    if ('IntersectionObserver' in window) {
        const lazyImages = document.querySelectorAll('img[loading="lazy"]');
        
        const imageObserver = new IntersectionObserver(function(entries, observer) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    const lazyImage = entry.target;
                    lazyImage.src = lazyImage.dataset.src || lazyImage.src;
                    lazyImage.classList.remove('lazy');
                    imageObserver.unobserve(lazyImage);
                }
            });
        });
        
        lazyImages.forEach(function(lazyImage) {
            imageObserver.observe(lazyImage);
        });
    }
}

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
 * Hiển thị thông báo toast
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại thông báo (success, danger, warning, info)
 * @param {number} duration - Thời gian hiển thị (ms)
 */
function showToast(message, type = 'primary', duration = 3000) {
    // Tạo toast container nếu chưa tồn tại
    if (!document.getElementById('toastContainer')) {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
    }
    
    // Tạo toast
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    // Thêm toast vào container
    document.getElementById('toastContainer').appendChild(toast);
    
    // Hiển thị toast
    const bsToast = new bootstrap.Toast(toast, {
        delay: duration
    });
    bsToast.show();
    
    // Tự động loại bỏ toast sau khi ẩn
    toast.addEventListener('hidden.bs.toast', function() {
        this.remove();
    });
}

/**
 * Khởi tạo chức năng hiển thị thông báo
 */
function initToasts() {
    // Tạo hàm global để các trang có thể sử dụng
    window.showToast = showToast;
}

/**
 * Lưu trạng thái đọc của người dùng
 * @param {string} mangaId - ID của manga
 * @param {string} chapterId - ID của chapter
 * @param {number} page - Trang hiện tại
 */
function saveReadingState(mangaId, chapterId, page) {
    if (!mangaId || !chapterId) return;
    
    const readingState = {
        mangaId: mangaId,
        chapterId: chapterId,
        page: page || 1,
        timestamp: Date.now()
    };
    
    // Lưu trạng thái đọc vào localStorage
    localStorage.setItem(`reading_${mangaId}`, JSON.stringify(readingState));
    
    // Lưu vào danh sách đã đọc gần đây
    const recentlyRead = JSON.parse(localStorage.getItem('recently_read') || '[]');
    
    // Kiểm tra nếu manga đã tồn tại trong danh sách
    const existingIndex = recentlyRead.findIndex(item => item.mangaId === mangaId);
    if (existingIndex !== -1) {
        // Xóa manga khỏi vị trí cũ
        recentlyRead.splice(existingIndex, 1);
    }
    
    // Thêm manga vào đầu danh sách
    recentlyRead.unshift(readingState);
    
    // Giới hạn danh sách chỉ lưu 20 manga gần nhất
    if (recentlyRead.length > 20) {
        recentlyRead.pop();
    }
    
    // Lưu danh sách cập nhật
    localStorage.setItem('recently_read', JSON.stringify(recentlyRead));
}

/**
 * Lấy trạng thái đọc của người dùng
 * @param {string} mangaId - ID của manga
 * @returns {Object|null} - Trạng thái đọc hoặc null nếu không tìm thấy
 */
function getReadingState(mangaId) {
    if (!mangaId) return null;
    
    const readingState = localStorage.getItem(`reading_${mangaId}`);
    return readingState ? JSON.parse(readingState) : null;
}

/**
 * Khởi tạo chức năng lưu trạng thái đọc
 */
function initReadingState() {
    // Tạo hàm global để các trang có thể sử dụng
    window.saveReadingState = saveReadingState;
    window.getReadingState = getReadingState;
}

/**
 * Lưu chủ đề hiện tại vào localStorage
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 */
function saveTheme(theme) {
    localStorage.setItem('theme', theme);
}

/**
 * Áp dụng chủ đề cho trang web
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 */
function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
    
    // Cập nhật trạng thái nút chuyển đổi
    const themeSwitch = document.getElementById('themeSwitch');
    const themeText = document.getElementById('themeText');
    
    if (themeSwitch && themeText) {
        if (theme === 'dark') {
            themeSwitch.checked = true;
            themeText.innerHTML = '<i class="bi bi-sun me-2"></i>Chế độ sáng';
        } else {
            themeSwitch.checked = false;
            themeText.innerHTML = '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
        }
    }
}

/**
 * Khởi tạo chức năng chuyển đổi chế độ tối/sáng
 */
function initThemeSwitcher() {
    const themeSwitch = document.getElementById('themeSwitch');
    const sidebarThemeSwitch = document.getElementById('sidebarThemeSwitch');
    const themeText = document.getElementById('themeText');
    const sidebarThemeText = document.getElementById('sidebarThemeText');
    const htmlElement = document.documentElement;

    // Kiểm tra theme đã lưu
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
        htmlElement.setAttribute('data-bs-theme', savedTheme);
        updateSwitches(savedTheme === 'dark');
    }

    // Cập nhật trạng thái các switcher
    function updateSwitches(isDark) {
        if (themeSwitch) themeSwitch.checked = isDark;
        if (sidebarThemeSwitch) sidebarThemeSwitch.checked = isDark;
        
        // Cập nhật text
        if (themeText) {
            themeText.innerHTML = isDark ? 
                '<i class="bi bi-sun me-2"></i>Chế độ sáng' : 
                '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
        }
        
        if (sidebarThemeText) {
            sidebarThemeText.innerHTML = isDark ? 
                '<i class="bi bi-sun me-2"></i>Chế độ sáng' : 
                '<i class="bi bi-moon-stars me-2"></i>Chế độ tối';
        }
        
        // Xóa bỏ toàn bộ inline style color trên các nav-link active
        cleanupActiveLinks();
    }

    // Hàm thay đổi theme
    function changeTheme(isDark, showNotification = true) {
        const theme = isDark ? 'dark' : 'light';
        
        // Thiết lập theme
        htmlElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('theme', theme);
        
        // Cập nhật UI
        updateSwitches(isDark);
        
        // Hiển thị thông báo nếu cần
        if (showNotification) {
            showToast(`Đã chuyển sang chế độ ${theme === 'dark' ? 'tối' : 'sáng'}!`, 'info');
        }
    }

    // Đăng ký sự kiện cho theme switch chính
    if (themeSwitch) {
        themeSwitch.addEventListener('change', function() {
            changeTheme(this.checked);
        });
    }
    
    // Đăng ký sự kiện cho sidebar theme switch
    if (sidebarThemeSwitch) {
        sidebarThemeSwitch.addEventListener('change', function() {
            changeTheme(this.checked);
        });
    }
    
    // Đánh dấu theme switcher container khi hover
    const themeSwitcherContainer = document.getElementById('themeSwitcher');
    const sidebarThemeSwitcherContainer = document.getElementById('sidebarThemeSwitcher');
    
    if (themeSwitcherContainer) {
        themeSwitcherContainer.addEventListener('mouseenter', function() {
            this.classList.add('active');
        });
        
        themeSwitcherContainer.addEventListener('mouseleave', function() {
            this.classList.remove('active');
        });
    }
    
    if (sidebarThemeSwitcherContainer) {
        sidebarThemeSwitcherContainer.addEventListener('mouseenter', function() {
            this.classList.add('active');
        });
        
        sidebarThemeSwitcherContainer.addEventListener('mouseleave', function() {
            this.classList.remove('active');
        });
    }
    
    // Khởi chạy lần đầu tiên mà không hiển thị thông báo
    changeTheme(savedTheme === 'dark', false);
    
    // Thêm phương thức toàn cục để các phần khác có thể sử dụng
    window.changeTheme = changeTheme;
}

/**
 * Khởi tạo nút back-to-top
 */
function initBackToTop() {
    const backToTopBtn = document.getElementById('backToTopBtn');
    if (!backToTopBtn) return;
    
    // Hiển thị nút khi cuộn xuống một khoảng cách nhất định
    window.addEventListener('scroll', function() {
        if (window.pageYOffset > 300) {
            backToTopBtn.classList.remove('d-none');
            backToTopBtn.classList.add('d-flex', 'justify-content-center', 'align-items-center');
        } else {
            backToTopBtn.classList.remove('d-flex', 'justify-content-center', 'align-items-center');
            backToTopBtn.classList.add('d-none');
        }
    });
    
    // Xử lý sự kiện khi click nút
    backToTopBtn.addEventListener('click', function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

/**
 * Khởi tạo chức năng xử lý lỗi
 */
function initErrorHandling() {
    // Kiểm tra và hiển thị thông báo lỗi từ API
    const errorContainer = document.querySelector('.api-error-container');
    if (errorContainer) {
        const retryButton = errorContainer.querySelector('.retry-button');
        if (retryButton) {
            retryButton.addEventListener('click', function() {
                window.location.reload();
            });
        }
    }

    // Xử lý lỗi khi tải hình ảnh
    document.querySelectorAll('img').forEach(img => {
        img.addEventListener('error', function() {
            this.src = '/images/no-cover.png';
        });
    });

    // Thêm event listener cho nút reconnect API
    const reconnectButtons = document.querySelectorAll('.reconnect-api');
    reconnectButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            showToast('Đang kết nối lại...', 'info');
            
            // Gửi request kiểm tra API
            fetch('/Home/ApiTest')
                .then(response => {
                    if (response.ok) {
                        showToast('Kết nối thành công!', 'success');
                        setTimeout(() => window.location.reload(), 1000);
                    } else {
                        showToast('Không thể kết nối đến API', 'error');
                    }
                })
                .catch(() => {
                    showToast('Không thể kết nối đến API', 'error');
                });
        });
    });
}

/**
 * Khởi tạo các chức năng responsive
 */
function initResponsive() {
    // Đóng navbar khi bấm vào liên kết (trên mobile)
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    if (navLinks && navbarToggler && navbarCollapse) {
        navLinks.forEach(function(link) {
            link.addEventListener('click', function() {
                if (window.innerWidth < 992 && navbarCollapse.classList.contains('show')) {
                    // Sử dụng Bootstrap API để đóng navbar
                    bootstrap.Collapse.getInstance(navbarCollapse).hide();
                }
            });
        });
    }
    
    // Xử lý sự kiện cuộn trang
    let lastScrollTop = 0;
    const navbar = document.querySelector('.navbar');
    
    if (navbar) {
        window.addEventListener('scroll', function() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            
            // Thêm box-shadow khi cuộn xuống
            if (scrollTop > 10) {
                navbar.classList.add('navbar-scrolled');
            } else {
                navbar.classList.remove('navbar-scrolled');
            }
            
            // Ẩn/hiện navbar khi cuộn (chỉ khi ở chế độ sticky)
            if (window.getComputedStyle(document.querySelector('.site-header')).position === 'sticky') {
                if (scrollTop > lastScrollTop && scrollTop > 150) {
                    // Cuộn xuống và đã cuộn quá 150px
                    navbar.classList.add('navbar-hidden');
                } else {
                    // Cuộn lên
                    navbar.classList.remove('navbar-hidden');
                }
            }
            
            lastScrollTop = scrollTop;
            
            // Kiểm tra footer khi cuộn
            adjustFooterPosition();
        });
    }
    
    // Kiểm tra lại vị trí footer khi cửa sổ thay đổi kích thước
    window.addEventListener('resize', adjustFooterPosition);
}

/**
 * Khắc phục vấn đề với accordion
 */
function fixAccordionIssues() {
    // Điều chỉnh z-index cho các thành phần accordion
    const accordions = document.querySelectorAll('.accordion');
    
    accordions.forEach(function(accordion) {
        // Lắng nghe sự kiện khi accordion được mở
        accordion.addEventListener('shown.bs.collapse', function(event) {
            const collapsedElement = event.target;
            
            // Đảm bảo rằng phần tử mở ra hiển thị đầy đủ
            setTimeout(function() {
                // Cuộn đến phần tử đã mở nếu nó bị che khuất
                const headerHeight = document.querySelector('.site-header').offsetHeight || 0;
                const elementTop = collapsedElement.getBoundingClientRect().top;
                
                if (elementTop < headerHeight + 20) {
                    window.scrollBy({
                        top: elementTop - headerHeight - 20,
                        behavior: 'smooth'
                    });
                }
                
                // Kiểm tra xem phần tử có bị che khuất bởi footer không
                const elementBottom = collapsedElement.getBoundingClientRect().bottom;
                const windowHeight = window.innerHeight;
                const footerHeight = document.querySelector('.site-footer').offsetHeight || 0;
                
                if (elementBottom > windowHeight - footerHeight - 20) {
                    // Điều chỉnh cuộn để hiển thị đầy đủ phần tử mở
                    collapsedElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'center'
                    });
                }
            }, 350); // Đợi để animation hoàn thành
        });
    });
    
    // Đặc biệt xử lý cho accordion trong chương
    const chapterAccordions = document.querySelectorAll('.chapters-container .accordion');
    
    chapterAccordions.forEach(function(accordion) {
        const buttons = accordion.querySelectorAll('.accordion-button');
        
        buttons.forEach(function(button) {
            button.addEventListener('click', function() {
                // Đảm bảo rằng không có phần tử bị che khuất
                setTimeout(function() {
                    const isExpanded = button.getAttribute('aria-expanded') === 'true';
                    
                    if (isExpanded) {
                        const headerHeight = document.querySelector('.site-header').offsetHeight || 0;
                        const buttonRect = button.getBoundingClientRect();
                        
                        if (buttonRect.top < headerHeight) {
                            window.scrollBy({
                                top: buttonRect.top - headerHeight - 20,
                                behavior: 'smooth'
                            });
                        }
                    }
                }, 350);
            });
        });
    });
    
    // Đảm bảo footer luôn ở dưới cùng
    adjustFooterPosition();
    window.addEventListener('resize', adjustFooterPosition);
    document.addEventListener('DOMContentLoaded', adjustFooterPosition);
}

/**
 * Tự động điều chỉnh vị trí footer
 */
function adjustFooterPosition() {
    const content = document.querySelector('.content-wrapper');
    const footer = document.querySelector('.site-footer');
    
    if (!content || !footer) return;
    
    function adjust() {
        const windowHeight = window.innerHeight;
        const contentHeight = content.getBoundingClientRect().height;
        
        // Đảm bảo footer luôn nằm dưới nội dung
        if (contentHeight < windowHeight - footer.offsetHeight) {
            footer.style.marginTop = 'auto';
            content.style.minHeight = `calc(100vh - ${footer.offsetHeight}px)`;
        } else {
            footer.style.marginTop = '0';
        }
    }
    
    // Điều chỉnh ngay khi trang tải xong
    adjust();
    
    // Điều chỉnh khi thay đổi kích thước cửa sổ
    window.addEventListener('resize', adjust);
    
    // Điều chỉnh khi nội dung trang thay đổi
    window.addEventListener('load', adjust);
    document.addEventListener('DOMContentLoaded', adjust);
}

/**
 * Tạo ảnh mặc định nếu không tìm thấy
 */
function createDefaultImage() {
    // Kiểm tra xem ảnh mặc định đã tồn tại trong localStorage chưa
    if (!localStorage.getItem('defaultCoverImage')) {
        // Tạo canvas để vẽ ảnh mặc định
        const canvas = document.createElement('canvas');
        canvas.width = 320;
        canvas.height = 450;
        const ctx = canvas.getContext('2d');
        
        // Vẽ nền
        ctx.fillStyle = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? '#2c2c2c' : '#f8f9fa';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Vẽ biểu tượng sách
        ctx.fillStyle = '#6c757d';
        ctx.font = 'bold 100px Bootstrap-icons';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('\uf02d', canvas.width / 2, canvas.height / 2 - 50);
        
        // Vẽ text
        ctx.fillStyle = '#6c757d';
        ctx.font = 'bold 24px Arial, sans-serif';
        ctx.fillText('No Cover', canvas.width / 2, canvas.height / 2 + 50);
        ctx.font = '18px Arial, sans-serif';
        ctx.fillText('Image Not Available', canvas.width / 2, canvas.height / 2 + 90);
        
        // Lưu ảnh vào localStorage
        try {
            localStorage.setItem('defaultCoverImage', canvas.toDataURL('image/png'));
        } catch (e) {
            console.error('Không thể lưu ảnh mặc định vào localStorage:', e);
        }
    }
}

/**
 * Khởi tạo sidebar menu và xử lý đồng bộ theme
 */
function initSidebar() {
    const sidebar = document.getElementById('sidebarMenu');
    const sidebarToggler = document.querySelector('.sidebar-toggler');
    const closeSidebarBtn = document.getElementById('closeSidebar');
    
    // Đánh dấu menu item active dựa trên URL hiện tại
    const currentUrl = window.location.pathname;
    const navLinks = document.querySelectorAll('.offcanvas-body .nav-link');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href) {
            // Xác định chính xác các điều kiện để đánh dấu link active
            const isExactMatch = href === currentUrl;
            const isIndexAction = (currentUrl === '/Home' || currentUrl === '/') && (href === '/' || href === '/Home' || href === '/Home/Index');
            const isMangaLink = href === '/Manga' && currentUrl === '/Manga';
            const isSubdirectory = currentUrl.startsWith(href + '/') && href !== '/' && href !== '/Home';
            
            if (isExactMatch || isIndexAction || isMangaLink || isSubdirectory) {
                link.classList.add('active');
                
                // Xóa bỏ inline style nếu có
                if (link.style.color) {
                    link.style.removeProperty('color');
                }
                
                // Nếu link thuộc về submenu, mở rộng parent
                const parent = link.closest('.collapse');
                if (parent) {
                    parent.classList.add('show');
                    const toggle = document.querySelector(`[data-bs-target="#${parent.id}"]`);
                    if (toggle) {
                        toggle.setAttribute('aria-expanded', 'true');
                    }
                }
            } else {
                // Đảm bảo không có active nếu không khớp
                link.classList.remove('active');
            }
        }
    });
    
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
        sidebar.style.transform = 'translateX(0)';
        saveSidebarState('open');
    }
    
    // Hàm đóng sidebar
    function closeSidebar() {
        document.body.classList.remove('sidebar-open');
        sidebar.style.transform = 'translateX(-280px)';
        saveSidebarState('closed');
    }
    
    // Xử lý trạng thái ban đầu
    const initialState = getSidebarState();
    if (initialState === 'open') {
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
    
    // Đóng sidebar khi nhấn phím Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && document.body.classList.contains('sidebar-open')) {
            closeSidebar();
        }
    });
    
    // Đóng sidebar khi click vào một liên kết trong sidebar
    navLinks.forEach(link => {
        link.addEventListener('click', function() {
            // Chỉ đóng sidebar nếu không phải là link dropdown
            if (!this.classList.contains('dropdown-toggle')) {
                closeSidebar();
            }
        });
    });
    
    // Đóng sidebar khi click vào nội dung chính
    const mainContent = document.querySelector('.site-content');
    if (mainContent) {
        mainContent.addEventListener('click', function(e) {
            if (document.body.classList.contains('sidebar-open')) {
                closeSidebar();
            }
        });
    }
}
